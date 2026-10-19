using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using TH_ExamGen.Core.MathModels;
using TH_ExamGen.Core.VietModels;

namespace TH_ExamGen.Core.Config
{
    /// <summary>
    /// Bản lưu trọn vẹn một đề đang soạn dở (ma trận + đề đã sinh + tên file xuất).
    /// Dùng cho chức năng "Lưu đề / Mở đề" và "tự động lưu tạm" để không mất công soạn.
    /// </summary>
    public class ExamDraft
    {
        public string Kind { get; set; } = "Math"; // "Math" hoặc "Viet"
        public int Grade { get; set; } = 4;
        public string Period { get; set; } = "Cuối Học kỳ II";
        public string ExportFileName { get; set; } = string.Empty;
        public DateTime SavedAt { get; set; } = DateTime.Now;
        /// <summary>Old or unmapped drafts stay flagged until all rows/questions carry reviewed curriculum IDs.</summary>
        public bool RequiresCurriculumReview { get; set; } = true;

        public MathExamPaper? MathPaper { get; set; }
        public List<MathMatrixItem>? MathMatrix { get; set; }

        public VietExamPaper? VietPaper { get; set; }
        public List<VietMatrixItem>? VietMatrix { get; set; }

        // Trạng thái ngữ liệu đầu vào của môn Tiếng Việt (để khôi phục đúng cái giáo viên đã nhập)
        public bool? UseCustomPassage { get; set; }
        public string? PassageTheme { get; set; }
        public string? CustomPassageTitle { get; set; }
        public string? CustomPassageAuthor { get; set; }
        public string? CustomPassageSource { get; set; }
        public string? CustomPassageContent { get; set; }
    }

    /// <summary>
    /// Đọc/ghi bản nháp đề ra đĩa (thư mục "Drafts" cạnh chương trình hoặc đường dẫn người dùng chọn).
    /// Mọi lỗi vào/ra đều được nuốt an toàn để không làm sập ứng dụng.
    /// </summary>
    public static class DraftStore
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            // Bỏ qua các thuộc tính chỉ dùng lúc chạy (nếu có) và giữ nguyên tiếng Việt
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public static string DraftDirectory =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Drafts");

        /// <summary>Đường dẫn file tự động lưu tạm cho từng môn.</summary>
        public static string AutoSavePath(string kind) =>
            Path.Combine(DraftDirectory, $"autosave_{kind.ToLowerInvariant()}.json");

        public static bool Save(ExamDraft draft, string path)
        {
            try
            {
                draft.SavedAt = DateTime.Now;
                string? dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                string json = JsonSerializer.Serialize(draft, Options);
                File.WriteAllText(path, json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static ExamDraft? Load(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                string json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<ExamDraft>(json, Options);
            }
            catch
            {
                return null;
            }
        }

        public static void Delete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
                // Bỏ qua
            }
        }
    }
}
