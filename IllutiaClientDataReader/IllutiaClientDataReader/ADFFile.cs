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
        public int Id { get; set; }
        public List<Frame> Frames { get; set; }

        public Animation(int id)
        {
            this.Id = id;
            this.Frames = new List<Frame>();
        }
    }

    public class Frame
    {
        public int Index { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int W { get; set; }
        public int H { get; set; }

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
        public int FileNumber { get; set; }
        public ADFType Type { get; set; }
        public byte Version { get; set; }
        public byte Offset { get; set; }
        public int FirstFrameIndex { get; set; }
        public int EndFrameIndex { get; set; }
        public int FrameCount { get; set; }
        public int FirstAnimationIndex { get; set; }
        public int EndAnimationIndex { get; set; }
        public int AnimationCount { get; set; }
        public List<Frame> Frames { get; set; }
        public List<Animation> Animations { get; set; }
        public byte[] FileData { get; set; }
        public byte[] ExtraBytes { get; set; }

        public ADFFile(string file)
        {
            using (var reader = new BinaryReader(File.Open(file, FileMode.Open)))
            {
                this.Type = (ADFType)Convert.ToInt32(reader.ReadByte());
                this.Version = reader.ReadByte();

                // not sure what these are.. just eat them
                int extraBytes = reader.ReadInt32();
                this.ExtraBytes = new byte[extraBytes];
                for (int i = 0; i < extraBytes; i++)
                    this.ExtraBytes[i] = reader.ReadByte();

                this.Offset = reader.ReadByte();

                this.FirstFrameIndex = this.Decode(reader.ReadInt32());
                this.FrameCount = this.Decode(reader.ReadInt32());
                this.EndFrameIndex = (this.FirstFrameIndex + this.FrameCount) - 1;
                this.FirstAnimationIndex = this.Decode(reader.ReadInt32());
                this.AnimationCount = this.Decode(reader.ReadInt32());
                this.EndAnimationIndex = (this.FirstAnimationIndex + this.AnimationCount) - 1;

                this.Frames = new List<Frame>();
                for (int i = this.FirstFrameIndex; i <= this.EndFrameIndex; i++)
                {
                    this.Frames.Add(new Frame(i,
                        this.Decode(reader.ReadInt32()),
                        this.Decode(reader.ReadInt32()),
                        this.Decode(reader.ReadInt32()),
                        this.Decode(reader.ReadInt32())));
                }

                if (this.AnimationCount > 0)
                {
                    this.Animations = new List<Animation>();
                    for (int i = this.FirstAnimationIndex; i <= this.EndAnimationIndex; i++)
                    {
                        var animation = new Animation(i);
                        int frameCount = this.DecodeByte(reader.ReadByte());
                        for (int j = 0; j < frameCount; j++)
                        {
                            int frameIndex = this.Decode(reader.ReadInt32());

                            var frame = this.Frames[frameIndex - this.FirstFrameIndex];
                            animation.Frames.Add(frame);
                        }

                        this.Animations.Add(animation);
                    }
                }

                int headerSize = this.Decode(reader.ReadInt32());
                int length = (int)(reader.BaseStream.Length - reader.BaseStream.Position);

                byte[] buffer = reader.ReadBytes(length);
                byte[] data = new byte[this.RealSize(buffer.Length)];
                for (int k = 0; k < buffer.Length; k++)
                {
                    if (k - (k / 790) >= data.Length) continue;

                    data[k - (k / 790)] = this.DecodeByte(buffer[k]);
                }

                this.FileData = data;
                this.FileNumber = Convert.ToInt32(Path.GetFileNameWithoutExtension(file));
            }
        }

        public int Decode(int data)
        {
            return (data - this.Offset);
        }

        public byte DecodeByte(byte data)
        {
            if (this.Offset > data)
            {
                data = (byte)(data + (0x100 - this.Offset));
                return data;
            }
            data = (byte)(data - this.Offset);
            return data;
        }

        public int RealSize(int datasize)
        {
            return (datasize - (datasize / 790));
        }

        public int Encode(int data)
        {
            return (data + this.Offset);
        }

        public byte EncodeByte(byte data)
        {
            if ((0x100 - data) < this.Offset)
            {
                data = (byte)(this.Offset - (0x100 - data));
                return data;
            }

            data = (byte)(data + this.Offset);
            return data;
        }

        public void WriteToFile(string filename)
        {
            using (var writer = new BinaryWriter(File.Open(filename, FileMode.Create)))
            {
                writer.Write((byte)this.Type);
                writer.Write(this.Version);

                writer.Write(this.ExtraBytes.Length);
                writer.Write(this.ExtraBytes);

                writer.Write(this.Offset);

                writer.Write(Encode(this.FirstFrameIndex));
                writer.Write(Encode(this.FrameCount));
                writer.Write(Encode(this.FirstAnimationIndex));
                writer.Write(Encode(this.Animations == null ? 0 : this.Animations.Count));

                foreach (var frame in this.Frames)
                {
                    writer.Write(Encode(frame.X));
                    writer.Write(Encode(frame.Y));
                    writer.Write(Encode(frame.W));
                    writer.Write(Encode(frame.H));
                }

                if (this.Animations != null && this.Animations.Count > 0)
                {
                    foreach (var animation in this.Animations)
                    {
                        writer.Write(EncodeByte((byte)animation.Frames.Count));

                        foreach (var frame in animation.Frames)
                        {
                            writer.Write(Encode(frame.Index));
                        }
                    }
                }

                writer.Write(this.Encode((int)writer.BaseStream.Position + 4));

                for (int i = 0, j = 0; i < this.FileData.Length; i++)
                {
                    writer.Write(EncodeByte(this.FileData[i]));

                    j++;
                    if (j == 789)
                    {
                        writer.Write((byte)0);
                        j = 0;
                    }
                }
            }
        }
    }
}
