using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RobloxPlayerLauncher
{
    /// <summary>
    /// How to start a client package. A package can ship RobloxServerClient.json next to the exes:
    /// <code>
    /// {
    ///   "PlayerExe": "RobloxApp_client.exe",
    ///   "ServerExe": "RobloxApp_server.exe",
    ///   "PlayerArgs": "-script \"dofile('{scriptasset}')\"",
    ///   "ServerArgs": "\"{place}\" -script \"dofile('{scriptasset}')\"",
    ///   "ServerLoadsPlace": false
    /// }
    /// </code>
    /// Without it the launcher looks for the usual 2012/2013 exe names and uses the arguments above.
    /// These clients run the text after -script as Lua code (the same "dofile(...)" call Novetus uses),
    /// so the generated file is loaded with dofile from rbxasset://scripts/.
    /// Placeholders: {script} (full path of the generated Lua file), {scriptasset} (rbxasset://scripts/...),
    /// {place} (downloaded place file, server only), {port}, {baseurl}.
    /// </summary>
    public class ClientManifest
    {
        public const string FileName = "RobloxServerClient.json";

        static readonly string[] PlayerCandidates =
        {
            "RobloxApp_client.exe", @"client\RobloxApp_client.exe", "RobloxPlayerBeta.exe", "RobloxPlayer.exe", "RobloxApp.exe"
        };

        static readonly string[] ServerCandidates =
        {
            "RobloxApp_server.exe", @"server\RobloxApp_server.exe", "RobloxApp_client.exe", @"client\RobloxApp_client.exe",
            "RobloxPlayerBeta.exe", "RobloxPlayer.exe", "RobloxApp.exe"
        };

        /// <summary>Studio executables, in the order they are tried (the manifest's "StudioExe" wins).</summary>
        static readonly string[] StudioCandidates =
        {
            "RobloxStudio.exe", "RobloxStudioBeta.exe", "RobloxApp_studio.exe", @"studio\RobloxApp_studio.exe", "RobloxStudioLauncher.exe"
        };

        public string Directory { get; private set; }
        public string PlayerExe { get; private set; }
        public string ServerExe { get; private set; }
        public string StudioExe { get; private set; }
        public string PlayerArgs { get; private set; }
        public string ServerArgs { get; private set; }
        public string StudioArgs { get; private set; }

        /// <summary>True: the game server script loads the place itself (game:Load) and no place file is passed.</summary>
        public bool ServerLoadsPlace { get; private set; }

        public static ClientManifest Load(string directory)
        {
            var manifest = new ClientManifest
            {
                Directory = directory,
                PlayerArgs = "-script \"dofile('{scriptasset}')\"",
                ServerArgs = "\"{place}\" -script \"dofile('{scriptasset}')\""
            };

            // {place} = the downloaded place file ("" for a new place), {ticket}/{authurl} = automatic login.
            manifest.StudioArgs = "\"{place}\" -t {ticket} -a {authurl}";

            string file = Path.Combine(directory, FileName);
            Dictionary<string, object> json = File.Exists(file) ? Json.Parse(File.ReadAllText(file)) : new Dictionary<string, object>();

            manifest.PlayerExe = manifest.FindExe(json.Str("PlayerExe"), PlayerCandidates);
            manifest.ServerExe = manifest.FindExe(json.Str("ServerExe"), ServerCandidates);
            manifest.StudioExe = manifest.FindExe(json.Str("StudioExe"), StudioCandidates);
            manifest.PlayerArgs = json.Str("PlayerArgs") ?? manifest.PlayerArgs;
            manifest.ServerArgs = json.Str("ServerArgs") ?? manifest.ServerArgs;
            manifest.StudioArgs = json.Str("StudioArgs") ?? manifest.StudioArgs;
            manifest.ServerLoadsPlace = json.Bool("ServerLoadsPlace") || manifest.ServerArgs.IndexOf("{place}", StringComparison.OrdinalIgnoreCase) < 0;
            return manifest;
        }

        string FindExe(string configured, string[] candidates)
        {
            IEnumerable<string> names = string.IsNullOrWhiteSpace(configured) ? candidates : new[] { configured };
            foreach (string name in names)
            {
                string path = Path.GetFullPath(Path.Combine(Directory, name));
                // A package must never point outside its own folder.
                if (Paths.IsInside(path, Directory) && File.Exists(path))
                {
                    return path;
                }
            }
            return null;
        }

        public string ExeFor(LaunchMode mode)
        {
            string exe = mode == LaunchMode.Host ? ServerExe : mode == LaunchMode.Studio ? StudioExe : PlayerExe;
            if (exe == null)
            {
                throw new LauncherException("The client package has no " + (mode == LaunchMode.Host ? "server" : mode == LaunchMode.Studio ? "Studio" : "player")
                    + " executable. Ask the website administrator to check " + FileName + ".");
            }
            return exe;
        }

        public string ArgsFor(LaunchMode mode, IDictionary<string, string> values)
        {
            string args = mode == LaunchMode.Host ? ServerArgs : mode == LaunchMode.Studio ? StudioArgs : PlayerArgs;
            foreach (var pair in values)
            {
                args = args.Replace("{" + pair.Key + "}", pair.Value ?? "");
            }
            return args;
        }

        /// <summary>content\scripts next to the exe, where rbxasset://scripts/ points.</summary>
        public static string ScriptFolder(string exe)
        {
            return Path.Combine(Path.GetDirectoryName(exe), "content", "scripts");
        }

        /// <summary>Removes generated scripts older than an hour (they hold one-time tickets or server keys).</summary>
        public static void CleanScripts(string exe)
        {
            try
            {
                string folder = ScriptFolder(exe);
                if (!System.IO.Directory.Exists(folder))
                {
                    return;
                }
                foreach (string file in System.IO.Directory.GetFiles(folder, "robloxserver_*.lua")
                    .Where(f => File.GetLastWriteTimeUtc(f) < DateTime.UtcNow.AddHours(-1)))
                {
                    File.Delete(file);
                }
            }
            catch (Exception ex)
            {
                Paths.Log("Could not clean old scripts: " + ex.Message);
            }
        }
    }
}
