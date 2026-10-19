using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using TH_ExamGen.Core.MathModels;
using TH_ExamGen.Core.VietModels;

namespace TH_ExamGen.Core.Curriculum
{
    /// <summary>Maps legacy matrix templates to catalog IDs only when their wording has a clear catalog match.</summary>
    public static class CurriculumMatrixMapper
    {
        public static List<MathMatrixItem> AttachMathIds(int grade, string period, IEnumerable<MathMatrixItem> matrix)
        {
            var topics = MathTopicCatalog.GetTopics(grade, period);
            var result = matrix.ToList();
            foreach (var row in result)
            {
                var candidates = topics.Where(topic => string.Equals(topic.MachKienThuc, row.ChuDe, StringComparison.OrdinalIgnoreCase)).ToList();
                row.MaMucChuongTrinh = BestMatch(row.NoiDungKienThuc + " " + row.YeuCauCanDat, candidates, topic => topic.ChuDe + " " + topic.NoiDung);
                var match = candidates.FirstOrDefault(topic => topic.MaMuc == row.MaMucChuongTrinh);
                if (match != null)
                {
                    // This mapper is only used for the application's built-in starter matrix.
                    // Normalize it to the selected catalog item; saved teacher drafts are never rewritten here.
                    row.ChuDe = match.MachKienThuc;
                    row.NoiDungKienThuc = match.ChuDe;
                    row.YeuCauCanDat = match.GetYeuCauCanDat(row.MucDo);
                }
            }
            return result;
        }

        /// <summary>
        /// Rewrites the three canonical text columns (ChuDe/NoiDungKienThuc/YeuCauCanDat) from a confirmed catalog
        /// topic for every row whose (MaMucChuongTrinh, MucDo) pair matches one; rows with an unknown id or an
        /// unsupported level are left untouched so the strict validator reports them. Used on AI-suggested rows
        /// right after JSON parsing: the model only picks the catalog id and level, the app restores the exact
        /// catalog wording before validation.
        /// </summary>
        public static void FillCatalogTexts(int grade, string period, IEnumerable<MathMatrixItem> rows)
        {
            var topics = CurriculumScopePolicy.GetConfirmedMathTopics(grade, period)
                .ToDictionary(topic => topic.MaMuc, StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.MaMucChuongTrinh))
                    continue;

                string cleanId = row.MaMucChuongTrinh.Trim();
                if (!topics.TryGetValue(cleanId, out var topic))
                    continue;

                row.MaMucChuongTrinh = topic.MaMuc;
                row.MucDo = (row.MucDo ?? string.Empty).Trim().ToUpperInvariant();
                if (row.MucDo is not ("M1" or "M2" or "M3"))
                    continue;

                row.ChuDe = topic.MachKienThuc;
                row.NoiDungKienThuc = topic.ChuDe;
                row.YeuCauCanDat = topic.GetYeuCauCanDat(row.MucDo);
            }
        }

        public static List<VietMatrixItem> AttachVietIds(int grade, string period, IEnumerable<VietMatrixItem> matrix)
        {
            var topics = VietTopicCatalog.GetTopics(grade, period);
            var readingTopics = topics.Where(topic => topic.MachKienThuc.Contains("Đọc hiểu", StringComparison.OrdinalIgnoreCase)).ToList();
            var knowledgeTopics = topics.Where(topic => topic.MachKienThuc.Contains("Tiếng Việt", StringComparison.OrdinalIgnoreCase)).ToList();
            var confirmedReadingTopics = readingTopics.Where(topic => topic.DaXacNhan).ToList();
            var result = matrix.ToList();
            foreach (var row in result)
            {
                bool isReading = row.MachKienThuc.Contains("Đọc hiểu", StringComparison.OrdinalIgnoreCase);
                var candidates = isReading ? readingTopics : knowledgeTopics;
                string match = isReading
                    ? (confirmedReadingTopics.Count == 1 ? confirmedReadingTopics[0].MaMuc : string.Empty)
                    : BestMatch(row.NoiDungKienThuc + " " + row.YeuCauCanDat, candidates, topic => topic.ChuDe + " " + topic.NoiDung);
                // Never guess a reading theme: a legacy template with generic reading skills stays unmapped
                // unless exactly one passage theme has already been approved for this exam.
                row.MaMucChuongTrinh = match;
                var topicMatch = candidates.FirstOrDefault(topic => topic.MaMuc == match);
                if (!isReading && topicMatch != null)
                {
                    row.MachKienThuc = topicMatch.MachKienThuc;
                    row.NoiDungKienThuc = topicMatch.ChuDe;
                    row.YeuCauCanDat = topicMatch.NoiDung;
                }
            }
            return result;
        }

        private static string BestMatch<T>(string rowText, IReadOnlyCollection<T> candidates, Func<T, string> describe)
            where T : ICurriculumTopicItem
        {
            if (candidates.Count == 0) return string.Empty;
            var rowTerms = Tokens(rowText);
            var scores = candidates.Select(item =>
            {
                string description = describe(item);
                var topicTerms = Tokens(description);
                int overlap = rowTerms.Intersect(topicTerms, StringComparer.Ordinal).Count();
                double ratio = overlap / (double)Math.Max(1, Math.Min(rowTerms.Count, topicTerms.Count));
                string normalizedRow = Normalize(rowText);
                string normalizedDescription = Normalize(description);
                if (normalizedRow.Contains(Normalize(item.ChuDe), StringComparison.Ordinal) || normalizedDescription.Contains(normalizedRow, StringComparison.Ordinal))
                    ratio = Math.Max(ratio, 0.95);
                return (item.MaMuc, Score: ratio, Overlap: overlap);
            }).OrderByDescending(x => x.Score).ThenByDescending(x => x.Overlap).ToList();

            if (scores[0].Overlap < 2 || scores[0].Score < 0.3) return string.Empty;
            if (scores.Count > 1 && Math.Abs(scores[0].Score - scores[1].Score) < 0.08 && scores[0].Overlap == scores[1].Overlap)
                return string.Empty;
            return scores[0].MaMuc;
        }

        private static HashSet<string> Tokens(string value) => Normalize(value)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(token => token.Length >= 3 && !StopWords.Contains(token))
            .ToHashSet(StringComparer.Ordinal);

        private static string Normalize(string value)
        {
            string decomposed = (value ?? string.Empty).Normalize(NormalizationForm.FormD);
            var chars = decomposed.Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark).ToArray();
            return new string(chars).Normalize(NormalizationForm.FormC).ToLowerInvariant();
        }

        private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
        {
            "bai", "toan", "lop", "ve", "va", "la", "cua", "cac", "mot", "nhieu", "biet", "de", "cho", "trong", "theo", "voi", "hoc", "tap", "on", "nhan", "nhan", "nhan", "doc", "hieu"
        };
    }
}
