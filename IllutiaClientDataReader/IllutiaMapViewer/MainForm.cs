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

            this.Draw();
        }

        private void Draw()
        {
            Graphics graphics = this.drawArea.CreateGraphics();
            graphics.Clear(Color.Black);

            System.Drawing.Imaging.ImageAttributes attr = new System.Drawing.Imaging.ImageAttributes();
            attr.SetColorKey(Color.Black, Color.Black);

            Image blocked = this.GetGraphic(13, 3002);

            int xOffset = 0; // todo get from scrollbar
            int yOffset = 0; // todo: get from scrollbar

            var selectedMap = (MapFile)this.mapsComboBox.SelectedItem;
            int visibleX = this.drawArea.Width / 32 + 1;
            int visibleY = this.drawArea.Height / 32 + 1;

            for (int layerIndex = 0; layerIndex < 5; layerIndex++)
            {
                for (int y = yOffset; y < visibleY; y++)
                {
                    for (int x = xOffset; x < visibleX; x++)
                    {
                        Tile tile = selectedMap[x, y];
                        Layer layer = tile.Layers[layerIndex];

                        if (layer.Sheet != 0 && layer.Graphic != 0) 
                        {
                            Image graphic = this.GetGraphic(layer.Sheet, layer.Graphic);
                            int drawX = x * 32;
                            int drawY = y * 32;

                            if (graphic.Width > 32)
                            {
                                drawX = x * 32 - (graphic.Width / 2) + 16;
                            }
                            if (graphic.Height > 32)
                            {
                                drawY = y * 32 - (graphic.Height) + 32;
                            }

                            Rectangle dstRect = new Rectangle(drawX, drawY, graphic.Width, graphic.Height);
                            graphics.DrawImage(graphic, dstRect, 0, 0, graphic.Width, graphic.Height, GraphicsUnit.Pixel, attr);
                        }

                        if (layerIndex == 4 && tile.IsBlocked())
                        {
                            Rectangle dstRect = new Rectangle(x * 32, y * 32, blocked.Width, blocked.Height);
                            graphics.DrawImage(blocked, dstRect, 0, 0, blocked.Width, blocked.Height, GraphicsUnit.Pixel, attr);
                        }
                    }
                }
            }
        }

        private void mapsComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.Draw();
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
    }
}
