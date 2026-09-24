#region Usings
using Novetus.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
#endregion

namespace NovetusLauncher
{
    #region RobloxServer Browser
    /// <summary>
    /// Lists the games published on a RobloxServer website and plays them with the Novetus clients:
    /// Play Solo, Host Server (registered on the website) or Join Server (with a 2015 style auth ticket).
    /// </summary>
    public partial class RobloxServerBrowser : Form
    {
        #region Private Variables
        RobloxServerApi api;
        List<RobloxServerGame> games = new List<RobloxServerGame>();
        bool busy;
        #endregion

        #region Constructor
        public RobloxServerBrowser()
        {
            InitializeComponent();

            try
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch (Exception)
            {
            }

            GamesListView.Columns.Add("Name", 190);
            GamesListView.Columns.Add("Creator", 110);
            GamesListView.Columns.Add("Client", 70, HorizontalAlignment.Center);
            GamesListView.Columns.Add("Playing", 60, HorizontalAlignment.Center);
            GamesListView.Columns.Add("Servers", 60, HorizontalAlignment.Center);
            GamesListView.Columns.Add("Visits", 70, HorizontalAlignment.Center);
            GamesListView.Columns.Add("Installed", 70, HorizontalAlignment.Center);
        }
        #endregion

        #region Form Events
        private void RobloxServerBrowser_Load(object sender, EventArgs e)
        {
            AddressBox.Text = GlobalVars.UserConfiguration.ReadSetting("RobloxServerAddress");
            UserBox.Text = GlobalVars.UserConfiguration.ReadSetting("RobloxServerUserName");
            CenterToScreen();
            UpdateButtons();
        }

        private async void RobloxServerBrowser_Shown(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(AddressBox.Text))
            {
                await LoadGames();
            }
        }

        private void AddressBox_TextChanged(object sender, EventArgs e)
        {
            GlobalVars.UserConfiguration.SaveSetting("RobloxServerAddress", AddressBox.Text.Trim());
            // A different server needs a new login.
            if (api != null && !string.Equals(api.BaseUrl, SafeNormalize(AddressBox.Text), StringComparison.OrdinalIgnoreCase))
            {
                api = null;
                SetStatus("Not logged in.");
                UpdateButtons();
            }
        }

        private async void RefreshButton_Click(object sender, EventArgs e)
        {
            await LoadGames();
        }

        private void WebsiteButton_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start(RobloxServerApi.NormalizeAddress(AddressBox.Text));
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private async void LoginButton_Click(object sender, EventArgs e)
        {
            if (api != null && api.LoggedIn)
            {
                await Run("Logging out...", () => Task.Run(() => api.Logout()));
                SetStatus("Not logged in.");
                UpdateButtons();
                return;
            }

            string user = UserBox.Text.Trim();
            string password = PasswordBox.Text;
            if (user.Length == 0 || password.Length == 0)
            {
                MessageBox.Show("Enter your RobloxServer username and password.", "RobloxServer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            bool ok = await Run("Logging in...", async () =>
            {
                RobloxServerApi connection = GetApi();
                await Task.Run(() => connection.Login(user, password));
            });

            PasswordBox.Text = "";
            if (ok)
            {
                GlobalVars.UserConfiguration.SaveSetting("RobloxServerUserName", api.UserName);
                SetStatus("Logged in as " + api.UserName + ".");
                await LoadGames();
            }
            UpdateButtons();
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                LoginButton.PerformClick();
            }
        }

