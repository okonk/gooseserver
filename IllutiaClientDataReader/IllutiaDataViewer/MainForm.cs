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
using System.Drawing.Imaging;

namespace IllutiaDataViewer
{
    public partial class MainForm : Form
    {
        private List<ADFFile> adfs = new List<ADFFile>();
        private CompiledEnc compiledEnc;
        private Frame selectedFrame;
        private int nextAdfNumber = 0;
        private int nextGraphicId = 0;
        private int nextAnimationId = 0;

        public MainForm()
        {
            InitializeComponent();

            this.compiledEnc = new CompiledEnc(@"data\compiled.enc");

            foreach (string file in Directory.EnumerateFiles(@"data\", "*.adf").OrderBy(f => Convert.ToInt32(Path.GetFileNameWithoutExtension(f))))
            {
                var adf = new ADFFile(file);
                adfs.Add(adf);

                if (adf.FileNumber >= nextAdfNumber)
                    nextAdfNumber = adf.FileNumber + 1;

                foreach (var frame in adf.Frames)
                {
                    if (frame.Index >= nextGraphicId)
                        nextGraphicId = frame.Index + 1;
                }

                if (adf.Animations != null)
                {
                    foreach (var animation in adf.Animations)
                    {
                        if (animation.Id >= nextAnimationId)
                            nextAnimationId = animation.Id + 1;
                    }
                }
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

        private void importAnimatedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "ADF files (*.adf)|*.adf";
                openFileDialog.Multiselect = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    var importedAdfs = new List<ADFFile>();

                    foreach (var filePath in openFileDialog.FileNames)
                    {
                        var importedAdf = new ADFFile(filePath);

                        if (openFileDialog.FileNames.Length == 11 && importedAdfs.Count == 10)
                        {
                            if (importedAdf.Frames.Count == 16)
                            {
                                var oldFrames = importedAdf.Frames.ToList();
                                importedAdf.Frames.Clear();

                                for (int i = 0; i < oldFrames.Count; i++)
                                {
                                    importedAdf.Frames.Add(oldFrames[i]);

                                    if ((i + 1) % 4 == 0)
                                    {
                                        var firstFrame = oldFrames[i - 3];
                                        var newFrame = new Frame(0, firstFrame.W * 4, firstFrame.Y, firstFrame.W, firstFrame.H);

                                        importedAdf.Frames.Add(newFrame);

                                        importedAdf.Animations[i / 4].Frames.Add(newFrame);
                                    }
                                }
                            }
                            else
                            {
                                for (int i = 4; i < importedAdf.Frames.Count; i += 5)
                                {
                                    var firstFrame = importedAdf.Frames[i - 4];
                                    var thisFrame = importedAdf.Frames[i];
                                    thisFrame.X = firstFrame.W * 4;
                                }
                            }

                            var lastFrame = importedAdf.Frames.Last();
                            var originalImage = (Bitmap)Bitmap.FromStream(new MemoryStream(importedAdf.FileData));
                            var newImage = new Bitmap(lastFrame.X + lastFrame.W, lastFrame.Y + lastFrame.H, PixelFormat.Format8bppIndexed);
                            newImage.Palette = originalImage.Palette;

                            var originalImageData = originalImage.LockBits(new Rectangle(0, 0, originalImage.Width, originalImage.Height), ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
                            var newImageData = newImage.LockBits(new Rectangle(0, 0, newImage.Width, newImage.Height), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

                            unsafe
                            {
                                byte* originalPtr = (byte*)originalImageData.Scan0.ToPointer();
                                byte* newPtr = (byte*)newImageData.Scan0.ToPointer();

                                for (int y = 0; y < newImage.Height; y++)
                                {
                                    for (int x = 0; x < newImage.Width; x++)
                                    {
                                        byte* originalPixel;

                                        if (x > originalImage.Width)
                                            originalPixel = originalPtr + y * originalImageData.Stride - lastFrame.W * 2 + x;
                                        else
                                            originalPixel = originalPtr + y * originalImageData.Stride + x;

                                        byte* newPixel = newPtr + y * newImageData.Stride + x;

                                        *newPixel = *originalPixel;
                                    }
                                }
                            }

                            originalImage.UnlockBits(originalImageData);
                            newImage.UnlockBits(newImageData);

                            using (var ms = new MemoryStream())
                            {
                                newImage.Save(ms, ImageFormat.Gif);
                                importedAdf.FileData = ms.ToArray();
                            }
                        }

                        importedAdf.FirstFrameIndex = nextGraphicId;
                        importedAdf.FrameCount = importedAdf.Frames.Count;
                        importedAdf.EndFrameIndex = (importedAdf.FirstFrameIndex + importedAdf.Frames.Count) - 1;
                        importedAdf.FirstAnimationIndex = importedAdf.Animations != null ? nextAnimationId : 0;
                        importedAdf.AnimationCount = importedAdf.Animations != null ? importedAdf.Animations.Count : 0;
                        importedAdf.EndAnimationIndex = (importedAdf.FirstAnimationIndex + importedAdf.AnimationCount) - 1;

                        foreach (var frame in importedAdf.Frames)
                        {
                            frame.Index = nextGraphicId;
                            nextGraphicId++;
                        }

                        if (importedAdf.Animations != null)
                        {
                            foreach (var animation in importedAdf.Animations)
                            {
                                animation.Id = nextAnimationId;
                                nextAnimationId++;
                            }
                        }

                        importedAdf.FileNumber = nextAdfNumber;
                        nextAdfNumber++;

                        importedAdfs.Add(importedAdf);

                        importedAdf.WriteToFile("data/" + importedAdf.FileNumber + ".adf");
                    }

                    AnimationType type;
                    if (Enum.TryParse((string)filterComboBox.SelectedItem, out type))
                    {
                        int nextTypeId = compiledEnc.CompiledAnimations.Where(c => c.Type == type).Max(c => c.Id) + 1;
                        var compiledAnimation = new CompiledAnimation(type, nextTypeId);

                        for (int i = 0; i < importedAdfs.Count; i++)
                        {
                            ADFFile importedAdf = importedAdfs[i];

                            for (int j = 0; j < importedAdf.Animations.Count; j++)
                            {
                                compiledAnimation.AnimationIndexes[j * 11 + i] = importedAdf.Animations[j].Id;
                            }

                            compiledEnc.SheetToAnimation[importedAdf.FileNumber] = compiledAnimation;
                            compiledAnimation.AnimationFiles[i] = importedAdf.FileNumber;
                        }

                        compiledEnc.CompiledAnimations.Add(compiledAnimation);

                        compiledEnc.WriteToFile("data/compiled.enc");
                    }

                    adfs.AddRange(importedAdfs);
                    adfFileComboBox.DataSource = adfs.Where(a => a.Type == ADFType.Graphic && a.FileNumber != 4589).ToList();
                }
            }
        }
    }
}
