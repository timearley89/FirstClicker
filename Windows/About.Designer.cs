﻿namespace MoneyMiner.Windows
{
    partial class About
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
            lblAbout = new Label();
            btnOk = new Button();
            SuspendLayout();
            // 
            // lblAbout
            // 
            lblAbout.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lblAbout.BackColor = Color.FromArgb(128, 255, 255);
            lblAbout.BorderStyle = BorderStyle.Fixed3D;
            lblAbout.Font = new Font("Bahnschrift SemiBold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblAbout.Location = new Point(12, 9);
            lblAbout.Margin = new Padding(3, 0, 3, 5);
            lblAbout.Name = "lblAbout";
            lblAbout.Size = new Size(337, 381);
            lblAbout.TabIndex = 3;
            lblAbout.Text = "label1";
            lblAbout.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // btnOk
            // 
            btnOk.Anchor = AnchorStyles.Bottom;
            btnOk.BackColor = Color.FromArgb(128, 255, 255);
            btnOk.DialogResult = DialogResult.OK;
            btnOk.FlatStyle = FlatStyle.Popup;
            btnOk.Font = new Font("Bahnschrift SemiBold", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnOk.Location = new Point(134, 398);
            btnOk.Name = "btnOk";
            btnOk.Size = new Size(92, 40);
            btnOk.TabIndex = 2;
            btnOk.Text = "Close";
            btnOk.UseVisualStyleBackColor = false;
            // 
            // About
            // 
            AcceptButton = btnOk;
            AutoScaleMode = AutoScaleMode.None;
            BackColor = Color.Tan;
            ClientSize = new Size(361, 450);
            ControlBox = false;
            Controls.Add(lblAbout);
            Controls.Add(btnOk);
            Font = new Font("Bahnschrift SemiBold", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "About";
            ShowIcon = false;
            ShowInTaskbar = false;
            SizeGripStyle = SizeGripStyle.Hide;
            StartPosition = FormStartPosition.CenterParent;
            Text = "About MoneyMiner";
            Load += About_Load;
            ResumeLayout(false);
        }

        #endregion

        private Label lblAbout;
        private Button btnOk;
    }
}