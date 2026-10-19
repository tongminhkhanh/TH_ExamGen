using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TH_ExamGen.Core.Config;
using TH_ExamGen.Services;

namespace TH_ExamGen.UI.Views
{
    public partial class SettingsControl : UserControl
    {
        private AppSettings _settings = new();

        public SettingsControl()
        {
            InitializeComponent();
            Loaded += SettingsControl_Loaded;
        }

        private void SettingsControl_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSettings();
        }

        public void LoadSettings()
        {
            _settings = AppSettings.Load();
            TxtApiKey.Text = _settings.GeminiApiKey;
            TxtDeepSeekKey.Text = _settings.DeepSeekApiKey;
            TxtUbnd.Text = _settings.TenUBND;
            TxtTruong.Text = _settings.TenTruong;
            TxtNamHoc.Text = _settings.NamHoc;
            TxtMiktexPath.Text = _settings.MiktexBinPath;
            TxtExportFolder.Text = _settings.ExportFolder;
            UpdateExportFolderHint();
            ChkHinhChuan.IsChecked = _settings.BaoDamHinhChuan;

            // Set Active Provider
            if (_settings.AiProvider?.Equals("DeepSeek", StringComparison.OrdinalIgnoreCase) == true)
            {
                RbProviderDeepSeek.IsChecked = true;
            }
            else
            {
                RbProviderGemini.IsChecked = true;
            }
            UpdateProviderBadges();

            // Set Gemini model
            SetComboBoxValue(CboModel, _settings.GeminiModel, "gemini-2.5-flash");

            // Set DeepSeek model
            SetComboBoxValue(CboDeepSeekModel, _settings.DeepSeekModel, "deepseek-chat");

            // Set HocKy
            foreach (ComboBoxItem item in CboHocKy.Items)
            {
                if (item.Content.ToString() == _settings.HocKy)
                {
                    item.IsSelected = true;
                    break;
                }
            }
        }

