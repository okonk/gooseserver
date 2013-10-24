using IllutiaClientDataReader;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace IllutiaDataViewer
{
    public partial class MainForm : Form
    {
        private List<ADFFile> adfs = new List<ADFFile>();
        private CompiledEnc compiledEnc;
        private Frame selectedFrame;

        public MainForm()
        {
            InitializeComponent();

            this.compiledEnc = new CompiledEnc(@"data\compiled.enc");

            foreach (string file in Directory.EnumerateFiles(@"data\", "*.adf").OrderBy(f => Convert.ToInt32(Path.GetFileNameWithoutExtension(f))))
            {
                adfs.Add(new ADFFile(file));
            }
            // for some reason 4589 is different, possibly skin info
            adfFileComboBox.DataSource = adfs.Where(a => a.Type == ADFType.Graphic && a.FileNumber != 4589).ToList();
            adfFileComboBox.ValueMember = "FileNumber";
            adfFileComboBox.DisplayMember = "FileNumber";

            filterComboBox.Items.AddRange(Enum.GetNames(typeof(Filters)));
            filterComboBox.SelectedIndex = 0;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            adfFileComboBox.SelectedItem = adfs[0];
            drawingPanel.Invalidate();
        }

        private void adfFileComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.selectedFrame = null;
            Draw();
        }

        private void Draw()
        {
            var selectedFile = ((ADFFile)adfFileComboBox.SelectedItem);

            if (compiledEnc.SheetToAnimation.ContainsKey(selectedFile.FileNumber))
            {
                this.animationInfoTextBox.Text = string.Format("{0}: {1}", 
                    compiledEnc.SheetToAnimation[selectedFile.FileNumber].Type, compiledEnc.SheetToAnimation[selectedFile.FileNumber].Id);
            }
            else
            {
                this.animationInfoTextBox.Text = "None";
            }

            var graphics = drawingPanel.CreateGraphics();
            var image = Image.FromStream(new MemoryStream(selectedFile.FileData));
            graphics.Clear(Color.Black);
            graphics.DrawImage(image, 0, 0);

            if (selectedFrame != null)
            {
                selectedIdTextBox.Text = selectedFrame.Index.ToString();
                graphics.DrawRectangle(Pens.Red, selectedFrame.X, selectedFrame.Y, selectedFrame.W, selectedFrame.H);

                if (selectedFile.Animations != null)
                {
                    Animation selectedAnimation = selectedFile.Animations.FirstOrDefault(a => a.Frames.Contains(selectedFrame));
                    if (selectedAnimation != null)
                    {
                        selectedAnimationTextBox.Text = selectedAnimation.Id.ToString();
                    }
                    else
                    {
                        selectedAnimationTextBox.Text = "None";
                    }
                }
                else
                {
                    selectedAnimationTextBox.Text = "None";
                }
            }
            else
            {
                selectedIdTextBox.Text = "None";
                selectedAnimationTextBox.Text = "None";
            }
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            Draw();
        }

        private void drawingPanel_MouseClick(object sender, MouseEventArgs e)
        {
            var selectedFile = ((ADFFile)adfFileComboBox.SelectedItem);
            Frame clickedFrame = selectedFile.Frames.FirstOrDefault(f => InRect(e.X, e.Y, f.X, f.Y, f.W, f.H));
            if (clickedFrame != null)
            {
                this.selectedFrame = clickedFrame;
            }

            Draw();
        }

        private bool InRect(int dx, int dy, int x, int y, int w, int h)
        {
            Rectangle rect = new Rectangle(x, y, w, h);
            return rect.Contains(dx, dy);
        }

        private void drawingPanel_Paint(object sender, PaintEventArgs e)
        {
            Draw();
        }

        private void DoFilters()
        {
            var selectedFilter = (Filters)Enum.Parse(typeof(Filters), (string)filterComboBox.SelectedItem);
            var filteredAdfs = this.adfs.Where(a => a.Type == ADFType.Graphic && a.FileNumber != 4589);

            switch (selectedFilter)
            {
                case Filters.All:
                    break;
                case Filters.Tiles:
                    filteredAdfs = filteredAdfs.Where(a => a.AnimationCount == 0 && !compiledEnc.SheetToAnimation.ContainsKey(a.FileNumber));
                    break;
                case Filters.Spells:
                    filteredAdfs = filteredAdfs.Where(a => a.AnimationCount > 0 && !compiledEnc.SheetToAnimation.ContainsKey(a.FileNumber));
                    break;

                default:
                    var equipType = (AnimationType)Enum.Parse(typeof(AnimationType), selectedFilter.ToString());
                    filteredAdfs = filteredAdfs.Where(a => compiledEnc.SheetToAnimation.ContainsKey(a.FileNumber) 
                        && compiledEnc.SheetToAnimation[a.FileNumber].Type == equipType);

                    break;
            }

            this.selectedFrame = null;
            adfFileComboBox.DataSource = filteredAdfs.ToList();
            adfFileComboBox.SelectedIndex = 0;

            Draw();
        }

        public enum Filters
        {
            All,
            Body,
            Hair,
            Eyes,
            Chest,
            Helm,
            Legs,
            Feet,
            Hand,
            //RightHand,
            //Mount,
            Tiles,
            Spells,
        }

        private void filterComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.DoFilters();
        }
    }
}
