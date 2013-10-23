using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace IllutiaClientDataReader
{
    public enum ADFType
    {
        Graphic = 1,
        Sound = 2,
    }

    public class Animation
    {
        public int Id { get; private set; }
        public List<Frame> Frames { get; private set; }

        public Animation(int id)
        {
            this.Id = id;
            this.Frames = new List<Frame>();
        }
    }

    public class Frame
    {
        public int Index { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }
        public int W { get; private set; }
        public int H { get; private set; }

        public Frame(int index, int x, int y, int w, int h)
        {
            this.Index = index;
            this.X = x;
            this.Y = y;
            this.W = w;
            this.H = h;
        }
    }

    public class ADFFile
    {
        public int FileNumber { get; private set; }
        public ADFType Type { get; private set; }
        public byte Version { get; private set; }
        public byte Offset { get; private set; }
        public int FirstFrameIndex { get; private set; }
        public int EndFrameIndex { get; private set; }
        public int FrameCount { get; private set; }
        public int FirstAnimationIndex { get; private set; }
        public int EndAnimationIndex { get; private set; }
        public int AnimationCount { get; private set; }
        public List<Frame> Frames { get; private set; }
        public List<Animation> Animations { get; set; }
        public byte[] FileData { get; private set; }

        public ADFFile(string file)
        {
            using (var reader = new BinaryReader(File.Open(file, FileMode.Open)))
            {
                this.Type = (ADFType)Convert.ToInt32(reader.ReadByte());
                this.Version = reader.ReadByte();

                // not sure what these are.. just eat them
                int extraBytes = reader.ReadInt32();
                for (int i = 0; i < extraBytes; i++) reader.ReadByte();

                this.Offset = reader.ReadByte();

                this.FirstFrameIndex = this.ApplyOffset(reader.ReadInt32());
                this.FrameCount = this.ApplyOffset(reader.ReadInt32());
                this.EndFrameIndex = (this.FirstFrameIndex + this.FrameCount) - 1;
                this.FirstAnimationIndex = this.ApplyOffset(reader.ReadInt32());
                this.AnimationCount = this.ApplyOffset(reader.ReadInt32());
                this.EndAnimationIndex = (this.FirstAnimationIndex + this.AnimationCount) - 1;

                this.Frames = new List<Frame>();
                for (int i = this.FirstFrameIndex; i <= this.EndFrameIndex; i++)
                {
                    this.Frames.Add(new Frame(i,
                        this.ApplyOffset(reader.ReadInt32()),
                        this.ApplyOffset(reader.ReadInt32()),
                        this.ApplyOffset(reader.ReadInt32()),
                        this.ApplyOffset(reader.ReadInt32())));
                }

                if (this.AnimationCount > 0)
                {
                    this.Animations = new List<Animation>();
                    for (int i = this.FirstAnimationIndex; i <= this.EndAnimationIndex; i++)
                    {
                        var animation = new Animation(i);
                        int frameCount = this.ApplyOffsetByte(reader.ReadByte());
                        for (int j = 0; j < frameCount; j++)
                        {
                            int frameIndex = this.ApplyOffset(reader.ReadInt32());

                            var frame = this.Frames[frameIndex - this.FirstFrameIndex];
                            animation.Frames.Add(frame);
                        }

                        this.Animations.Add(animation);
                    }
                }

                int headerSize = this.ApplyOffset(reader.ReadInt32());
                int length = (int)(reader.BaseStream.Length - reader.BaseStream.Position);

                byte[] buffer = reader.ReadBytes(length);
                byte[] data = new byte[this.RealSize(buffer.Length, 0x315)];
                for (int k = 0; k < buffer.Length; k++)
                {
                    if (k - (k / 790) >= data.Length) continue;

                    data[k - (k / 790)] = this.ApplyOffsetByte(buffer[k]);
                }

                this.FileData = data;
                this.FileNumber = Convert.ToInt32(Path.GetFileNameWithoutExtension(file));
            }
        }

        public int ApplyOffset(int data)
        {
            return (data - this.Offset);
        }

        public byte ApplyOffsetByte(byte data)
        {
            if (this.Offset > data)
            {
                data = (byte)(data + (0x100 - this.Offset));
                return data;
            }
            data = (byte)(data - this.Offset);
            return data;
        }

        public int RealSize(int datasize, int chunksize)
        {
            return (datasize - (datasize / (chunksize + 1)));
        }
    }
}
