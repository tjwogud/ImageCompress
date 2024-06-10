using ImageFormat;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Diagnostics;

Console.Write("> ");
string dir = Console.ReadLine() ?? "";
string bmp = Path.Combine(dir, "image_bmp.bmp");
string png = Path.Combine(dir, "image_png.png");
string jpeg = Path.Combine(dir, "image_jpg.jpg");
string aci = Path.Combine(dir, "image_aci.aci");
Image<Rgba32>? image = null;
while (true)
{
    Console.Write("> ");
    string? line = Console.ReadLine();
    if (line == null)
        continue;
    string cmd = line.Split(' ')[0].ToLower();
    Stopwatch stopwatch = new();
    switch (cmd)
    {
        case "load":
            string file = line[(cmd.Length + 1)..];
            if (File.Exists(Path.Combine(dir, file)))
                file = Path.Combine(dir, file);
            else if (!File.Exists(file))
            {
                Console.WriteLine("파일이 존재하지 않습니다.");
                continue;
            }
            image = Image.Load<Rgba32>(file);
            Console.WriteLine("이미지가 로드되었습니다.");
            break;
        case "save":
            if (image == null)
            {
                Console.WriteLine("이미지가 로드되지 않았습니다.");
                continue;
            }
            image.SaveAsBmp(bmp);
            image.SaveAsPng(png);
            image.SaveAsJpeg(jpeg);
            stopwatch.Restart();
            ACI.SaveImage(image, aci);
            stopwatch.Stop();
            Console.WriteLine($"이미지가 저장되었습니다. ({stopwatch.ElapsedMilliseconds / 1000f}s)");
            break;
        case "check":
            if (image == null)
            {
                Console.WriteLine("이미지가 로드되지 않았습니다.");
                continue;
            }
            Image<Rgba32> restored = ACI.LoadImage(aci);
            restored.SaveAsBmp(Path.Combine(dir, "restored.bmp"));
            if (image.Width != restored.Width || image.Height != restored.Height)
            {
                Console.WriteLine("이미지가 다릅니다. (WH)");
                continue;
            }
            bool diff = false;
            image.ProcessPixelRows(restored, (origin, target) =>
            {
                for (int i = 0; i < origin.Height; i++)
                {
                    Span<Rgba32> originRow = origin.GetRowSpan(i);
                    Span<Rgba32> targetRow = target.GetRowSpan(i);
                    for (int j = 0; j < originRow.Length; j++)
                    {
                        if (originRow[j] != targetRow[j])
                        {
                            diff = true;
                            break;
                        }
                    }
                    if (diff)
                        break;
                }
            });
            if (diff)
                Console.WriteLine($"이미지가 다릅니다. (D)");
            else
            {
                Console.WriteLine("이미지가 동일합니다.");
                Console.WriteLine($"png  : {100 - (100f * new FileInfo(png).Length / new FileInfo(bmp).Length)}%");
                Console.WriteLine($"jpeg : {100 - (100f * new FileInfo(jpeg).Length / new FileInfo(bmp).Length)}%");
                Console.WriteLine($"aci  : {100 - (100f * new FileInfo(aci).Length / new FileInfo(bmp).Length)}%");
            }
            break;
        case "gc":
            GC.Collect();
            break;
    }
}