namespace NovetusLauncher
{
    partial class RobloxServerBrowser
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.AddressLabel = new System.Windows.Forms.Label();
            this.AddressBox = new System.Windows.Forms.TextBox();
            this.RefreshButton = new System.Windows.Forms.Button();
            this.WebsiteButton = new System.Windows.Forms.Button();
            this.UserLabel = new System.Windows.Forms.Label();
            this.UserBox = new System.Windows.Forms.TextBox();
            this.PasswordLabel = new System.Windows.Forms.Label();
            this.PasswordBox = new System.Windows.Forms.TextBox();
            this.LoginButton = new System.Windows.Forms.Button();
            this.StatusLabel = new System.Windows.Forms.Label();
            this.GamesListView = new System.Windows.Forms.ListView();
            this.DescriptionBox = new System.Windows.Forms.TextBox();
            this.PlaySoloButton = new System.Windows.Forms.Button();
            this.HostButton = new System.Windows.Forms.Button();
            this.JoinButton = new System.Windows.Forms.Button();
            this.No3DCheckBox = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            //
            // AddressLabel
            //
            this.AddressLabel.AutoSize = true;
            this.AddressLabel.Location = new System.Drawing.Point(12, 13);
            this.AddressLabel.Name = "AddressLabel";
            this.AddressLabel.Size = new System.Drawing.Size(73, 13);
            this.AddressLabel.TabIndex = 0;
            this.AddressLabel.Text = "RobloxServer:";
            //
            // AddressBox
            //
            this.AddressBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.AddressBox.Location = new System.Drawing.Point(91, 10);
            this.AddressBox.Name = "AddressBox";
            this.AddressBox.Size = new System.Drawing.Size(453, 20);
            this.AddressBox.TabIndex = 1;
            this.AddressBox.TextChanged += new System.EventHandler(this.AddressBox_TextChanged);
            //
            // RefreshButton
            //
            this.RefreshButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.RefreshButton.Location = new System.Drawing.Point(550, 8);
            this.RefreshButton.Name = "RefreshButton";
            this.RefreshButton.Size = new System.Drawing.Size(75, 23);
            this.RefreshButton.TabIndex = 2;
            this.RefreshButton.Text = "REFRESH";
            this.RefreshButton.UseVisualStyleBackColor = true;
            this.RefreshButton.Click += new System.EventHandler(this.RefreshButton_Click);
            //
            // WebsiteButton
            //
            this.WebsiteButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.WebsiteButton.Location = new System.Drawing.Point(631, 8);
            this.WebsiteButton.Name = "WebsiteButton";
            this.WebsiteButton.Size = new System.Drawing.Size(75, 23);
            this.WebsiteButton.TabIndex = 3;
            this.WebsiteButton.Text = "WEBSITE";
            this.WebsiteButton.UseVisualStyleBackColor = true;
            this.WebsiteButton.Click += new System.EventHandler(this.WebsiteButton_Click);
            //
            // UserLabel
            //
            this.UserLabel.AutoSize = true;
            this.UserLabel.Location = new System.Drawing.Point(12, 42);
            this.UserLabel.Name = "UserLabel";
            this.UserLabel.Size = new System.Drawing.Size(58, 13);
            this.UserLabel.TabIndex = 4;
            this.UserLabel.Text = "Username:";
            //
            // UserBox
            //
            this.UserBox.Location = new System.Drawing.Point(91, 39);
            this.UserBox.MaxLength = 20;
            this.UserBox.Name = "UserBox";
            this.UserBox.Size = new System.Drawing.Size(140, 20);
            this.UserBox.TabIndex = 5;
            //
            // PasswordLabel
            //
            this.PasswordLabel.AutoSize = true;
            this.PasswordLabel.Location = new System.Drawing.Point(240, 42);
            this.PasswordLabel.Name = "PasswordLabel";
            this.PasswordLabel.Size = new System.Drawing.Size(56, 13);
            this.PasswordLabel.TabIndex = 6;
            this.PasswordLabel.Text = "Password:";
            //
            // PasswordBox
            //
            this.PasswordBox.Location = new System.Drawing.Point(302, 39);
            this.PasswordBox.Name = "PasswordBox";
            this.PasswordBox.Size = new System.Drawing.Size(140, 20);
            this.PasswordBox.TabIndex = 7;
            this.PasswordBox.UseSystemPasswordChar = true;
            this.PasswordBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.PasswordBox_KeyDown);
            //
            // LoginButton
            //
            this.LoginButton.Location = new System.Drawing.Point(448, 37);
            this.LoginButton.Name = "LoginButton";
            this.LoginButton.Size = new System.Drawing.Size(75, 23);
            this.LoginButton.TabIndex = 8;
            this.LoginButton.Text = "LOG IN";
            this.LoginButton.UseVisualStyleBackColor = true;
            this.LoginButton.Click += new System.EventHandler(this.LoginButton_Click);
            //
            // StatusLabel
            //
            this.StatusLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.StatusLabel.AutoEllipsis = true;
            this.StatusLabel.Location = new System.Drawing.Point(529, 42);
            this.StatusLabel.Name = "StatusLabel";
            this.StatusLabel.Size = new System.Drawing.Size(177, 13);
            this.StatusLabel.TabIndex = 9;
            this.StatusLabel.Text = "Not logged in.";
            //
            // GamesListView
            //
            this.GamesListView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.GamesListView.FullRowSelect = true;
            this.GamesListView.HideSelection = false;
            this.GamesListView.Location = new System.Drawing.Point(12, 68);
            this.GamesListView.MultiSelect = false;
            this.GamesListView.Name = "GamesListView";
            this.GamesListView.Size = new System.Drawing.Size(694, 300);
            this.GamesListView.TabIndex = 10;
            this.GamesListView.UseCompatibleStateImageBehavior = false;
            this.GamesListView.View = System.Windows.Forms.View.Details;
            this.GamesListView.SelectedIndexChanged += new System.EventHandler(this.GamesListView_SelectedIndexChanged);
            this.GamesListView.DoubleClick += new System.EventHandler(this.GamesListView_DoubleClick);
            //
            // DescriptionBox
            //
            this.DescriptionBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.DescriptionBox.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.DescriptionBox.Location = new System.Drawing.Point(12, 374);
            this.DescriptionBox.Multiline = true;
            this.DescriptionBox.Name = "DescriptionBox";
            this.DescriptionBox.ReadOnly = true;
            this.DescriptionBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.DescriptionBox.Size = new System.Drawing.Size(694, 60);
            this.DescriptionBox.TabIndex = 11;
            //
            // PlaySoloButton
            //
            this.PlaySoloButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.PlaySoloButton.Location = new System.Drawing.Point(388, 442);
            this.PlaySoloButton.Name = "PlaySoloButton";
            this.PlaySoloButton.Size = new System.Drawing.Size(100, 28);
            this.PlaySoloButton.TabIndex = 13;
            this.PlaySoloButton.Text = "PLAY SOLO";
            this.PlaySoloButton.UseVisualStyleBackColor = true;
            this.PlaySoloButton.Click += new System.EventHandler(this.PlaySoloButton_Click);
            //
            // HostButton
            //
            this.HostButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.HostButton.Location = new System.Drawing.Point(497, 442);
            this.HostButton.Name = "HostButton";
            this.HostButton.Size = new System.Drawing.Size(100, 28);
            this.HostButton.TabIndex = 14;
            this.HostButton.Text = "HOST SERVER";
            this.HostButton.UseVisualStyleBackColor = true;
            this.HostButton.Click += new System.EventHandler(this.HostButton_Click);
            //
            // JoinButton
            //
            this.JoinButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.JoinButton.Location = new System.Drawing.Point(606, 442);
            this.JoinButton.Name = "JoinButton";
            this.JoinButton.Size = new System.Drawing.Size(100, 28);
            this.JoinButton.TabIndex = 15;
            this.JoinButton.Text = "JOIN SERVER";
            this.JoinButton.UseVisualStyleBackColor = true;
            this.JoinButton.Click += new System.EventHandler(this.JoinButton_Click);
            //
            // No3DCheckBox
            //
            this.No3DCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.No3DCheckBox.AutoSize = true;
            this.No3DCheckBox.Location = new System.Drawing.Point(12, 449);
            this.No3DCheckBox.Name = "No3DCheckBox";
            this.No3DCheckBox.Size = new System.Drawing.Size(185, 17);
            this.No3DCheckBox.TabIndex = 12;
            this.No3DCheckBox.Text = "Host without graphics (No3D mode)";
            this.No3DCheckBox.UseVisualStyleBackColor = true;
            //
            // RobloxServerBrowser
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.ClientSize = new System.Drawing.Size(718, 480);
            this.Controls.Add(this.No3DCheckBox);
            this.Controls.Add(this.JoinButton);
            this.Controls.Add(this.HostButton);
            this.Controls.Add(this.PlaySoloButton);
            this.Controls.Add(this.DescriptionBox);
            this.Controls.Add(this.GamesListView);
            this.Controls.Add(this.StatusLabel);
            this.Controls.Add(this.LoginButton);
            this.Controls.Add(this.PasswordBox);
            this.Controls.Add(this.PasswordLabel);
            this.Controls.Add(this.UserBox);
            this.Controls.Add(this.UserLabel);
            this.Controls.Add(this.WebsiteButton);
            this.Controls.Add(this.RefreshButton);
            this.Controls.Add(this.AddressBox);
            this.Controls.Add(this.AddressLabel);
            this.MinimumSize = new System.Drawing.Size(734, 519);
            this.Name = "RobloxServerBrowser";
            this.Text = "RobloxServer Games";
            this.Load += new System.EventHandler(this.RobloxServerBrowser_Load);
            this.Shown += new System.EventHandler(this.RobloxServerBrowser_Shown);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label AddressLabel;
        private System.Windows.Forms.TextBox AddressBox;
        private System.Windows.Forms.Button RefreshButton;
        private System.Windows.Forms.Button WebsiteButton;
        private System.Windows.Forms.Label UserLabel;
        private System.Windows.Forms.TextBox UserBox;
        private System.Windows.Forms.Label PasswordLabel;
        private System.Windows.Forms.TextBox PasswordBox;
        private System.Windows.Forms.Button LoginButton;
        private System.Windows.Forms.Label StatusLabel;
        private System.Windows.Forms.ListView GamesListView;
        private System.Windows.Forms.TextBox DescriptionBox;
        private System.Windows.Forms.Button PlaySoloButton;
        private System.Windows.Forms.Button HostButton;
        private System.Windows.Forms.Button JoinButton;
        private System.Windows.Forms.CheckBox No3DCheckBox;
    }
}
