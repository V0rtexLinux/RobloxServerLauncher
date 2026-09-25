using System;
using System.Diagnostics;
using System.Threading;

namespace RobloxPlayerLauncher
{
    /// <summary>
    /// A game server started with "Host Server": keeps the job alive on the website with heartbeats
    /// and unregisters it when the client closes, the user clicks Stop, or the launcher exits.
    /// </summary>
    public class GameHost : IDisposable
    {
        readonly SiteClient site;
        readonly Process process;
        readonly Timer timer;
        int stopped;

        public HostJob Job { get; private set; }
        public string GameName { get; private set; }
        public int Port { get; private set; }
        public int Players { get; private set; }

        /// <summary>Raised on a thread pool thread when the server process exits.</summary>
        public event EventHandler Exited;

        /// <summary>Raised after each heartbeat with the latest player count.</summary>
        public event EventHandler Updated;

        public GameHost(SiteClient site, HostJob job, Process process, string gameName, int port)
        {
            this.site = site;
            this.process = process;
            Job = job;
            GameName = gameName;
            Port = port;

            process.EnableRaisingEvents = true;
            process.Exited += (sender, e) =>
            {
                Paths.Log("Game server for job " + job.JobId + " exited.");
                Stop();
                var handler = Exited;
                if (handler != null)
                {
                    handler(this, EventArgs.Empty);
                }
            };

            int interval = job.HeartbeatSeconds * 1000;
            timer = new Timer(_ => Heartbeat(), null, interval, interval);
        }

        void Heartbeat()
        {
            if (stopped != 0)
            {
                return;
            }
            try
            {
                site.Heartbeat(Job);
                Players = site.GetPlayerCount(Job);
                var handler = Updated;
                if (handler != null)
                {
                    handler(this, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                Paths.Log("Heartbeat failed: " + ex.Message);
            }
        }

        /// <summary>Closes the game server and removes the job from the website. Safe to call twice.</summary>
        public void Stop()
        {
            if (Interlocked.Exchange(ref stopped, 1) != 0)
            {
                return;
            }

            timer.Dispose();
            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }
            }
            catch (Exception ex)
            {
                Paths.Log("Could not close the game server: " + ex.Message);
            }

            try
            {
                site.Unregister(Job);
            }
            catch (Exception ex)
            {
                Paths.Log("Could not unregister job " + Job.JobId + ": " + ex.Message);
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
