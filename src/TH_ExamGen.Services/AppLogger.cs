using System;
using System.IO;

namespace TH_ExamGen.Services
{
    /// <summary>
    /// Ghi log đơn giản ra file cạnh chương trình (thư mục "logs/app.log").
    /// Dùng cho các tình huống mà Debug.WriteLine không hiển thị ở bản phát hành (.exe).
    /// Việc ghi log không bao giờ được làm sập ứng dụng.
    /// </summary>
    public static class AppLogger
    {
        private static readonly object _lock = new object();
        private const long MaxBytes = 1_000_000; // ~1MB thì xoay vòng

        public static string LogDirectory =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");

        public static string LogFilePath => Path.Combine(LogDirectory, "app.log");

        public static void Info(string message) => Write("INFO", message);

        public static void Warn(string message) => Write("WARN", message);

        public static void Error(string message) => Write("ERROR", message);

        public static void Error(string message, Exception ex) =>
            Write("ERROR", $"{message} | {ex.GetType().Name}: {ex.Message}");

        private static void Write(string level, string message)
        {
            try
            {
                lock (_lock)
                {
                    Directory.CreateDirectory(LogDirectory);

                    if (File.Exists(LogFilePath) && new FileInfo(LogFilePath).Length > MaxBytes)
                    {
                        string backup = LogFilePath + ".1";
                        try
                        {
                            if (File.Exists(backup)) File.Delete(backup);
                            File.Move(LogFilePath, backup);
                        }
                        catch
                        {
                            // Bỏ qua nếu không xoay vòng được
                        }
                    }

                    string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}{Environment.NewLine}";
                    File.AppendAllText(LogFilePath, line);
                }
            }
            catch
            {
                // Không được để việc ghi log làm ảnh hưởng tới ứng dụng
            }
        }
    }
}
