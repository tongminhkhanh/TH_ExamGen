using System.IO;
using System.Text.Json;

namespace TH_ExamGen.Core.Config
{
    public class AppSettings
    {
        public string AiProvider { get; set; } = "Gemini"; // "Gemini" hoặc "DeepSeek"
        public string GeminiApiKey { get; set; } = string.Empty;
        public string GeminiModel { get; set; } = "gemini-3.8-flash";
        public string DeepSeekApiKey { get; set; } = string.Empty;
        public string DeepSeekModel { get; set; } = "deepseek-chat";
        public string TenUBND { get; set; } = "ỦY BAN NHÂN DÂN XÃ MƯỜNG LA";
        public string TenTruong { get; set; } = "TRƯỜNG TIỂU HỌC ÍT ONG";
        public string NamHoc { get; set; } = "2025 - 2026";
        public string HocKy { get; set; } = "CUỐI HỌC KỲ II";
        public string MiktexBinPath { get; set; } = string.Empty;

        /// <summary>Chế độ "Bảo đảm hình chuẩn": ưu tiên bộ dựng hình mẫu (Template) cho mọi câu có hình thay vì tin vào mã TikZ do AI sinh.</summary>
        public bool BaoDamHinhChuan { get; set; } = false;

        /// <summary>Thư mục xuất file Word. Để trống = dùng thư mục mặc định trên Desktop.</summary>
        public string ExportFolder { get; set; } = string.Empty;

        /// <summary>Khối lớp đã chọn lần trước (4 hoặc 5; 0 = chưa từng chọn).</summary>
        public int LastGrade { get; set; } = 0;

        /// <summary>Kỳ kiểm tra đã chọn lần trước (để trống = dùng mặc định của phần mềm).</summary>
        public string LastPeriod { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonIgnore]
        public string ActiveApiKey => (AiProvider?.Equals("DeepSeek", System.StringComparison.OrdinalIgnoreCase) == true)
            ? DeepSeekApiKey : GeminiApiKey;

        [System.Text.Json.Serialization.JsonIgnore]
        public string ActiveModel => (AiProvider?.Equals("DeepSeek", System.StringComparison.OrdinalIgnoreCase) == true)
            ? (string.IsNullOrWhiteSpace(DeepSeekModel) ? "deepseek-chat" : DeepSeekModel)
            : (string.IsNullOrWhiteSpace(GeminiModel) ? "gemini-3.8-flash" : GeminiModel);

        [System.Text.Json.Serialization.JsonIgnore]
        public string ActiveProviderDisplayName => (AiProvider?.Equals("DeepSeek", System.StringComparison.OrdinalIgnoreCase) == true)
            ? $"DeepSeek ({ActiveModel})" : $"Gemini ({ActiveModel})";

        private static readonly string SettingsFilePath = Path.Combine(
            System.AppDomain.CurrentDomain.BaseDirectory,
            "appsettings.json"
        );

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null) return settings;
                }
            }
            catch
            {
                // Fallback to default
            }

            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch
            {
                // Handle file write errors if needed
            }
        }
    }
}