        private void SetComboBoxValue(ComboBox cbo, string? value, string defaultValue)
        {
            string target = string.IsNullOrWhiteSpace(value) ? defaultValue : value;
            bool found = false;
            foreach (var item in cbo.Items)
            {
                string text = item is ComboBoxItem cbi ? (cbi.Content?.ToString() ?? "") : item.ToString() ?? "";
                if (text.Equals(target, StringComparison.OrdinalIgnoreCase))
                {
                    cbo.SelectedItem = item;
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                cbo.Text = target;
            }
        }

        private void RbProvider_Checked(object sender, RoutedEventArgs e)
        {
            UpdateProviderBadges();
        }

        private void UpdateProviderBadges()
        {
            if (BadgeGeminiActive == null || BadgeDeepSeekActive == null) return;
            bool isDeepSeek = RbProviderDeepSeek.IsChecked == true;
            BadgeDeepSeekActive.Visibility = isDeepSeek ? Visibility.Visible : Visibility.Collapsed;
            BadgeGeminiActive.Visibility = isDeepSeek ? Visibility.Collapsed : Visibility.Visible;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            _settings.AiProvider = RbProviderDeepSeek.IsChecked == true ? "DeepSeek" : "Gemini";
            _settings.GeminiApiKey = TxtApiKey.Text.Trim();
            _settings.DeepSeekApiKey = TxtDeepSeekKey.Text.Trim();
            _settings.TenUBND = TxtUbnd.Text.Trim();
            _settings.TenTruong = TxtTruong.Text.Trim();
            _settings.NamHoc = TxtNamHoc.Text.Trim();
            _settings.MiktexBinPath = TxtMiktexPath.Text.Trim();
            _settings.ExportFolder = TxtExportFolder.Text.Trim();
            _settings.BaoDamHinhChuan = ChkHinhChuan.IsChecked == true;

            _settings.GeminiModel = GetComboBoxText(CboModel, "gemini-2.5-flash");
            _settings.DeepSeekModel = GetComboBoxText(CboDeepSeekModel, "deepseek-chat");

            if (CboHocKy.SelectedItem is ComboBoxItem selHocKy)
            {
                _settings.HocKy = selHocKy.Content.ToString() ?? "CUỐI HỌC KỲ II";
            }

            _settings.Save();
            ShowStatus($"Đã lưu cài đặt thành công! (Mặc định: {_settings.ActiveProviderDisplayName})", true);

            // Cập nhật lại thanh trạng thái trên MainWindow nếu có
            if (Window.GetWindow(this) is MainWindow mw)
            {
                mw.UpdateStatusInfo();
            }
        }

        private string GetComboBoxText(ComboBox cbo, string defaultValue)
        {
            if (cbo.SelectedItem is ComboBoxItem cbi)
            {
                return cbi.Content?.ToString() ?? defaultValue;
            }
            if (!string.IsNullOrWhiteSpace(cbo.Text))
            {
                return cbo.Text.Trim();
            }
            return defaultValue;
        }

        private async void BtnTestKey_Click(object sender, RoutedEventArgs e)
        {
            string apiKey = TxtApiKey.Text.Trim();
            if (string.IsNullOrEmpty(apiKey))
            {
                ShowStatus("Vui lòng nhập Gemini API Key trước khi kiểm tra.", false);
                return;
            }

            string model = GetComboBoxText(CboModel, "gemini-2.5-flash");

            BtnTestKey.IsEnabled = false;
            ShowStatus($"Đang kiểm tra kết nối Google Gemini ({model})...", true);

            try
            {
                var service = new GeminiService();
                string result = await service.TestConnectionAsync("Gemini", apiKey, model);
                ShowStatus($"Kết nối Gemini thành công! ({model}): {result}", true);
            }
            catch (Exception ex)
            {
                ShowStatus($"Lỗi kết nối Gemini: {ex.Message}", false);
            }
            finally
            {
                BtnTestKey.IsEnabled = true;
            }
        }

        private async void BtnTestDeepSeek_Click(object sender, RoutedEventArgs e)
        {
            string apiKey = TxtDeepSeekKey.Text.Trim();
            if (string.IsNullOrEmpty(apiKey))
            {
                ShowStatus("Vui lòng nhập DeepSeek API Key trước khi kiểm tra.", false);
                return;
            }

            string model = GetComboBoxText(CboDeepSeekModel, "deepseek-chat");

            BtnTestDeepSeek.IsEnabled = false;
            ShowStatus($"Đang kiểm tra kết nối DeepSeek ({model})...", true);

            try
            {
                var service = new GeminiService();
                string result = await service.TestConnectionAsync("DeepSeek", apiKey, model);
                ShowStatus($"Kết nối DeepSeek thành công! ({model}): {result}", true);
            }
            catch (Exception ex)
            {
                ShowStatus($"Lỗi kết nối DeepSeek: {ex.Message}", false);
            }
            finally
            {
                BtnTestDeepSeek.IsEnabled = true;
            }
        }

        // Danh sách model gợi ý (tĩnh) dùng khi không lấy được danh sách động từ Google.
        private static readonly string[] FallbackGeminiModels =
        {
            "gemini-2.5-flash",
            "gemini-2.5-pro",
            "gemini-2.5-flash-lite",
            "gemini-2.0-flash",
            "gemini-2.0-flash-lite"
        };

        private async void BtnRefreshModels_Click(object sender, RoutedEventArgs e)
        {
            string apiKey = TxtApiKey.Text.Trim();
            if (string.IsNullOrEmpty(apiKey))
            {
                ShowStatus("Vui lòng nhập Gemini API Key trước khi làm mới danh sách model.", false);
                return;
            }

            string? current = GetComboBoxText(CboModel, "gemini-2.5-flash");

            BtnRefreshModels.IsEnabled = false;
            ShowStatus("Đang lấy danh sách model Gemini hiện có từ Google...", true);

            try
            {
                var service = new GeminiService();
                var models = await service.GetGeminiModelsAsync(apiKey);

                if (models.Count == 0)
                {
                    // Không lấy được (offline / key sai) -> dùng danh sách tĩnh dự phòng
                    models = FallbackGeminiModels.ToList();
                    ShowStatus("Không lấy được danh sách từ Google (kiểm tra Internet hoặc API Key). Đã dùng danh sách gợi ý có sẵn.", false);
                }
                else
                {
                    ShowStatus($"Đã cập nhật {models.Count} model Gemini hiện có. Hãy chọn model mong muốn rồi bấm 'Lưu cài đặt'.", true);
                }

                CboModel.Items.Clear();
                foreach (string m in models)
                {
                    CboModel.Items.Add(new ComboBoxItem { Content = m });
                }

                SetComboBoxValue(CboModel, current, "gemini-2.5-flash");
            }
            catch (Exception ex)
            {
                AppLogger.Error("Lỗi làm mới danh sách model Gemini", ex);
                ShowStatus($"Lỗi làm mới danh sách model: {ex.Message}", false);
            }
            finally
            {
                BtnRefreshModels.IsEnabled = true;
            }
        }

        private void BtnBrowseMiktex_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Chọn thư mục chứa pdflatex.exe"
            };

            if (dialog.ShowDialog() == true)
            {
                TxtMiktexPath.Text = dialog.FolderName;
            }
        }

        private void TxtExportFolder_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateExportFolderHint();
        }

        private void BtnBrowseExportFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Chọn thư mục xuất file Word"
            };

            string current = TxtExportFolder.Text.Trim();
            if (!string.IsNullOrWhiteSpace(current) && Directory.Exists(current))
            {
                dialog.InitialDirectory = current;
            }

            if (dialog.ShowDialog() == true)
            {
                TxtExportFolder.Text = dialog.FolderName;
                UpdateExportFolderHint();
            }
        }

        private void BtnResetExportFolder_Click(object sender, RoutedEventArgs e)
        {
            TxtExportFolder.Text = string.Empty;
            UpdateExportFolderHint();
        }

        private void UpdateExportFolderHint()
        {
            if (TxtExportFolderHint == null) return;

            string folder = ExportPaths.ResolveExportFolder(TxtExportFolder?.Text.Trim());
            TxtExportFolderHint.Text = $"Tệp Word sẽ được lưu vào: {folder}\n(Nếu để trống, chương trình dùng thư mục mặc định trên Desktop.)";
        }

        private void ShowStatus(string msg, bool isSuccess)
        {
            TxtStatusMessage.Text = msg;
            TxtStatusMessage.Foreground = isSuccess ? new SolidColorBrush(Color.FromRgb(22, 163, 74)) : new SolidColorBrush(Color.FromRgb(220, 38, 38));
        }
    }
}
