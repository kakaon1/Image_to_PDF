using iText.IO.Image;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;
using SysPath = System.IO.Path;

class Program
{
    const float A4W = 595.28f;
    const float A4H = 841.89f;

    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        string exeDir   = SysPath.GetDirectoryName(Environment.ProcessPath ?? AppContext.BaseDirectory)
                          ?? AppContext.BaseDirectory;
        string mergeDir = SysPath.Combine(exeDir, "merge");

        Console.WriteLine("=== Image_to_PDF ===");

        // merge 폴더가 없으면 생성 후 종료
        if (!Directory.Exists(mergeDir))
        {
            Directory.CreateDirectory(mergeDir);
            Console.WriteLine($"merge 폴더를 생성했습니다: {mergeDir}");
            Console.WriteLine("변환할 이미지를 merge 폴더에 넣고 다시 실행하세요.");
            Pause();
            return;
        }

        // 전체(루트 + 하위폴더) 이미지 존재 여부 확인
        if (GetImageFiles(mergeDir, SearchOption.AllDirectories).Count == 0)
        {
            Console.WriteLine($"merge 폴더에 이미지가 없습니다: {mergeDir}");
            Console.WriteLine("변환할 JPG / JPEG / PNG 파일을 넣고 다시 실행하세요.");
            Pause();
            return;
        }

        // 루트 이미지 → output.pdf
        var rootFiles = GetImageFiles(mergeDir, SearchOption.TopDirectoryOnly);
        if (rootFiles.Count > 0)
            ConvertToPdf(rootFiles, SysPath.Combine(exeDir, "output.pdf"));

        // 직하위 폴더별 → {폴더명}.pdf
        foreach (var subDir in Directory.GetDirectories(mergeDir, "*", SearchOption.TopDirectoryOnly))
        {
            var subFiles = GetImageFiles(subDir, SearchOption.AllDirectories);
            if (subFiles.Count == 0) continue;

            string pdfName = SysPath.GetFileName(subDir) + ".pdf";
            ConvertToPdf(subFiles, SysPath.Combine(exeDir, pdfName));
        }

        Pause();
    }

    // ---------------------------------------------------------------

    static void ConvertToPdf(List<string> files, string outputFile)
    {
        Console.WriteLine($"\n변환 시작 — {files.Count}개 파일 → {SysPath.GetFileName(outputFile)}");

        try
        {
            using var writer = new PdfWriter(outputFile);
            using var pdf    = new PdfDocument(writer);

            foreach (var file in files)
            {
                try
                {
                    var (imageData, pixelW, pixelH) = LoadImage(file);

                    bool  landscape = pixelW > pixelH;
                    float pageW = landscape ? A4H : A4W;
                    float pageH = landscape ? A4W : A4H;

                    float scale = Math.Min(pageW / pixelW, pageH / pixelH);
                    float imgW  = pixelW * scale;
                    float imgH  = pixelH * scale;
                    float x     = (pageW - imgW) / 2f;
                    float y     = (pageH - imgH) / 2f;

                    var page   = pdf.AddNewPage(new PageSize(pageW, pageH));
                    var canvas = new PdfCanvas(page);
                    canvas.AddImageWithTransformationMatrix(imageData, imgW, 0f, 0f, imgH, x, y);

                    Console.WriteLine($"  추가: {SysPath.GetFileName(file)}  [{pixelW}×{pixelH}px]");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  건너뜀: {SysPath.GetFileName(file)}  ({ex.Message})");
                }
            }

            pdf.Close();
            Console.WriteLine($"완료: {outputFile}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"오류: {ex.Message}");
        }
    }

    // ---------------------------------------------------------------

    static (ImageData Data, int PixelW, int PixelH) LoadImage(string file)
    {
        string ext   = SysPath.GetExtension(file).ToLowerInvariant();
        bool   isJpg = ext is ".jpg" or ".jpeg";
        Exception? err1 = null;

        // JPEG: raw 바이트 그대로 전달 (화질 손실 없음)
        if (isJpg)
        {
            try
            {
                var info  = SixLabors.ImageSharp.Image.Identify(file);
                var bytes = File.ReadAllBytes(file);
                return (ImageDataFactory.Create(bytes), info.Width, info.Height);
            }
            catch (Exception ex) { err1 = ex; }
        }

        // PNG / JPEG 폴백: ImageSharp → 흰 배경 합성 → RGB PNG
        try
        {
            using var img = SixLabors.ImageSharp.Image.Load<Rgba32>(file);
            using var ms  = new MemoryStream();
            using var rgb = CompositeOnWhite(img);
            rgb.Save(ms, new PngEncoder());
            return (ImageDataFactory.Create(ms.ToArray()), img.Width, img.Height);
        }
        catch (Exception ex) { err1 ??= ex; }

        // 최종 폴백: System.Drawing
        try
        {
            using var bmp        = new Bitmap(file);
            using var normalized = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format32bppRgb);
            using (var g = Graphics.FromImage(normalized))
            {
                g.Clear(System.Drawing.Color.White);
                g.DrawImage(bmp, 0, 0, bmp.Width, bmp.Height);
            }
            using var ms = new MemoryStream();
            normalized.Save(ms, ImageFormat.Png);
            return (ImageDataFactory.Create(ms.ToArray()), bmp.Width, bmp.Height);
        }
        catch (Exception ex)
        {
            throw new Exception($"로드 실패 (1차: {err1?.Message} / 폴백: {ex.Message})");
        }
    }

    static Image<Rgb24> CompositeOnWhite(Image<Rgba32> src)
    {
        var result = new Image<Rgb24>(src.Width, src.Height);
        src.ProcessPixelRows(result, (srcAcc, dstAcc) =>
        {
            for (int row = 0; row < srcAcc.Height; row++)
            {
                var s = srcAcc.GetRowSpan(row);
                var d = dstAcc.GetRowSpan(row);
                for (int col = 0; col < s.Length; col++)
                {
                    float a = s[col].A / 255f;
                    d[col] = new Rgb24(
                        (byte)(s[col].R * a + 255 * (1 - a) + 0.5f),
                        (byte)(s[col].G * a + 255 * (1 - a) + 0.5f),
                        (byte)(s[col].B * a + 255 * (1 - a) + 0.5f)
                    );
                }
            }
        });
        return result;
    }

    static List<string> GetImageFiles(string dir, SearchOption option) =>
        Directory.GetFiles(dir, "*.jpg",  option)
            .Concat(Directory.GetFiles(dir, "*.jpeg", option))
            .Concat(Directory.GetFiles(dir, "*.png",  option))
            .OrderBy(f => ExtractTrailingNumber(SysPath.GetFileNameWithoutExtension(f)))
            .ThenBy(f  => SysPath.GetFileName(f))
            .ToList();

    static long ExtractTrailingNumber(string name)
    {
        var match = Regex.Match(name, @"(\d+)\D*$");
        if (!match.Success) return long.MaxValue;
        return long.TryParse(match.Groups[1].Value, out long n) ? n : long.MaxValue;
    }

    static void Pause()
    {
        Console.WriteLine("\n2초 후 자동 종료...");
        Thread.Sleep(2000);
    }
}
