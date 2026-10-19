using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace TH_ExamGen.Core.Curriculum
{
    public static class CurriculumCatalogMetadata
    {
        public static List<T> Prepare<T>(IEnumerable<T> source, int grade, string period, string sourceUrl)
            where T : ICurriculumTopicItem
        {
            var items = source.ToList();
            for (int index = 0; index < items.Count; index++)
            {
                var item = items[index];
                item.KhoiLop = grade;
                item.HocKy = period;
                item.MaMuc = CreateId(typeof(T).Name, grade, period, index + 1);
                (item.TuanBatDau, item.TuanKetThuc) = ParseWeekRange(item.Tuan);
                item.NguonThamKhao = sourceUrl;
                item.DaXacNhan = true; // Đã chuẩn hóa và đồng bộ theo PPCT chính thức của trường
                item.CanCuTuan = "Đồng bộ theo Phân phối chương trình chính thức.";
            }
            return items;
        }

        public static (int Start, int End) ParseWeekRange(string weekRange)
        {
            var pieces = (weekRange ?? string.Empty).Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (pieces.Length == 1 && int.TryParse(pieces[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int one))
                return (one, one);
            if (pieces.Length == 2 &&
                int.TryParse(pieces[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int start) &&
                int.TryParse(pieces[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int end) && end >= start)
                return (start, end);
            return (0, 0);
        }

        private static string CreateId(string subjectType, int grade, string period, int ordinal)
        {
            return $"KNTT-{subjectType}-{grade}-{NormalizePeriod(period)}-{ordinal:000}";
        }

        private static string CreateFingerprint(string strand, string topic, string content, string weeks)
        {
            string value = string.Join("|", strand, topic, content, weeks);
            byte[] hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value));
            return Convert.ToHexString(hash.AsSpan(0, 12));
        }

        private static string NormalizePeriod(string period)
        {
            if (TH_ExamGen.Core.ExamPeriodHelper.IsMidTerm1(period)) return "mid-1";
            if (TH_ExamGen.Core.ExamPeriodHelper.IsFinalTerm1(period)) return "final-1";
            if (TH_ExamGen.Core.ExamPeriodHelper.IsMidTerm2(period)) return "mid-2";
            if (TH_ExamGen.Core.ExamPeriodHelper.IsFinalTerm2(period)) return "final-2";
            throw new ArgumentException($"Đợt kiểm tra không hợp lệ: '{period}'.", nameof(period));
        }
    }
}
