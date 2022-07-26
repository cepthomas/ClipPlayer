
namespace ClipPlayer
{
    partial class Transport
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.progress = new NBagOfUis.Meter();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.sldVolume = new NBagOfUis.Slider();
            this.chkPlay = new System.Windows.Forms.CheckBox();
            this.chkLoop = new System.Windows.Forms.CheckBox();
            this.btnRewind = new System.Windows.Forms.Button();
            this.btnSettings = new System.Windows.Forms.Button();
            this.rtbLog = new System.Windows.Forms.RichTextBox();
            this.cmbDrumChannel = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // progress
            // 
            this.progress.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.progress.DrawColor = System.Drawing.Color.White;
            this.progress.Label = "";
            this.progress.Location = new System.Drawing.Point(128, 4);
            this.progress.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.progress.Maximum = 100D;
            this.progress.MeterType = NBagOfUis.MeterType.Linear;
            this.progress.Minimum = 0D;
            this.progress.Name = "progress";
            this.progress.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.progress.Size = new System.Drawing.Size(110, 44);
            this.progress.TabIndex = 3;
            this.toolTip.SetToolTip(this.progress, "Progress");
            // 
            // toolTip
            // 
            this.toolTip.AutomaticDelay = 50;
            this.toolTip.AutoPopDelay = 5000;
            this.toolTip.InitialDelay = 50;
            this.toolTip.ReshowDelay = 10;
            // 
            // sldVolume
            // 
            this.sldVolume.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.sldVolume.DrawColor = System.Drawing.Color.White;
            this.sldVolume.Label = "";
            this.sldVolume.Location = new System.Drawing.Point(80, 4);
            this.sldVolume.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.sldVolume.Maximum = 1D;
            this.sldVolume.Minimum = 0D;
            this.sldVolume.Name = "sldVolume";
            this.sldVolume.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.sldVolume.Resolution = 0.05D;
            this.sldVolume.Size = new System.Drawing.Size(44, 44);
            this.sldVolume.TabIndex = 9;
            this.toolTip.SetToolTip(this.sldVolume, "Volume");
            this.sldVolume.Value = 0.5D;
            // 
            // chkPlay
            // 
            this.chkPlay.Appearance = System.Windows.Forms.Appearance.Button;
            this.chkPlay.FlatAppearance.BorderSize = 0;
            this.chkPlay.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.chkPlay.Image = global::ClipPlayer.Properties.Resources.glyphicons_174_play;
            this.chkPlay.Location = new System.Drawing.Point(2, 4);
            this.chkPlay.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.chkPlay.Name = "chkPlay";
            this.chkPlay.Size = new System.Drawing.Size(36, 45);
            this.chkPlay.TabIndex = 10;
            this.toolTip.SetToolTip(this.chkPlay, "Start or stop");
            this.chkPlay.UseVisualStyleBackColor = true;
            // 
            // chkLoop
            // 
            this.chkLoop.Appearance = System.Windows.Forms.Appearance.Button;
            this.chkLoop.FlatAppearance.BorderSize = 0;
            this.chkLoop.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.chkLoop.Image = global::ClipPlayer.Properties.Resources.glyphicons_82_refresh;
            this.chkLoop.Location = new System.Drawing.Point(244, 4);
            this.chkLoop.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.chkLoop.Name = "chkLoop";
            this.chkLoop.Size = new System.Drawing.Size(36, 45);
            this.chkLoop.TabIndex = 85;
            this.toolTip.SetToolTip(this.chkLoop, "Go round and round");
            this.chkLoop.UseVisualStyleBackColor = true;
            // 
            // btnRewind
            // 
            this.btnRewind.FlatAppearance.BorderSize = 0;
            this.btnRewind.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRewind.Image = global::ClipPlayer.Properties.Resources.glyphicons_173_rewind;
            this.btnRewind.Location = new System.Drawing.Point(37, 4);
            this.btnRewind.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnRewind.Name = "btnRewind";
            this.btnRewind.Size = new System.Drawing.Size(36, 45);
            this.btnRewind.TabIndex = 82;
            this.toolTip.SetToolTip(this.btnRewind, "Go back jack");
            this.btnRewind.UseVisualStyleBackColor = true;
            // 
            // btnSettings
            // 
            this.btnSettings.FlatAppearance.BorderSize = 0;
            this.btnSettings.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSettings.Image = global::ClipPlayer.Properties.Resources.glyphicons_137_cogwheel;
            this.btnSettings.Location = new System.Drawing.Point(279, 4);
            this.btnSettings.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnSettings.Name = "btnSettings";
            this.btnSettings.Size = new System.Drawing.Size(36, 45);
            this.btnSettings.TabIndex = 83;
            this.toolTip.SetToolTip(this.btnSettings, "Your settings");
            this.btnSettings.UseVisualStyleBackColor = true;
            // 
            // rtbLog
            // 
            this.rtbLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rtbLog.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.rtbLog.Location = new System.Drawing.Point(2, 56);
            this.rtbLog.Name = "rtbLog";
            this.rtbLog.Size = new System.Drawing.Size(364, 263);
            this.rtbLog.TabIndex = 86;
            this.rtbLog.Text = "";
            // 
            // cmbDrumChannel
            // 
            this.cmbDrumChannel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbDrumChannel.FormattingEnabled = true;
            this.cmbDrumChannel.Location = new System.Drawing.Point(318, 13);
            this.cmbDrumChannel.Name = "cmbDrumChannel";
            this.cmbDrumChannel.Size = new System.Drawing.Size(42, 28);
            this.cmbDrumChannel.TabIndex = 87;
            this.toolTip.SetToolTip(this.cmbDrumChannel, "Drum channel if not default");
            // 
            // Transport
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(371, 323);
            this.Controls.Add(this.cmbDrumChannel);
            this.Controls.Add(this.rtbLog);
            this.Controls.Add(this.chkLoop);
            this.Controls.Add(this.btnSettings);
            this.Controls.Add(this.btnRewind);
            this.Controls.Add(this.chkPlay);
            this.Controls.Add(this.sldVolume);
            this.Controls.Add(this.progress);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "Transport";
            this.Text = "Clip Player";
            this.ResumeLayout(false);

        }

        #endregion
        private NBagOfUis.Meter progress;
        private System.Windows.Forms.ToolTip toolTip;
        private NBagOfUis.Slider sldVolume;
        private System.Windows.Forms.CheckBox chkPlay;
        private System.Windows.Forms.Button btnRewind;
        private System.Windows.Forms.Button btnSettings;
        private System.Windows.Forms.CheckBox chkLoop;
        private System.Windows.Forms.RichTextBox rtbLog;
        private System.Windows.Forms.ComboBox cmbDrumChannel;
    }
}