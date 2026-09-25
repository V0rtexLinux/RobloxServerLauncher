using System;
using System.IO;
using System.Reflection;

namespace RobloxPlayerLauncher
{
    /// <summary>
    /// Everything is installed per user, like the 2013 launcher in %LocalAppData%\Roblox:
    ///   %LocalAppData%\RobloxServer\RobloxPlayerLauncher.exe
    ///   %LocalAppData%\RobloxServer\Versions\{site}\{client}-version-{hash}\
    ///   %LocalAppData%\RobloxServer\Places, Downloads, Logs, Settings.ini
    /// </summary>
    public static class Paths
    {
        public const string LauncherFileName = "RobloxPlayerLauncher.exe";

        public static string Root
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RobloxServer"); }
        }

        public static string InstalledLauncher
        {
            get { return Path.Combine(Root, LauncherFileName); }
        }

        public static string Versions
        {
            get { return Path.Combine(Root, "Versions"); }
        }

        public static string Downloads
        {
            get { return Path.Combine(Root, "Downloads"); }
        }

        public static string Places
        {
            get { return Path.Combine(Root, "Places"); }
        }

        public static string SettingsFile
        {
            get { return Path.Combine(Root, "Settings.ini"); }
        }

        public static string LogFile
        {
            get { return Path.Combine(Root, "Logs", "launcher.log"); }
        }

        public static string CurrentExe
        {
            get { return Path.GetFullPath(Assembly.GetEntryAssembly().Location); }
        }

        public static bool SamePath(string a, string b)
        {
            return string.Equals(Path.GetFullPath(a).TrimEnd('\\', '/'), Path.GetFullPath(b).TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>True when <paramref name="path"/> is <paramref name="directory"/> or inside it.</summary>
        public static bool IsInside(string path, string directory)
        {
            string full = Path.GetFullPath(path);
            string dir = Path.GetFullPath(directory).TrimEnd('\\', '/') + Path.DirectorySeparatorChar;
            return full.StartsWith(dir, StringComparison.OrdinalIgnoreCase) || SamePath(full, directory);
        }

        static readonly object LogSync = new object();

        public static void Log(string message)
        {
            try
            {
                lock (LogSync)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(LogFile));
                    var info = new FileInfo(LogFile);
                    if (info.Exists && info.Length > 1024 * 1024)
                    {
                        File.Delete(LogFile);
                    }
                    File.AppendAllText(LogFile, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + message + Environment.NewLine);
                }
            }
            catch (Exception)
            {
                // Logging must never stop a game from starting.
            }
        }
    }
}
