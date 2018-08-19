using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ConsoleApplication1
{
    class MapReader
    {
        public static void Main(string[] args)
        {
            var r = new MapReader();

            foreach (var map in Directory.EnumerateFiles(@"C:\Users\Hayden\Desktop\Illutia_updated\maps", "*.map"))
            {
                r.Read(map);
            }
        }

        public void Read(string file)
        {
            BinaryReader reader = new BinaryReader(File.Open(file, FileMode.Open));

            short version = reader.ReadInt16();
            short editorVersion = reader.ReadInt16();
            int width = reader.ReadInt32();
            int height = reader.ReadInt32();
            List<Tuple<int, int>> blockedTiles = new List<Tuple<int, int>>();
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    int flags = reader.ReadInt32();

                    if ((flags & 2) > 0)
                    {
                        blockedTiles.Add(new Tuple<int, int>(j + 1, i + 1));
                    }

                    for (int k = 0; k < 5; k++)
                    {
                        long graphicId = reader.ReadInt32();
                        int sheetNum = reader.ReadInt16();

                    }
                }
            }
            reader.Close();

            string mapFile = Path.GetFileName(file);
            int mapNumber = Convert.ToInt32(Path.GetFileNameWithoutExtension(mapFile).Substring(3));
            using (StreamWriter writer = new StreamWriter("maps.sql", true))
            {
                writer.WriteLine("INSERT INTO maps (map_id, map_filename, map_name, map_x, map_y) VALUES ({0}, '{1}', '{2}', {3}, {4});",
                    mapNumber,
                    mapFile,
                    mapNumber,
                    width,
                    height);
            }

            using (StreamWriter writer = new StreamWriter("blockedtiles.sql", true))
            {
                foreach (var blocked in blockedTiles)
                {
                writer.WriteLine("INSERT INTO blockedtiles (map_id, map_x, map_y) VALUES ({0}, {1}, {2});",
                    mapNumber,
                    blocked.Item1,
                    blocked.Item2);
                }
            }
        }
    }
}
