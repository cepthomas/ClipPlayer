
namespace ClipPlayer
{
    partial class Transport
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
            this.progress = new NBagOfTricks.UI.Meter();
            this.pbPlay = new System.Windows.Forms.PictureBox();
            this.pbStop = new System.Windows.Forms.PictureBox();
            this.pbRewind = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pbPlay)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbStop)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbRewind)).BeginInit();
            this.SuspendLayout();
            // 
            // progress
            // 
            this.progress.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.progress.DrawColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(255)))));
            this.progress.Label = "";
            this.progress.Location = new System.Drawing.Point(118, 3);
            this.progress.Maximum = 100D;
            this.progress.MeterType = NBagOfTricks.UI.MeterType.Linear;
            this.progress.Minimum = 0D;
            this.progress.Name = "progress";
            this.progress.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.progress.Size = new System.Drawing.Size(203, 30);
            this.progress.TabIndex = 3;
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
            this.pbPlay.Click += new System.EventHandler(this.Play_Click);
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
            this.pbStop.Click += new System.EventHandler(this.Stop_Click);
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
            this.pbRewind.Click += new System.EventHandler(this.Rewind_Click);
            // 
            // Transport
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(324, 39);
            this.Controls.Add(this.pbRewind);
            this.Controls.Add(this.pbStop);
            this.Controls.Add(this.pbPlay);
            this.Controls.Add(this.progress);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "Transport";
            this.Text = "Transport";
            ((System.ComponentModel.ISupportInitialize)(this.pbPlay)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbStop)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbRewind)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private NBagOfTricks.UI.Meter progress;
        private System.Windows.Forms.PictureBox pbPlay;
        private System.Windows.Forms.PictureBox pbStop;
        private System.Windows.Forms.PictureBox pbRewind;
    }
}