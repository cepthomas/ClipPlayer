
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
            this.progress = new NBagOfTricks.UI.Meter();
            this.pbPlay = new System.Windows.Forms.PictureBox();
            this.pbStop = new System.Windows.Forms.PictureBox();
            this.pbRewind = new System.Windows.Forms.PictureBox();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.pbSettings = new System.Windows.Forms.PictureBox();
            this.logBox = new System.Windows.Forms.RichTextBox();
            this.sldVolume = new NBagOfTricks.UI.Slider();
            ((System.ComponentModel.ISupportInitialize)(this.pbPlay)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbStop)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbRewind)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbSettings)).BeginInit();
            this.SuspendLayout();
            // 
            // progress
            // 
            this.progress.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.progress.DrawColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(255)))));
            this.progress.Label = "";
            this.progress.Location = new System.Drawing.Point(164, 3);
            this.progress.Maximum = 100D;
            this.progress.MeterType = NBagOfTricks.UI.MeterType.Linear;
            this.progress.Minimum = 0D;
            this.progress.Name = "progress";
            this.progress.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.progress.Size = new System.Drawing.Size(167, 30);
            this.progress.TabIndex = 3;
            this.progress.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Progress_MouseDown);
            this.progress.MouseMove += new System.Windows.Forms.MouseEventHandler(this.Progress_MouseMove);
            // 
            // pbPlay
            // 
            this.pbPlay.BackgroundImage = global::ClipPlayer.Properties.Resources.glyphicons_174_play;
            this.pbPlay.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.pbPlay.Location = new System.Drawing.Point(2, 3);
            this.pbPlay.Name = "pbPlay";
            this.pbPlay.Size = new System.Drawing.Size(32, 32);
            this.pbPlay.TabIndex = 4;
            this.pbPlay.TabStop = false;
            this.toolTip.SetToolTip(this.pbPlay, "Go go go");
            // 
            // pbStop
            // 
            this.pbStop.BackgroundImage = global::ClipPlayer.Properties.Resources.glyphicons_176_stop;
            this.pbStop.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.pbStop.Location = new System.Drawing.Point(41, 3);
            this.pbStop.Name = "pbStop";
            this.pbStop.Size = new System.Drawing.Size(32, 32);
            this.pbStop.TabIndex = 5;
            this.pbStop.TabStop = false;
            this.toolTip.SetToolTip(this.pbStop, "Hold your horses");
            // 
            // pbRewind
            // 
            this.pbRewind.BackgroundImage = global::ClipPlayer.Properties.Resources.glyphicons_173_rewind;
            this.pbRewind.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.pbRewind.Location = new System.Drawing.Point(79, 3);
            this.pbRewind.Name = "pbRewind";
            this.pbRewind.Size = new System.Drawing.Size(32, 32);
            this.pbRewind.TabIndex = 6;
            this.pbRewind.TabStop = false;
            this.toolTip.SetToolTip(this.pbRewind, "Go back Jack");
            // 
            // toolTip
            // 
            this.toolTip.AutomaticDelay = 50;
            // 
            // pbSettings
            // 
            this.pbSettings.BackgroundImage = global::ClipPlayer.Properties.Resources.glyphicons_137_cogwheel;
            this.pbSettings.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.pbSettings.Location = new System.Drawing.Point(337, 3);
            this.pbSettings.Name = "pbSettings";
            this.pbSettings.Size = new System.Drawing.Size(32, 32);
            this.pbSettings.TabIndex = 8;
            this.pbSettings.TabStop = false;
            this.toolTip.SetToolTip(this.pbSettings, "How do you like it");
            this.pbSettings.Click += new System.EventHandler(this.Settings_Click);
            // 
            // logBox
            // 
            this.logBox.Location = new System.Drawing.Point(2, 50);
            this.logBox.Name = "logBox";
            this.logBox.Size = new System.Drawing.Size(367, 144);
            this.logBox.TabIndex = 7;
            this.logBox.Text = "";
            // 
            // sldVolume
            // 
            this.sldVolume.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.sldVolume.DecPlaces = 1;
            this.sldVolume.DrawColor = System.Drawing.Color.Pink;
            this.sldVolume.Label = "";
            this.sldVolume.Location = new System.Drawing.Point(118, 3);
            this.sldVolume.Maximum = 1D;
            this.sldVolume.Minimum = 0D;
            this.sldVolume.Name = "sldVolume";
            this.sldVolume.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.sldVolume.ResetValue = 0D;
            this.sldVolume.Size = new System.Drawing.Size(40, 32);
            this.sldVolume.TabIndex = 9;
            this.sldVolume.Value = 0.5D;
            // 
            // Transport
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(375, 197);
            this.Controls.Add(this.sldVolume);
            this.Controls.Add(this.pbSettings);
            this.Controls.Add(this.logBox);
            this.Controls.Add(this.pbRewind);
            this.Controls.Add(this.pbStop);
            this.Controls.Add(this.pbPlay);
            this.Controls.Add(this.progress);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "Transport";
            this.Text = "Clip Player";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Transport_FormClosing);
            this.Load += new System.EventHandler(this.Transport_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pbPlay)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbStop)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbRewind)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbSettings)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private NBagOfTricks.UI.Meter progress;
        private System.Windows.Forms.PictureBox pbPlay;
        private System.Windows.Forms.PictureBox pbStop;
        private System.Windows.Forms.PictureBox pbRewind;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.RichTextBox logBox;
        private System.Windows.Forms.PictureBox pbSettings;
        private NBagOfTricks.UI.Slider sldVolume;
    }
}