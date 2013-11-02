namespace IllutiaMapViewer
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
            this.mapsComboBox = new System.Windows.Forms.ComboBox();
            this.drawArea = new System.Windows.Forms.Panel();
            this.vScrollBar = new System.Windows.Forms.VScrollBar();
            this.hScrollBar = new System.Windows.Forms.HScrollBar();
            this.blockedTilesCheckBox = new System.Windows.Forms.CheckBox();
            this.showRoofsCheckBox = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // mapsComboBox
            // 
            this.mapsComboBox.FormattingEnabled = true;
            this.mapsComboBox.Location = new System.Drawing.Point(12, 12);
            this.mapsComboBox.Name = "mapsComboBox";
            this.mapsComboBox.Size = new System.Drawing.Size(144, 21);
            this.mapsComboBox.TabIndex = 0;
            this.mapsComboBox.SelectedIndexChanged += new System.EventHandler(this.mapsComboBox_SelectedIndexChanged);
            // 
            // drawArea
            // 
            this.drawArea.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.drawArea.Location = new System.Drawing.Point(12, 39);
            this.drawArea.Name = "drawArea";
            this.drawArea.Size = new System.Drawing.Size(597, 351);
            this.drawArea.TabIndex = 1;
            this.drawArea.Paint += new System.Windows.Forms.PaintEventHandler(this.drawArea_Paint);
            this.drawArea.Resize += new System.EventHandler(this.drawArea_Resize);
            // 
            // vScrollBar
            // 
            this.vScrollBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.vScrollBar.LargeChange = 32;
            this.vScrollBar.Location = new System.Drawing.Point(609, 39);
            this.vScrollBar.Name = "vScrollBar";
            this.vScrollBar.Size = new System.Drawing.Size(17, 351);
            this.vScrollBar.SmallChange = 32;
            this.vScrollBar.TabIndex = 2;
            this.vScrollBar.Scroll += new System.Windows.Forms.ScrollEventHandler(this.vScrollBar_Scroll);
            // 
            // hScrollBar
            // 
            this.hScrollBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.hScrollBar.Location = new System.Drawing.Point(12, 390);
            this.hScrollBar.Name = "hScrollBar";
            this.hScrollBar.Size = new System.Drawing.Size(597, 17);
            this.hScrollBar.TabIndex = 3;
            this.hScrollBar.Scroll += new System.Windows.Forms.ScrollEventHandler(this.hScrollBar_Scroll);
            // 
            // blockedTilesCheckBox
            // 
            this.blockedTilesCheckBox.AutoSize = true;
            this.blockedTilesCheckBox.Checked = true;
            this.blockedTilesCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.blockedTilesCheckBox.Location = new System.Drawing.Point(162, 12);
            this.blockedTilesCheckBox.Name = "blockedTilesCheckBox";
            this.blockedTilesCheckBox.Size = new System.Drawing.Size(90, 17);
            this.blockedTilesCheckBox.TabIndex = 4;
            this.blockedTilesCheckBox.Text = "Blocked Tiles";
            this.blockedTilesCheckBox.UseVisualStyleBackColor = true;
            this.blockedTilesCheckBox.CheckedChanged += new System.EventHandler(this.blockedTilesCheckBox_CheckedChanged);
            // 
            // showRoofsCheckBox
            // 
            this.showRoofsCheckBox.AutoSize = true;
            this.showRoofsCheckBox.Checked = true;
            this.showRoofsCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.showRoofsCheckBox.Location = new System.Drawing.Point(258, 12);
            this.showRoofsCheckBox.Name = "showRoofsCheckBox";
            this.showRoofsCheckBox.Size = new System.Drawing.Size(54, 17);
            this.showRoofsCheckBox.TabIndex = 5;
            this.showRoofsCheckBox.Text = "Roofs";
            this.showRoofsCheckBox.UseVisualStyleBackColor = true;
            this.showRoofsCheckBox.CheckedChanged += new System.EventHandler(this.showRoofsCheckBox_CheckedChanged);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(635, 416);
            this.Controls.Add(this.showRoofsCheckBox);
            this.Controls.Add(this.blockedTilesCheckBox);
            this.Controls.Add(this.hScrollBar);
            this.Controls.Add(this.vScrollBar);
            this.Controls.Add(this.drawArea);
            this.Controls.Add(this.mapsComboBox);
            this.Name = "MainForm";
            this.Text = "Illutia Map Viewer";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox mapsComboBox;
        private System.Windows.Forms.Panel drawArea;
        private System.Windows.Forms.VScrollBar vScrollBar;
        private System.Windows.Forms.HScrollBar hScrollBar;
        private System.Windows.Forms.CheckBox blockedTilesCheckBox;
        private System.Windows.Forms.CheckBox showRoofsCheckBox;
    }
}

