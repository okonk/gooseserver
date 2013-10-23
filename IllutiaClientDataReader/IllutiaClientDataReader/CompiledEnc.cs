using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace IllutiaClientDataReader
{
    public enum AnimationType
    {
        Body,
        Hair,
        Eyes,
        Chest,
        Helm,
        Legs,
        Feet,
        LeftHand,
        RightHand,
        Mount
    }

    public class CompiledAnimation
    {
        public AnimationType Type { get; private set; }
        public int Id { get; private set; }

        public CompiledAnimation(AnimationType type, int id)
        {
            this.Type = type;
            this.Id = id;
        }
    }

    public class CompiledEnc
    {
        public List<CompiledAnimation> CompiledAnimations { get; private set; }
        public Dictionary<int, CompiledAnimation> SheetToAnimation { get; private set; }

        public CompiledEnc(string file)
        {
            this.CompiledAnimations = new List<CompiledAnimation>();
            this.SheetToAnimation = new Dictionary<int, CompiledAnimation>();

            using (BinaryReader reader = new BinaryReader(File.Open(file, FileMode.Open)))
            {
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    AnimationType type = (AnimationType)Convert.ToInt32(reader.ReadInt16()) - 1;
                    int id = reader.ReadInt32();

                    var animation = new CompiledAnimation(type, id);

                    int length = 4;
                    // directions
                    for (int i = 0; i < length; i++)
                    {
                        for (int k = 0; k < 11; k++)
                        {
                            int frameIndex = reader.ReadInt32();
                        }
                    }
                    // files
                    for (int j = 0; j < 11; j++)
                    {
                        int fileNumber = reader.ReadInt32();
                        this.SheetToAnimation[fileNumber] = animation;
                    }
                }
            }
        }
    }
}
