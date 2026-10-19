using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace TH_ExamGen.Core.Curriculum
{
    /// <summary>Persists the teacher's explicit review decisions without modifying draft files.</summary>
    public static class CurriculumReviewStore
    {
        private static readonly string ReviewFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TH_ExamGen",
            "curriculum-review.json");

        private static readonly object Sync = new();

        public static bool IsConfirmed(string itemId, string version)
        {
            if (string.IsNullOrWhiteSpace(itemId) || string.IsNullOrWhiteSpace(version)) return false;
            lock (Sync) return ReadDecisions().TryGetValue(itemId, out string? approvedVersion) && approvedVersion == version;
        }

        public static bool Confirm(IEnumerable<ICurriculumTopicItem> items, out string error)
        {
            error = string.Empty;
            var itemsToAdd = items
                .Where(item => !string.IsNullOrWhiteSpace(item.MaMuc) && !string.IsNullOrWhiteSpace(item.MaPhienBan))
                .GroupBy(item => item.MaMuc, StringComparer.Ordinal)
                .Select(group => group.Last())
                .ToList();
            if (itemsToAdd.Count == 0) return true;

            lock (Sync)
            {
                try
                {
                    var decisions = ReadDecisions();
                    foreach (var item in itemsToAdd) decisions[item.MaMuc] = item.MaPhienBan;
                    string? directory = Path.GetDirectoryName(ReviewFilePath);
                    if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                    File.WriteAllText(ReviewFilePath, JsonSerializer.Serialize(decisions, new JsonSerializerOptions { WriteIndented = true }));
                    return true;
                }
                catch (Exception ex)
                {
                    error = $"Không lưu được xác nhận danh mục chương trình: {ex.Message}";
                    return false;
                }
            }
        }

        private static Dictionary<string, string> ReadDecisions()
        {
            try
            {
                if (!File.Exists(ReviewFilePath)) return new Dictionary<string, string>(StringComparer.Ordinal);
                var decisions = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(ReviewFilePath));
                return new Dictionary<string, string>(decisions ?? new Dictionary<string, string>(), StringComparer.Ordinal);
            }
            catch
            {
                // Malformed state fails closed: no topic becomes eligible from a corrupt review file.
                return new Dictionary<string, string>(StringComparer.Ordinal);
            }
        }
    }
}
