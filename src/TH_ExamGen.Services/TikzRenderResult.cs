using System.IO;

namespace TH_ExamGen.Services
{
    /// <summary>
    /// Kết quả render một hình TikZ: cho biết thành công/thất bại, đường dẫn ảnh,
    /// có dùng dịch vụ online (Kroki) hay không, và lý do lỗi (nếu có).
    /// Giúp giao diện cảnh báo rõ ràng thay vì "mất hình âm thầm".
    /// </summary>
    public class TikzRenderResult
    {
        public string? Path { get; set; }

        public bool UsedOnline { get; set; }

        public string? Error { get; set; }

        public bool Success => !string.IsNullOrWhiteSpace(Path) && File.Exists(Path!);
    }
}
