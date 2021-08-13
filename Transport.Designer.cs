
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
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.sldVolume = new NBagOfTricks.UI.Slider();
            this.chkPlay = new System.Windows.Forms.CheckBox();
            this.chkPatch = new System.Windows.Forms.CheckBox();
            this.btnPatch = new System.Windows.Forms.Button();
            this.cmbPatchList = new System.Windows.Forms.ComboBox();
            this.txtPatchChannel = new System.Windows.Forms.TextBox();
            this.txtDrumChannel = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.btnLog = new System.Windows.Forms.Button();
            this.btnRewind = new System.Windows.Forms.Button();
            this.btnSettings = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // progress
            // 
            this.progress.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.progress.DrawColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(255)))));
            this.progress.Label = "";
            this.progress.Location = new System.Drawing.Point(128, 3);
            this.progress.Maximum = 100D;
            this.progress.MeterType = NBagOfTricks.UI.MeterType.Linear;
            this.progress.Minimum = 0D;
            this.progress.Name = "progress";
            this.progress.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.progress.Size = new System.Drawing.Size(110, 36);
            this.progress.TabIndex = 3;
            this.toolTip.SetToolTip(this.progress, "Progress");
            this.progress.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Progress_MouseDown);
            this.progress.MouseMove += new System.Windows.Forms.MouseEventHandler(this.Progress_MouseMove);
            // 
            // toolTip
            // 
            this.toolTip.AutomaticDelay = 50;
            // 
            // sldVolume
            // 
            this.sldVolume.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.sldVolume.DecPlaces = 1;
            this.sldVolume.DrawColor = System.Drawing.Color.Pink;
            this.sldVolume.Label = "";
            this.sldVolume.Location = new System.Drawing.Point(80, 3);
            this.sldVolume.Maximum = 1D;
            this.sldVolume.Minimum = 0D;
            this.sldVolume.Name = "sldVolume";
            this.sldVolume.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.sldVolume.ResetValue = 0D;
            this.sldVolume.Size = new System.Drawing.Size(44, 36);
            this.sldVolume.TabIndex = 9;
            this.toolTip.SetToolTip(this.sldVolume, "Volume");
            this.sldVolume.Value = 0.5D;
            // 
            // chkPlay
            // 
            this.chkPlay.Appearance = System.Windows.Forms.Appearance.Button;
            this.chkPlay.FlatAppearance.CheckedBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
            this.chkPlay.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.chkPlay.Image = global::ClipPlayer.Properties.Resources.glyphicons_174_play;
            this.chkPlay.Location = new System.Drawing.Point(2, 3);
            this.chkPlay.Name = "chkPlay";
            this.chkPlay.Size = new System.Drawing.Size(36, 36);
            this.chkPlay.TabIndex = 10;
            this.toolTip.SetToolTip(this.chkPlay, "Start or stop");
            this.chkPlay.UseVisualStyleBackColor = true;
            // 
            // chkPatch
            // 
            this.chkPatch.Appearance = System.Windows.Forms.Appearance.Button;
            this.chkPatch.FlatAppearance.BorderSize = 0;
            this.chkPatch.FlatAppearance.CheckedBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
            this.chkPatch.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.chkPatch.Font = new System.Drawing.Font("Microsoft Sans Serif", 6F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkPatch.Location = new System.Drawing.Point(276, 1);
            this.chkPatch.Name = "chkPatch";
            this.chkPatch.Size = new System.Drawing.Size(22, 22);
            this.chkPatch.TabIndex = 12;
            this.chkPatch.Text = "M";
            this.toolTip.SetToolTip(this.chkPatch, "Show midi patching functions");
            this.chkPatch.UseVisualStyleBackColor = true;
            // 
            // btnPatch
            // 
            this.btnPatch.Location = new System.Drawing.Point(9, 92);
            this.btnPatch.Name = "btnPatch";
            this.btnPatch.Size = new System.Drawing.Size(57, 23);
            this.btnPatch.TabIndex = 80;
            this.btnPatch.Text = "Patch";
            this.toolTip.SetToolTip(this.btnPatch, "Send the patch to channel");
            this.btnPatch.UseVisualStyleBackColor = true;
            this.btnPatch.Click += new System.EventHandler(this.Patch_Click);
            // 
            // cmbPatchList
            // 
            this.cmbPatchList.BackColor = System.Drawing.SystemColors.Control;
            this.cmbPatchList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPatchList.DropDownWidth = 150;
            this.cmbPatchList.FormattingEnabled = true;
            this.cmbPatchList.Location = new System.Drawing.Point(116, 91);
            this.cmbPatchList.Name = "cmbPatchList";
            this.cmbPatchList.Size = new System.Drawing.Size(179, 24);
            this.cmbPatchList.TabIndex = 79;
            this.toolTip.SetToolTip(this.cmbPatchList, "Patch name");
            // 
            // txtPatchChannel
            // 
            this.txtPatchChannel.BackColor = System.Drawing.SystemColors.Control;
            this.txtPatchChannel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtPatchChannel.Location = new System.Drawing.Point(74, 92);
            this.txtPatchChannel.Name = "txtPatchChannel";
            this.txtPatchChannel.Size = new System.Drawing.Size(34, 22);
            this.txtPatchChannel.TabIndex = 78;
            this.toolTip.SetToolTip(this.txtPatchChannel, "Patch channel number");
            // 
            // txtDrumChannel
            // 
            this.txtDrumChannel.BackColor = System.Drawing.SystemColors.Control;
            this.txtDrumChannel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtDrumChannel.Location = new System.Drawing.Point(110, 58);
            this.txtDrumChannel.Name = "txtDrumChannel";
            this.txtDrumChannel.Size = new System.Drawing.Size(25, 22);
            this.txtDrumChannel.TabIndex = 77;
            this.txtDrumChannel.Text = "10";
            this.toolTip.SetToolTip(this.txtDrumChannel, "Reassign the drum channel");
            this.txtDrumChannel.TextChanged += new System.EventHandler(this.DrumChannel_TextChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 60);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(98, 17);
            this.label1.TabIndex = 76;
            this.label1.Text = "Drum Channel";
            // 
            // btnLog
            // 
            this.btnLog.FlatAppearance.BorderSize = 0;
            this.btnLog.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLog.Font = new System.Drawing.Font("Microsoft Sans Serif", 6F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnLog.Location = new System.Drawing.Point(276, 17);
            this.btnLog.Name = "btnLog";
            this.btnLog.Size = new System.Drawing.Size(22, 22);
            this.btnLog.TabIndex = 81;
            this.btnLog.Text = "L";
            this.btnLog.UseVisualStyleBackColor = false;
            this.btnLog.Click += new System.EventHandler(this.Log_Click);
            // 
            // btnRewind
            // 
            this.btnRewind.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRewind.Image = global::ClipPlayer.Properties.Resources.glyphicons_173_rewind;
            this.btnRewind.Location = new System.Drawing.Point(41, 3);
            this.btnRewind.Name = "btnRewind";
            this.btnRewind.Size = new System.Drawing.Size(36, 36);
            this.btnRewind.TabIndex = 82;
            this.btnRewind.UseVisualStyleBackColor = true;
            // 
            // btnSettings
            // 
            this.btnSettings.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSettings.Image = global::ClipPlayer.Properties.Resources.glyphicons_137_cogwheel;
            this.btnSettings.Location = new System.Drawing.Point(242, 3);
            this.btnSettings.Name = "btnSettings";
            this.btnSettings.Size = new System.Drawing.Size(36, 36);
            this.btnSettings.TabIndex = 83;
            this.btnSettings.UseVisualStyleBackColor = true;
            this.btnSettings.Click += new System.EventHandler(this.Settings_Click);
            // 
            // Transport
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(307, 131);
            this.Controls.Add(this.btnSettings);
            this.Controls.Add(this.btnRewind);
            this.Controls.Add(this.btnLog);
            this.Controls.Add(this.btnPatch);
            this.Controls.Add(this.cmbPatchList);
            this.Controls.Add(this.txtPatchChannel);
            this.Controls.Add(this.txtDrumChannel);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.chkPatch);
            this.Controls.Add(this.chkPlay);
            this.Controls.Add(this.sldVolume);
            this.Controls.Add(this.progress);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "Transport";
            this.Text = "Clip Player";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Transport_FormClosing);
            this.Load += new System.EventHandler(this.Transport_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private NBagOfTricks.UI.Meter progress;
        private System.Windows.Forms.ToolTip toolTip;
        private NBagOfTricks.UI.Slider sldVolume;
        private System.Windows.Forms.CheckBox chkPlay;
        private System.Windows.Forms.CheckBox chkPatch;
        private System.Windows.Forms.Button btnPatch;
        private System.Windows.Forms.ComboBox cmbPatchList;
        private System.Windows.Forms.TextBox txtPatchChannel;
        private System.Windows.Forms.TextBox txtDrumChannel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnLog;
        private System.Windows.Forms.Button btnRewind;
        private System.Windows.Forms.Button btnSettings;
    }
}