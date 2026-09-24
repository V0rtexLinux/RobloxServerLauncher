#region Usings
using Novetus.Core;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
#endregion

namespace NovetusLauncher
{
    #region RobloxServer Game Launcher
    /// <summary>
    /// Starts games published on a RobloxServer with the regular Novetus client pipeline:
    /// Play Solo (local), Host (registers a job + heartbeats) and Join (PlaceLauncher/Join.ashx + auth ticket).
    /// </summary>
    public static class RobloxServerGameLauncher
    {
        static Timer heartbeatTimer;
        static RobloxServerApi hostApi;
        static RobloxServerJob hostJob;
        static GameServer previousServer;

        public static string MapFolder
        {
            get { return GlobalPaths.MapsDirCustom + @"\\RobloxServer"; }
        }

        static void EnsureIdle()
        {
            if (GlobalVars.GameOpened != ScriptType.None)
            {
                throw new RobloxServerException("A game is already running. Close it first.");
            }
        }

        static void EnsureClient(string client)
        {
            if (string.IsNullOrWhiteSpace(client) || !Client.IsClientValid(client))
            {
                throw new RobloxServerException("This game uses the client '" + client + "', which is not installed in this launcher.");
            }
        }

        static void UseClient(string client)
        {
            GlobalVars.UserConfiguration.SaveSetting("SelectedClient", client);
            Client.ReadClientValues(client);
        }

        static string SafeFileName(string name)
        {
            string safe = Regex.Replace(name ?? "Place", @"[^A-Za-z0-9 _\-]", "").Trim();
            return safe.Length == 0 ? "Place" : (safe.Length > 40 ? safe.Substring(0, 40) : safe);
        }

        /// <summary>Downloads the place (when missing or outdated) into maps\Custom\RobloxServer and selects it.</summary>
        public static void PrepareMap(RobloxServerApi api, RobloxServerGame game)
        {
            Directory.CreateDirectory(MapFolder);
            string fileName = SafeFileName(game.Name) + " [" + game.Id + "].rbxl";
            string path = MapFolder + @"\\" + fileName;

            bool upToDate = File.Exists(path) && !string.IsNullOrEmpty(game.Md5) &&
                string.Equals(SecurityFuncs.GenerateMD5(path), game.Md5, StringComparison.OrdinalIgnoreCase);
            if (!upToDate)
            {
                Util.ConsolePrint("RobloxServer: downloading '" + game.Name + "' (" + game.Id + ")...", 3);
                api.DownloadPlace(game, path);
            }

            // Same format LauncherFormShared.SelectMap() writes.
            string snip = GlobalPaths.MapsDirBase + @"\\Custom\\RobloxServer\\" + fileName;
            GlobalVars.UserConfiguration.SaveSetting("Map", fileName);
            GlobalVars.UserConfiguration.SaveSetting("MapPathSnip", snip);
            GlobalVars.UserConfiguration.SaveSetting("MapPath", GlobalPaths.BasePath + @"\\" + snip);
        }

        #region Play Solo
        public static async Task PlaySolo(RobloxServerApi api, RobloxServerGame game)
        {
            EnsureIdle();
            EnsureClient(game.Client);
            await Task.Run(() => PrepareMap(api, game));
            UseClient(game.Client);

            _ = Task.Run(() =>
            {
                try { api.RecordSoloVisit(game.Id); } catch (Exception) { }
            });

            Client.LaunchRBXClient(game.Client, ScriptType.SoloServer, false, false, new EventHandler(SoloServerExited));

            // Same delay LauncherFormShared.StartGame uses for Play Solo (bigger places need longer).
            int length = (int)(new FileInfo(GlobalVars.UserConfiguration.ReadSetting("MapPath")).Length / 1048576);
            int delay = Util.Clamp(1500 * (length / 10), 1500);
            await Task.Delay(delay);

            Client.LaunchRBXClient(game.Client, ScriptType.Solo, false, true, new EventHandler(SoloExited));
        }

        static void SoloServerExited(object sender, EventArgs e)
        {
            GlobalVars.GameOpened = ScriptType.None;
            Cleanup();
        }

        static void SoloExited(object sender, EventArgs e)
        {
            if (GlobalVars.GameOpened != ScriptType.Studio)
            {
                GlobalVars.GameOpened = ScriptType.None;
            }
            Cleanup();
        }
        #endregion

