namespace IllutiaDataViewer
{
    partial class MainForm
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
            this.drawingPanel = new System.Windows.Forms.Panel();
            this.adfFileComboBox = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.animationInfoTextBox = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.selectedIdTextBox = new System.Windows.Forms.TextBox();
            this.selectedAnimationTextBox = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.filterComboBox = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // drawingPanel
            // 
            this.drawingPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.drawingPanel.Cursor = System.Windows.Forms.Cursors.Default;
            this.drawingPanel.Location = new System.Drawing.Point(12, 87);
            this.drawingPanel.Name = "drawingPanel";
            this.drawingPanel.Size = new System.Drawing.Size(645, 504);
            this.drawingPanel.TabIndex = 2;
            this.drawingPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.drawingPanel_Paint);
            this.drawingPanel.MouseClick += new System.Windows.Forms.MouseEventHandler(this.drawingPanel_MouseClick);
            // 
            // adfFileComboBox
            // 
            this.adfFileComboBox.FormattingEnabled = true;
            this.adfFileComboBox.Location = new System.Drawing.Point(83, 10);
            this.adfFileComboBox.Name = "adfFileComboBox";
            this.adfFileComboBox.Size = new System.Drawing.Size(109, 21);
            this.adfFileComboBox.TabIndex = 3;
            this.adfFileComboBox.SelectedIndexChanged += new System.EventHandler(this.adfFileComboBox_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(50, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "ADF File:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(431, 45);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(87, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Body/Equip Info:";
            // 
            // animationInfoTextBox
            // 
            this.animationInfoTextBox.Location = new System.Drawing.Point(538, 42);
            this.animationInfoTextBox.Name = "animationInfoTextBox";
            this.animationInfoTextBox.ReadOnly = true;
            this.animationInfoTextBox.Size = new System.Drawing.Size(121, 20);
            this.animationInfoTextBox.TabIndex = 6;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 45);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(66, 13);
            this.label3.TabIndex = 7;
            this.label3.Text = "Selected ID:";
            // 
            // selectedIdTextBox
            // 
            this.selectedIdTextBox.Location = new System.Drawing.Point(83, 42);
            this.selectedIdTextBox.Name = "selectedIdTextBox";
            this.selectedIdTextBox.ReadOnly = true;
            this.selectedIdTextBox.Size = new System.Drawing.Size(109, 20);
            this.selectedIdTextBox.TabIndex = 8;
            // 
            // selectedAnimationTextBox
            // 
            this.selectedAnimationTextBox.Location = new System.Drawing.Point(305, 42);
            this.selectedAnimationTextBox.Name = "selectedAnimationTextBox";
            this.selectedAnimationTextBox.ReadOnly = true;
            this.selectedAnimationTextBox.Size = new System.Drawing.Size(119, 20);
            this.selectedAnimationTextBox.TabIndex = 10;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(198, 45);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(101, 13);
            this.label4.TabIndex = 9;
            this.label4.Text = "Selected Animation:";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(262, 13);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(37, 13);
            this.label5.TabIndex = 12;
            this.label5.Text = "Show:";
            // 
            // filterComboBox
            // 
            this.filterComboBox.FormattingEnabled = true;
            this.filterComboBox.Location = new System.Drawing.Point(305, 10);
            this.filterComboBox.Name = "filterComboBox";
            this.filterComboBox.Size = new System.Drawing.Size(119, 21);
            this.filterComboBox.TabIndex = 11;
            this.filterComboBox.SelectedIndexChanged += new System.EventHandler(this.filterComboBox_SelectedIndexChanged);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(669, 603);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.filterComboBox);
            this.Controls.Add(this.selectedAnimationTextBox);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.selectedIdTextBox);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.animationInfoTextBox);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.adfFileComboBox);
            this.Controls.Add(this.drawingPanel);
            this.Name = "MainForm";
            this.Text = "Illutia Data Viewer";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.Resize += new System.EventHandler(this.MainForm_Resize);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel drawingPanel;
        private System.Windows.Forms.ComboBox adfFileComboBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox animationInfoTextBox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox selectedIdTextBox;
        private System.Windows.Forms.TextBox selectedAnimationTextBox;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox filterComboBox;
    }
}

