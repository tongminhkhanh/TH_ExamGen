using System;
using System.IO;
using System.Windows;
using TH_ExamGen.Services;

namespace TH_ExamGen.UI.Helpers
{
    /// <summary>
    /// Bọc thao tác ghi file Word để xử lý các lỗi vào/ra thường gặp khi giáo viên dùng
    /// (đặc biệt là tệp đang mở trong Microsoft Word -> IOException "sharing violation").
    /// Thay vì hiện thông báo kỹ thuật khó hiểu, chương trình hướng dẫn rõ ràng và cho
    /// phép chọn tên / thư mục khác để lưu.
    /// </summary>
    public static class ExportHelper
    {
        /// <summary>
        /// Chạy <paramref name="exportAction"/> để ghi ra <paramref name="fullPath"/>.
        /// Nếu gặp lỗi do tệp đang bị khóa hoặc không có quyền ghi, sẽ hỏi và cho chọn nơi lưu khác.
        /// Trả về đường dẫn đã xuất thành công, hoặc null nếu người dùng chọn bỏ qua.
        /// </summary>
        public static string? RunGuarded(string fullPath, Action<string> exportAction, string dialogTitle)
        {
            while (true)
            {
                try
                {
                    exportAction(fullPath);
                    return fullPath;
                }
                catch (IOException ex)
                {
                    AppLogger.Warn($"Không ghi được tệp (có thể đang mở trong Word): {fullPath} | {ex.Message}");

                    var res = MessageBox.Show(
                        "Không ghi được tệp vì tệp đang được mở (thường là đang mở trong Microsoft Word).\n\n" +
                        $"Tệp: {Path.GetFileName(fullPath)}\n\n" +
                        "Cách xử lý: hãy đóng tệp đó trong Word rồi thử lại.\n\n" +
                        "Bạn có muốn chọn tên hoặc thư mục khác để lưu ngay bây giờ không?",
                        "Không ghi được tệp", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                    if (res != MessageBoxResult.Yes) return null;

                    string? picked = PromptSavePath(dialogTitle, fullPath);
                    if (string.IsNullOrWhiteSpace(picked)) return null;
                    fullPath = picked;
                }
                catch (UnauthorizedAccessException ex)
                {
                    AppLogger.Warn($"Không có quyền ghi tệp: {fullPath} | {ex.Message}");

                    var res = MessageBox.Show(
                        "Không có quyền ghi vào thư mục này.\n\n" +
                        $"Thư mục: {Path.GetDirectoryName(fullPath)}\n\n" +
                        "Hãy chọn một thư mục khác (ví dụ: Documents hoặc Desktop) để lưu tệp.",
                        "Không có quyền ghi tệp", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                    if (res != MessageBoxResult.Yes) return null;

                    string? picked = PromptSavePath(dialogTitle, fullPath);
                    if (string.IsNullOrWhiteSpace(picked)) return null;
                    fullPath = picked;
                }
            }
        }

        /// <summary>Hiện hộp thoại chọn nơi lưu, gợi ý sẵn tên tệp hiện tại.</summary>
        public static string? PromptSavePath(string title, string currentPath)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Title = title,
                FileName = Path.GetFileName(currentPath),
                DefaultExt = Path.GetExtension(currentPath),
                Filter = "Tệp Word (*.docx)|*.docx|Tất cả tệp (*.*)|*.*",
                OverwritePrompt = true
            };

            string? dir = Path.GetDirectoryName(currentPath);
            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
            {
                dlg.InitialDirectory = dir;
            }

            return dlg.ShowDialog() == true ? dlg.FileName : null;
        }
    }
}
