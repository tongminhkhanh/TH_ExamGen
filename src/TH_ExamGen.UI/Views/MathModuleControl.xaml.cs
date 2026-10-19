using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TH_ExamGen.Core.Curriculum;
using TH_ExamGen.Core.Config;
using TH_ExamGen.Core.Licensing;
using TH_ExamGen.Core.MathModels;
using TH_ExamGen.Services;
using TH_ExamGen.UI.Helpers;

namespace TH_ExamGen.UI.Views
{
    public partial class MathModuleControl : UserControl
    {
        private ObservableCollection<MathMatrixItem> _matrixItems = new();
        private MathExamPaper? _currentPaper;
        private string _lastExportedFilePath = string.Empty;
        private System.Threading.CancellationTokenSource? _mathGenCts;
        private bool _initialized;
        private bool _isApplyingDraft;

        // Ma trận ngay trước lần AI gợi ý gần nhất (chỉ trong phiên làm việc hiện tại), phục vụ nút "Khôi phục bản trước AI".
        private List<MathMatrixItem>? _preAiMatrixSnapshot;

        public MathModuleControl()
        {
            InitializeComponent();
            Loaded += MathModuleControl_Loaded;

            // Tự động lưu ngay khi giáo viên sửa xong một ô trong ma trận.
            // Dùng Background để giá trị vừa nhập đã được ghi vào đối tượng trước khi lưu.
            DgMatrix.CellEditEnding += (s, args) =>
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(AutoSaveDraft));
        }

        private void MathModuleControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Chỉ khởi tạo MỘT lần. Nếu không, mỗi lần quay lại tab Toán ma trận sẽ bị nạp lại
            // và xóa mất các chỉnh sửa giáo viên đã nhập.
            if (_initialized) return;
            _initialized = true;

            // Nhớ lại Khối lớp / Kỳ kiểm tra giáo viên đã chọn lần trước
            ApplyLastSelectionFromSettings();
            LoadDefaultMatrix();
            UpdateSampleButtonContent();
            UpdateExportFolderHint();

            // Dọn ảnh TikZ cũ để tránh thư mục cache phình to theo thời gian
            string tikzDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TikzCache");
            TikzRenderService.CleanupCache(tikzDir);

