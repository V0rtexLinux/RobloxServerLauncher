using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace RobloxPlayerLauncher
{
    /// <summary>
    /// RobloxPlayerLauncher.exe
    ///   (no arguments)                  install for this user and register robloxserver-player:
    ///   robloxserver-player:1+...       what the website's Play / Host Server buttons open
    ///   --uninstall                     remove the protocol, the clients and the downloaded places
    ///   --updated --wait-pid N LINK     internal: a self update taking over from the old launcher
    /// </summary>
    public static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string link = null;
            bool updated = false;
            int waitPid = 0;
            bool uninstall = false;
            bool botHost = false;
            string site = null, apiKey = null, setKey = null, siteDir = null;
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (LaunchRequest.LooksLikeLaunchUri(arg))
                {
                    link = arg;
                }
                else if (arg == "--updated")
                {
                    updated = true;
                }
                else if (arg == "--wait-pid" && i + 1 < args.Length)
                {
                    int.TryParse(args[++i], out waitPid);
                }
                else if (arg == "--uninstall" || arg == "/uninstall")
                {
                    uninstall = true;
                }
                else if (arg == "--site-dir" && i + 1 < args.Length)
                {
                    siteDir = args[++i];
                }
                else if (arg == "--set-key" && i + 1 < args.Length)
                {
                    setKey = args[++i];
                }
                else if (arg == "--bot-host")
                {
                    botHost = true;
                }
                else if (arg == "--site" && i + 1 < args.Length)
                {
                    site = args[++i];
                }
                else if (arg == "--api-key" && i + 1 < args.Length)
                {
                    apiKey = args[++i];
                }
            }

            try
            {
                if (setKey != null)
                {
                    BotKey.Save(setKey);
                    MessageBox.Show("Chave dos bots salva (criptografada para este usuario do Windows).", "ROBLOX", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return 0;
                }

                if (botHost)
                {
                    return BotHost.Run(site, apiKey, siteDir);
                }

                if (uninstall)
                {
                    Installer.Uninstall();
                    MessageBox.Show("ROBLOX has been uninstalled.", "ROBLOX", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return 0;
                }

                if (waitPid > 0)
                {
                    Installer.WaitForProcess(waitPid);
                }

                if (link == null)
                {
                    Installer.Install();
                    MessageBox.Show("ROBLOX is successfully installed!\r\n\r\nGo back to the website and click Play on any game.",
                        "ROBLOX", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return 0;
                }

                // Started from a link: make sure the protocol points at the installed copy (first run,
                // self update, or the user ran a downloaded exe from another folder).
                if (updated || !Installer.IsInstalled())
                {
                    Installer.Install();
                }

                LaunchRequest request = LaunchRequest.Parse(link);
                if (!ConfirmSite(request))
                {
                    return 1;
                }

                Application.Run(new LauncherForm(request, link, updated));
                return 0;
            }
            catch (FormatException ex)
            {
                Paths.Log("Bad launch link: " + ex.Message);
                MessageBox.Show(ex.Message, "ROBLOX", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }
            catch (Exception ex)
            {
                Paths.Log("Unexpected error: " + ex);
                MessageBox.Show("An error occurred: " + ex.Message, "ROBLOX", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }
        }

        /// <summary>
        /// Any web page can open a robloxserver-player: link, and the website decides which client
        /// gets downloaded and run. So the user approves every website once.
        /// </summary>
        static bool ConfirmSite(LaunchRequest request)
        {
            Settings settings = Settings.Load();
            if (settings.IsTrusted(request.BaseUrl))
            {
                return true;
            }

            DialogResult answer = MessageBox.Show(
                "Do you want to play games from " + Settings.SiteKey(request.BaseUrl) + "?\r\n\r\n"
                + "ROBLOX will download the game client from this website and run it on your computer. "
                + "Only allow websites you trust.",
                "ROBLOX", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (answer != DialogResult.Yes)
            {
                return false;
            }

            settings.Trust(request.BaseUrl);
            settings.Save();
            return true;
        }
    }

    /// <summary>
    /// Modo bot (sem janela):  RobloxPlayerLauncher.exe --bot-host --site http://localhost:8080/ --api-key CHAVE
    /// Le a lista de servidores do site, e para cada um faz o mesmo que o botao "Host Server"
    /// (GameStarter.Host) usando o bot "HOST". Reinicia sozinho se o servidor cair ou o site esquecer o job.
    /// </summary>
    public static class BotHost
    {
        static readonly object StartLock = new object();
        static string KeyFolder;

        // A chave fica em <pasta do site>/App_Data/bot.key (o site cria se faltar; o launcher tambem).
        static string KeyFromFolder()
        {
            if (string.IsNullOrEmpty(KeyFolder))
            {
                return null;
            }
            try
            {
                string dir = Path.Combine(KeyFolder, "App_Data");
                if (!Directory.Exists(dir))
                {
                    Paths.Log("Bot: pasta nao encontrada: " + dir);
                    return null;
                }
                string file = Path.Combine(dir, "bot.key");
                if (!File.Exists(file))
                {
                    byte[] bytes = new byte[32];
                    using (var rng = new System.Security.Cryptography.RNGCryptoServiceProvider())
                    {
                        rng.GetBytes(bytes);
                    }
                    File.WriteAllText(file, BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant());
                }
                string key = File.ReadAllText(file).Trim();
                return key.Length >= 32 ? key : null;
            }
            catch (Exception ex)
            {
                Paths.Log("Bot: nao consegui ler a chave do site: " + ex.Message);
                return null;
            }
        }
        static readonly object WantedLock = new object();
        static readonly HashSet<string> Wanted = new HashSet<string>();

        class BotServer
        {
            public long PlaceId;
            public int Port;
            public string Address;
            public string Key { get { return PlaceId + ":" + Port; } }
        }

        public static int Run(string site, string apiKey, string siteDir)
        {
            if (!string.IsNullOrEmpty(siteDir))
            {
                KeyFolder = siteDir;
                if (string.IsNullOrEmpty(site))
                {
                    site = "http://localhost:8080/";
                }
            }
            if (string.IsNullOrEmpty(apiKey))
            {
                apiKey = KeyFromFolder() ?? BotKey.Load();
            }
            if (string.IsNullOrEmpty(site) || string.IsNullOrEmpty(apiKey))
            {
                Paths.Log("Bot: use --bot-host --site-dir PASTA_DO_SITE (ou --set-key CHAVE antes)");
                return 1;
            }
            bool created;
            using (var mutex = new Mutex(true, "RobloxServerBotHost", out created))
            {
                if (!created)
                {
                    return 0;
                }
                Uri baseUrl = LaunchRequest.ParseBaseUrl(site);
                var running = new HashSet<string>();
                while (true)
                {
                    try
                    {
                        List<BotServer> list = FetchList(baseUrl, apiKey);
                        lock (WantedLock)
                        {
                            Wanted.Clear();
                            foreach (BotServer s in list) { Wanted.Add(s.Key); }
                        }
                        foreach (BotServer s in list)
                        {
                            if (running.Add(s.Key))
                            {
                                BotServer bot = s;
                                var t = new Thread(() => Loop(baseUrl, apiKey, bot)) { IsBackground = true };
                                t.Start();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Paths.Log("Bot: nao consegui ler a lista: " + ex.Message);
                    }
                    Thread.Sleep(60000);
                }
            }
        }

        static bool IsWanted(string key)
        {
            lock (WantedLock) { return Wanted.Contains(key); }
        }

        static void Loop(Uri baseUrl, string apiKey, BotServer s)
        {
            int failures = 0;
            while (IsWanted(s.Key))
            {
                GameHost host = null;
                try
                {
                    host = StartOnce(baseUrl, apiKey, s);
                    failures = 0;
                    var exited = new ManualResetEvent(false);
                    host.Exited += (a, b) => exited.Set();
                    while (IsWanted(s.Key) && !host.ExitedOnItsOwn && !host.ExitCode.HasValue && !exited.WaitOne(15000))
                    {
                        // 3 batimentos seguidos falhando = o site reiniciou e esqueceu o job
                        if (host.HeartbeatFailures >= 3)
                        {
                            Paths.Log("Bot " + s.Key + ": o site perdeu o servidor, reiniciando.");
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    failures++;
                    Paths.Log("Bot " + s.Key + ": " + ex.Message);
                }
                finally
                {
                    if (host != null) { host.Stop(); }
                }
                Thread.Sleep(Math.Min(300, 10 * (1 << Math.Min(failures, 5))) * 1000);
            }
        }

        static GameHost StartOnce(Uri baseUrl, string apiKey, BotServer s)
        {
            string ticket = Get(new Uri(baseUrl, "Game/Servers.ashx?action=botticket&placeId=" + s.PlaceId), apiKey).Trim();
            string link = LaunchRequest.Scheme + ":1+launchmode:host+gameinfo:" + ticket + "+placeid:" + s.PlaceId
                + "+baseurl:" + Uri.EscapeDataString(baseUrl.ToString());
            var starter = new GameStarter(LaunchRequest.Parse(link), Settings.ForBot(s.Port, s.Address));
            // um por vez: cada inicio limpa a pasta de scripts do cliente
            lock (StartLock)
            {
                GameHost host = starter.Run(CancellationToken.None);
                Thread.Sleep(5000);
                return host;
            }
        }

        // Linhas "placeId;porta;endereco" (endereco vazio = o site decide)
        static List<BotServer> FetchList(Uri baseUrl, string apiKey)
        {
            var list = new List<BotServer>();
            string text = Get(new Uri(baseUrl, "Game/Servers.ashx?action=botlist"), apiKey);
            foreach (string raw in text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] p = raw.Trim().Split(';');
                long placeId; int port;
                if (p.Length >= 2 && long.TryParse(p[0], out placeId) && int.TryParse(p[1], out port))
                {
                    list.Add(new BotServer { PlaceId = placeId, Port = port, Address = p.Length > 2 ? p[2].Trim() : "" });
                }
            }
            return list;
        }

        static string Get(Uri url, string apiKey)
        {
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.Timeout = 20000;
            req.ReadWriteTimeout = 20000;
            req.Headers["X-Api-Key"] = KeyFromFolder() ?? apiKey;
            using (var resp = req.GetResponse())
            using (var reader = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }
    }

    /// <summary>
    /// Chave dos bots guardada criptografada com o DPAPI do Windows (so este usuario, nesta maquina, decifra).
    /// Salvar uma vez:  RobloxPlayerLauncher.exe --set-key SUACHAVE
    /// </summary>
    public static class BotKey
    {
        static readonly byte[] Entropy = Encoding.UTF8.GetBytes("RobloxServer.BotKey");

        static string FilePath
        {
            get { return Path.Combine(Paths.Root, "bot.key"); }
        }

        public static void Save(string key)
        {
            byte[] data = System.Security.Cryptography.ProtectedData.Protect(
                Encoding.UTF8.GetBytes(key), Entropy, System.Security.Cryptography.DataProtectionScope.CurrentUser);
            Directory.CreateDirectory(Paths.Root);
            File.WriteAllBytes(FilePath, data);
        }

        public static string Load()
        {
            if (!File.Exists(FilePath))
            {
                return null;
            }
            try
            {
                byte[] data = System.Security.Cryptography.ProtectedData.Unprotect(
                    File.ReadAllBytes(FilePath), Entropy, System.Security.Cryptography.DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(data);
            }
            catch (Exception ex)
            {
                Paths.Log("Bot: nao consegui ler a chave salva (rode --set-key de novo neste usuario): " + ex.Message);
                return null;
            }
        }
    }
}
