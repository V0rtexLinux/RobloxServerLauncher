using System;
using System.Drawing;
using System.Windows.Forms;

namespace RobloxPlayerLauncher
{
    /// <summary>Stays open while this PC hosts a game server; closing it stops the server.</summary>
    public class HostForm : Form
    {
        readonly GameHost host;
        readonly Label players;

        public HostForm(GameHost host)
        {
            this.host = host;

            Text = "ROBLOX Game Server - " + host.GameName;
            Icon = LauncherForm.LoadIcon();
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;
            ClientSize = new Size(420, 230);
            Font = new Font("Arial", 9f);

            var logo = new PictureBox
            {
                Image = LauncherForm.LoadLogo(),
                SizeMode = PictureBoxSizeMode.CenterImage,
                Location = new Point(0, 10),
                Size = new Size(420, 74)
            };

            var title = new Label
            {
                Text = "Hosting " + host.GameName,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Arial", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0x34, 0x34, 0x34),
                Location = new Point(10, 90),
                Size = new Size(400, 24),
                AutoEllipsis = true
            };

            var details = new Label
            {
                Text = "UDP port " + host.Port + " (forward it on your router for players outside your network).\r\n"
                    + "Players join with the Play button on the website.",
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(0x55, 0x55, 0x55),
                Location = new Point(10, 116),
                Size = new Size(400, 36)
            };

            players = new Label
            {
                Text = "Players: 0",
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(0x36, 0xa4, 0x00),
                Font = new Font("Arial", 10f, FontStyle.Bold),
                Location = new Point(10, 154),
                Size = new Size(400, 20)
            };

            var stop = new Button
            {
                Text = "Stop Server",
                Location = new Point(150, 186),
                Size = new Size(120, 30),
                FlatStyle = FlatStyle.System
            };
            stop.Click += (sender, e) => Close();

            Controls.Add(logo);
            Controls.Add(title);
            Controls.Add(details);
            Controls.Add(players);
            Controls.Add(stop);

            host.Updated += (sender, e) => OnUi(() => players.Text = "Players: " + host.Players);
            host.Exited += (sender, e) => OnUi(ServerExited);
        }

        void ServerExited()
        {
            TimeSpan uptime = DateTime.Now - host.Started;
            if (host.ExitedOnItsOwn && uptime < TimeSpan.FromMinutes(2))
            {
                // Closing right away usually means the client did not accept its arguments or script.
                MessageBox.Show(this,
                    "The game server closed after " + (int)uptime.TotalSeconds + " seconds (exit code "
                    + (host.ExitCode.HasValue ? host.ExitCode.Value.ToString() : "unknown") + ").\r\n\r\n"
                    + "Check that this client can run as a server with the arguments in its RobloxServerClient.json.\r\n"
                    + "Details: " + Paths.LogFile,
                    "ROBLOX", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            Close();
        }

        void OnUi(Action action)
        {
            if (IsDisposed || !IsHandleCreated)
            {
                return;
            }
            BeginInvoke(action);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            host.Stop();
            base.OnFormClosing(e);
        }
    }
}
