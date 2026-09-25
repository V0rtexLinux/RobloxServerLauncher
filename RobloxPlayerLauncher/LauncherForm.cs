using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RobloxPlayerLauncher
{
    /// <summary>The small white "ROBLOX" window with the logo, a status line and a progress bar.</summary>
    public class LauncherForm : Form
    {
        readonly string launchUri;
        readonly bool skipUpdate;
        readonly GameStarter starter;
        readonly CancellationTokenSource cancel = new CancellationTokenSource();

        readonly Label status;
        readonly ProgressBar progress;
        readonly Button cancelButton;

        public LauncherForm(LaunchRequest request, string launchUri, bool skipUpdate)
        {
            this.launchUri = launchUri;
            this.skipUpdate = skipUpdate;
            starter = new GameStarter(request, Settings.Load());

            Text = "ROBLOX";
            Icon = LoadIcon();
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = true;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;
            ClientSize = new Size(420, 200);
            Font = new Font("Arial", 9f);

            var logo = new PictureBox
            {
                Image = LoadLogo(),
                SizeMode = PictureBoxSizeMode.CenterImage,
                Location = new Point(0, 14),
                Size = new Size(420, 74)
            };

            status = new Label
            {
                Text = "Starting ROBLOX...",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Arial", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0x34, 0x34, 0x34),
                Location = new Point(10, 96),
                Size = new Size(400, 24),
                AutoEllipsis = true
            };

            progress = new ProgressBar
            {
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30,
                Location = new Point(40, 126),
                Size = new Size(340, 16)
            };

            cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(320, 160),
                Size = new Size(80, 26),
                FlatStyle = FlatStyle.System
            };
            cancelButton.Click += (sender, e) => Close();

            Controls.Add(logo);
            Controls.Add(status);
            Controls.Add(progress);
            Controls.Add(cancelButton);
            CancelButton = cancelButton;
        }

        public static Icon LoadIcon()
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("RobloxPlayerLauncher.Roblox.ico"))
            {
                return stream != null ? new Icon(stream) : SystemIcons.Application;
            }
        }

        public static Image LoadLogo()
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("RobloxPlayerLauncher.RobloxLogo.png");
            return stream != null ? Image.FromStream(stream) : null;
        }

        void SetStatus(string text, int percent)
        {
            if (IsDisposed)
            {
                return;
            }
            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action(() => SetStatus(text, percent)));
                }
                catch (InvalidOperationException)
                {
                    // Closing.
                }
                return;
            }

            status.Text = text;
            if (percent < 0)
            {
                progress.Style = ProgressBarStyle.Marquee;
            }
            else
            {
                progress.Style = ProgressBarStyle.Continuous;
                progress.Value = Math.Max(0, Math.Min(100, percent));
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            starter.Report = SetStatus;
            Task.Run(() => Work());
        }

        void Work()
        {
            try
            {
                if (!skipUpdate && Installer.UpdateFromSite(starter.Site, launchUri, SetStatus, cancel.Token))
                {
                    // The new launcher handles the link.
                    Finish(null, null, true);
                    return;
                }

                GameHost host = starter.Run(cancel.Token);
                Finish(host, null, false);
            }
            catch (OperationCanceledException)
            {
                Finish(null, null, true);
            }
            catch (LauncherException ex)
            {
                Paths.Log("Launch failed: " + ex.Message);
                Finish(null, ex.Message, false);
            }
            catch (Exception ex)
            {
                Paths.Log("Launch failed: " + ex);
                Finish(null, "An error occurred while starting ROBLOX: " + ex.Message, false);
            }
        }

        void Finish(GameHost host, string error, bool closeNow)
        {
            try
            {
                BeginInvoke(new Action(() => Done(host, error, closeNow)));
            }
            catch (InvalidOperationException)
            {
                // The window was closed meanwhile: never leave a game server registered.
                if (host != null)
                {
                    host.Stop();
                }
            }
        }

        void Done(GameHost host, string error, bool closeNow)
        {
            if (closeNow)
            {
                Close();
                return;
            }

            if (error != null)
            {
                progress.Style = ProgressBarStyle.Continuous;
                progress.Value = 0;
                status.Text = "Could not start ROBLOX";
                MessageBox.Show(this, error, "ROBLOX", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
                return;
            }

            if (cancel.IsCancellationRequested)
            {
                if (host != null)
                {
                    host.Stop();
                }
                Close();
                return;
            }

            if (host == null)
            {
                // Play: the client is starting, the launcher is done (the 2013 one closed too).
                SetStatus("ROBLOX is starting. Get ready to play!", 100);
                var timer = new System.Windows.Forms.Timer { Interval = 2500 };
                timer.Tick += (sender, e) => { timer.Stop(); Close(); };
                timer.Start();
                return;
            }

            Hide();
            var hostForm = new HostForm(host);
            hostForm.FormClosed += (sender, e) => Close();
            hostForm.Show();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            cancel.Cancel();
            base.OnFormClosing(e);
        }
    }
}