        private void GamesListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            RobloxServerGame game = SelectedGame;
            DescriptionBox.Text = game == null ? "" :
                game.Name + " by " + game.CreatorName + " - client " + game.Client +
                (game.FilteringEnabled ? " - FilteringEnabled" : "") + Environment.NewLine +
                (string.IsNullOrWhiteSpace(game.Description) ? "No description." : game.Description);
            UpdateButtons();
        }

        private void GamesListView_DoubleClick(object sender, EventArgs e)
        {
            RobloxServerGame game = SelectedGame;
            if (game == null)
            {
                return;
            }

            if (game.Servers > 0 && api != null && api.LoggedIn)
            {
                JoinButton.PerformClick();
            }
            else
            {
                PlaySoloButton.PerformClick();
            }
        }

        private async void PlaySoloButton_Click(object sender, EventArgs e)
        {
            RobloxServerGame game = SelectedGame;
            if (game == null)
            {
                return;
            }

            await Run("Starting Play Solo...", () => RobloxServerGameLauncher.PlaySolo(GetApi(), game));
        }

        private async void HostButton_Click(object sender, EventArgs e)
        {
            RobloxServerGame game = SelectedGame;
            if (game == null)
            {
                return;
            }

            if (GlobalVars.UserConfiguration.ReadSettingBool("FirstServerLaunch"))
            {
                MessageBox.Show("Players join on port " + GlobalVars.UserConfiguration.ReadSettingInt("RobloxPort") + " (UDP).\n\n" +
                    "If a Raspberry Pi gateway is set up for this RobloxServer, make sure this PC and port are listed in its GAME_FORWARDS. " +
                    "Otherwise forward the port in your router or enable UPnP in the launcher settings.",
                    "RobloxServer - Hosting", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            bool no3d = No3DCheckBox.Checked;
            bool ok = await Run("Starting server...", () => RobloxServerGameLauncher.Host(GetApi(), game, no3d));
            if (ok)
            {
                await LoadGames();
            }
        }

        private async void JoinButton_Click(object sender, EventArgs e)
        {
            RobloxServerGame game = SelectedGame;
            if (game == null)
            {
                return;
            }

            await Run("Joining...", () => RobloxServerGameLauncher.Join(GetApi(), game));
        }
        #endregion

        #region Functions
        RobloxServerGame SelectedGame
        {
            get
            {
                if (GamesListView.SelectedItems.Count == 0)
                {
                    return null;
                }
                return GamesListView.SelectedItems[0].Tag as RobloxServerGame;
            }
        }

        static string SafeNormalize(string address)
        {
            try
            {
                return RobloxServerApi.NormalizeAddress(address);
            }
            catch (Exception)
            {
                return "";
            }
        }

        RobloxServerApi GetApi()
        {
            string address = RobloxServerApi.NormalizeAddress(AddressBox.Text);
            if (api == null || !string.Equals(api.BaseUrl, address, StringComparison.OrdinalIgnoreCase))
            {
                api = new RobloxServerApi(address);
            }
            return api;
        }

        void SetStatus(string text)
        {
            StatusLabel.Text = text;
        }

        void UpdateButtons()
        {
            bool loggedIn = api != null && api.LoggedIn;
            RobloxServerGame game = SelectedGame;

            LoginButton.Text = loggedIn ? "LOG OUT" : "LOG IN";
            UserBox.Enabled = !loggedIn && !busy;
            PasswordBox.Enabled = !loggedIn && !busy;
            LoginButton.Enabled = !busy;
            RefreshButton.Enabled = !busy;
            PlaySoloButton.Enabled = !busy && game != null;
            HostButton.Enabled = !busy && game != null && loggedIn && game.CanHost;
            JoinButton.Enabled = !busy && game != null && loggedIn && game.Servers > 0;
        }

        void ShowError(Exception ex)
        {
            Exception inner = ex is AggregateException ? ex.GetBaseException() : ex;
            if (!(inner is RobloxServerException))
            {
                Util.LogExceptions(inner);
            }
            MessageBox.Show(inner.Message, "RobloxServer - Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>Runs an action with the form marked as busy; errors become message boxes.</summary>
        async Task<bool> Run(string status, Func<Task> action)
        {
            if (busy)
            {
                return false;
            }

            string oldText = Text;
            busy = true;
            Text = "RobloxServer Games (" + status + ")";
            UpdateButtons();
            try
            {
                await action();
                return true;
            }
            catch (Exception ex)
            {
                ShowError(ex);
                return false;
            }
            finally
            {
                busy = false;
                Text = oldText;
                UpdateButtons();
            }
        }

        async Task LoadGames()
        {
            List<RobloxServerGame> loaded = null;
            string siteName = null;

            bool ok = await Run("Loading games...", async () =>
            {
                RobloxServerApi connection = GetApi();
                await Task.Run(() =>
                {
                    siteName = connection.SiteName();
                    loaded = connection.GetGames();
                });
            });

            if (!ok)
            {
                return;
            }

            games = loaded;
            long selectedId = SelectedGame != null ? SelectedGame.Id : 0;

            GamesListView.BeginUpdate();
            GamesListView.Items.Clear();
            foreach (RobloxServerGame game in games)
            {
                var item = new ListViewItem(game.Name) { Tag = game, UseItemStyleForSubItems = false };
                item.SubItems.Add(game.CreatorName);
                bool installed = Client.IsClientValid(game.Client);
                var client = item.SubItems.Add(game.Client);
                if (!installed)
                {
                    client.ForeColor = Color.Red;
                }
                item.SubItems.Add(game.Playing.ToString());
                item.SubItems.Add(game.Servers.ToString());
                item.SubItems.Add(game.Visits.ToString("N0"));
                item.SubItems.Add(installed ? "Yes" : "No");
                item.Selected = game.Id == selectedId;
                GamesListView.Items.Add(item);
            }
            GamesListView.EndUpdate();

            if (api == null || !api.LoggedIn)
            {
                SetStatus(siteName + ": not logged in.");
            }
            UpdateButtons();
        }
        #endregion
    }
    #endregion
}
