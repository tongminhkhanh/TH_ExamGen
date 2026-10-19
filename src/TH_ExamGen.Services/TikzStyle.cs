using System.Globalization;

namespace TH_ExamGen.Services
{
    /// <summary>
    /// Quy ước trình bày chung cho mọi hình TikZ do phần mềm tự dựng
    /// (độ dày nét, màu tô, cỡ chữ nhãn, cách viết số) để các hình đồng nhất như SGK.
    /// </summary>
    internal static class TikzStyle
    {
        public const string Thick = "thick";
        public const string Dash = "thick, dashed";
        public const string Hidden = "thick, dashed, blue!80!black";
        public const string Fill = "blue!35";
        public const string LabelFont = @"font=\footnotesize";

        /// <summary>Viết số theo kiểu Việt Nam (dấu phẩy thập phân), gọn tối đa (8; 3,5).</summary>
        public static string Num(double value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture).Replace('.', ',');
        }

        /// <summary>Nhãn số đo, ví dụ "8 cm" hoặc "3,5 cm".</summary>
        public static string Len(double value, string unit = "cm")
        {
            return $"{Num(value)} {unit}";
        }
    }
}
