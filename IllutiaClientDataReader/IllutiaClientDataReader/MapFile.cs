using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace IllutiaClientDataReader
{
    public class Layer
    {
        public int Sheet { get; set; }
        public int Graphic { get; set; }
    }

    public class Tile
    {
        public int Flags { get; set; }
        public Layer[] Layers { get; set; }

        public Tile()
        {
            this.Layers = new Layer[5];
        }

        public bool IsBlocked()
        {
            return ((this.Flags & 2) > 0);
        }
    }

    public class MapFile
    {
        public string FileName { get; set; }
        public short Version { get; set; }
        public short EditorVersion { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public Tile[] Tiles { get; set; }

        public MapFile(string path)
        {
            this.FileName = Path.GetFileName(path);

            using (BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open)))
            {
                this.Version = reader.ReadInt16();
                this.EditorVersion = reader.ReadInt16();
                this.Width = reader.ReadInt32();
                this.Height = reader.ReadInt32();

                this.Tiles = new Tile[this.Width * this.Height];
                
                for (int i = 0; i < this.Height; i++)
                {
                    for (int j = 0; j < this.Width; j++)
                    {
                        Tile tile = new Tile();
                        tile.Flags = reader.ReadInt32();

                        for (int k = 0; k < 5; k++)
                        {
                            Layer layer = new Layer();
                            layer.Graphic = reader.ReadInt32();
                            layer.Sheet = reader.ReadInt16();

                            tile.Layers[k] = layer;
                        }

                        //System.Diagnostics.Debug.WriteLine("{0} - {1}, {2}, {3}, {4}", this.Tiles.Length, j, i, this.Width, this.Height);
                        this.Tiles[i * this.Width + j] = tile;
                    }
                }
            }
        }

        public Tile this[int x, int y]
        {
            get { return this.Tiles[y * this.Width + x]; }
        }
    }
}