        #region Host
        public static async Task Host(RobloxServerApi api, RobloxServerGame game, bool no3d)
        {
            EnsureIdle();
            EnsureClient(game.Client);
            if (!api.LoggedIn)
            {
                throw new RobloxServerException("Log in to host a game.");
            }

            await Task.Run(() => PrepareMap(api, game));
            UseClient(game.Client);

            int port = GlobalVars.UserConfiguration.ReadSettingInt("RobloxPort");
            string serverName = GlobalVars.UserConfiguration.ReadSetting("ServerBrowserServerName");
            string address = GlobalVars.UserConfiguration.ReadSetting("AlternateServerIP");
            int maxPlayers = GlobalVars.UserConfiguration.ReadSettingInt("PlayerLimit");

            RobloxServerJob job = await Task.Run(() => api.RegisterServer(game.Id, port, game.Client,
                string.IsNullOrWhiteSpace(serverName) || serverName == "Novetus" ? game.Name : serverName,
                maxPlayers, address, GlobalVars.ProgramInformation.Version));

            hostApi = api;
            hostJob = job;
            GlobalVars.RobloxServerBaseUrl = api.BaseUrl;
            GlobalVars.RobloxServerJobId = job.JobId;
            GlobalVars.RobloxServerServerKey = job.ServerKey;
            GlobalVars.RobloxServerPlaceId = job.PlaceId;
            GlobalVars.RobloxServerRequireAuth = job.RequireAuthTickets;
            GlobalVars.RobloxServerFilteringEnabled = job.FilteringEnabled;

            Client.LaunchRBXClient(game.Client, ScriptType.Server, no3d, false, new EventHandler(ServerExited));

            if (GlobalVars.GameOpened != ScriptType.Server)
            {
                // The client did not start (missing files, failed validation...).
                StopHosting();
                throw new RobloxServerException("The server could not be started. Check the Novetus console.");
            }

            int interval = Math.Max(15, job.HeartbeatSeconds) * 1000;
            heartbeatTimer = new Timer(_ => SendHeartbeat(), null, interval, interval);
            Util.ConsolePrint("RobloxServer: hosting '" + game.Name + "' as job " + job.JobId + " on port " + port + ".", 4);
        }

        static void SendHeartbeat()
        {
            RobloxServerApi api = hostApi;
            RobloxServerJob job = hostJob;
            if (api == null || job == null)
            {
                return;
            }

            try
            {
                api.Heartbeat(job);
            }
            catch (Exception ex)
            {
                Util.ConsolePrint("RobloxServer: heartbeat failed (" + ex.Message + ").", 2);
            }
        }

        static void StopHosting()
        {
            if (heartbeatTimer != null)
            {
                heartbeatTimer.Dispose();
                heartbeatTimer = null;
            }

            RobloxServerApi api = hostApi;
            RobloxServerJob job = hostJob;
            hostApi = null;
            hostJob = null;

            GlobalVars.RobloxServerBaseUrl = "";
            GlobalVars.RobloxServerJobId = "";
            GlobalVars.RobloxServerServerKey = "";
            GlobalVars.RobloxServerPlaceId = 0;
            GlobalVars.RobloxServerRequireAuth = false;
            GlobalVars.RobloxServerFilteringEnabled = false;

            if (api != null && job != null)
            {
                try
                {
                    api.Unregister(job);
                }
                catch (Exception ex)
                {
                    Util.ConsolePrint("RobloxServer: could not unregister the server (" + ex.Message + ").", 2);
                }
            }
        }

        static void ServerExited(object sender, EventArgs e)
        {
            GlobalVars.GameOpened = ScriptType.None;
            StopHosting();
            Cleanup();
        }
        #endregion

        #region Join
        public static async Task Join(RobloxServerApi api, RobloxServerGame game)
        {
            EnsureIdle();
            if (!api.LoggedIn)
            {
                throw new RobloxServerException("Log in to join a game.");
            }

            RobloxServerJoin join = await Task.Run(() => api.RequestJoin(game.Id));
            string client = string.IsNullOrWhiteSpace(join.Client) ? game.Client : join.Client;
            EnsureClient(client);

            previousServer = new GameServer(GlobalVars.CurrentServer.ServerIP, GlobalVars.CurrentServer.ServerPort);
            string previousName = GlobalVars.UserConfiguration.ReadSetting("PlayerName");
            string previousId = GlobalVars.UserConfiguration.ReadSetting("UserID");

            GlobalVars.CurrentServer.ServerIP = join.Address;
            GlobalVars.CurrentServer.ServerPort = join.Port;
            GlobalVars.RobloxServerAuthTicket = join.Ticket;
            // In game the player uses the RobloxServer account (the game server checks it against the ticket).
            GlobalVars.UserConfiguration.SaveSetting("PlayerName", join.UserName);
            GlobalVars.UserConfiguration.SaveSetting("UserID", join.UserId.ToString());

            try
            {
                Client.LaunchRBXClient(client, ScriptType.Client, false, true, new EventHandler(ClientExited));
            }
            finally
            {
                // The values above are baked into the generated launch script, restore the user's settings now.
                GlobalVars.RobloxServerAuthTicket = "";
                GlobalVars.UserConfiguration.SaveSetting("PlayerName", previousName);
                GlobalVars.UserConfiguration.SaveSetting("UserID", previousId);
            }

            Util.ConsolePrint("RobloxServer: joining '" + game.Name + "' at " + join.Address + ":" + join.Port + " with " + client + ".", 4);
        }

        static void ClientExited(object sender, EventArgs e)
        {
            if (!GlobalVars.LocalPlayMode && GlobalVars.GameOpened != ScriptType.Server)
            {
                GlobalVars.GameOpened = ScriptType.None;
            }

            if (previousServer != null)
            {
                GlobalVars.CurrentServer.ServerIP = previousServer.ServerIP;
                GlobalVars.CurrentServer.ServerPort = previousServer.ServerPort;
                previousServer = null;
            }
            Cleanup();
        }
        #endregion

        static void Cleanup()
        {
            Client.UpdateRichPresence(Client.GetStateForType(GlobalVars.GameOpened));
            Client.ResetDecompressedMap();
            Client.ResetScripts(true);
        }

        /// <summary>Called when the launcher closes while hosting.</summary>
        public static void Shutdown()
        {
            if (hostJob != null)
            {
                StopHosting();
            }
        }
    }
    #endregion
}
