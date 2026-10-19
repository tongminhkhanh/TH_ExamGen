using System;
using System.IO;
using System.Text.RegularExpressions;
using CSharpMath.SkiaSharp;
using SkiaSharp;

namespace TH_ExamGen.Services
{
    public class MathFractionRenderService
    {
        private static readonly Regex LatexMathRegex = new Regex(@"\\frac\{([^{}]+)\}\{([^{}]+)\}", RegexOptions.Compiled);

        public static byte[] RenderLatexToPng(string latex, float fontSize = 22f, SKColor? textColor = null)
        {
            try
            {
                var painter = new MathPainter
                {
                    LaTeX = latex,
                    FontSize = fontSize,
                    TextColor = textColor ?? SKColors.Black
                };

                using var stream = painter.DrawAsStream(format: SKEncodedImageFormat.Png);
                if (stream is MemoryStream ms)
                {
                    return ms.ToArray();
                }

                using var memoryStream = new MemoryStream();
                stream?.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                AppLogger.Warn($"Lỗi render LaTeX '{latex}': {ex.Message}");
                return Array.Empty<byte>();
            }
        }

        public static bool ContainsFractions(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            return LatexMathRegex.IsMatch(text);
        }

        public static MatchCollection GetFractionMatches(string text)
        {
            return LatexMathRegex.Matches(text);
        }
    }
}
