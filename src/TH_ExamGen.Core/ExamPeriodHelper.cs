using System;

namespace TH_ExamGen.Core
{
    /// <summary>
    /// So khớp chuỗi đợt kiểm tra chuẩn của app ("Giữa Học kỳ I", "Cuối Học kỳ I", "Giữa Học kỳ II", "Cuối Học kỳ II").
    /// Dùng SO SÁNH BẰNG sau khi chuẩn hóa (bỏ khoảng trắng, không phân biệt hoa thường, chấp nhận "1"/"2" thay cho "I"/"II")
    /// để tránh lỗi "Giữa Học kỳ II".Contains("Giữa Học kỳ I") = true (khớp nhầm đợt Giữa HK2 thành Giữa HK1).
    /// </summary>
    public static class ExamPeriodHelper
    {
        public static bool IsMidTerm1(string period) => Match(period, "Giữa Học kỳ I");
        public static bool IsFinalTerm1(string period) => Match(period, "Cuối Học kỳ I");
        public static bool IsMidTerm2(string period) => Match(period, "Giữa Học kỳ II");
        public static bool IsFinalTerm2(string period) => Match(period, "Cuối Học kỳ II");
        public static bool IsFirstTerm(string period) => IsMidTerm1(period) || IsFinalTerm1(period);
        public static bool IsSecondTerm(string period) => IsMidTerm2(period) || IsFinalTerm2(period);

        private static bool Match(string period, string canonical)
        {
            if (string.IsNullOrWhiteSpace(period)) return false;
            return Normalize(period) == Normalize(canonical);
        }

        /// <summary>Bỏ khoảng trắng, viết hoa toàn bộ, đưa số 1/2 về ký hiệu I/II.</summary>
        private static string Normalize(string value)
        {
            var text = value.Trim().ToUpperInvariant().Replace(" ", "");
            text = text.Replace("2", "II").Replace("1", "I");
            return text;
        }
    }
}