            // Khôi phục bản soạn dở (nếu có) để không mất công soạn đề
            TryRestoreAutoSave();
        }

        private void BtnCancelGenerate_Click(object sender, RoutedEventArgs e)
        {
            if (_mathGenCts == null || _mathGenCts.IsCancellationRequested) return;

            _mathGenCts.Cancel();
            TxtAiStatus.Text = "Đang hủy theo yêu cầu...";
            BtnCancelGenerate.IsEnabled = false;
        }

        private int GetSelectedGrade()
        {
            return CboGrade?.SelectedIndex == 1 ? 5 : 4;
        }

        private string GetSelectedPeriod()
        {
            if (CboPeriod?.SelectedItem is ComboBoxItem item)
            {
                return item.Content?.ToString() ?? "Cuối Học kỳ II";
            }
            return "Cuối Học kỳ II";
        }

        private void LoadDefaultMatrix()
        {
            int grade = GetSelectedGrade();
            string period = GetSelectedPeriod();
            var list = MathCurriculumBank.GetMatrixByGradeAndPeriod(grade, period);
            _matrixItems = new ObservableCollection<MathMatrixItem>(list);
            DgMatrix.ItemsSource = _matrixItems;
            ClearPreAiSnapshot();
            UpdateSummary();
        }

        private void CboGradeOrPeriod_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgMatrix == null) return;
            if (_isApplyingDraft) return; // Tránh nạp lại ma trận khi đang khôi phục đề đã lưu
            LoadDefaultMatrix();
            UpdateSampleButtonContent();
            SaveLastSelection();
        }

        /// <summary>Nạp lại Khối lớp / Kỳ kiểm tra đã chọn ở lần dùng trước.</summary>
        private void ApplyLastSelectionFromSettings()
        {
            var settings = AppSettings.Load();
            if (CboGrade != null && settings.LastGrade == 5)
            {
                CboGrade.SelectedIndex = 1;
            }

            if (CboPeriod != null && !string.IsNullOrWhiteSpace(settings.LastPeriod))
            {
                foreach (ComboBoxItem item in CboPeriod.Items)
                {
                    if (string.Equals(item.Content?.ToString(), settings.LastPeriod, StringComparison.Ordinal))
                    {
                        CboPeriod.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        /// <summary>Ghi nhớ Khối lớp / Kỳ kiểm tra đang chọn để lần sau tự nạp lại.</summary>
        private void SaveLastSelection()
        {
            try
            {
                var settings = AppSettings.Load();
                settings.LastGrade = GetSelectedGrade();
                settings.LastPeriod = GetSelectedPeriod();
                settings.Save();
            }
            catch (Exception ex)
            {
                AppLogger.Warn("Không lưu được lựa chọn Khối/Kỳ: " + ex.Message);
            }
        }

        /// <summary>Cập nhật dòng nhắc thư mục xuất file Word ở Bước 3.</summary>
        private void UpdateExportFolderHint()
        {
            if (TxtExportFolderHint == null) return;
            var settings = AppSettings.Load();
            TxtExportFolderHint.Text = $"Nơi lưu file: {ExportPaths.ResolveExportFolder(settings.ExportFolder)}";
        }

        /// <summary>Hiện tổng điểm của đề hiện tại và cảnh báo nếu chưa đúng 10 điểm.</summary>
        private void UpdatePaperPointsBadge()
        {
            if (TxtPaperPoints == null) return;

            if (_currentPaper == null || _currentPaper.DanhSachCauHoi.Count == 0)
            {
                TxtPaperPoints.Text = string.Empty;
                return;
            }

            double total = _currentPaper.DanhSachCauHoi.Sum(x => x.Diem);
            bool ok = Math.Abs(total - 10.0) < 0.05;
            TxtPaperPoints.Text = ok
                ? $"Tổng điểm đề hiện tại: {total:0.#}/10 điểm ✓"
                : $"Tổng điểm đề hiện tại: {total:0.#}/10 điểm — chưa đúng 10 điểm, bạn nên kiểm tra lại điểm từng câu.";
            TxtPaperPoints.Foreground = ok
                ? new SolidColorBrush(Color.FromRgb(22, 163, 74))
                : new SolidColorBrush(Color.FromRgb(220, 38, 38));
        }

        /// <summary>Hỏi và lưu thư mục xuất file Word mới.</summary>
        private void BtnChangeExportFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Chọn thư mục xuất file Word"
            };

            string current = ExportPaths.ResolveExportFolder(AppSettings.Load().ExportFolder);
            if (Directory.Exists(current))
            {
                dialog.InitialDirectory = current;
            }

            if (dialog.ShowDialog() != true) return;

            var settings = AppSettings.Load();
            settings.ExportFolder = dialog.FolderName;
            settings.Save();

            UpdateExportFolderHint();
            MessageBox.Show($"Đã đổi nơi lưu file Word thành:\n{dialog.FolderName}", "Đã đổi thư mục", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>Vẽ lại toàn bộ hình TikZ của đề hiện tại (hữu ích khi có hình bị thiếu/lỗi).</summary>
        private async void BtnRerenderTikz_Click(object sender, RoutedEventArgs e)
        {
            var paper = _currentPaper;
            if (paper == null || paper.DanhSachCauHoi.Count == 0)
            {
                MessageBox.Show("Chưa có đề nào. Hãy sinh đề hoặc nạp đề mẫu trước.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var tikzQuestions = paper.DanhSachCauHoi
                .Where(q => q.CoHinhTikz)
                .OrderBy(q => q.SoThuTu)
                .ToList();

            if (tikzQuestions.Count == 0)
            {
                MessageBox.Show("Đề hiện tại không có câu nào sử dụng hình vẽ (TikZ).", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var choice = MessageBox.Show(
                $"Đề hiện tại có {tikzQuestions.Count} câu sử dụng hình vẽ.\n\n" +
                "• Chọn «Có»: chỉ vẽ những hình còn thiếu (nhanh).\n" +
                "• Chọn «Không»: vẽ lại TẤT CẢ hình, kể cả hình đã có (dùng khi hình cũ bị sai).\n" +
                "• Chọn «Hủy»: không làm gì.",
                "Vẽ lại hình TikZ", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

            if (choice == MessageBoxResult.Cancel) return;
            bool forceAll = choice == MessageBoxResult.No;

            var settings = AppSettings.Load();
            string appDataTikzDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TikzCache");
            Directory.CreateDirectory(appDataTikzDir);

            // Những câu có hình nhưng chưa có mã TikZ -> nhờ AI sinh mã trước khi vẽ
            var missingCode = tikzQuestions.Any(q => string.IsNullOrWhiteSpace(q.MaTikz));
            var failed = new List<int>();

            BtnRerenderTikz.IsEnabled = false;
            PbLoading.Visibility = Visibility.Visible;

            try
            {
                if (missingCode || settings.BaoDamHinhChuan)
                {
                    TxtAiStatus.Text = settings.BaoDamHinhChuan
                        ? "Chế độ Bảo đảm hình chuẩn: đang kiểm tra và vẽ lại hình bằng bộ dựng chuẩn..."
                        : "Đang tạo mã TikZ cho các câu còn thiếu bằng AI...";
                    await EnsureSmartTikzCodesAsync(paper, settings);
                }

                int index = 0;
                foreach (var q in tikzQuestions)
                {
                    index++;

                    if (string.IsNullOrWhiteSpace(q.MaTikz))
                    {
                        failed.Add(q.SoThuTu);
                        continue;
                    }

                    string pngPath = Path.Combine(appDataTikzDir, TikzRenderService.GetCacheFileName(q.MaTikz));

                    if (!forceAll && File.Exists(pngPath))
                    {
                        q.AnhTikzPath = pngPath;
                        continue;
                    }

                    TxtAiStatus.Text = $"Đang vẽ lại hình Câu {q.SoThuTu} ({index}/{tikzQuestions.Count})...";

                    var renderResult = await TikzRenderService.RenderTikzToPngResultAsync(q.MaTikz, pngPath, settings.MiktexBinPath);
                    if (renderResult.Success)
                    {
                        q.AnhTikzPath = renderResult.Path;
                    }
                    else
                    {
                        q.AnhTikzPath = null;
                        failed.Add(q.SoThuTu);
                        AppLogger.Warn($"Vẽ lại hình Câu {q.SoThuTu} thất bại: {renderResult.Error}");
                    }
                }

                DisplayQuestionsPreview(paper);
                AutoSaveDraft();

                if (failed.Count > 0)
                {
                    string list = string.Join(", ", failed.OrderBy(x => x));
                    TxtAiStatus.Text = $"Đã vẽ lại hình: {tikzQuestions.Count - failed.Count}/{tikzQuestions.Count} câu thành công.";
                    MessageBox.Show(
                        $"Có {failed.Count} hình chưa vẽ được: Câu {list}.\n\n" +
                        "Hãy kiểm tra kết nối Internet (dùng dịch vụ vẽ online Kroki) hoặc cài MiKTeX, rồi thử lại.",
                        "Một số hình chưa vẽ được", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    TxtAiStatus.Text = $"Đã vẽ lại thành công toàn bộ {tikzQuestions.Count} hình TikZ.";
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("Lỗi khi vẽ lại toàn bộ hình TikZ", ex);
                MessageBox.Show($"Lỗi khi vẽ lại hình: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnRerenderTikz.IsEnabled = true;
                PbLoading.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateSampleButtonContent()
        {
            if (BtnLoadSamplePaper == null) return;
            int grade = GetSelectedGrade();
            BtnLoadSamplePaper.Content = $"Nạp Đề Mẫu Chuẩn Khối {grade}";
        }

        private void DataGridCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Chỉ bỏ đánh dấu vàng khi giáo viên thực sự đổi lựa chọn (ô đang có focus),
            // tránh việc bảng tái sử dụng dòng khi cuộn làm đổi giá trị binding cũng bị coi là sửa.
            if (e.RemovedItems.Count > 0
                && sender is ComboBox cb
                && (cb.IsKeyboardFocusWithin || cb.IsDropDownOpen)
                && cb.DataContext is MathMatrixItem item)
            {
                item.IsAiNew = false;
            }

            Dispatcher.BeginInvoke(new Action(() =>
            {
                UpdateSummary();
            }));
        }

        private void DgMatrix_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            ClearAiMarkOnCellEdit(e);
            Dispatcher.BeginInvoke(new Action(() =>
            {
                UpdateSummary();
            }));
        }

        /// <summary>
        /// Khi giáo viên sửa một dòng do AI tạo (đang tô vàng) thì bỏ đánh dấu vàng của dòng đó.
        /// Ô văn bản chỉ bỏ dấu khi giá trị thực sự thay đổi; ô Điểm/TikZ coi thao tác sửa là đã rà soát;
        /// ô ComboBox xử lý riêng ở DataGridCombo_SelectionChanged.
        /// </summary>
        private static void ClearAiMarkOnCellEdit(DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit) return;
            if (e.Row.Item is not MathMatrixItem item || !item.IsAiNew) return;
            if (e.EditingElement is ComboBox) return;

            if (e.EditingElement is TextBox tb && e.Column is DataGridTextColumn tc && tc.Binding is Binding b)
            {
                string path = b.Path?.Path ?? string.Empty;
                if (path == nameof(MathMatrixItem.NoiDungKienThuc) && tb.Text.Trim() == (item.NoiDungKienThuc ?? string.Empty).Trim()) return;
                if (path == nameof(MathMatrixItem.YeuCauCanDat) && tb.Text.Trim() == (item.YeuCauCanDat ?? string.Empty).Trim()) return;
            }

            item.IsAiNew = false;
        }

        private void UpdateSummary()
        {
            int totalQuestions = _matrixItems.Count;
            double totalPoints = _matrixItems.Sum(x => x.Diem);
            int tnCount = _matrixItems.Count(x => x.HinhThuc == "TN");
            double tnPoints = _matrixItems.Where(x => x.HinhThuc == "TN").Sum(x => x.Diem);
            int tlCount = _matrixItems.Count(x => x.HinhThuc == "TL");
            double tlPoints = _matrixItems.Where(x => x.HinhThuc == "TL").Sum(x => x.Diem);

            TxtSummary.Text = $"{totalQuestions} câu | {totalPoints:0.#} điểm ({tnCount} câu TN - {tnPoints:0.#}đ | {tlCount} câu TL - {tlPoints:0.#}đ)";

            double m1Pts = _matrixItems.Where(x => x.MucDo == "M1").Sum(x => x.Diem);
            double m2Pts = _matrixItems.Where(x => x.MucDo == "M2").Sum(x => x.Diem);
            double m3Pts = _matrixItems.Where(x => x.MucDo == "M3").Sum(x => x.Diem);

            double p1 = totalPoints > 0 ? (m1Pts / totalPoints) * 100 : 0;
            double p2 = totalPoints > 0 ? (m2Pts / totalPoints) * 100 : 0;
            double p3 = totalPoints > 0 ? (m3Pts / totalPoints) * 100 : 0;

            string accText = $"Phân bổ mức độ: M1: {m1Pts:0.#}đ ({p1:0}%) | M2: {m2Pts:0.#}đ ({p2:0}%) | M3: {m3Pts:0.#}đ ({p3:0}%)";

            if (BadgeM1Text != null) BadgeM1Text.Text = $"{m1Pts:0.#}đ ({p1:0}%)";
            if (BadgeM2Text != null) BadgeM2Text.Text = $"{m2Pts:0.#}đ ({p2:0}%)";
            if (BadgeM3Text != null) BadgeM3Text.Text = $"{m3Pts:0.#}đ ({p3:0}%)";

            bool isTotalValid = Math.Abs(totalPoints - 10.0) < 0.05;
            bool isRatioReasonable = p1 >= 30 && p1 <= 55 && p2 >= 20 && p2 <= 40 && p3 >= 15 && p3 <= 35;

            if (TxtAccreditation != null && BadgeAccreditation != null)
            {
                if (isTotalValid && isRatioReasonable)
                {
                    TxtAccreditation.Text = "Đạt chuẩn TT 27";
                    TxtAccreditation.Foreground = new SolidColorBrush(Color.FromRgb(22, 163, 74));
                    BadgeAccreditation.Background = new SolidColorBrush(Color.FromRgb(236, 253, 245));
                    BadgeAccreditation.BorderBrush = new SolidColorBrush(Color.FromRgb(167, 243, 208));
                    if (IcoAccreditation != null) IcoAccreditation.Stroke = new SolidColorBrush(Color.FromRgb(22, 163, 74));
                    BadgeAccreditation.ToolTip = "Đạt chuẩn Thông tư 27: tổng 10 điểm, tỉ lệ điểm theo mức nằm trong khung khuyến cáo (M1: 30-55%, M2: 20-40%, M3: 15-35%). Cấu trúc tham chiếu 4:4:2 (40% - 40% - 20%).";
                }
                else if (!isTotalValid)
                {
                    TxtAccreditation.Text = $"Chưa đủ 10đ ({totalPoints:0.#}đ)";
                    TxtAccreditation.Foreground = new SolidColorBrush(Color.FromRgb(220, 38, 38));
                    BadgeAccreditation.Background = new SolidColorBrush(Color.FromRgb(254, 242, 242));
                    BadgeAccreditation.BorderBrush = new SolidColorBrush(Color.FromRgb(254, 202, 202));
                    if (IcoAccreditation != null) IcoAccreditation.Stroke = new SolidColorBrush(Color.FromRgb(220, 38, 38));
                    BadgeAccreditation.ToolTip = $"Cảnh báo: Tổng điểm của ma trận hiện tại là {totalPoints:0.#}đ (chưa đạt chuẩn 10,0đ)";
                }
                else
                {
                    TxtAccreditation.Text = "Lệch tỷ lệ khuyến cáo";
                    TxtAccreditation.Foreground = new SolidColorBrush(Color.FromRgb(217, 119, 6));
                    BadgeAccreditation.Background = new SolidColorBrush(Color.FromRgb(255, 251, 235));
                    BadgeAccreditation.BorderBrush = new SolidColorBrush(Color.FromRgb(253, 230, 138));
                    if (IcoAccreditation != null) IcoAccreditation.Stroke = new SolidColorBrush(Color.FromRgb(217, 119, 6));
                    BadgeAccreditation.ToolTip = $"Lưu ý: tỉ lệ điểm theo mức đang lệch khung khuyến cáo Thông tư 27 (M1: 30-55%, M2: 20-40%, M3: 15-35%). Hiện tại: M1 {p1:0}%, M2 {p2:0}%, M3 {p3:0}%. Cấu trúc tham chiếu 4:4:2 (40% - 40% - 20%).";
                }
            }
        }

        private void BtnLoadDefaultMatrix_Click(object sender, RoutedEventArgs e)
        {
            LoadDefaultMatrix();
        }

        private async void BtnAiSuggestMatrix_Click(object sender, RoutedEventArgs e)
        {
            var settings = AppSettings.Load();
            if (string.IsNullOrWhiteSpace(settings.GeminiApiKey))
            {
                MessageBox.Show("Chưa thiết lập Gemini API Key. Vui lòng vào Cài đặt để cấu hình API Key.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int grade = GetSelectedGrade();
            string period = GetSelectedPeriod();
            var topics = MathTopicCatalog.GetTopics(grade, period);

            var dialog = new Window
            {
                Title = $"Gợi ý Ma trận & Bản đặc tả bằng AI - Toán {grade} ({period})",
                Width = 680,
                Height = 620,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this),
                ResizeMode = ResizeMode.CanResizeWithGrip,
                Background = new SolidColorBrush(Color.FromRgb(248, 250, 252))
            };

            var mainGrid = new Grid { Margin = new Thickness(16) };
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Hướng dẫn
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Nội dung
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Nút bấm

            var lblGuide = new TextBlock
            {
                Text = $"Chọn phương thức gợi ý cho đề {period} môn Toán lớp {grade} (sách Kết nối tri thức):",
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(lblGuide, 0);
            mainGrid.Children.Add(lblGuide);

            var contentStack = new StackPanel();

            // --- CÁCH 1: TÍCH CHỌN CHỦ ĐỀ SGK (KHUYÊN DÙNG) ---
            var rbSelectTopics = new RadioButton
            {
                Content = "Cách 1: Tích chọn chủ đề từ danh mục SGK Kết nối tri thức (Khuyên dùng)",
                IsChecked = true,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
                Margin = new Thickness(0, 0, 0, 6)
            };
            contentStack.Children.Add(rbSelectTopics);

            var topicsContainer = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10),
                Margin = new Thickness(20, 0, 0, 8)
            };

            var topicsInnerPanel = new Grid();
            topicsInnerPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            topicsInnerPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var toolbarPanel = new Grid { Margin = new Thickness(0, 0, 0, 6) };
            toolbarPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            toolbarPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var quickButtons = new StackPanel { Orientation = Orientation.Horizontal };
            var btnSelectAll = new Button
            {
                Content = "✓ Chọn tất cả",
                Padding = new Thickness(8, 3, 8, 3),
                FontSize = 11,
                Margin = new Thickness(0, 0, 6, 0),
                Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225))
            };
            var btnDeselectAll = new Button
            {
                Content = "✖ Bỏ chọn",
                Padding = new Thickness(8, 3, 8, 3),
                FontSize = 11,
                Margin = new Thickness(0, 0, 6, 0),
                Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225))
            };
            var btnDefaultTopics = new Button
            {
                Content = "⚡ Mặc định",
                Padding = new Thickness(8, 3, 8, 3),
                FontSize = 11,
                Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225))
            };
            quickButtons.Children.Add(btnSelectAll);
            quickButtons.Children.Add(btnDeselectAll);
            quickButtons.Children.Add(btnDefaultTopics);
            Grid.SetColumn(quickButtons, 0);
            toolbarPanel.Children.Add(quickButtons);

            var lblTopicCounter = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(14, 116, 144))
            };
            Grid.SetColumn(lblTopicCounter, 1);
            toolbarPanel.Children.Add(lblTopicCounter);
            Grid.SetRow(toolbarPanel, 0);
            topicsInnerPanel.Children.Add(toolbarPanel);

            var topicScrollViewer = new ScrollViewer
            {
                Height = 200,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };
            var topicsListPanel = new StackPanel();

            var checkBoxes = new List<(CheckBox cb, MathTopicItem topic)>();

            void UpdateCounter()
            {
                int checkedCount = checkBoxes.Count(x => x.cb.IsChecked == true);
                lblTopicCounter.Text = $"Đã chọn: {checkedCount}/{checkBoxes.Count} chủ đề";
            }

            btnSelectAll.Click += (s, ev) =>
            {
                foreach (var item in checkBoxes) item.cb.IsChecked = true;
                UpdateCounter();
            };
            btnDeselectAll.Click += (s, ev) =>
            {
                foreach (var item in checkBoxes) item.cb.IsChecked = false;
                UpdateCounter();
            };
            btnDefaultTopics.Click += (s, ev) =>
            {
                foreach (var item in checkBoxes) item.cb.IsChecked = true;
                UpdateCounter();
            };

            var grouped = topics.GroupBy(t => t.MachKienThuc);
            foreach (var group in grouped)
            {
                var groupHeader = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(6, 3, 6, 3),
                    Margin = new Thickness(0, 6, 0, 3)
                };
                var groupTitle = new TextBlock
                {
                    Text = $"📌 {group.Key} ({group.Count()} chủ đề)",
                    FontWeight = FontWeights.Bold,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59))
                };
                groupHeader.Child = groupTitle;
                topicsListPanel.Children.Add(groupHeader);

                foreach (var t in group)
                {
                    var cb = new CheckBox
                    {
                        Margin = new Thickness(8, 2, 0, 2),
                        IsChecked = t.DaXacNhan,
                        VerticalContentAlignment = VerticalAlignment.Center
                    };
                    var tb = new TextBlock
                    {
                        Text = $"{(t.DaXacNhan ? "✓ ĐÃ XÁC NHẬN" : "⚠ CẦN RÀ SOÁT")} [Tuần {t.Tuan}] {t.ChuDe}: {t.NoiDung}",
                        TextWrapping = TextWrapping.Wrap,
                        FontSize = 12,
                        Foreground = new SolidColorBrush(Color.FromRgb(51, 65, 85))
                    };
                    cb.ToolTip = $"Mã: {t.MaMuc}{Environment.NewLine}Nguồn SGK: {t.NguonThamKhao}{Environment.NewLine}{t.CanCuTuan}";
                    cb.Content = tb;
                    cb.Checked += (s, ev) => UpdateCounter();
                    cb.Unchecked += (s, ev) => UpdateCounter();

                    checkBoxes.Add((cb, t));
                    topicsListPanel.Children.Add(cb);
                }
            }
            UpdateCounter();

            topicScrollViewer.Content = topicsListPanel;
            Grid.SetRow(topicScrollViewer, 1);
            topicsInnerPanel.Children.Add(topicScrollViewer);

            topicsContainer.Child = topicsInnerPanel;
            contentStack.Children.Add(topicsContainer);

            // --- CÁCH 2: AI TỰ CHỌN CHỦ ĐỀ THEO SGK ---
            var rbAuto = new RadioButton
            {
                Content = "Cách 2: Để AI tự chọn chủ đề theo SGK Kết nối tri thức",
                FontSize = 13,
                Margin = new Thickness(0, 4, 0, 0),
                Foreground = new SolidColorBrush(Color.FromRgb(51, 65, 85))
            };
            contentStack.Children.Add(rbAuto);

            var lblAutoDesc = new TextBlock
            {
                Text = $"AI sẽ bám phân phối chương trình đợt \"{period}\", tự chọn các chủ đề trọng tâm cho đề Toán lớp {grade} (thầy/cô không cần nhập gì), phủ đủ 3 mạch kiến thức và giữ nguyên cấu trúc chuẩn (10 câu, 7 TN + 3 TL, M1/M2/M3 = 4:4:2).",
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12,
                FontStyle = FontStyles.Italic,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                Margin = new Thickness(24, 2, 0, 8)
            };
            contentStack.Children.Add(lblAutoDesc);

            // --- CÁCH 3: TỰ NHẬP ĐỊNH HƯỚNG ---
            var rbManual = new RadioButton
            {
                Content = "Cách 3: Thầy/cô tự nhập định hướng kiểm tra / yêu cầu riêng",
                FontSize = 13,
                Margin = new Thickness(0, 4, 0, 0),
                Foreground = new SolidColorBrush(Color.FromRgb(51, 65, 85))
            };
            contentStack.Children.Add(rbManual);

            var txtInput = new TextBox
            {
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                FontSize = 12,
                Padding = new Thickness(8),
                Height = 65,
                Margin = new Thickness(24, 4, 0, 0),
                Text = $"Đề kiểm tra {period} môn Toán lớp {grade} sách Kết nối tri thức. Đảm bảo cấu trúc 10 câu (7 TN, 3 TL), tổng điểm 10 điểm, phân bổ M1 40%, M2 30%, M3 30%. Có câu hỏi hình vẽ trực quan.",
                BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                IsEnabled = false,
                Opacity = 0.5
            };
            contentStack.Children.Add(txtInput);

            // Event toggles
            rbSelectTopics.Checked += (s, ev) =>
            {
                topicsContainer.IsEnabled = true;
                topicsContainer.Opacity = 1.0;
                txtInput.IsEnabled = false;
                txtInput.Opacity = 0.5;
            };
            rbAuto.Checked += (s, ev) =>
            {
                topicsContainer.IsEnabled = false;
                topicsContainer.Opacity = 0.5;
                txtInput.IsEnabled = false;
                txtInput.Opacity = 0.5;
            };
            rbManual.Checked += (s, ev) =>
            {
                topicsContainer.IsEnabled = false;
                topicsContainer.Opacity = 0.5;
                txtInput.IsEnabled = true;
                txtInput.Opacity = 1.0;
            };

            Grid.SetRow(contentStack, 1);
            mainGrid.Children.Add(contentStack);

            // Row 2: Nút bấm
            var btnPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 12, 0, 0)
            };

            var btnCancel = new Button
            {
                Content = "Đóng",
                Padding = new Thickness(14, 6, 14, 6),
                Margin = new Thickness(0, 0, 8, 0),
                Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                Foreground = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225))
            };
            btnCancel.Click += (s, ev) => dialog.DialogResult = false;
            btnPanel.Children.Add(btnCancel);

            var btnOk = new Button
            {
                Content = "Bắt đầu gợi ý",
                Padding = new Thickness(16, 6, 16, 6),
                Background = new SolidColorBrush(Color.FromRgb(22, 163, 74)),
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                BorderThickness = new Thickness(0)
            };
            btnOk.Click += (s, ev) =>
            {
                if (rbSelectTopics.IsChecked == true)
                {
                    var selected = checkBoxes.Where(x => x.cb.IsChecked == true).Select(x => x.topic).ToList();
                    int checkedCount = selected.Count;
                    if (checkedCount == 0)
                    {
                        MessageBox.Show("Thầy/cô chưa chọn chủ đề nào. Vui lòng tích chọn ít nhất 1 chủ đề hoặc bấm '✓ Chọn tất cả'.", "Chưa chọn chủ đề", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var pendingReview = selected.Where(x => !x.DaXacNhan).ToList();
                    if (pendingReview.Count > 0)
                    {
                        string reviewList = string.Join(Environment.NewLine, pendingReview.Select(x =>
                            $"• Tuần {x.Tuan}: {x.ChuDe} — {x.NoiDung}{Environment.NewLine}  Căn cứ tuần: {x.CanCuTuan}{Environment.NewLine}  Nguồn: {x.NguonThamKhao}"));
                        var review = MessageBox.Show(
                            "Các mục sau chưa được xác nhận. Hãy đối chiếu tuần và nội dung với phân phối bài/tiết của trường trước khi xác nhận. Việc xác nhận được lưu để dùng cho lần sau:\n\n" + reviewList + "\n\nThầy/cô xác nhận các mục này đủ điều kiện dùng cho kỳ đang chọn?",
                            "Rà soát danh mục Kết nối tri thức",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);
                        if (review != MessageBoxResult.Yes) return;
                        if (!CurriculumReviewStore.Confirm(pendingReview, out string reviewError))
                        {
                            MessageBox.Show(reviewError, "Không lưu được xác nhận", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        foreach (var topic in pendingReview) topic.DaXacNhan = true;
                    }
                }
                else if (CurriculumScopePolicy.GetConfirmedMathTopics(grade, period).Count == 0)
                {
                    MessageBox.Show("Chưa có mục chương trình Toán nào được xác nhận cho kỳ này. Hãy chọn 'Tích chọn chủ đề', rà soát nguồn/tuần và xác nhận ít nhất một mục trước khi dùng AI tự chọn hoặc nhập yêu cầu.", "Danh mục chưa được rà soát", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                dialog.DialogResult = true;
            };
            btnPanel.Children.Add(btnOk);

            Grid.SetRow(btnPanel, 2);
            mainGrid.Children.Add(btnPanel);

            dialog.Content = mainGrid;

            if (dialog.ShowDialog() == true)
            {
                bool selectTopicsMode = rbSelectTopics.IsChecked == true;
                bool autoMode = rbAuto.IsChecked == true;
                List<string>? selectedTopicsList = selectTopicsMode
                    ? checkBoxes.Where(x => x.cb.IsChecked == true).Select(x => x.topic.MaMuc).ToList()
                    : null;
                string requirement = (rbManual.IsChecked == true) ? txtInput.Text.Trim() : string.Empty;

                // Chụp lại ma trận hiện tại (nếu có) để có thể khôi phục sau khi AI thay thế.
                var preAiItems = _matrixItems.Count > 0 ? CloneMatrixItems(_matrixItems) : null;

                SetAiBusyUi(true);
                if (selectTopicsMode)
                {
                    TxtSummary.Text = $"Đang để AI lập Ma trận theo {selectedTopicsList!.Count} chủ đề đã chọn ({settings.ActiveProviderDisplayName})...";
                }
                else if (autoMode)
                {
                    TxtSummary.Text = $"Đang để AI tự chọn chủ đề SGK và lập Ma trận ({settings.ActiveProviderDisplayName})...";
                }
                else
                {
                    TxtSummary.Text = $"Đang gửi yêu cầu và nhận gợi ý Ma trận từ AI ({settings.ActiveProviderDisplayName})...";
                }

                try
                {
                    var gemini = new GeminiService();
                    var suggestedItems = await gemini.SuggestMathMatrixAsync(
                        settings,
                        grade,
                        period,
                        requirement,
                        autoMode,
                        selectedTopicsList
                    );

                    var (addedCount, sameCount, removedCount) = ApplyAiDiffMarks(suggestedItems, preAiItems);

                    _matrixItems = new ObservableCollection<MathMatrixItem>(suggestedItems);
                    DgMatrix.ItemsSource = _matrixItems;
                    UpdateSummary();

                    // Bật nút "Khôi phục bản trước AI" nếu trước đó đã có ma trận để quay lại.
                    _preAiMatrixSnapshot = preAiItems;
                    BtnRestorePreAiMatrix.IsEnabled = _preAiMatrixSnapshot is { Count: > 0 };

                    string topicNote = string.Empty;
                    if (selectTopicsMode)
                    {
                        topicNote = $"\n\nĐã bám sát {selectedTopicsList!.Count} chủ đề SGK Kết nối tri thức do thầy/cô tích chọn.";
                    }
                    else if (autoMode)
                    {
                        var picked = suggestedItems
                            .Select(i => (i.NoiDungKienThuc ?? string.Empty).Trim())
                            .Where(t => t.Length > 0)
                            .Distinct()
                            .Take(8)
                            .ToList();
                        if (picked.Count > 0)
                        {
                            topicNote = "\n\nAI đã tự chọn các nội dung chính: " + string.Join("; ", picked) + ".";
                        }
                    }

                    string changeNote = preAiItems == null
                        ? $"\n\nToàn bộ {_matrixItems.Count} dòng đều là nội dung mới (đã tô vàng để thầy/cô rà soát)."
                        : $"\n\nSo với bản trước: {addedCount} dòng mới (tô vàng), {sameCount} dòng trùng nội dung cũ, {removedCount} dòng cũ không còn trong bản mới.";

                    string restoreHint = BtnRestorePreAiMatrix.IsEnabled
                        ? "\n\nNếu chưa hài lòng, bấm \"Khôi phục bản trước AI\" ở thanh công cụ để quay lại bản cũ."
                        : string.Empty;

                    // Nháy biểu tượng trên taskbar nếu thầy/cô đang làm việc ở cửa sổ khác.
                    TaskbarFlash.Flash(Window.GetWindow(this));

                    MessageBox.Show($"Đã tạo thành công gợi ý Ma trận và Bản đặc tả ({_matrixItems.Count} câu) bám sát chương trình Kết nối tri thức!{topicNote}{changeNote}{restoreHint}", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi tạo gợi ý ma trận: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    UpdateSummary();
                }
                finally
                {
                    SetAiBusyUi(false);
                }
            }
        }

        /// <summary>Tắt/mở giao diện trong lúc AI đang xử lý (khóa sửa bảng, hiện thanh chạy, đổi chữ nút).</summary>
        private void SetAiBusyUi(bool busy)
        {
            BtnAiSuggestMatrix.IsEnabled = !busy;
            DgMatrix.IsReadOnly = busy;
            BtnAddRow.IsEnabled = !busy;
            BtnDeleteRow.IsEnabled = !busy;
            PbMatrixLoading.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;

            if (BtnAiSuggestMatrix.Content is StackPanel sp && sp.Children.Count > 1 && sp.Children[1] is TextBlock tb)
            {
                tb.Text = busy ? "Đang tạo..." : "AI Gợi ý ma trận";
            }
        }

        /// <summary>Xóa bản "trước AI" và khóa nút khôi phục (dùng khi ma trận bị thay bằng bản khác ngoài AI).</summary>
        private void ClearPreAiSnapshot()
        {
            _preAiMatrixSnapshot = null;
            if (BtnRestorePreAiMatrix != null) BtnRestorePreAiMatrix.IsEnabled = false;
        }

        private static List<MathMatrixItem> CloneMatrixItems(IEnumerable<MathMatrixItem> items)
        {
            return items.Select(x => new MathMatrixItem
            {
                SoThuTu = x.SoThuTu,
                MaMucChuongTrinh = x.MaMucChuongTrinh,
                ChuDe = x.ChuDe,
                NoiDungKienThuc = x.NoiDungKienThuc,
                YeuCauCanDat = x.YeuCauCanDat,
                MucDo = x.MucDo,
                HinhThuc = x.HinhThuc,
                Diem = x.Diem,
                CoHinhTikz = x.CoHinhTikz
            }).ToList();
        }

        private static string AiMatchKey(MathMatrixItem item)
        {
            return ((item.ChuDe ?? string.Empty).Trim() + "|" + (item.NoiDungKienThuc ?? string.Empty).Trim()).ToLowerInvariant();
        }

        /// <summary>
        /// So bản AI mới với bản trước đó, đánh dấu vàng các dòng chưa từng có nội dung tương ứng.
        /// Trả về (số dòng mới, số dòng trùng nội dung cũ, số dòng cũ không còn trong bản mới).
        /// </summary>
        private static (int Added, int Same, int Removed) ApplyAiDiffMarks(
            IList<MathMatrixItem> newItems, List<MathMatrixItem>? oldItems)
        {
            var remainingOldKeys = oldItems?.Select(AiMatchKey).ToList() ?? new List<string>();
            int added = 0, same = 0;

            foreach (var item in newItems)
            {
                string key = AiMatchKey(item);
                int idx = remainingOldKeys.IndexOf(key);
                if (idx >= 0)
                {
                    remainingOldKeys.RemoveAt(idx);
                    same++;
                    item.IsAiNew = false;
                }
                else
                {
                    added++;
                    item.IsAiNew = true;
                }
            }

            return (added, same, remainingOldKeys.Count);
        }

        private void BtnRestorePreAiMatrix_Click(object sender, RoutedEventArgs e)
        {
            if (_preAiMatrixSnapshot is not { Count: > 0 })
            {
                MessageBox.Show("Không có bản ma trận nào trước AI để khôi phục.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                ClearPreAiSnapshot();
                return;
            }

            var res = MessageBox.Show(
                $"Khôi phục lại ma trận ngay trước lần AI gợi ý gần nhất ({_preAiMatrixSnapshot.Count} dòng)?\n\nBảng hiện tại (kể cả các chỉnh sửa sau đó) sẽ bị thay thế bằng bản cũ.",
                "Khôi phục bản trước AI", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res != MessageBoxResult.Yes) return;

            _matrixItems = new ObservableCollection<MathMatrixItem>(_preAiMatrixSnapshot);
            DgMatrix.ItemsSource = _matrixItems;
            ClearPreAiSnapshot();
            UpdateSummary();

            MessageBox.Show($"Đã khôi phục ma trận trước AI ({_matrixItems.Count} dòng).", "Đã khôi phục", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void BtnAuditMatrix_Click(object sender, RoutedEventArgs e)
        {
            if (_matrixItems.Count == 0)
            {
                MessageBox.Show("Ma trận hiện chưa có câu hỏi nào để thẩm định.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var settings = AppSettings.Load();
            if (string.IsNullOrWhiteSpace(settings.GeminiApiKey))
            {
                MessageBox.Show("Chưa thiết lập Gemini API Key. Vui lòng vào Cài đặt để cấu hình API Key.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int grade = GetSelectedGrade();
            string period = GetSelectedPeriod();

            BtnAuditMatrix.IsEnabled = false;
            TxtSummary.Text = $"Đang gửi Ma trận lên AI ({settings.ActiveProviderDisplayName}) để thẩm định chuyên môn...";

            try
            {
                var gemini = new GeminiService();
                string auditReport = await gemini.AuditMathMatrixAsync(
                    settings,
                    grade,
                    period,
                    _matrixItems.ToList()
                );

                var reportWindow = new Window
                {
                    Title = $"Báo cáo thẩm định chuyên môn Ma trận & Đặc tả - Toán {grade} ({period})",
                    Width = 720,
                    Height = 520,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = Window.GetWindow(this),
                    Background = new SolidColorBrush(Color.FromRgb(248, 250, 252))
                };

                var rootGrid = new Grid { Margin = new Thickness(16) };
                rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var header = new TextBlock
                {
                    Text = $"KẾT QUẢ THẨM ĐỊNH CHUYÊN MÔN THEO THÔNG TƯ 27 VÀ CHƯƠNG TRÌNH 2018\nToán Lớp {grade} - {period} (Sách Kết nối tri thức)",
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
                    Margin = new Thickness(0, 0, 0, 10),
                    TextAlignment = TextAlignment.Center
                };
                Grid.SetRow(header, 0);
                rootGrid.Children.Add(header);

                var rtb = new RichTextBox
                {
                    Document = WpfMarkdownRenderer.RenderToFlowDocument(auditReport),
                    IsReadOnly = true,
                    IsDocumentEnabled = true,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    BorderThickness = new Thickness(0),
                    Background = Brushes.White
                };
                var borderReport = new Border
                {
                    Background = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Child = rtb
                };
                Grid.SetRow(borderReport, 1);
                rootGrid.Children.Add(borderReport);

                var bottomBar = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 12, 0, 0)
                };

                var btnCopy = new Button
                {
                    Content = "Sao chép báo cáo",
                    Padding = new Thickness(14, 6, 14, 6),
                    Margin = new Thickness(0, 0, 8, 0),
                    Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                    Foreground = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225))
                };
                btnCopy.Click += (s, ev) =>
                {
                    Clipboard.SetText(auditReport);
                    MessageBox.Show("Đã sao chép nội dung thẩm định vào bộ nhớ tạm.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                };
                bottomBar.Children.Add(btnCopy);

                var btnClose = new Button
                {
                    Content = "Đóng",
                    Padding = new Thickness(16, 6, 16, 6),
                    Background = new SolidColorBrush(Color.FromRgb(79, 70, 229)),
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.SemiBold,
                    BorderThickness = new Thickness(0)
                };
                btnClose.Click += (s, ev) => reportWindow.Close();
                bottomBar.Children.Add(btnClose);

                Grid.SetRow(bottomBar, 2);
                rootGrid.Children.Add(bottomBar);

                reportWindow.Content = rootGrid;
                reportWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi thẩm định ma trận: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnAuditMatrix.IsEnabled = true;
                UpdateSummary();
            }
        }

        private void BtnAddRow_Click(object sender, RoutedEventArgs e)
        {
            int nextStt = _matrixItems.Count + 1;
            _matrixItems.Add(new MathMatrixItem
            {
                SoThuTu = nextStt,
                ChuDe = "Số và phép tính",
                NoiDungKienThuc = "Phân số",
                YeuCauCanDat = "Nhận biết phân số",
                MucDo = "M1",
                HinhThuc = "TN",
                Diem = 1.0,
                CoHinhTikz = false
            });
            UpdateSummary();
            AutoSaveDraft();
        }

        private void BtnDeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (DgMatrix.SelectedItem is MathMatrixItem selected)
            {
                _matrixItems.Remove(selected);
                // Re-index
                for (int i = 0; i < _matrixItems.Count; i++)
                {
                    _matrixItems[i].SoThuTu = i + 1;
                }
                DgMatrix.Items.Refresh();
                UpdateSummary();
                AutoSaveDraft();
            }
            else
            {
                MessageBox.Show("Vui lòng chọn dòng cần xóa trong bảng.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Step1_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            TabMath.SelectedIndex = 0;
        }

        private void Step2_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            TabMath.SelectedIndex = 1;
        }

        private void Step3_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            TabMath.SelectedIndex = 2;
        }

        private void TabMath_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source != TabMath) return;
            UpdateStepperVisuals(TabMath.SelectedIndex);
            if (TabMath.SelectedIndex == 2)
            {
                UpdateOlySectionHeader();
            }
        }

        private void UpdateStepperVisuals(int stepIndex)
        {
            if (Step1Border == null || Step2Border == null || Step3Border == null) return;

            var activeBg = new SolidColorBrush(Color.FromRgb(238, 242, 255));
            var activeBorder = new SolidColorBrush(Color.FromRgb(199, 210, 254));
            var activeBadgeBg = new SolidColorBrush(Color.FromRgb(79, 70, 229));
            var activeSubFg = new SolidColorBrush(Color.FromRgb(79, 70, 229));
            var activeTitleFg = new SolidColorBrush(Color.FromRgb(15, 23, 42));

            var inactiveBg = Brushes.Transparent;
            var inactiveBorder = new SolidColorBrush(Color.FromRgb(226, 232, 240));
            var inactiveBadgeBg = new SolidColorBrush(Color.FromRgb(241, 245, 249));
            var inactiveSubFg = new SolidColorBrush(Color.FromRgb(100, 116, 139));
            var inactiveTitleFg = new SolidColorBrush(Color.FromRgb(71, 85, 105));

            SetStepVisual(Step1Border, Step1Badge, Step1Sub, Step1Title, stepIndex == 0, activeBg, activeBorder, activeBadgeBg, activeSubFg, activeTitleFg, inactiveBg, inactiveBorder, inactiveBadgeBg, inactiveSubFg, inactiveTitleFg);
            SetStepVisual(Step2Border, Step2Badge, Step2Sub, Step2Title, stepIndex == 1, activeBg, activeBorder, activeBadgeBg, activeSubFg, activeTitleFg, inactiveBg, inactiveBorder, inactiveBadgeBg, inactiveSubFg, inactiveTitleFg);
            SetStepVisual(Step3Border, Step3Badge, Step3Sub, Step3Title, stepIndex == 2, activeBg, activeBorder, activeBadgeBg, activeSubFg, activeTitleFg, inactiveBg, inactiveBorder, inactiveBadgeBg, inactiveSubFg, inactiveTitleFg);
        }

        private void SetStepVisual(Border border, Border badge, TextBlock sub, TextBlock title, bool isActive,
            Brush aBg, Brush aBrd, Brush aBdg, Brush aSub, Brush aTitle,
            Brush iBg, Brush iBrd, Brush iBdg, Brush iSub, Brush iTitle)
        {
            border.Background = isActive ? aBg : iBg;
            border.BorderBrush = isActive ? aBrd : iBrd;
            border.BorderThickness = new Thickness(isActive ? 1.5 : 1);
            badge.Background = isActive ? aBdg : iBdg;
            if (badge.Child is TextBlock tb)
            {
                tb.Foreground = isActive ? Brushes.White : new SolidColorBrush(Color.FromRgb(100, 116, 139));
            }
            sub.Foreground = isActive ? aSub : iSub;
            title.Foreground = isActive ? aTitle : iTitle;
        }

        private void BtnGoToStep2_Click(object sender, RoutedEventArgs e)
        {
            TabMath.SelectedIndex = 1;
        }

        private void BtnGoToStep3_Click(object sender, RoutedEventArgs e)
        {
            TabMath.SelectedIndex = 2;
            UpdateExportFolderHint();
            UpdatePaperPointsBadge();
            UpdateOlySectionHeader();
        }

        private void UpdateOlySectionHeader()
        {
            if (TxtOlySectionTitle == null) return;
            if (_currentPaper?.DanhSachCauHoi != null && _currentPaper.DanhSachCauHoi.Count > 0)
            {
                var essayQuestions = _currentPaper.DanhSachCauHoi
                    .Where(q => WordExportMathService.IsQuestionEssay(q))
                    .OrderBy(q => q.SoThuTu)
                    .ToList();

                if (essayQuestions.Count > 0)
                {
                    string qList = string.Join(", ", essayQuestions.Select(x => $"Câu {x.SoThuTu}"));
                    TxtOlySectionTitle.Text = $"TÙY CHỌN LƯỚI Ô LY BÀI GIẢI CÁC CÂU TỰ LUẬN ({qList})";
                    return;
                }
            }
            TxtOlySectionTitle.Text = "TÙY CHỌN LƯỚI Ô LY BÀI GIẢI CÁC CÂU TỰ LUẬN";
        }

        private async void BtnLoadSamplePaper_Click(object sender, RoutedEventArgs e)
        {
            int grade = GetSelectedGrade();
            string selectedPeriod = GetSelectedPeriod();
            if (!TH_ExamGen.Core.ExamPeriodHelper.IsFinalTerm2(selectedPeriod))
            {
                MessageBox.Show("Đề mẫu hiện tại được biên soạn cho cuối học kỳ II. Chọn đúng kỳ này để xem mẫu; đề mẫu không được chuyển sang kỳ khác.", "Không đúng kỳ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            BtnLoadSamplePaper.IsEnabled = false;
            TxtAiStatus.Text = $"Đang nạp đề mẫu chuẩn Khối {grade} và biên dịch hình vẽ TikZ...";
            PbLoading.Visibility = Visibility.Visible;

            try
            {
                _currentPaper = grade == 5 ? MathExamPaper.GetSampleGrade5Paper() : MathExamPaper.GetSampleGrade4Paper();
                string period = selectedPeriod;
                _currentPaper.Period = period;
                _currentPaper.TieuDe = $"BÀI KIỂM TRA ĐỊNH KỲ {period.ToUpper()}";
                string periodClean = period.Replace(" ", "_");
                if (string.IsNullOrWhiteSpace(TxtExportFileName.Text) || TxtExportFileName.Text.StartsWith("De_Kiem_Tra_Toan_Lop_"))
                {
                    TxtExportFileName.Text = $"De_Kiem_Tra_Toan_Lop_{grade}_{periodClean}.docx";
                }

                var settings = AppSettings.Load();
                string appDataTikzDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TikzCache");
                Directory.CreateDirectory(appDataTikzDir);

                foreach (var q in _currentPaper.DanhSachCauHoi)
                {
                    if (q.CoHinhTikz && !string.IsNullOrWhiteSpace(q.MaTikz))
                    {
                        string pngPath = Path.Combine(appDataTikzDir, TikzRenderService.GetCacheFileName(q.MaTikz));
                        var renderResult = await TikzRenderService.RenderTikzToPngResultAsync(q.MaTikz, pngPath, settings.MiktexBinPath);
                        if (renderResult.Success)
                        {
                            q.AnhTikzPath = renderResult.Path;
                        }
                        else
                        {
                            q.AnhTikzPath = null;
                            AppLogger.Warn($"Đề mẫu: không vẽ được hình Câu {q.SoThuTu}: {renderResult.Error}");
                        }
                    }
                }

                DisplayQuestionsPreview(_currentPaper);
                TxtAiStatus.Text = $"Đã nạp đề mẫu lớp {grade} cuối học kỳ II để tham khảo. Đây là nội dung minh họa chưa gắn mục chương trình đã duyệt nên không thể sinh lại hoặc xuất cho tới khi được lập ma trận và kiểm tra theo danh mục hiện hành.";
                AutoSaveDraft();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi nạp đề mẫu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtAiStatus.Text = "Lỗi khi nạp đề mẫu.";
            }
            finally
            {
                BtnLoadSamplePaper.IsEnabled = true;
                PbLoading.Visibility = Visibility.Collapsed;
            }
        }

        private async void BtnGenerateExam_Click(object sender, RoutedEventArgs e)
        {
            if (!LicenseManager.CanUseSoftware(out string licenseErr))
            {
                MessageBox.Show(licenseErr, "Thông báo Bản Quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
                var licenseWin = new LicenseWindow { Owner = Window.GetWindow(this) };
                licenseWin.ShowDialog();
                if (Window.GetWindow(this) is MainWindow mw) mw.UpdateStatusInfo();
                return;
            }

            var settings = AppSettings.Load();
            if (string.IsNullOrWhiteSpace(settings.ActiveApiKey))
            {
                MessageBox.Show($"Chưa thiết lập {settings.ActiveProviderDisplayName} API Key!\nVui lòng vào mục 'Cài đặt' để nhập API Key của bạn trước.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _mathGenCts?.Dispose();
            _mathGenCts = new System.Threading.CancellationTokenSource();
            var ct = _mathGenCts.Token;

            BtnGenerateExam.IsEnabled = false;
            BtnCancelGenerate.IsEnabled = true;
            BtnCancelGenerate.Visibility = Visibility.Visible;
            PbLoading.Visibility = Visibility.Visible;
            TxtAiStatus.Text = $"Đang gửi Ma trận đặc tả lên AI ({settings.ActiveProviderDisplayName}) và tạo câu hỏi...";
            AppLogger.Info($"Bắt đầu sinh đề Toán lớp {GetSelectedGrade()} bằng {settings.ActiveProviderDisplayName}/{settings.ActiveModel}.");

            var failedTikzQuestions = new List<int>();

            try
            {
                var gemini = new GeminiService();
                int grade = GetSelectedGrade();
                string period = GetSelectedPeriod();
                _currentPaper = await gemini.GenerateMathExamAsync(
                    settings,
                    grade,
                    40,
                    _matrixItems.ToList(),
                    period,
                    ct
                );
                _currentPaper.TieuDe = $"BÀI KIỂM TRA ĐỊNH KỲ {period.ToUpper()}";
                _currentPaper.KhoiLop = grade;
                _currentPaper.Period = period;
                _currentPaper.DanhSachMaTran = _matrixItems.ToList();
                string periodClean = period.Replace(" ", "_");
                if (string.IsNullOrWhiteSpace(TxtExportFileName.Text) || TxtExportFileName.Text.StartsWith("De_Kiem_Tra_Toan_Lop_"))
                {
                    TxtExportFileName.Text = $"De_Kiem_Tra_Toan_Lop_{grade}_{periodClean}.docx";
                }

                if (_currentPaper.CanhBaoKiemTra.Count > 0)
                {
                    MessageBox.Show(
                        "Đề Toán đã được sinh nhưng có điểm cần giáo viên rà soát:\n\n- " + string.Join("\n- ", _currentPaper.CanhBaoKiemTra) +
                        "\n\nBạn vẫn có thể xem trước và xuất Word; nên kiểm tra kỹ các câu liên quan (mã mục, hình thức câu) trước khi dùng.",
                        "Đề sinh kèm cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                ct.ThrowIfCancellationRequested();
                TxtAiStatus.Text = "Đang kiểm tra và sinh mã TikZ thông minh từ đề bài...";

                // Đảm bảo các câu có hình luôn có mã TikZ chuẩn theo đúng đề bài
                await EnsureSmartTikzCodesAsync(_currentPaper, settings, ct);

                // Render TikZ if available
                string appDataTikzDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TikzCache");
                Directory.CreateDirectory(appDataTikzDir);

                foreach (var q in _currentPaper.DanhSachCauHoi)
                {
                    ct.ThrowIfCancellationRequested();

                    if (!q.CoHinhTikz || string.IsNullOrWhiteSpace(q.MaTikz)) continue;

                    string pngPath = Path.Combine(appDataTikzDir, TikzRenderService.GetCacheFileName(q.MaTikz));

                    // Nếu ảnh của mã hiện tại đã có trong cache thì dùng luôn, khỏi vẽ lại
                    if (File.Exists(pngPath))
                    {
                        q.AnhTikzPath = pngPath;
                        continue;
                    }

                    var renderResult = await TikzRenderService.RenderTikzToPngResultAsync(q.MaTikz, pngPath, settings.MiktexBinPath);

                    // Cơ chế tự sửa (Self-healing): Nếu biên dịch lần đầu thất bại, yêu cầu AI chuyên biệt sinh lại và biên dịch lần 2
                    if (!renderResult.Success && !string.IsNullOrWhiteSpace(settings.ActiveApiKey))
                    {
                        TxtAiStatus.Text = $"Câu {q.SoThuTu} chưa vẽ được hình, đang sinh lại bằng AI chuyên biệt...";
                        string fallbackTikz = await TikzSpecialistService.GenerateTikzForQuestionAsync(settings, q, _currentPaper?.KhoiLop ?? 0);
                        if (!string.IsNullOrWhiteSpace(fallbackTikz) && fallbackTikz != q.MaTikz)
                        {
                            q.MaTikz = fallbackTikz;
                            pngPath = Path.Combine(appDataTikzDir, TikzRenderService.GetCacheFileName(q.MaTikz));
                            renderResult = await TikzRenderService.RenderTikzToPngResultAsync(q.MaTikz, pngPath, settings.MiktexBinPath);
                        }
                    }

                    if (renderResult.Success)
                    {
                        q.AnhTikzPath = renderResult.Path;
                    }
                    else
                    {
                        q.AnhTikzPath = null;
                        failedTikzQuestions.Add(q.SoThuTu);
                        AppLogger.Warn($"Không vẽ được hình TikZ cho Câu {q.SoThuTu}: {renderResult.Error}");
                    }
                }

                DisplayQuestionsPreview(_currentPaper!);
                TxtAiStatus.Text = "Đã sinh đề Toán hoàn chỉnh 10 câu thành công!";
                LicenseManager.IncrementTrialCount();
                if (Window.GetWindow(this) is MainWindow mwSuccess) mwSuccess.UpdateStatusInfo();
                AutoSaveDraft();
            }
            catch (OperationCanceledException)
            {
                TxtAiStatus.Text = "Đã hủy sinh đề theo yêu cầu.";
                AppLogger.Info("Người dùng đã hủy sinh đề Toán.");
            }
            catch (AiServiceException ex)
            {
                AppLogger.Error("Lỗi dịch vụ AI khi sinh đề Toán", ex);
                MessageBox.Show(ex.Message, "Không sinh được đề", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtAiStatus.Text = "Không sinh được đề do lỗi từ dịch vụ AI.";
            }
            catch (Exception ex)
            {
                AppLogger.Error("Lỗi sinh đề Toán", ex);
                MessageBox.Show($"Lỗi trong quá trình sinh đề: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtAiStatus.Text = "Quá trình sinh đề gặp sự cố. Vui lòng kiểm tra API Key hoặc thử lại.";
            }
            finally
            {
                BtnGenerateExam.IsEnabled = true;
                BtnCancelGenerate.IsEnabled = true;
                BtnCancelGenerate.Visibility = Visibility.Collapsed;
                PbLoading.Visibility = Visibility.Collapsed;
            }

            // Cảnh báo sau khi sinh xong: những câu không vẽ được hình
            if (failedTikzQuestions.Count > 0)
            {
                string list = string.Join(", ", failedTikzQuestions.OrderBy(x => x));
                MessageBox.Show(
                    $"Đã sinh đề xong, nhưng có {failedTikzQuestions.Count} câu chưa vẽ được hình: Câu {list}.\n\n" +
                    "Bạn có thể mở từng câu và bấm 'Vẽ lại từ đề (AI)' hoặc 'Biên dịch lại' sau khi kiểm tra mạng / cài MiKTeX.\n" +
                    "Nếu không cài MiKTeX, hãy chắc chắn máy có Internet để dùng dịch vụ vẽ online Kroki.",
                    "Một số hình chưa vẽ được",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private static async System.Threading.Tasks.Task EnsureSmartTikzCodesAsync(MathExamPaper paper, AppSettings settings, System.Threading.CancellationToken ct = default)
        {
            foreach (var q in paper.DanhSachCauHoi)
            {
                ct.ThrowIfCancellationRequested();
                if (!q.CoHinhTikz) continue;

                // Chế độ "Bảo đảm hình chuẩn": luôn ưu tiên bộ dựng tham số chuẩn (không phụ thuộc AI).
                // Biểu đồ cột / sơ đồ đoạn thẳng bị loại khỏi bước này để tránh bịa số liệu;
                // các câu đó rơi xuống quy trình thường (bộ dựng chuẩn trước, AI sau).
                if (settings.BaoDamHinhChuan)
                {
                    string? stableTikz = TikzSpecialistService.TryGenerateParametricTikz(q, excludeStatisticalFigures: true);
                    if (!string.IsNullOrWhiteSpace(stableTikz))
                    {
                        q.MaTikz = stableTikz;
                        q.TikzSource = "Template";
                        continue;
                    }
                }

                if (string.IsNullOrWhiteSpace(q.MaTikz))
                {
                    q.MaTikz = await TikzSpecialistService.GenerateTikzForQuestionAsync(settings, q, paper.KhoiLop);
                }
            }
        }

        private void DisplayQuestionsPreview(MathExamPaper paper)
        {
            PnlQuestionsList.Children.Clear();
            var settings = AppSettings.Load();
            string appDataTikzDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TikzCache");
            Directory.CreateDirectory(appDataTikzDir);

            foreach (var q in paper.DanhSachCauHoi.OrderBy(x => x.SoThuTu))
            {
                var card = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(16),
                    Margin = new Thickness(0, 0, 0, 14)
                };

                var sp = new StackPanel();

                // Header line: Câu X | Mức độ | Điểm
                var headerGrid = new Grid();
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var titleSp = new StackPanel { Orientation = Orientation.Horizontal };
                titleSp.Children.Add(new TextBlock
                {
                    Text = $"Câu {q.SoThuTu}: ",
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59))
                });

                // Badge Mức độ
                var badge = new Border
                {
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(8, 2, 8, 2),
                    Margin = new Thickness(6, 0, 8, 0),
                    Background = q.MucDo == "M3" ? new SolidColorBrush(Color.FromRgb(254, 226, 226)) :
                                 q.MucDo == "M2" ? new SolidColorBrush(Color.FromRgb(254, 243, 199)) :
                                                   new SolidColorBrush(Color.FromRgb(219, 234, 254))
                };
                badge.Child = new TextBlock
                {
                    Text = q.MucDo,
                    FontWeight = FontWeights.Bold,
                    FontSize = 11,
                    Foreground = q.MucDo == "M3" ? new SolidColorBrush(Color.FromRgb(220, 38, 38)) :
                                 q.MucDo == "M2" ? new SolidColorBrush(Color.FromRgb(217, 119, 6)) :
                                                   new SolidColorBrush(Color.FromRgb(29, 78, 216))
                };
                titleSp.Children.Add(badge);

                titleSp.Children.Add(new TextBlock
                {
                    Text = $"({WordExportMathService.FormatPoint(q.Diem)} điểm) - {q.ChuDe}",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                    VerticalAlignment = VerticalAlignment.Center
                });
                headerGrid.Children.Add(titleSp);

                sp.Children.Add(headerGrid);

                // Label đề bài
                var lblContent = new TextBlock
                {
                    Text = "Đề bài (chứa công thức LaTeX, ví dụ \\frac{3}{5}):",
                    FontSize = 11.5,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(71, 85, 105)),
                    Margin = new Thickness(0, 8, 0, 3)
                };
                sp.Children.Add(lblContent);

                // TextBox chỉnh sửa nội dung câu hỏi
                var txtContent = new TextBox
                {
                    Text = q.NoiDung,
                    FontSize = 13,
                    TextWrapping = TextWrapping.Wrap,
                    AcceptsReturn = true,
                    Padding = new Thickness(8),
                    Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                    Margin = new Thickness(0, 0, 0, 6)
                };
                txtContent.TextChanged += (s, ev) => { q.NoiDung = txtContent.Text; };
                sp.Children.Add(txtContent);

                // Khung hiển thị xem trước công thức trực quan (Live Math Preview)
                var previewCard = MathTextHelper.CreateMathPreviewCard("Xem trước công thức", txtContent, 13.5);
                sp.Children.Add(previewCard);

                // TikZ Editor Panel (nếu câu hỏi có TikZ)
                if (q.CoHinhTikz)
                {
                    var tikzBorder = new Border
                    {
                        Background = new SolidColorBrush(Color.FromRgb(245, 243, 255)),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(221, 214, 254)),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(6),
                        Padding = new Thickness(12),
                        Margin = new Thickness(0, 4, 0, 10)
                    };

                    var tikzSp = new StackPanel();
                    var tikzHeaderGrid = new Grid();
                    tikzHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    tikzHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    var lblTikz = new TextBlock
                    {
                        Text = "Mã TikZ hình vẽ / sơ đồ / biểu đồ:",
                        FontWeight = FontWeights.Bold,
                        FontSize = 12,
                        Foreground = new SolidColorBrush(Color.FromRgb(79, 70, 229)),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    tikzHeaderGrid.Children.Add(lblTikz);

                    var btnActionsPanel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right
                    };

                    var btnRegenTikz = new Button
                    {
                        Content = "Vẽ lại từ đề (AI)",
                        Padding = new Thickness(10, 5, 10, 5),
                        Margin = new Thickness(0, 0, 8, 0),
                        Background = new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                        Foreground = Brushes.White,
                        FontWeight = FontWeights.SemiBold,
                        FontSize = 11.5,
                        BorderThickness = new Thickness(0),
                        Cursor = System.Windows.Input.Cursors.Hand,
                        ToolTip = "Đọc kỹ nội dung đề bài và tự động sinh mã TikZ hình vẽ chính xác 100%"
                    };
                    btnActionsPanel.Children.Add(btnRegenTikz);

                    var btnRecompile = new Button
                    {
                        Content = "Biên dịch lại",
                        Padding = new Thickness(10, 5, 10, 5),
                        Background = new SolidColorBrush(Color.FromRgb(79, 70, 229)),
                        Foreground = Brushes.White,
                        FontWeight = FontWeights.SemiBold,
                        FontSize = 11.5,
                        BorderThickness = new Thickness(0),
                        Cursor = System.Windows.Input.Cursors.Hand,
                        ToolTip = "Biên dịch mã TikZ hiện tại sang ảnh PNG"
                    };
                    btnActionsPanel.Children.Add(btnRecompile);

                    Grid.SetColumn(btnActionsPanel, 1);
                    tikzHeaderGrid.Children.Add(btnActionsPanel);

                    tikzSp.Children.Add(tikzHeaderGrid);

                    var txtTikz = new TextBox
                    {
                        Text = TikzRenderService.SanitizeTikzCode(q.MaTikz),
                        FontFamily = new FontFamily("Consolas"),
                        FontSize = 11.5,
                        AcceptsReturn = true,
                        MinHeight = 85,
                        Height = 110,
                        TextWrapping = TextWrapping.Wrap,
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                        Margin = new Thickness(0, 6, 0, 6),
                        Background = Brushes.White,
                        BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225))
                    };
                    txtTikz.TextChanged += (s, ev) => { q.MaTikz = txtTikz.Text; };
                    tikzSp.Children.Add(txtTikz);

                    var lblStatus = new TextBlock
                    {
                        Text = !string.IsNullOrEmpty(q.AnhTikzPath) ? "Đã có ảnh biên dịch sẵn sàng" : "Chưa có ảnh. Bấm 'Vẽ lại từ đề (AI)' hoặc 'Biên dịch lại' để tạo ảnh.",
                        FontSize = 11,
                        FontStyle = FontStyles.Italic,
                        Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                        Margin = new Thickness(0, 0, 0, 6)
                    };
                    tikzSp.Children.Add(lblStatus);

                    var imgPreview = new Image
                    {
                        MaxWidth = 280,
                        MaxHeight = 180,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 4, 0, 4)
                    };

                    if (!string.IsNullOrEmpty(q.AnhTikzPath) && File.Exists(q.AnhTikzPath))
                    {
                        LoadImageSafely(imgPreview, q.AnhTikzPath);
                    }
                    tikzSp.Children.Add(imgPreview);

                    btnRegenTikz.Click += async (s, ev) =>
                    {
                        btnRegenTikz.IsEnabled = false;
                        btnRecompile.IsEnabled = false;
                        lblStatus.Text = "Đang phân tích đề bài và dùng AI sinh hình vẽ chính xác...";
                        lblStatus.Foreground = new SolidColorBrush(Color.FromRgb(79, 70, 229));

                        try
                        {
                            // Người dùng chủ động bấm 'Vẽ lại từ đề (AI)': truyền forceAi = true để ưu tiên AI đọc đề bài
                            string newTikz = await TikzSpecialistService.GenerateTikzForQuestionAsync(settings, q, paper.KhoiLop, forceAi: true);
                            if (!string.IsNullOrWhiteSpace(newTikz))
                            {
                                newTikz = TikzRenderService.SanitizeTikzCode(newTikz);
                                q.MaTikz = newTikz;
                                txtTikz.Text = newTikz;

                                lblStatus.Text = "Đang biên dịch hình vẽ vừa sinh...";
                                string pngPath = Path.Combine(appDataTikzDir, TikzRenderService.GetCacheFileName(newTikz));
                                try { if (File.Exists(pngPath)) File.Delete(pngPath); } catch { }
                                var res = await TikzRenderService.RenderTikzToPngResultAsync(newTikz, pngPath, settings.MiktexBinPath);
                                if (res.Success && !string.IsNullOrEmpty(res.Path) && File.Exists(res.Path))
                                {
                                    q.AnhTikzPath = res.Path;
                                    LoadImageSafely(imgPreview, res.Path);
                                    lblStatus.Text = res.UsedOnline ? "Đã phân tích đề và vẽ hình thành công (qua Kroki)!" : "Đã phân tích đề và vẽ hình thành công (MiKTeX Offline)!";
                                    lblStatus.Foreground = new SolidColorBrush(Color.FromRgb(22, 163, 74));
                                }
                                else
                                {
                                    lblStatus.Text = $"Đã sinh mã TikZ mới nhưng lỗi khi biên dịch ra ảnh. {res.Error}";
                                    lblStatus.Foreground = new SolidColorBrush(Color.FromRgb(220, 38, 38));
                                }
                            }
                            else
                            {
                                lblStatus.Text = "Không nhận diện được hình mẫu phù hợp từ đề bài.";
                                lblStatus.Foreground = new SolidColorBrush(Color.FromRgb(220, 38, 38));
                            }
                        }
                        catch (Exception exTikz)
                        {
                            lblStatus.Text = $"Lỗi: {exTikz.Message}";
                            lblStatus.Foreground = new SolidColorBrush(Color.FromRgb(220, 38, 38));
                        }
                        finally
                        {
                            btnRegenTikz.IsEnabled = true;
                            btnRecompile.IsEnabled = true;
                        }
                    };

                    btnRecompile.Click += async (s, ev) =>
                    {
                        btnRegenTikz.IsEnabled = false;
                        btnRecompile.IsEnabled = false;
                        lblStatus.Text = "Đang biên dịch mã TikZ (MiKTeX cục bộ / Kroki)...";
                        lblStatus.Foreground = new SolidColorBrush(Color.FromRgb(79, 70, 229));

                        try
                        {
                            string raw = txtTikz.Text;
                            string sanitized = TikzRenderService.SanitizeTikzCode(raw);
                            if (sanitized != raw)
                            {
                                txtTikz.Text = sanitized;
                            }
                            q.MaTikz = sanitized;

                            string pngPath = Path.Combine(appDataTikzDir, TikzRenderService.GetCacheFileName(sanitized));
                            try { if (File.Exists(pngPath)) File.Delete(pngPath); } catch { }

                            var res = await TikzRenderService.RenderTikzToPngResultAsync(sanitized, pngPath, settings.MiktexBinPath);
                            if (res.Success && !string.IsNullOrEmpty(res.Path) && File.Exists(res.Path))
                            {
                                q.AnhTikzPath = res.Path;
                                LoadImageSafely(imgPreview, res.Path);
                                lblStatus.Text = res.UsedOnline ? "Biên dịch TikZ thành công (qua dịch vụ Kroki)!" : "Biên dịch TikZ thành công (MiKTeX Offline)!";
                                lblStatus.Foreground = new SolidColorBrush(Color.FromRgb(22, 163, 74));
                            }
                            else
                            {
                                lblStatus.Text = $"Lỗi biên dịch: {res.Error}";
                                lblStatus.Foreground = new SolidColorBrush(Color.FromRgb(220, 38, 38));
                            }
                        }
                        catch (Exception exRec)
                        {
                            lblStatus.Text = $"Lỗi khi biên dịch: {exRec.Message}";
                            lblStatus.Foreground = new SolidColorBrush(Color.FromRgb(220, 38, 38));
                        }
                        finally
                        {
                            btnRegenTikz.IsEnabled = true;
                            btnRecompile.IsEnabled = true;
                        }
                    };

                    tikzBorder.Child = tikzSp;
                    sp.Children.Add(tikzBorder);
                }

                // Trắc nghiệm: 4 phương án
                if (!string.IsNullOrEmpty(q.DapAnA))
                {
                    var optGrid = new Grid { Margin = new Thickness(0, 4, 0, 6) };
                    optGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    optGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    optGrid.RowDefinitions.Add(new RowDefinition());
                    optGrid.RowDefinitions.Add(new RowDefinition());

                    AddEditableOption(optGrid, "A", q.DapAnA, val => q.DapAnA = val, q.DapAnDung == "A", 0, 0);
                    AddEditableOption(optGrid, "B", q.DapAnB, val => q.DapAnB = val, q.DapAnDung == "B", 0, 1);
                    AddEditableOption(optGrid, "C", q.DapAnC, val => q.DapAnC = val, q.DapAnDung == "C", 1, 0);
                    AddEditableOption(optGrid, "D", q.DapAnD, val => q.DapAnD = val, q.DapAnDung == "D", 1, 1);

                    sp.Children.Add(optGrid);
                }

                // Tự luận: Các bước giải
                if (q.CacBuocGiai != null && q.CacBuocGiai.Count > 0)
                {
                    var stepsBorder = new Border
                    {
                        Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                        CornerRadius = new CornerRadius(6),
                        Padding = new Thickness(12, 8, 12, 8),
                        Margin = new Thickness(0, 6, 0, 0)
                    };
                    var stepsSp = new StackPanel();
                    stepsSp.Children.Add(new TextBlock
                    {
                        Text = "Hướng dẫn chấm từng bước:",
                        FontWeight = FontWeights.SemiBold,
                        FontSize = 12,
                        Foreground = new SolidColorBrush(Color.FromRgb(71, 85, 105)),
                        Margin = new Thickness(0, 0, 0, 4)
                    });

                    foreach (var step in q.CacBuocGiai)
                    {
                        stepsSp.Children.Add(new TextBlock
                        {
                            Text = $"• Bước {step.Buoc}: {step.NoiDung} ({WordExportMathService.FormatPoint(step.Diem)}đ)",
                            FontSize = 12,
                            Foreground = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
                            Margin = new Thickness(0, 2, 0, 2)
                        });
                    }
                    stepsBorder.Child = stepsSp;
                    sp.Children.Add(stepsBorder);
                }

                // Tùy chọn số dòng lưới ô ly bài giải cho mọi câu tự luận (Câu 8, 9, 10...)
                if (WordExportMathService.IsQuestionEssay(q))
                {
                    var olyPnl = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 4) };
                    olyPnl.Children.Add(new TextBlock
                    {
                        Text = "Số dòng ô ly bài giải:",
                        FontWeight = FontWeights.SemiBold,
                        FontSize = 12,
                        Foreground = new SolidColorBrush(Color.FromRgb(71, 85, 105)),
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 8, 0)
                    });

                    var cboOly = new ComboBox { Width = 240, Padding = new Thickness(6, 3, 6, 3) };
                    cboOly.Items.Add(new ComboBoxItem { Content = "0 dòng lớn (Không kẻ ô ly)", Tag = 0 });
                    cboOly.Items.Add(new ComboBoxItem { Content = "4 dòng lớn (16 hàng con - Đặt tính)", Tag = 4 });
                    cboOly.Items.Add(new ComboBoxItem { Content = "6 dòng lớn (24 hàng con)", Tag = 6 });
                    cboOly.Items.Add(new ComboBoxItem { Content = "8 dòng lớn (32 hàng con - Chuẩn)", Tag = 8 });
                    cboOly.Items.Add(new ComboBoxItem { Content = "10 dòng lớn (40 hàng con)", Tag = 10 });
                    cboOly.Items.Add(new ComboBoxItem { Content = "12 dòng lớn (48 hàng con)", Tag = 12 });

                    int curLines = q.SoDongOly;
                    if (curLines < 0)
                    {
                        curLines = (q.SoThuTu == 8 || WordExportMathService.IsCalculationQuestion(q)) ? 4 : 8;
                    }

                    bool matched = false;
                    foreach (ComboBoxItem item in cboOly.Items)
                    {
                        if (item.Tag is int val && val == curLines)
                        {
                            cboOly.SelectedItem = item;
                            matched = true;
                            break;
                        }
                    }
                    if (!matched)
                    {
                        cboOly.SelectedIndex = (q.SoThuTu == 8 || WordExportMathService.IsCalculationQuestion(q)) ? 1 : 3;
                    }

                    cboOly.SelectionChanged += (s, ev) =>
                    {
                        if (cboOly.SelectedItem is ComboBoxItem selItem && selItem.Tag is int linesVal)
                        {
                            q.SoDongOly = linesVal;
                        }
                    };

                    olyPnl.Children.Add(cboOly);
                    sp.Children.Add(olyPnl);
                }

                card.Child = sp;
                PnlQuestionsList.Children.Add(card);
            }

            UpdatePaperPointsBadge();
        }

        private static void LoadImageSafely(Image imgControl, string imagePath)
        {
            try
            {
                if (!File.Exists(imagePath)) return;
                byte[] bytes = File.ReadAllBytes(imagePath);
                var bmp = new BitmapImage();
                using (var ms = new MemoryStream(bytes))
                {
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                    bmp.StreamSource = ms;
                    bmp.EndInit();
                }
                bmp.Freeze();
                imgControl.Source = bmp;
            }
            catch (Exception ex)
            {
                AppLogger.Warn($"LoadImageSafely error: {ex.Message}");
            }
        }

        private void AddEditableOption(Grid grid, string label, string currentVal, Action<string> onUpdate, bool isCorrect, int row, int col)
        {
            var pnl = new StackPanel { Margin = new Thickness(0, 2, 8, 4) };

            var dock = new DockPanel();
            var lbl = new TextBlock
            {
                Text = $"{label}. ",
                FontWeight = isCorrect ? FontWeights.Bold : FontWeights.SemiBold,
                Foreground = isCorrect ? new SolidColorBrush(Color.FromRgb(22, 163, 74)) : new SolidColorBrush(Color.FromRgb(71, 85, 105)),
                VerticalAlignment = VerticalAlignment.Center,
                Width = 22
            };
            DockPanel.SetDock(lbl, Dock.Left);
            dock.Children.Add(lbl);

            var tb = new TextBox
            {
                Text = currentVal,
                FontSize = 12.5,
                Padding = new Thickness(4),
                Background = isCorrect ? new SolidColorBrush(Color.FromRgb(240, 253, 244)) : Brushes.White,
                BorderBrush = isCorrect ? new SolidColorBrush(Color.FromRgb(134, 239, 172)) : new SolidColorBrush(Color.FromRgb(203, 213, 225))
            };
            tb.TextChanged += (s, e) => onUpdate(tb.Text);
            dock.Children.Add(tb);
            pnl.Children.Add(dock);

            // Khung xem trước công thức trực quan cho phương án trắc nghiệm
            var optPreview = MathTextHelper.CreateOptionPreviewBlock(tb, isCorrect);
            pnl.Children.Add(optPreview);

            Grid.SetRow(pnl, row);
            Grid.SetColumn(pnl, col);
            grid.Children.Add(pnl);
        }

        // ========================= LƯU / MỞ / TỰ ĐỘNG LƯU ĐỀ =========================

        private ExamDraft BuildDraft()
        {
            return new ExamDraft
            {
                Kind = "Math",
                Grade = GetSelectedGrade(),
                Period = GetSelectedPeriod(),
                ExportFileName = TxtExportFileName.Text.Trim(),
                RequiresCurriculumReview = CurriculumScopePolicy.NeedsMathCurriculumReview(GetSelectedGrade(), GetSelectedPeriod(), _matrixItems.ToList(), _currentPaper?.DanhSachCauHoi),
                MathPaper = _currentPaper,
                MathMatrix = _matrixItems.ToList()
            };
        }

        private void SaveDraftTo(string path)
        {
            if (DraftStore.Save(BuildDraft(), path))
            {
                AppLogger.Info($"Đã lưu đề Toán vào: {path}");
            }
            else
            {
                AppLogger.Warn($"Không lưu được đề Toán vào: {path}");
            }
        }

        /// <summary>
        /// Tự động lưu tạm để khôi phục khi mở lại chương trình.
        /// Lưu cả khi mới có ma trận mà chưa sinh đề, vì công dựng ma trận cũng rất mất thời gian.
        /// </summary>
        private void AutoSaveDraft()
        {
            if (_isApplyingDraft) return; // Đang khôi phục thì không ghi đè bản nháp dở dang

            bool hasPaper = _currentPaper is { DanhSachCauHoi.Count: > 0 };
            bool hasMatrix = _matrixItems.Count > 0;
            if (!hasPaper && !hasMatrix) return;

            DraftStore.Save(BuildDraft(), DraftStore.AutoSavePath("Math"));
        }

        private void TryRestoreAutoSave()
        {
            var draft = DraftStore.Load(DraftStore.AutoSavePath("Math"));
            if (draft == null) return;

            bool hasPaper = draft.MathPaper is { DanhSachCauHoi.Count: > 0 };
            bool hasMatrix = draft.MathMatrix is { Count: > 0 };
            if (!hasPaper && !hasMatrix) return;

            ApplyDraft(draft);
            TxtAiStatus.Text = hasPaper
                ? $"Đã khôi phục đề Toán soạn dở (lưu lúc {draft.SavedAt:HH:mm dd/MM}). Bấm 'Đề mới' nếu muốn bắt đầu lại."
                : $"Đã khôi phục ma trận Toán soạn dở (lưu lúc {draft.SavedAt:HH:mm dd/MM}). Bấm 'Đề mới' nếu muốn bắt đầu lại.";
            if (draft.RequiresCurriculumReview)
                TxtAiStatus.Text += " ⚠ Bản nháp cũ/chưa ánh xạ chương trình: sinh đề và xuất bị khóa đến khi lập lại ma trận với mục đã xác nhận.";
            AppLogger.Info("Đã khôi phục bản nháp đề Toán soạn dở.");
        }

        private void ApplyDraft(ExamDraft draft)
        {
            draft.RequiresCurriculumReview |= CurriculumScopePolicy.NeedsMathCurriculumReview(draft.Grade, draft.Period, draft.MathMatrix ?? new List<MathMatrixItem>(), draft.MathPaper?.DanhSachCauHoi);
            _isApplyingDraft = true;
            try
            {
                // Lớp (index 0 = Lớp 4, index 1 = Lớp 5)
                CboGrade.SelectedIndex = draft.Grade == 5 ? 1 : 0;

                // Kỳ kiểm tra
                if (CboPeriod != null)
                {
                    foreach (ComboBoxItem item in CboPeriod.Items)
                    {
                        if (string.Equals(item.Content?.ToString(), draft.Period, StringComparison.OrdinalIgnoreCase))
                        {
                            CboPeriod.SelectedItem = item;
                            break;
                        }
                    }
                }

                // Ma trận
                if (draft.MathMatrix != null)
                {
                    _matrixItems = new ObservableCollection<MathMatrixItem>(draft.MathMatrix);
                    DgMatrix.ItemsSource = _matrixItems;
                    ClearPreAiSnapshot();
                    UpdateSummary();
                }

                // Đề + tên tệp xuất
                _currentPaper = draft.MathPaper;
                if (_currentPaper != null) _currentPaper.Period = draft.Period;
                if (!string.IsNullOrWhiteSpace(draft.ExportFileName))
                {
                    TxtExportFileName.Text = draft.ExportFileName;
                }

                // Bỏ các đường dẫn ảnh đã không còn tồn tại (cache bị dọn) để tránh lỗi khi xuất
                if (_currentPaper != null)
                {
                    foreach (var q in _currentPaper.DanhSachCauHoi)
                    {
                        if (!string.IsNullOrEmpty(q.AnhTikzPath) && !File.Exists(q.AnhTikzPath))
                        {
                            q.AnhTikzPath = null;
                        }
                    }
                    DisplayQuestionsPreview(_currentPaper);
                }
            }
            finally
            {
                _isApplyingDraft = false;
            }

            UpdateSampleButtonContent();
        }

        private void BtnSaveDraft_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPaper == null || _currentPaper.DanhSachCauHoi.Count == 0)
            {
                MessageBox.Show("Chưa có đề nào để lưu. Hãy sinh đề hoặc nạp đề mẫu trước.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Lưu đề Toán để mở lại sau",
                FileName = $"DeToan_Lop{GetSelectedGrade()}_{GetSelectedPeriod().Replace(' ', '_')}.exam.json",
                DefaultExt = ".json",
                Filter = "Tệp đề TH_ExamGen (*.exam.json)|*.exam.json|Tệp JSON (*.json)|*.json",
                AddExtension = true,
                OverwritePrompt = true
            };

            if (dlg.ShowDialog() != true) return;

            SaveDraftTo(dlg.FileName);
            AutoSaveDraft();
            MessageBox.Show($"Đã lưu đề thành công:\n{dlg.FileName}", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnOpenDraft_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Mở đề Toán đã lưu",
                Filter = "Tệp đề TH_ExamGen (*.exam.json;*.json)|*.exam.json;*.json|Tất cả tệp (*.*)|*.*"
            };
            if (Directory.Exists(DraftStore.DraftDirectory))
            {
                dlg.InitialDirectory = DraftStore.DraftDirectory;
            }

            if (dlg.ShowDialog() != true) return;

            var draft = DraftStore.Load(dlg.FileName);
            if (draft == null)
            {
                MessageBox.Show("Không đọc được tệp. Có thể tệp đã hỏng hoặc không đúng định dạng đề.", "Không mở được", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.Equals(draft.Kind, "Viet", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Tệp này là đề Tiếng Việt. Hãy mở ở mục 'Tiếng Việt'.", "Sai môn học", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (draft.MathPaper == null || draft.MathPaper.DanhSachCauHoi.Count == 0)
            {
                MessageBox.Show("Tệp này không có nội dung đề Toán hợp lệ.", "Không mở được", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ApplyDraft(draft);
            TxtAiStatus.Text = $"Đã mở đề Toán đã lưu (lúc {draft.SavedAt:HH:mm dd/MM}).";
            if (draft.RequiresCurriculumReview)
                TxtAiStatus.Text += " ⚠ Bản nháp cũ/chưa ánh xạ chương trình; sinh hoặc xuất sẽ bị chặn cho đến khi lập lại ma trận theo mục đã xác nhận.";
            AppLogger.Info($"Đã mở đề Toán từ: {dlg.FileName}");
        }

        private void BtnNewExam_Click(object sender, RoutedEventArgs e)
        {
            var res = MessageBox.Show(
                "Bắt đầu đề mới?\n\nMa trận sẽ trở về mặc định và bản tự động lưu sẽ bị xóa. Đề đang soạn trên màn hình cũng sẽ bị bỏ.",
                "Đề mới", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res != MessageBoxResult.Yes) return;

            _currentPaper = null;
            PnlQuestionsList.Children.Clear();
            PnlQuestionsList.Children.Add(TxtEmptyNotice);

            LoadDefaultMatrix();
            DraftStore.Delete(DraftStore.AutoSavePath("Math"));
            TxtAiStatus.Text = "Đã tạo đề mới. Ma trận đã trở về mặc định.";
        }

        private void BtnExportMatrixOnly_Click(object sender, RoutedEventArgs e)
        {
            if (_matrixItems.Count == 0)
            {
                MessageBox.Show("Ma trận chưa có câu hỏi nào.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var settings = AppSettings.Load();
            int grade = GetSelectedGrade();
            string period = GetSelectedPeriod();
            string exportDir = ExportPaths.GetExportFolder(settings);
            string periodClean = period.Replace(" ", "_");
            string fullPath = Path.Combine(exportDir, $"Ma_Tran_Va_Dac_Ta_Toan_Lop_{grade}_{periodClean}.docx");

            try
            {
                CurriculumScopePolicy.ValidateMathMatrix(grade, period, _matrixItems.ToList());
                string? saved = ExportHelper.RunGuarded(
                    fullPath,
                    p => WordExportMathService.ExportMathMatrixOnlyToDocx(_matrixItems.ToList(), settings, grade, p, period),
                    "Lưu Ma trận Toán với tên khác");
                if (saved == null) return;

                var res = MessageBox.Show($"Đã xuất thành công tệp Ma trận & Bản đặc tả:\n{saved}\n\nBạn có muốn mở tệp ngay không?", "Thành công", MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (res == MessageBoxResult.Yes)
                {
                    Process.Start(new ProcessStartInfo { FileName = saved, UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("Lỗi khi xuất Ma trận Toán", ex);
                MessageBox.Show($"Lỗi khi xuất Ma trận: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnExportWord_Click(object sender, RoutedEventArgs e)
        {
            if (!LicenseManager.CanUseSoftware(out string licenseErr))
            {
                MessageBox.Show(licenseErr, "Thông báo Bản Quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
                var licenseWin = new LicenseWindow { Owner = Window.GetWindow(this) };
                licenseWin.ShowDialog();
                if (Window.GetWindow(this) is MainWindow mw) mw.UpdateStatusInfo();
                return;
            }

            if (_currentPaper == null || _currentPaper.DanhSachCauHoi.Count == 0)
            {
                MessageBox.Show("Vui lòng thực hiện Bước 2 (Sinh đề bằng AI) trước khi xuất file Word.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var settings = AppSettings.Load();
            string fileName = TxtExportFileName.Text.Trim();
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = "De_Kiem_Tra_Toan_Lop_4.docx";
            }
            if (!fileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            {
                fileName += ".docx";
            }

            string exportDir = ExportPaths.GetExportFolder(settings);
            string fullPath = Path.Combine(exportDir, fileName);

            // Sai tổng điểm bị chặn trước khi xuất để ma trận và đề không thể lệch nhau.
            double paperTotal = _currentPaper.DanhSachCauHoi.Sum(q => q.Diem);
            if (Math.Abs(paperTotal - 10.0) >= 0.05)
            {
                MessageBox.Show($"Không thể xuất: tổng điểm đề hiện tại là {paperTotal:0.#}, chưa khớp 10 điểm.", "Đề chưa hợp lệ", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                int scopeGrade = _currentPaper.KhoiLop > 0 ? _currentPaper.KhoiLop : GetSelectedGrade();
                bool skipValidation = false;
                try
                {
                    CurriculumScopePolicy.ValidateMathQuestions(scopeGrade, GetSelectedPeriod(), _currentPaper.DanhSachCauHoi, _matrixItems.ToList());
                    CurriculumScopePolicy.ValidateMathQuestionScope(scopeGrade, GetSelectedPeriod(), _currentPaper.DanhSachCauHoi);
                }
                catch (Exception vex) when (vex is InvalidOperationException || vex is ArgumentException)
                {
                    // Đề được sinh kèm cảnh báo (AI chưa khớp ma trận): giáo viên đã được báo; cho xuất sau khi xác nhận.
                    if (_currentPaper.CanhBaoKiemTra.Count == 0) throw;
                    var choice = MessageBox.Show(
                        "Đề Toán còn điểm cần giáo viên rà soát:\n\n- " + string.Join("\n- ", _currentPaper.CanhBaoKiemTra) +
                        $"\n\nKiểm tra chặn xuất: {vex.Message}\n\nVẫn xuất file Word?",
                        "Đề có cảnh báo", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (choice != MessageBoxResult.Yes) return;
                    skipValidation = true;
                }
                _currentPaper.Period = GetSelectedPeriod();
                _currentPaper.DanhSachMaTran = _matrixItems.ToList();
                // Đồng bộ số dòng ô ly từ Bước 3
                int defaultOly = -1;
                if (CboDefaultOlyLines?.SelectedItem is ComboBoxItem cboItem && int.TryParse(cboItem.Tag?.ToString(), out int parsedLines))
                {
                    defaultOly = parsedLines;
                }

                foreach (var q in _currentPaper.DanhSachCauHoi)
                {
                    bool isEssay = WordExportMathService.IsQuestionEssay(q);
                    if (!isEssay) continue;

                    if (defaultOly == -1)
                    {
                        // Giữ theo cấu hình từng câu ở Bước 2. Nếu chưa gán thì dùng chuẩn
                        if (q.SoDongOly < 0)
                        {
                            q.SoDongOly = (q.SoThuTu == 8 || WordExportMathService.IsCalculationQuestion(q)) ? 4 : 8;
                        }
                    }
                    else if (defaultOly == 0)
                    {
                        // 0 dòng lớn: Không kẻ ô ly cho toàn bộ đề
                        q.SoDongOly = 0;
                    }
                    else
                    {
                        // Áp dụng số dòng được chọn ở Bước 3
                        if (q.SoThuTu == 8 || WordExportMathService.IsCalculationQuestion(q))
                        {
                            q.SoDongOly = Math.Min(4, defaultOly);
                        }
                        else
                        {
                            q.SoDongOly = defaultOly;
                        }
                    }
                }

                string? saved = ExportHelper.RunGuarded(
                    fullPath,
                    p =>
                    {
                        if (RbExportExamOnly.IsChecked == true)
                        {
                            WordExportMathService.ExportMathExamOnlyToDocx(_currentPaper!, settings, p, skipValidation);
                        }
                        else
                        {
                            WordExportMathService.ExportMathFullPackageToDocx(_currentPaper!, settings, p, skipValidation);
                        }
                    },
                    "Lưu đề Toán với tên khác");
                if (saved == null) return;

                _lastExportedFilePath = saved;

                TxtExportStatus.Text = $"Đã xuất thành công tệp Word:\n{saved}";
                BtnOpenFolder.Visibility = Visibility.Visible;
                BtnOpenFile.Visibility = Visibility.Visible;

                AutoSaveDraft();
            }
            catch (Exception ex)
            {
                AppLogger.Error("Lỗi khi xuất Word (Toán)", ex);
                MessageBox.Show($"Lỗi khi xuất Word: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_lastExportedFilePath) && File.Exists(_lastExportedFilePath))
            {
                string dir = Path.GetDirectoryName(_lastExportedFilePath) ?? string.Empty;
                Process.Start(new ProcessStartInfo
                {
                    FileName = dir,
                    UseShellExecute = true
                });
            }
        }

        private void BtnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_lastExportedFilePath) && File.Exists(_lastExportedFilePath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = _lastExportedFilePath,
                    UseShellExecute = true
                });
            }
        }
    }
}
