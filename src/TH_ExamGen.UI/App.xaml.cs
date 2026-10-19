using System;
using System.IO;
using System.Linq;
using System.Windows;
using TH_ExamGen.Core.Config;
using TH_ExamGen.Core.MathModels;
using TH_ExamGen.Core.VietModels;
using TH_ExamGen.Services;

namespace TH_ExamGen.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // Tránh hiện liên tiếp nhiều hộp thoại báo lỗi nếu lỗi lặp lại trong thời gian ngắn.
        private static DateTime _lastErrorDialogUtc = DateTime.MinValue;

        protected override async void OnStartup(StartupEventArgs e)
        {
            // Đăng ký bắt lỗi toàn cục ngay từ đầu để không bao giờ tắt ngang làm mất công soạn đề.
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += AppDomain_UnhandledException;
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            base.OnStartup(e);

            if (e.Args.Contains("--test-export"))
            {
                int exitCode = 0;
                try
                {
                    Console.WriteLine("Running headless test export...");
                    var settings = AppSettings.Load();
                    var paper = MathExamPaper.GetSampleGrade4Paper();

                    string exportDir = ExportPaths.GetExportFolder(settings);
                    string tikzCacheDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TikzCache");
                    Directory.CreateDirectory(tikzCacheDir);
                    TikzRenderService.CleanupCache(tikzCacheDir);

                    foreach (var q in paper.DanhSachCauHoi)
                    {
                        if (q.CoHinhTikz && !string.IsNullOrWhiteSpace(q.MaTikz))
                        {
                            Console.WriteLine($"Compiling TikZ for question {q.SoThuTu}...");
                            string pngPath = Path.Combine(tikzCacheDir, TikzRenderService.GetCacheFileName(q.MaTikz));
                            var renderResult = await TikzRenderService.RenderTikzToPngResultAsync(q.MaTikz, pngPath, settings.MiktexBinPath);
                            if (renderResult.Success)
                            {
                                q.AnhTikzPath = renderResult.Path;
                                Console.WriteLine($"TikZ q{q.SoThuTu} rendered to: {renderResult.Path}");
                            }
                            else
                            {
                                Console.WriteLine($"TikZ q{q.SoThuTu} render failed: {renderResult.Error}");
                            }
                        }
                    }

                    string outFile = Path.Combine(exportDir, "De_Kiem_Tra_Toan_Lop_4.docx");
                    WordExportMathService.ExportMathExamOnlyToDocx(paper, settings, outFile);
                    Console.WriteLine($"Successfully exported test exam to: {outFile}");

                    string matrixOutFile = Path.Combine(exportDir, "Ma_Tran_Va_Dac_Ta_Toan_Lop_4.docx");
                    WordExportMathService.ExportMathMatrixOnlyToDocx(paper.DanhSachMaTran, settings, 4, matrixOutFile);
                    Console.WriteLine($"Successfully exported test matrix to: {matrixOutFile}");

                    var vietMatrix = VietCurriculumBank.GetGrade4FinalTerm2Matrix();
                    string vietMatrixOutFile = Path.Combine(exportDir, "Ma_Tran_Va_Dac_Ta_TiengViet_Lop_4.docx");
                    WordExportVietService.ExportVietMatrixOnlyToDocx(vietMatrix, settings, 4, vietMatrixOutFile, "Cuối Học kỳ II");
                    Console.WriteLine($"Successfully exported test viet matrix to: {vietMatrixOutFile}");
                }
                catch (Exception ex)
                {
                    exitCode = 1;
                    Console.WriteLine($"Error during test export: {ex}");
                }
                finally
                {
                    Shutdown(exitCode);
                }
            }
        }

        /// <summary>
        /// Bắt mọi lỗi chưa xử lý trên luồng giao diện: ghi log, báo thân thiện và CHO ỨNG DỤNG TIẾP TỤC CHẠY
        /// thay vì tắt ngang. Nhờ vậy giáo viên không mất đề đang soạn dở.
        /// </summary>
        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            AppLogger.Error("Lỗi không mong đợi trên luồng giao diện", e.Exception);

            if ((DateTime.UtcNow - _lastErrorDialogUtc).TotalSeconds > 3)
            {
                _lastErrorDialogUtc = DateTime.UtcNow;
                try
                {
                    MessageBox.Show(
                        "Chương trình gặp một lỗi không mong đợi nhưng vẫn tiếp tục chạy để không mất công soạn đề của bạn.\n\n" +
                        "Để an toàn, bạn nên bấm 'Lưu đề' lại rồi khởi động lại chương trình.\n\n" +
                        $"Nội dung lỗi: {e.Exception.Message}\n\n" +
                        $"Chi tiết đã ghi vào: {AppLogger.LogFilePath}",
                        "Có lỗi xảy ra", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                catch
                {
                    // Nếu cả hộp thoại cũng lỗi thì bỏ qua
                }
            }

            e.Handled = true; // Quan trọng: ngăn ứng dụng tắt ngang
        }

        /// <summary>Lỗi trên luồng nền ngoài tầm kiểm soát — chỉ kịp ghi log.</summary>
        private static void AppDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception
                     ?? new Exception(e.ExceptionObject?.ToString() ?? "Lỗi không xác định");
            AppLogger.Error("Lỗi nghiêm trọng (không thể phục hồi)", ex);
        }

        /// <summary>Nuốt lỗi từ tác vụ nền (Task) để không kích hoạt tắt ứng dụng.</summary>
        private static void TaskScheduler_UnobservedTaskException(object? sender, System.Threading.Tasks.UnobservedTaskExceptionEventArgs e)
        {
            AppLogger.Error("Lỗi trong tác vụ nền", e.Exception);
            e.SetObserved();
        }
    }
}
