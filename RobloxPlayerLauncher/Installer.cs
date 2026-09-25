using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.Win32;

namespace RobloxPlayerLauncher
{
    /// <summary>
    /// Per user install, like the 2013 RobloxPlayerLauncher: copies itself to
    /// %LocalAppData%\RobloxServer and registers the robloxserver-player: protocol in
    /// HKEY_CURRENT_USER\Software\Classes so the website's Play button can start it.
    /// </summary>
    public static class Installer
    {
        const string ProtocolKey = @"Software\Classes\" + LaunchRequest.Scheme;

        public static bool IsInstalledCopy
        {
            get { return Paths.SamePath(Paths.CurrentExe, Paths.InstalledLauncher); }
        }

        /// <summary>Copies the running exe into place (unless it is already there) and registers the protocol.</summary>
        public static void Install()
        {
            Directory.CreateDirectory(Paths.Root);
            if (!IsInstalledCopy)
            {
                CopyWithRetry(Paths.CurrentExe, Paths.InstalledLauncher);
            }
            RegisterProtocol(Paths.InstalledLauncher);
            Paths.Log("Installed " + Paths.InstalledLauncher);
        }

        /// <summary>True when the protocol points at the installed launcher and that file exists.</summary>
        public static bool IsInstalled()
        {
            try
            {
                using (RegistryKey command = Registry.CurrentUser.OpenSubKey(ProtocolKey + @"\shell\open\command"))
                {
                    string value = command != null ? command.GetValue("") as string : null;
                    return value != null && File.Exists(Paths.InstalledLauncher)
                        && value.IndexOf(Paths.InstalledLauncher, StringComparison.OrdinalIgnoreCase) >= 0;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        static void RegisterProtocol(string exe)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(ProtocolKey))
            {
                key.SetValue("", "URL: RobloxServer Protocol");
                key.SetValue("URL Protocol", "");
                using (RegistryKey icon = key.CreateSubKey("DefaultIcon"))
                {
                    icon.SetValue("", "\"" + exe + "\",0");
                }
                using (RegistryKey command = key.CreateSubKey(@"shell\open\command"))
                {
                    command.SetValue("", "\"" + exe + "\" \"%1\"");
                }
            }
        }

        public static void Uninstall()
        {
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree(ProtocolKey, false);
            }
            catch (Exception ex)
            {
                Paths.Log("Could not remove the protocol: " + ex.Message);
            }

            foreach (string folder in new[] { Paths.Versions, Paths.Downloads, Paths.Places })
            {
                try
                {
                    if (Directory.Exists(folder))
                    {
                        Directory.Delete(folder, true);
                    }
                }
                catch (Exception ex)
                {
                    Paths.Log("Could not remove " + folder + ": " + ex.Message);
                }
            }

            if (!IsInstalledCopy && File.Exists(Paths.InstalledLauncher))
            {
                try
                {
                    File.Delete(Paths.InstalledLauncher);
                }
                catch (Exception ex)
                {
                    Paths.Log("Could not remove the launcher: " + ex.Message);
                }
            }
        }

        static void CopyWithRetry(string source, string destination)
        {
            for (int attempt = 0; ; attempt++)
            {
                try
                {
                    File.Copy(source, destination, true);
                    return;
                }
                catch (IOException)
                {
                    // The previous launcher may still be closing (self update).
                    if (attempt >= 20)
                    {
                        throw;
                    }
                    Thread.Sleep(500);
                }
            }
        }

        /// <summary>
        /// Self update, like the 2013 bootstrapper: when the website hosts a different
        /// RobloxPlayerLauncher.exe, download it, check its SHA-256 and start it with the same launch
        /// link. Returns true when the new launcher took over and this one must exit.
        /// </summary>
        public static bool UpdateFromSite(SiteClient site, string launchUri, Action<string, int> report, CancellationToken cancel)
        {
            PackageInfo package;
            try
            {
                package = site.GetPackage("Launcher");
            }
            catch (LauncherException ex)
            {
                Paths.Log("Launcher update check failed: " + ex.Message);
                return false;
            }

            if (!package.Available || string.IsNullOrEmpty(package.Sha256)
                || string.Equals(package.Sha256, ClientInstaller.Sha256Of(Paths.CurrentExe), StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            report("Updating ROBLOX...", 0);
            string folder = Path.Combine(Paths.Downloads, "launcher-" + package.Sha256.Substring(0, 16));
            string exe = Path.Combine(folder, Paths.LauncherFileName);
            site.Download("Install/Download.ashx?client=Launcher", exe, (received, total) =>
            {
                report("Updating ROBLOX...", total > 0 ? (int)Math.Min(100, received * 100 / total) : -1);
            }, cancel);

            if (!string.Equals(ClientInstaller.Sha256Of(exe), package.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                Paths.Log("The downloaded launcher does not match its SHA-256, keeping the current one.");
                return false;
            }

            Paths.Log("Updating the launcher to " + package.Version);
            var info = new ProcessStartInfo(exe, "--updated --wait-pid " + Process.GetCurrentProcess().Id + " \"" + launchUri + "\"")
            {
                UseShellExecute = false
            };
            Process.Start(info);
            return true;
        }

        /// <summary>Waits for the launcher that started this update to close.</summary>
        public static void WaitForProcess(int pid)
        {
            try
            {
                using (Process old = Process.GetProcessById(pid))
                {
                    old.WaitForExit(30000);
                }
            }
            catch (ArgumentException)
            {
                // Already gone.
            }
        }
    }
}
