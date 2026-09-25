using System;
using System.Net;
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
            }

            try
            {
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
}
