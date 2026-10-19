using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TH_ExamGen.Core.Config;
using TH_ExamGen.Core.Licensing;
using TH_ExamGen.Services;
using TH_ExamGen.UI.Views;

namespace TH_ExamGen.UI
{
    public partial class MainWindow : Window
    {
        private readonly MathModuleControl _mathControl = new();
        private readonly VietModuleControl _vietControl = new();
        private readonly SettingsControl _settingsControl = new();

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateStatusInfo();
            // Default to Math module
            NavigateTo(_mathControl, BtnNavMath);
            await CheckEnvironmentAsync();
        }

        /// <summary>
        /// Kiểm tra môi trường LaTeX (MiKTeX/pdflatex) khi khởi động để báo trước cho giáo viên
        /// biết hình TikZ sẽ được vẽ cục bộ hay phải nhờ dịch vụ online Kroki.
        /// </summary>
        private async System.Threading.Tasks.Task CheckEnvironmentAsync()
        {
            try
            {
                var settings = AppSettings.Load();
                bool latexOk = await System.Threading.Tasks.Task.Run(
                    () => TikzRenderService.IsLatexAvailable(settings.MiktexBinPath));

                if (latexOk)
                {
                    TxtEnvStatus.Text = "✔ Đã tìm thấy MiKTeX (pdflatex): hình TikZ được vẽ ngay trên máy, không cần Internet.";
                }
                else
                {
                    TxtEnvStatus.Text = "⚠ Chưa tìm thấy MiKTeX (pdflatex). Hình TikZ sẽ vẽ qua dịch vụ online Kroki nên cần Internet. Có thể cài MiKTeX và trỏ đường dẫn trong mục 'Cài đặt'.";
                    TxtEnvStatus.Foreground = new SolidColorBrush(Color.FromRgb(251, 191, 36));
                    AppLogger.Warn("Không tìm thấy pdflatex/MiKTeX. Hình TikZ sẽ dùng dịch vụ Kroki online.");
                }
            }
            catch (Exception ex)
            {
                AppLogger.Warn($"Không kiểm tra được môi trường LaTeX: {ex.Message}");
                TxtEnvStatus.Text = "Không kiểm tra được môi trường LaTeX.";
            }
        }

        public void UpdateStatusInfo()
        {
            try
            {
                var settings = AppSettings.Load();
                TxtActiveAiEngine.Text = $"AI: {settings.ActiveProviderDisplayName}";
            }
            catch
            {
                TxtActiveAiEngine.Text = "AI: Sẵn sàng";
            }

            try
            {
                bool isLicensed = LicenseManager.GetLicenseStatus(out string statusMsg, out bool isOfficial, out int daysLeft, out int remainingCount);
                if (TxtLicenseStatus != null)
                {
                    TxtLicenseStatus.Text = isOfficial 
                        ? statusMsg 
                        : (isLicensed ? $"Dùng thử: còn {remainingCount} lượt" : "Hết hạn bản quyền");
                    TxtLicenseStatus.Foreground = isOfficial 
                        ? new SolidColorBrush(Color.FromRgb(56, 189, 248)) 
                        : (isLicensed ? new SolidColorBrush(Color.FromRgb(251, 191, 36)) : new SolidColorBrush(Color.FromRgb(248, 113, 113)));
                }

                if (TxtNavLicenseSub != null)
                {
                    TxtNavLicenseSub.Text = isOfficial ? "Đã kích hoạt chính thức" : (isLicensed ? $"Dùng thử: còn {remainingCount} lượt" : "Hết hạn — Kích hoạt");
                }
            }
            catch { }
        }

        private void BtnNavMath_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(_mathControl, BtnNavMath);
        }

        private void BtnNavViet_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(_vietControl, BtnNavViet);
        }

        private void BtnNavLicense_Click(object sender, RoutedEventArgs e)
        {
            var licenseWin = new LicenseWindow { Owner = this };
            licenseWin.ShowDialog();
            UpdateStatusInfo();
        }

        private void BtnNavSettings_Click(object sender, RoutedEventArgs e)
        {
            _settingsControl.LoadSettings();
            NavigateTo(_settingsControl, BtnNavSettings);
        }

        private void NavigateTo(UserControl control, Button activeButton)
        {
            MainContentHost.Content = control;

            // Reset navigation button backgrounds
            ResetNavButton(BtnNavMath);
            ResetNavButton(BtnNavViet);
            ResetNavButton(BtnNavLicense);
            ResetNavButton(BtnNavSettings);

            // Set active button style
            activeButton.Background = new SolidColorBrush(Color.FromRgb(30, 41, 59));
        }

        private void ResetNavButton(Button btn)
        {
            if (btn != null) btn.Background = Brushes.Transparent;
        }
    }
}