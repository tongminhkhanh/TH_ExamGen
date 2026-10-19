using System;
using System.IO;

namespace TH_ExamGen.Core.Config
{
    /// <summary>
    /// Xác định thư mục dùng để xuất file Word (.docx).
    /// Mặc định là thư mục <c>TH_ExamGen_XuatDe</c> trên Desktop, nhưng giáo viên có thể
    /// đổi sang thư mục khác trong mục "Cài đặt" (lưu ở <see cref="AppSettings.ExportFolder"/>).
    /// </summary>
    public static class ExportPaths
    {
        public const string DefaultFolderName = "TH_ExamGen_XuatDe";

        /// <summary>Thư mục xuất mặc định: Desktop\TH_ExamGen_XuatDe.</summary>
        public static string GetDefaultExportFolder()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                DefaultFolderName);
        }

        /// <summary>
        /// Trả về đường dẫn thư mục xuất theo cấu hình (không tạo thư mục).
        /// Nếu cấu hình trống thì trả về thư mục mặc định.
        /// </summary>
        public static string ResolveExportFolder(string? configuredFolder)
        {
            return string.IsNullOrWhiteSpace(configuredFolder)
                ? GetDefaultExportFolder()
                : configuredFolder.Trim();
        }

        /// <summary>
        /// Trả về thư mục xuất đang dùng và cố gắng tạo sẵn nếu chưa có.
        /// Nếu thư mục cấu hình không dùng được (sai đường dẫn, ổ đĩa rời...),
        /// tự động quay về thư mục mặc định để việc xuất file vẫn tiếp tục được.
        /// </summary>
        public static string GetExportFolder(AppSettings? settings)
        {
            string configured = ResolveExportFolder(settings?.ExportFolder);

            if (TryEnsureDirectory(configured))
            {
                return configured;
            }

            string fallback = GetDefaultExportFolder();
            TryEnsureDirectory(fallback);
            return fallback;
        }

        private static bool TryEnsureDirectory(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path)) return false;
                Directory.CreateDirectory(path);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
