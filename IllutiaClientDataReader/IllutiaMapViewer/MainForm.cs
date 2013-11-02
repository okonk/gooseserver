using IllutiaClientDataReader;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace IllutiaMapViewer
{
    public partial class MainForm : Form
    {
        private List<ADFFile> adfs = new List<ADFFile>();
        private CompiledEnc compiledEnc;
        private List<MapFile> maps = new List<MapFile>();

        Dictionary<int, Bitmap> graphicCache = new Dictionary<int, Bitmap>();

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            this.compiledEnc = new CompiledEnc(@"data\compiled.enc");

            foreach (string file in Directory.EnumerateFiles(@"data\", "*.adf").OrderBy(f => Convert.ToInt32(Path.GetFileNameWithoutExtension(f))))
            {
                adfs.Add(new ADFFile(file));
            }

            foreach (string file in Directory.EnumerateFiles(@"maps\", "*.map").OrderBy(p => Convert.ToInt32(Path.GetFileNameWithoutExtension(p).Substring(3))))
            {
                maps.Add(new MapFile(file));
            }

            this.mapsComboBox.DataSource = maps.ToList();
            this.mapsComboBox.ValueMember = "FileName";
            this.mapsComboBox.DisplayMember = "FileName";

            this.mapsComboBox.SelectedIndex = 0;

            this.drawArea.Invalidate();
        }

        private void Draw()
        {
            Graphics graphics = this.drawArea.CreateGraphics();
            graphics.Clear(Color.Black);

            System.Drawing.Imaging.ImageAttributes attr = new System.Drawing.Imaging.ImageAttributes();
            attr.SetColorKey(Color.Black, Color.Black);

            Image blocked = this.GetGraphic(13, 3002);

            int xOffset = Math.Max(this.hScrollBar.Value / 32, 0);
            int yOffset = Math.Max(this.vScrollBar.Value / 32, 0);

            var selectedMap = (MapFile)this.mapsComboBox.SelectedItem;
            int visibleX = Math.Min(this.drawArea.Width / 32 + 1, selectedMap.Width);
            int visibleY = Math.Min(this.drawArea.Height / 32 + 1, selectedMap.Height);

            for (int layerIndex = 0; layerIndex < 5; layerIndex++)
            {
                for (int y = yOffset; y < yOffset + visibleY; y++)
                {
                    for (int x = xOffset; x < xOffset + visibleX; x++)
                    {
                        Tile tile = selectedMap[x, y];
                        Layer layer = tile.Layers[layerIndex];

                        int drawX = (x * 32) - (xOffset * 32);
                        int drawY = (y * 32) - (yOffset * 32);

                        if (layer.Sheet != 0 && layer.Graphic != 0 && (layerIndex < 4 || (layerIndex == 4 && showRoofsCheckBox.Checked))) 
                        {
                            Image graphic = this.GetGraphic(layer.Sheet, layer.Graphic);

                            if (graphic.Width > 32)
                            {
                                drawX -= (graphic.Width / 2) - 16;
                            }
                            if (graphic.Height > 32)
                            {
                                drawY -= graphic.Height - 32;
                            }

                            Rectangle dstRect = new Rectangle(drawX, drawY, graphic.Width, graphic.Height);
                            graphics.DrawImage(graphic, dstRect, 0, 0, graphic.Width, graphic.Height, GraphicsUnit.Pixel, attr);
                        }

                        if (layerIndex == 4 && blockedTilesCheckBox.Checked && tile.IsBlocked())
                        {
                            Rectangle dstRect = new Rectangle(drawX, drawY, blocked.Width, blocked.Height);
                            graphics.DrawImage(blocked, dstRect, 0, 0, blocked.Width, blocked.Height, GraphicsUnit.Pixel, attr);
                        }
                    }
                }
            }
        }

        private void mapsComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedMap = (MapFile)this.mapsComboBox.SelectedItem;
            this.vScrollBar.Value = 0;
            this.hScrollBar.Value = 0;

            this.Resized(selectedMap);

            this.Draw();
        }

        private void Resized(MapFile selectedMap)
        {
            this.vScrollBar.Enabled = true;
            this.vScrollBar.SmallChange = 32;
            this.vScrollBar.LargeChange = 32;
            this.vScrollBar.Minimum = 0;
            this.vScrollBar.Maximum = selectedMap.Height * 32 - this.drawArea.Height + 32;
            if (this.vScrollBar.Maximum < 0)
            {
                this.vScrollBar.Maximum = 0;
                this.vScrollBar.Enabled = false;
            }

            this.hScrollBar.Enabled = true;
            this.hScrollBar.SmallChange = 32;
            this.hScrollBar.LargeChange = 32;
            this.hScrollBar.Minimum = 0;
            this.hScrollBar.Maximum = selectedMap.Width * 32 - this.drawArea.Width + 32;
            if (this.hScrollBar.Maximum < 0)
            {
                this.hScrollBar.Maximum = 0;
                this.hScrollBar.Enabled = false;
            }
        }

        private Image GetGraphic(int sheet, int graphic)
        {
            Bitmap graphicTile;
            if (this.graphicCache.TryGetValue(graphic, out graphicTile))
            {
                return graphicTile;
            }

            ADFFile file = this.adfs.First(a => a.FileNumber == sheet);
            Frame frame = file.Frames.FirstOrDefault(f => f.Index == graphic);

            if (frame == null)
            {
                return this.GetGraphic(12, 3001);
            }

            Bitmap sheetGraphic = (Bitmap)Bitmap.FromStream(new MemoryStream(file.FileData));
            graphicTile = sheetGraphic.Clone(new Rectangle(frame.X, frame.Y, frame.W, frame.H), sheetGraphic.PixelFormat);
            this.graphicCache[graphic] = graphicTile;

            return graphicTile;
        }

        private void blockedTilesCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            this.Draw();
        }

        private void vScrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            this.Draw();
        }

        private void hScrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            this.Draw();
        }

        private void drawArea_Resize(object sender, EventArgs e)
        {
            var selectedMap = (MapFile)this.mapsComboBox.SelectedItem;
            this.Resized(selectedMap);
            this.Draw();
        }

        private void drawArea_Paint(object sender, PaintEventArgs e)
        {
            this.Draw();
        }

        private void showRoofsCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            this.Draw();
        }
    }
}
