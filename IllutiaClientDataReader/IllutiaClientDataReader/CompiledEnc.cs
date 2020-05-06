using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AsperetaClient
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
        Hand
    }

    public class CompiledAnimation
    {
        public AnimationType Type { get; set; }
        public int Id { get; set; }

        public int[] AnimationIndexes { get; private set; }

        public int[] AnimationFiles { get; private set; }

        public CompiledAnimation(AnimationType type, int id)
        {
            this.Type = type;
            this.Id = id;
            this.AnimationIndexes = new int[4 * 3];
            this.AnimationFiles = new int[3];
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
                        for (int k = 0; k < 3; k++)
                        {
                            animation.AnimationIndexes[i * 3 + k] = reader.ReadInt32();
                        }
                    }
                    // files
                    for (int j = 0; j < 3; j++)
                    {
                        int fileNumber = reader.ReadInt32();
                        this.SheetToAnimation[fileNumber] = animation;
                        animation.AnimationFiles[j] = fileNumber;
                    }

                    this.CompiledAnimations.Add(animation);
                }
            }
        }

        public void WriteToFile(string filename)
        {
            using (var writer = new BinaryWriter(File.Open(filename, FileMode.Create)))
            {
                foreach (var animation in this.CompiledAnimations)
                {
                    writer.Write((short)(animation.Type+1));
                    writer.Write(animation.Id);

                    foreach (var animationIndex in animation.AnimationIndexes)
                        writer.Write(animationIndex);

                    foreach (var animationFile in animation.AnimationFiles)
                        writer.Write(animationFile);
                }
            }
        }
    }
}
