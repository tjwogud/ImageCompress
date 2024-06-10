using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Runtime.InteropServices;
using System.Text;

namespace ImageFormat
{
    public class ACI
    {
        public static readonly byte[] header = Encoding.Unicode.GetBytes("thisiswasansimage");

        public static byte[] SaveImage(Image<Rgba32> image)
        {
            MemoryStream buffer = new();
            BinaryWriter writer = new(buffer, Encoding.Unicode);
            writer.Write(header);
            writer.Write(image.Width);
            writer.Write(image.Height);
            uint[] rgbas = new uint[image.Width * image.Height];
            image.ProcessPixelRows(rows =>
            {
                for (int i = 0; i < rows.Height; i++) {
                    Span<Rgba32> span = rows.GetRowSpan(i);
                    for (int j = 0; j < span.Length; j++)
                        rgbas[i * rows.Width + j] = span[j].Rgba;
                }
            });
            byte[] data = MemoryMarshal.Cast<uint, byte>(rgbas).ToArray();
            byte[] compressed = Lz77Compress.Compress(data);
            writer.Write(compressed.Length);
            writer.Write(compressed);
            return buffer.ToArray();
        }

        public static void SaveImage(Image<Rgba32> image, string path)
        {
            File.WriteAllBytes(path, SaveImage(image));
        }

        public static Image<Rgba32> LoadImage(string file)
        {
            using FileStream fs = new(file, FileMode.Open);
            using BufferedStream bs = new(fs);
            using BinaryReader reader = new(bs, Encoding.Unicode);
            byte[] header = reader.ReadBytes(ACI.header.Length);
            if (!Enumerable.SequenceEqual(header, ACI.header))
                throw new ArgumentException("file is not wasans image");
            int width = reader.ReadInt32();
            int height = reader.ReadInt32();
            int size = reader.ReadInt32();
            byte[] compressed = reader.ReadBytes(size);
            byte[] data = Lz77Compress.Decompress(compressed);
            uint[] rgbas = MemoryMarshal.Cast<byte, uint>(data).ToArray();
            Image<Rgba32> image = new(width, height);
            image.ProcessPixelRows(rows =>
            {
                for (int i = 0; i < rows.Height; i++)
                {
                    Span<Rgba32> span = rows.GetRowSpan(i);
                    for (int j = 0; j < span.Length; j++)
                        span[j] = new Rgba32(rgbas[i * width + j]);
                }
            });
            return image;
        }
    }
}
