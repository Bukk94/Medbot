namespace Medbot_UI
{
    partial class MainFrame
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainFrame));
            this.textBox = new System.Windows.Forms.RichTextBox();
            this.messageBox = new System.Windows.Forms.RichTextBox();
            this.startStopButton = new System.Windows.Forms.Button();
            this.botInfoStatus = new System.Windows.Forms.Label();
            this.colorPanel = new System.Windows.Forms.Panel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.infoLabelChannel = new System.Windows.Forms.Label();
            this.uptimeLabel = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // textBox
            // 
            this.textBox.BackColor = System.Drawing.SystemColors.Control;
            this.textBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox.Location = new System.Drawing.Point(12, 12);
            this.textBox.Name = "textBox";
            this.textBox.Size = new System.Drawing.Size(547, 342);
            this.textBox.TabIndex = 1;
            this.textBox.Text = "";
            // 
            // messageBox
            // 
            this.messageBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.messageBox.Location = new System.Drawing.Point(565, 41);
            this.messageBox.Name = "messageBox";
            this.messageBox.Size = new System.Drawing.Size(259, 250);
            this.messageBox.TabIndex = 2;
            this.messageBox.Text = "";
            // 
            // startStopButton
            // 
            this.startStopButton.Location = new System.Drawing.Point(599, 12);
            this.startStopButton.Name = "startStopButton";
            this.startStopButton.Size = new System.Drawing.Size(75, 23);
            this.startStopButton.TabIndex = 3;
            this.startStopButton.Text = "Start";
            this.startStopButton.UseVisualStyleBackColor = true;
            this.startStopButton.Click += new System.EventHandler(this.StartStopButton_Click);
            // 
            // botInfoStatus
            // 
            this.botInfoStatus.AutoSize = true;
            this.botInfoStatus.Font = new System.Drawing.Font("Microsoft Uighur", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.botInfoStatus.Location = new System.Drawing.Point(706, 17);
            this.botInfoStatus.Name = "botInfoStatus";
            this.botInfoStatus.Size = new System.Drawing.Size(64, 14);
            this.botInfoStatus.TabIndex = 4;
            this.botInfoStatus.Text = "Bot is IDLE";
            // 
            // colorPanel
            // 
            this.colorPanel.BackColor = System.Drawing.Color.Red;
            this.colorPanel.Location = new System.Drawing.Point(682, 15);
            this.colorPanel.Name = "colorPanel";
            this.colorPanel.Size = new System.Drawing.Size(16, 18);
            this.colorPanel.TabIndex = 5;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.uptimeLabel);
            this.panel1.Controls.Add(this.infoLabelChannel);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Location = new System.Drawing.Point(565, 298);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(259, 56);
            this.panel1.TabIndex = 6;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(2, 4);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(96, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Deployed channel:";
            // 
            // infoLabelChannel
            // 
            this.infoLabelChannel.AutoSize = true;
            this.infoLabelChannel.Location = new System.Drawing.Point(99, 4);
            this.infoLabelChannel.Name = "infoLabelChannel";
            this.infoLabelChannel.Size = new System.Drawing.Size(50, 13);
            this.infoLabelChannel.TabIndex = 1;
            this.infoLabelChannel.Text = "--NONE--";
            // 
            // uptimeLabel
            // 
            this.uptimeLabel.AutoSize = true;
            this.uptimeLabel.Location = new System.Drawing.Point(74, 30);
            this.uptimeLabel.Name = "uptimeLabel";
            this.uptimeLabel.Size = new System.Drawing.Size(16, 13);
            this.uptimeLabel.TabIndex = 2;
            this.uptimeLabel.Text = "---";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 30);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(67, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Bot\'s uptime:";
            // 
            // MainFrame
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(836, 366);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.colorPanel);
            this.Controls.Add(this.botInfoStatus);
            this.Controls.Add(this.startStopButton);
            this.Controls.Add(this.messageBox);
            this.Controls.Add(this.textBox);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "MainFrame";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "MedBot";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainFrame_FormClosing);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.RichTextBox textBox;
        private System.Windows.Forms.RichTextBox messageBox;
        private System.Windows.Forms.Button startStopButton;
        private System.Windows.Forms.Label botInfoStatus;
        private System.Windows.Forms.Panel colorPanel;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label infoLabelChannel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label uptimeLabel;
        private System.Windows.Forms.Label label2;
    }
}

