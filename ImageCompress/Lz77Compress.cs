namespace ImageFormat
{
    public class Lz77Compress
    {
        public static byte[] Compress(byte[] data, ushort window = 32767, byte lookahead = 63)
        {
            Span<ushort> input = new ushort[(int)Math.Ceiling((double)data.Length / 2)];
            BinaryReader reader = new(new MemoryStream(data));
            int index = 0;
            while (reader.BaseStream.Length >= reader.BaseStream.Position + 2)
            {
                input[index] = reader.ReadUInt16();
                index++;
            }
            if (reader.BaseStream.Length > reader.BaseStream.Position)
                input[^1] = BitConverter.ToUInt16([data[^1], 0]);

            MemoryStream stream = new();
            BinaryWriter writer = new(stream);

            void WriteLLD(int dist, int length, ushort c)
            {
                writer.Write((ushort)dist);
                writer.Write(c);
                if (dist != 0)
                    writer.Write((byte)length);
            }

            writer.Write(input.Length);
            WriteLLD(0, 0, input[0]);
            for (int i = 1; i < input.Length; i++)
            {
                ReadOnlySpan<ushort> windowBuff = input[Math.Max(0, i - window)..i];
                ReadOnlySpan<ushort> lookaheadBuff = input[i..Math.Min(input.Length, i + lookahead)];
                var result = KmpSearch.SearchLongest(windowBuff, lookaheadBuff);
                if (result.index == -1)
                    WriteLLD(0, 0, input[i]);
                else
                {
                    int len = Math.Min(lookaheadBuff.Length - 1, result.length);
                    WriteLLD(windowBuff.Length - result.index, len, lookaheadBuff[len]);
                    i += len;
                }
            }
            return stream.ToArray();
        }

        public static byte[] Decompress(byte[] data)
        {
            BinaryReader reader = new(new MemoryStream(data));

            int size = reader.ReadInt32();
            ushort[] chars = new ushort[size];
            int index = 0;

            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                ushort dist = reader.ReadUInt16();
                ushort c = reader.ReadUInt16();
                if (dist != 0)
                {
                    byte length = reader.ReadByte();
                    Array.Copy(chars, index - dist, chars, index, length);
                    index += length;
                }
                chars[index] = c;
                index++;
            }

            MemoryStream stream = new();
            BinaryWriter writer = new(stream);
            foreach (ushort c in chars)
                writer.Write(c);

            return stream.ToArray();
        }
    }
}
