namespace ImageFormat
{
    public class Lz77Compress
    {
        public struct LLD(int dist, int len, char c)
        {
            public int dist = dist, len = len;
            public char c = c;
        }

        public static byte[] Compress(byte[] data, ushort window = 32767, byte lookahead = 63)
        {
            Span<int> input = new int[(int)Math.Ceiling((double)data.Length / 4)];
            BinaryReader reader = new(new MemoryStream(data));
            int index = 0;
            while (reader.BaseStream.Length >= reader.BaseStream.Position + 4)
            {
                input[index] = reader.ReadInt32();
                index++;
            }
            if (reader.BaseStream.Length > reader.BaseStream.Position)
                input[^1] = BitConverter.ToInt32([data[^1], 0, 0, 0]);

            MemoryStream stream = new();
            BinaryWriter writer = new(stream);

            void WriteLLD(int dist, int length, int c)
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
                ReadOnlySpan<int> windowBuff = input[Math.Max(0, i - window)..i];
                ReadOnlySpan<int> lookaheadBuff = input[i..Math.Min(input.Length, i + lookahead)];
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
            int[] chars = new int[size];
            int index = 0;

            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                ushort dist = reader.ReadUInt16();
                int c = reader.ReadInt32();
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
            foreach (int c in chars)
                writer.Write(c);

            return stream.ToArray();
        }
    }
}
