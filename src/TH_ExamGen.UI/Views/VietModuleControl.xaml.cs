using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using TH_ExamGen.Core.Curriculum;
using TH_ExamGen.Core.Config;
using TH_ExamGen.Core.Licensing;
using TH_ExamGen.Core.VietModels;
using TH_ExamGen.Services;
using TH_ExamGen.UI.Helpers;

namespace TH_ExamGen.UI.Views
{
    public partial class VietModuleControl : UserControl
    {
        private ObservableCollection<VietMatrixItem> _matrixItems = new();
        private VietExamPaper? _currentPaper;
        private string _lastExportedFilePath = string.Empty;
        private System.Threading.CancellationTokenSource? _vietGenCts;
        private bool _initialized;
        private bool _isApplyingDraft;

        // Ma trận ngay trước lần AI gợi ý gần nhất (chỉ trong phiên làm việc hiện tại), phục vụ nút "Khôi phục bản trước AI".
        private List<VietMatrixItem>? _preAiMatrixSnapshot;

        public VietModuleControl()
        {
            InitializeComponent();
            Loaded += VietModuleControl_Loaded;

            // Tự động lưu ngay khi giáo viên sửa xong một ô trong ma trận.
            // Dùng Background để giá trị vừa nhập đã được ghi vào đối tượng trước khi lưu.
            DgVietMatrix.CellEditEnding += (s, args) =>
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(AutoSaveDraft));
        }

        private void VietModuleControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Chỉ khởi tạo MỘT lần để không xóa chỉnh sửa ma trận khi quay lại tab Tiếng Việt.
            if (_initialized) return;
            _initialized = true;

            // Nhớ lại Khối lớp / Kỳ kiểm tra giáo viên đã chọn lần trước
            ApplyLastSelectionFromSettings();
            LoadDefaultMatrix();
            UpdateExportFileName();
            UpdateExportFolderHint();

            // Khôi phục bản soạn dở (nếu có)
            TryRestoreAutoSave();
        }

        private void BtnCancelVietGenerate_Click(object sender, RoutedEventArgs e)
        {
            if (_vietGenCts == null || _vietGenCts.IsCancellationRequested) return;

            _vietGenCts.Cancel();
            TxtVietAiStatus.Text = "Đang hủy theo yêu cầu...";
            BtnCancelVietGenerate.IsEnabled = false;
        }

        private int GetSelectedGrade()
        {
            return CboGradeViet?.SelectedIndex == 1 ? 5 : 4;
        }

        private string GetSelectedPeriod()
        {
            if (CboPeriodViet?.SelectedItem is ComboBoxItem item)
            {
                return item.Content?.ToString() ?? "Cuối Học kỳ II";
            }
            return "Cuối Học kỳ II";
        }

        private void LoadDefaultMatrix()
        {
            int grade = GetSelectedGrade();
            string period = GetSelectedPeriod();
            var list = VietCurriculumBank.GetMatrixByGradeAndPeriod(grade, period);
            _matrixItems = new ObservableCollection<VietMatrixItem>(list);
            DgVietMatrix.ItemsSource = _matrixItems;
            ClearPreAiSnapshot();

            if (TxtPassageTheme != null)
            {
                var confirmedThemes = CurriculumScopePolicy.GetConfirmedVietReadingTopics(grade, period).Select(x => x.ChuDe.Replace("Chủ điểm", string.Empty, StringComparison.OrdinalIgnoreCase).Trim(' ', '"')).Distinct().ToList();
                TxtPassageTheme.Text = confirmedThemes.Count == 1 ? confirmedThemes[0] : string.Empty;
                TxtPassageTheme.Tag = "auto";
            }

            UpdateSummary();
        }

        private void CboGradeOrPeriodViet_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgVietMatrix == null) return;
            if (_isApplyingDraft) return; // Tránh nạp lại ma trận khi đang khôi phục đề đã lưu
            LoadDefaultMatrix();
            UpdateExportFileName();
            SaveLastSelection();
        }

        /// <summary>Nạp lại Khối lớp / Kỳ kiểm tra đã chọn ở lần dùng trước.</summary>
        private void ApplyLastSelectionFromSettings()
        {
            var settings = AppSettings.Load();
            if (CboGradeViet != null && settings.LastGrade == 5)
            {
                CboGradeViet.SelectedIndex = 1;
            }

            if (CboPeriodViet != null && !string.IsNullOrWhiteSpace(settings.LastPeriod))
            {
                foreach (ComboBoxItem item in CboPeriodViet.Items)
                {
                    if (string.Equals(item.Content?.ToString(), settings.LastPeriod, StringComparison.Ordinal))
                    {
                        CboPeriodViet.SelectedItem = item;
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
                AppLogger.Warn("Không lưu được lựa chọn Khối/Kỳ (Tiếng Việt): " + ex.Message);
            }
        }

        /// <summary>Cập nhật dòng nhắc thư mục xuất file Word ở Bước 3.</summary>
        private void UpdateExportFolderHint()
        {
            if (TxtExportFolderHint == null) return;
            var settings = AppSettings.Load();
            TxtExportFolderHint.Text = $"Nơi lưu file: {ExportPaths.ResolveExportFolder(settings.ExportFolder)}";
        }

        /// <summary>
        /// Hiện tổng điểm của đề hiện tại và cảnh báo nếu chưa đúng 10 điểm.
        /// Riêng Tiếng Việt: điểm các câu + 2,0 điểm đọc thành tiếng mới đủ 10 điểm.
        /// </summary>
        private void UpdatePaperPointsBadge()
        {
            if (TxtPaperPoints == null) return;

            if (_currentPaper == null || _currentPaper.DanhSachCauHoi.Count == 0)
            {
                TxtPaperPoints.Text = string.Empty;
                return;
            }

            double cauDiem = _currentPaper.DanhSachCauHoi.Sum(x => x.Diem);
            double total = cauDiem + 2.0; // 2,0 điểm phần đọc thành tiếng
            bool ok = Math.Abs(total - 10.0) < 0.05;
            TxtPaperPoints.Text = ok
                ? $"Tổng điểm đề hiện tại: {cauDiem:0.#}đ (câu hỏi) + 2,0đ (đọc thành tiếng) = {total:0.#}/10 điểm ✓"
                : $"Tổng điểm đề hiện tại: {cauDiem:0.#}đ (câu hỏi) + 2,0đ (đọc thành tiếng) = {total:0.#}/10 điểm — chưa đúng 10 điểm, bạn nên kiểm tra lại điểm từng câu.";
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

        private void UpdateExportFileName()
        {
            if (TxtExportVietFileName == null) return;
            int grade = _currentPaper != null && _currentPaper.KhoiLop > 0 ? _currentPaper.KhoiLop : GetSelectedGrade();
            string period = GetSelectedPeriod();
            string periodClean = period.Replace(" ", "_");

            string currentText = TxtExportVietFileName.Text.Trim();
            if (string.IsNullOrWhiteSpace(currentText) || currentText.StartsWith("De_Kiem_Tra_TiengViet_Lop_"))
            {
                TxtExportVietFileName.Text = $"De_Kiem_Tra_TiengViet_Lop_{grade}_{periodClean}.docx";
            }
        }

        private void UpdateSummary()
        {
            int totalQuestions = _matrixItems.Count;
            double totalPoints = _matrixItems.Sum(x => x.Diem);
            int docHieuCount = _matrixItems.Count(x => x.MachKienThuc.Contains("Đọc hiểu"));
            double docHieuPoints = _matrixItems.Where(x => x.MachKienThuc.Contains("Đọc hiểu")).Sum(x => x.Diem);
            int tvCount = _matrixItems.Count(x => x.MachKienThuc.Contains("Tiếng Việt"));
            double tvPoints = _matrixItems.Where(x => x.MachKienThuc.Contains("Tiếng Việt")).Sum(x => x.Diem);

            TxtVietSummary.Text = $"{totalQuestions} câu | {totalPoints:0.#}đ (Đọc hiểu: {docHieuCount} câu - {docHieuPoints:0.#}đ | LTVC: {tvCount} câu - {tvPoints:0.#}đ) + 2,0đ Đọc thành tiếng = 10,0đ";
        }

        private void RbPassageSource_Checked(object sender, RoutedEventArgs e)
        {
            if (PnlAiTheme == null || PnlCustomPassage == null) return;

            if (RbAiGeneratePassage.IsChecked == true)
            {
                PnlAiTheme.Visibility = Visibility.Visible;
                PnlCustomPassage.Visibility = Visibility.Collapsed;
            }
            else
            {
                PnlAiTheme.Visibility = Visibility.Collapsed;
                PnlCustomPassage.Visibility = Visibility.Visible;
            }
        }

        private void BtnLoadDefaultVietMatrix_Click(object sender, RoutedEventArgs e)
        {
            LoadDefaultMatrix();
        }

        private void BtnAddRowViet_Click(object sender, RoutedEventArgs e)
        {
            int nextStt = _matrixItems.Count + 1;
            _matrixItems.Add(new VietMatrixItem
            {
                SoThuTu = nextStt,
                MachKienThuc = "Kiến thức Tiếng Việt",
                NoiDungKienThuc = "Từ loại",
                YeuCauCanDat = "Nhận biết từ loại",
                MucDo = "M1",
                HinhThuc = "TN",
                Diem = 0.5
            });
            UpdateSummary();
            AutoSaveDraft();
        }

        private void BtnDeleteRowViet_Click(object sender, RoutedEventArgs e)
        {
            if (DgVietMatrix.SelectedItem is VietMatrixItem selected)
            {
                _matrixItems.Remove(selected);
                for (int i = 0; i < _matrixItems.Count; i++)
                {
                    _matrixItems[i].SoThuTu = i + 1;
                }
                DgVietMatrix.Items.Refresh();
                UpdateSummary();
                AutoSaveDraft();
            }
            else
            {
                MessageBox.Show("Vui lòng chọn dòng cần xóa trong bảng.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void BtnAiSuggestVietMatrix_Click(object sender, RoutedEventArgs e)
        {
            var settings = AppSettings.Load();
            if (string.IsNullOrWhiteSpace(settings.GeminiApiKey))
            {
                MessageBox.Show("Chưa thiết lập Gemini API Key. Vui lòng vào Cài đặt để cấu hình API Key.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int grade = GetSelectedGrade();
            string period = GetSelectedPeriod();
            var topics = VietTopicCatalog.GetTopics(grade, period);

            var dialog = new Window
            {
                Title = $"Gợi ý Ma trận & Bản đặc tả bằng AI - Tiếng Việt {grade} ({period})",
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
                Text = $"Chọn phương thức gợi ý cho đề {period} môn Tiếng Việt lớp {grade} (sách Kết nối tri thức):",
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
                Content = "Cách 1: Tích chọn chủ điểm đọc hiểu & kiến thức TV từ SGK Kết nối tri thức (Khuyên dùng)",
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

            var topicControls = new List<(ToggleButton control, VietTopicItem topic)>();

            void UpdateCounter()
            {
                int checkedReading = topicControls.Count(x => x.control.IsChecked == true && x.topic.MachKienThuc.Contains("Đọc hiểu", StringComparison.OrdinalIgnoreCase));
                int checkedTv = topicControls.Count(x => x.control.IsChecked == true && !x.topic.MachKienThuc.Contains("Đọc hiểu", StringComparison.OrdinalIgnoreCase));
                lblTopicCounter.Text = $"Đã chọn: {checkedReading + checkedTv}/{topicControls.Count} ({checkedReading} đọc hiểu, {checkedTv} kiến thức TV)";
            }

            btnSelectAll.Click += (s, ev) =>
            {
                // Chọn tất cả các mục Kiến thức Tiếng Việt
                foreach (var item in topicControls)
                {
                    if (!item.topic.MachKienThuc.Contains("Đọc hiểu", StringComparison.OrdinalIgnoreCase))
                    {
                        item.control.IsChecked = true;
                    }
                }
                // Giữ nguyên hoặc chọn 1 chủ điểm Đọc hiểu
                var readingItems = topicControls.Where(x => x.topic.MachKienThuc.Contains("Đọc hiểu", StringComparison.OrdinalIgnoreCase)).ToList();
                if (readingItems.Count > 0 && !readingItems.Any(x => x.control.IsChecked == true))
                {
                    readingItems[0].control.IsChecked = true;
                }
                UpdateCounter();
            };

            btnDeselectAll.Click += (s, ev) =>
            {
                // Bỏ chọn tất cả Kiến thức Tiếng Việt (giữ nguyên chủ điểm đọc hiểu đang chọn)
                foreach (var item in topicControls)
                {
                    if (!item.topic.MachKienThuc.Contains("Đọc hiểu", StringComparison.OrdinalIgnoreCase))
                    {
                        item.control.IsChecked = false;
                    }
                }
                UpdateCounter();
            };

            btnDefaultTopics.Click += (s, ev) =>
            {
                // Mặc định: chọn chủ điểm đọc đầu tiên
                var readingItems = topicControls.Where(x => x.topic.MachKienThuc.Contains("Đọc hiểu", StringComparison.OrdinalIgnoreCase)).ToList();
                for (int i = 0; i < readingItems.Count; i++)
                {
                    readingItems[i].control.IsChecked = (i == 0);
                }
                // Kiến thức TV: chọn các mục đã xác nhận
                foreach (var item in topicControls)
                {
                    if (!item.topic.MachKienThuc.Contains("Đọc hiểu", StringComparison.OrdinalIgnoreCase))
                    {
                        item.control.IsChecked = item.topic.DaXacNhan;
                    }
                }
                UpdateCounter();
            };

            var grouped = topics.GroupBy(t => t.MachKienThuc);
            bool firstReadingChecked = false;
            foreach (var group in grouped)
            {
                bool isReadingGroup = group.Key.Contains("Đọc hiểu", StringComparison.OrdinalIgnoreCase);

                var groupHeader = new Border
                {
                    Background = isReadingGroup
                        ? new SolidColorBrush(Color.FromRgb(238, 242, 255))
                        : new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(8, 4, 8, 4),
                    Margin = new Thickness(0, 6, 0, 3)
                };
                var groupTitle = new TextBlock
                {
                    Text = isReadingGroup
                        ? $"📌 {group.Key} ({group.Count()} chủ điểm - Chọn đúng 1 chủ điểm cho bài đọc)"
                        : $"📌 {group.Key} ({group.Count()} nội dung - Có thể chọn nhiều)",
                    FontWeight = FontWeights.Bold,
                    FontSize = 11,
                    Foreground = isReadingGroup
                        ? new SolidColorBrush(Color.FromRgb(67, 56, 202))
                        : new SolidColorBrush(Color.FromRgb(30, 41, 59))
                };
                groupHeader.Child = groupTitle;
                topicsListPanel.Children.Add(groupHeader);

                foreach (var t in group)
                {
                    ToggleButton control;
                    if (isReadingGroup)
                    {
                        var rb = new RadioButton
                        {
                            GroupName = "VietReadingTopicGroup",
                            Margin = new Thickness(8, 3, 0, 3),
                            VerticalContentAlignment = VerticalAlignment.Center,
                            IsChecked = !firstReadingChecked
                        };
                        if (!firstReadingChecked) firstReadingChecked = true;
                        control = rb;
                    }
                    else
                    {
                        var cb = new CheckBox
                        {
                            Margin = new Thickness(8, 2, 0, 2),
                            IsChecked = t.DaXacNhan,
                            VerticalContentAlignment = VerticalAlignment.Center
                        };
                        control = cb;
                    }

                    var tb = new TextBlock
                    {
                        Text = $"{(t.DaXacNhan ? "✓ ĐÃ XÁC NHẬN" : "⚠ CẦN RÀ SOÁT")} [Tuần {t.Tuan}] {t.ChuDe}: {t.NoiDung}",
                        TextWrapping = TextWrapping.Wrap,
                        FontSize = 12,
                        Foreground = new SolidColorBrush(Color.FromRgb(51, 65, 85))
                    };
                    control.ToolTip = $"Mã: {t.MaMuc}{Environment.NewLine}Nguồn SGK: {t.NguonThamKhao}{Environment.NewLine}{t.CanCuTuan}";
                    control.Content = tb;
                    control.Checked += (s, ev) => UpdateCounter();
                    control.Unchecked += (s, ev) => UpdateCounter();

                    topicControls.Add((control, t));
                    topicsListPanel.Children.Add(control);
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
                Text = $"AI sẽ bám phân phối chương trình đợt \"{period}\", tự chọn chủ điểm đọc hiểu + đơn vị kiến thức Tiếng Việt trọng tâm cho đề lớp {grade} (thầy/cô không cần nhập gì), phủ đủ 2 mạch (Đọc hiểu văn bản, Kiến thức Tiếng Việt) và giữ nguyên cấu trúc chuẩn (11 câu - 8.0 điểm).",
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
                Text = $"Đề kiểm tra {period} môn Tiếng Việt lớp {grade} sách Kết nối tri thức. 11 câu hỏi (8.0đ), phân bổ Đọc hiểu 5.0đ (4 TN, 3 TL), Luyện từ và câu 3.0đ, bám sát Thông tư 27.",
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
                    var selected = topicControls.Where(x => x.control.IsChecked == true).Select(x => x.topic).ToList();
                    int checkedCount = selected.Count;
                    if (checkedCount == 0)
                    {
                        MessageBox.Show("Thầy/cô chưa chọn chủ điểm/kiến thức nào. Vui lòng chọn ít nhất 1 nội dung hoặc bấm '✓ Chọn tất cả'.", "Chưa chọn nội dung", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    if (!selected.Any(x => x.MachKienThuc.Contains("Đọc hiểu", StringComparison.OrdinalIgnoreCase)))
                    {
                        MessageBox.Show("Cần chọn đúng một chủ điểm Đọc hiểu để làm căn cứ cho bài đọc duy nhất của đề.", "Thiếu chủ điểm bài đọc", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    var selectedReadingThemesForValidation = selected
                        .Where(x => x.MachKienThuc.Contains("Đọc hiểu", StringComparison.OrdinalIgnoreCase))
                        .Select(x => x.MaMuc)
                        .Distinct(StringComparer.Ordinal)
                        .ToList();
                    if (selectedReadingThemesForValidation.Count != 1)
                    {
                        MessageBox.Show("Mỗi đề dùng một bài đọc, vì vậy hãy chọn đúng một chủ điểm Đọc hiểu. Có thể chọn thêm nhiều mục Kiến thức Tiếng Việt.", "Chọn một chủ điểm đọc", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var pendingReview = selected.Where(x => !x.DaXacNhan).ToList();
                    if (pendingReview.Count > 0)
                    {
                        string reviewList = string.Join(Environment.NewLine, pendingReview.Select(x =>
                            $"• Tuần {x.Tuan}: {x.ChuDe} — {x.NoiDung}{Environment.NewLine}  Căn cứ tuần: {x.CanCuTuan}{Environment.NewLine}  Nguồn: {x.NguonThamKhao}"));
                        var review = MessageBox.Show(
                            "Các mục sau chưa được xác nhận. Hãy đối chiếu tuần và chủ điểm/kỹ năng với phân phối bài/tiết của trường trước khi xác nhận. Việc xác nhận được lưu để dùng cho lần sau:\n\n" + reviewList + "\n\nThầy/cô xác nhận các mục này đủ điều kiện dùng cho kỳ đang chọn?",
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
                else if (CurriculumScopePolicy.GetConfirmedVietTopics(grade, period).Count == 0)
                {
                    MessageBox.Show("Chưa có mục chương trình Tiếng Việt nào được xác nhận cho kỳ này. Hãy chọn 'Tích chọn chủ điểm', rà soát nguồn/tuần và xác nhận ít nhất một mục trước khi dùng AI tự chọn hoặc nhập yêu cầu.", "Danh mục chưa được rà soát", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                    ? topicControls.Where(x => x.control.IsChecked == true).Select(x => x.topic.MaMuc).ToList()
                    : null;
                string requirement = (rbManual.IsChecked == true) ? txtInput.Text.Trim() : string.Empty;
                if (selectTopicsMode)
                {
                    var selectedReadingThemes = topicControls
                        .Where(x => x.control.IsChecked == true && x.topic.MachKienThuc.Contains("Đọc hiểu", StringComparison.OrdinalIgnoreCase))
                        .Select(x => x.topic.ChuDe.Replace("Chủ điểm", string.Empty, StringComparison.OrdinalIgnoreCase).Trim(' ', '"'))
                        .Distinct()
                        .ToList();
                    TxtPassageTheme.Text = selectedReadingThemes.FirstOrDefault() ?? string.Empty;
                    TxtPassageTheme.Tag = "selected";
                }

                // Chụp lại ma trận hiện tại (nếu có) để có thể khôi phục sau khi AI thay thế.
                var preAiItems = _matrixItems.Count > 0 ? CloneMatrixItems(_matrixItems) : null;

                SetAiBusyUi(true);
                if (selectTopicsMode)
                {
                    TxtVietSummary.Text = $"Đang để AI lập Ma trận theo {selectedTopicsList!.Count} chủ điểm/kiến thức đã chọn ({settings.ActiveProviderDisplayName})...";
                }
                else if (autoMode)
                {
                    TxtVietSummary.Text = $"Đang để AI tự chọn chủ đề SGK và lập Ma trận Tiếng Việt ({settings.ActiveProviderDisplayName})...";
                }
                else
                {
                    TxtVietSummary.Text = $"Đang gửi yêu cầu và nhận gợi ý Ma trận Tiếng Việt từ AI ({settings.ActiveProviderDisplayName})...";
                }

                try
                {
                    var gemini = new GeminiService();
                    var suggestedItems = await gemini.SuggestVietMatrixAsync(
                        settings,
                        grade,
                        period,
                        requirement,
                        autoMode,
                        selectedTopicsList
                    );

                    var (addedCount, sameCount, removedCount) = ApplyAiDiffMarks(suggestedItems, preAiItems);

                    _matrixItems = new ObservableCollection<VietMatrixItem>(suggestedItems);
                    DgVietMatrix.ItemsSource = _matrixItems;
                    UpdateSummary();

                    // Bật nút "Khôi phục bản trước AI" nếu trước đó đã có ma trận để quay lại.
                    _preAiMatrixSnapshot = preAiItems;
                    BtnRestorePreAiViet.IsEnabled = _preAiMatrixSnapshot is { Count: > 0 };

                    string topicNote = string.Empty;
                    if (selectTopicsMode)
                    {
                        topicNote = $"\n\nĐã bám sát {selectedTopicsList!.Count} chủ điểm/kiến thức SGK Kết nối tri thức do thầy/cô tích chọn.";
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
                            topicNote = "\n\nAI đã chọn các nội dung chính: " + string.Join("; ", picked) + ".";
                        }
                    }

                    string changeNote = preAiItems == null
                        ? $"\n\nToàn bộ {_matrixItems.Count} dòng đều là nội dung mới (đã tô vàng để thầy/cô rà soát)."
                        : $"\n\nSo với bản trước: {addedCount} dòng mới (tô vàng), {sameCount} dòng trùng nội dung cũ, {removedCount} dòng cũ không còn trong bản mới.";

                    string restoreHint = BtnRestorePreAiViet.IsEnabled
                        ? "\n\nNếu chưa hài lòng, bấm \"Khôi phục bản trước AI\" ở thanh công cụ để quay lại bản cũ."
                        : string.Empty;

                    // Nháy biểu tượng trên taskbar nếu thầy/cô đang làm việc ở cửa sổ khác.
                    TaskbarFlash.Flash(Window.GetWindow(this));

                    MessageBox.Show($"Đã tạo thành công gợi ý Ma trận và Bản đặc tả Tiếng Việt ({_matrixItems.Count} câu) bám sát chương trình Kết nối tri thức!{topicNote}{changeNote}{restoreHint}", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
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
            BtnAiSuggestVietMatrix.IsEnabled = !busy;
            DgVietMatrix.IsReadOnly = busy;
            BtnAddRowViet.IsEnabled = !busy;
            BtnDeleteRowViet.IsEnabled = !busy;
            PbVietMatrixLoading.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;

            if (BtnAiSuggestVietMatrix.Content is StackPanel sp && sp.Children.Count > 1 && sp.Children[1] is TextBlock tb)
            {
                tb.Text = busy ? "Đang tạo..." : "AI Gợi ý ma trận";
            }
        }

        /// <summary>Xóa bản "trước AI" và khóa nút khôi phục (dùng khi ma trận bị thay bằng bản khác ngoài AI).</summary>
        private void ClearPreAiSnapshot()
        {
            _preAiMatrixSnapshot = null;
            if (BtnRestorePreAiViet != null) BtnRestorePreAiViet.IsEnabled = false;
        }

        private static List<VietMatrixItem> CloneMatrixItems(IEnumerable<VietMatrixItem> items)
        {
            return items.Select(x => new VietMatrixItem
            {
                SoThuTu = x.SoThuTu,
                MaMucChuongTrinh = x.MaMucChuongTrinh,
                MachKienThuc = x.MachKienThuc,
                NoiDungKienThuc = x.NoiDungKienThuc,
                YeuCauCanDat = x.YeuCauCanDat,
                MucDo = x.MucDo,
                HinhThuc = x.HinhThuc,
                Diem = x.Diem
            }).ToList();
        }

        private static string AiMatchKey(VietMatrixItem item)
        {
            return ((item.MachKienThuc ?? string.Empty).Trim() + "|" + (item.NoiDungKienThuc ?? string.Empty).Trim()).ToLowerInvariant();
        }

        /// <summary>
        /// So bản AI mới với bản trước đó, đánh dấu vàng các dòng chưa từng có nội dung tương ứng.
        /// Trả về (số dòng mới, số dòng trùng nội dung cũ, số dòng cũ không còn trong bản mới).
        /// </summary>
        private static (int Added, int Same, int Removed) ApplyAiDiffMarks(
            IList<VietMatrixItem> newItems, List<VietMatrixItem>? oldItems)
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

        private void BtnRestorePreAiViet_Click(object sender, RoutedEventArgs e)
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

            _matrixItems = new ObservableCollection<VietMatrixItem>(_preAiMatrixSnapshot);
            DgVietMatrix.ItemsSource = _matrixItems;
            ClearPreAiSnapshot();
            UpdateSummary();

            MessageBox.Show($"Đã khôi phục ma trận trước AI ({_matrixItems.Count} dòng).", "Đã khôi phục", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void DgVietMatrix_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            ClearAiMarkOnCellEdit(e);
            Dispatcher.BeginInvoke(new Action(() =>
            {
                UpdateSummary();
            }));
        }

        /// <summary>
        /// Khi giáo viên sửa một dòng do AI tạo (đang tô vàng) thì bỏ đánh dấu vàng của dòng đó.
        /// Ô văn bản chỉ bỏ dấu khi giá trị thực sự thay đổi; ô Điểm coi thao tác sửa là đã rà soát.
        /// </summary>
        private static void ClearAiMarkOnCellEdit(DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit) return;
            if (e.Row.Item is not VietMatrixItem item || !item.IsAiNew) return;

            if (e.EditingElement is TextBox tb && e.Column is DataGridTextColumn tc && tc.Binding is Binding b)
            {
                string path = b.Path?.Path ?? string.Empty;
                if (path == nameof(VietMatrixItem.MachKienThuc) && tb.Text.Trim() == (item.MachKienThuc ?? string.Empty).Trim()) return;
                if (path == nameof(VietMatrixItem.NoiDungKienThuc) && tb.Text.Trim() == (item.NoiDungKienThuc ?? string.Empty).Trim()) return;
                if (path == nameof(VietMatrixItem.YeuCauCanDat) && tb.Text.Trim() == (item.YeuCauCanDat ?? string.Empty).Trim()) return;
                if (path == nameof(VietMatrixItem.MucDo) && tb.Text.Trim() == (item.MucDo ?? string.Empty).Trim()) return;
                if (path == nameof(VietMatrixItem.HinhThuc) && tb.Text.Trim() == (item.HinhThuc ?? string.Empty).Trim()) return;
            }

            item.IsAiNew = false;
        }

        private async void BtnAuditVietMatrix_Click(object sender, RoutedEventArgs e)
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

            BtnAuditVietMatrix.IsEnabled = false;
            TxtVietSummary.Text = $"Đang gửi Ma trận Tiếng Việt lên AI ({settings.ActiveProviderDisplayName}) để thẩm định chuyên môn...";

            try
            {
                var gemini = new GeminiService();
                string auditReport = await gemini.AuditVietMatrixAsync(
                    settings,
                    grade,
                    period,
                    _matrixItems.ToList()
                );

                var reportWindow = new Window
                {
                    Title = $"Báo cáo thẩm định chuyên môn Ma trận & Đặc tả - Tiếng Việt {grade} ({period})",
                    Width = 720,
                    Height = 520,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = Window.GetWindow(this),
                    Background = new SolidColorBrush(Color.FromRgb(248, 250, 252))
                };

                var reportGrid = new Grid { Margin = new Thickness(18) };
                reportGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                reportGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                reportGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var headerPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
                headerPanel.Children.Add(new TextBlock
                {
                    Text = "KẾT QUẢ THẨM ĐỊNH CHUYÊN MÔN MA TRẬN & ĐẶC TẢ",
                    FontWeight = FontWeights.Bold,
                    FontSize = 15,
                    Foreground = new SolidColorBrush(Color.FromRgb(217, 119, 6))
                });
                headerPanel.Children.Add(new TextBlock
                {
                    Text = $"Môn Tiếng Việt Lớp {grade} — Đợt: {period} (SGK Kết nối tri thức với cuộc sống - Thông tư 27)",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                    Margin = new Thickness(0, 2, 0, 0)
                });
                Grid.SetRow(headerPanel, 0);
                reportGrid.Children.Add(headerPanel);

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
                    BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Child = rtb
                };
                Grid.SetRow(borderReport, 1);
                reportGrid.Children.Add(borderReport);

                var bottomPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 12, 0, 0)
                };
                var btnCopy = new Button
                {
                    Content = "📋 Sao chép nội dung",
                    Padding = new Thickness(14, 6, 14, 6),
                    Margin = new Thickness(0, 0, 8, 0),
                    Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                    Foreground = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225))
                };
                btnCopy.Click += (s, ev) =>
                {
                    Clipboard.SetText(auditReport);
                    MessageBox.Show("Đã sao chép báo cáo thẩm định vào Clipboard!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                };
                bottomPanel.Children.Add(btnCopy);

                var btnClose = new Button
                {
                    Content = "Đóng",
                    Padding = new Thickness(16, 6, 16, 6),
                    Background = new SolidColorBrush(Color.FromRgb(71, 85, 105)),
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.SemiBold,
                    BorderThickness = new Thickness(0)
                };
                btnClose.Click += (s, ev) => reportWindow.Close();
                bottomPanel.Children.Add(btnClose);

                Grid.SetRow(bottomPanel, 2);
                reportGrid.Children.Add(bottomPanel);

                reportWindow.Content = reportGrid;
                reportWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi thẩm định ma trận: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnAuditVietMatrix.IsEnabled = true;
                UpdateSummary();
            }
        }

        private void StepViet1_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            TabViet.SelectedIndex = 0;
        }

        private void StepViet2_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            TabViet.SelectedIndex = 1;
        }

        private void StepViet3_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            TabViet.SelectedIndex = 2;
        }

        private void TabViet_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source != TabViet) return;
            UpdateVietStepperVisuals(TabViet.SelectedIndex);
        }

        private void UpdateVietStepperVisuals(int stepIndex)
        {
            if (StepViet1Border == null || StepViet2Border == null || StepViet3Border == null) return;

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

            SetVietStepVisual(StepViet1Border, StepViet1Badge, StepViet1Sub, StepViet1Title, stepIndex == 0, activeBg, activeBorder, activeBadgeBg, activeSubFg, activeTitleFg, inactiveBg, inactiveBorder, inactiveBadgeBg, inactiveSubFg, inactiveTitleFg);
            SetVietStepVisual(StepViet2Border, StepViet2Badge, StepViet2Sub, StepViet2Title, stepIndex == 1, activeBg, activeBorder, activeBadgeBg, activeSubFg, activeTitleFg, inactiveBg, inactiveBorder, inactiveBadgeBg, inactiveSubFg, inactiveTitleFg);
            SetVietStepVisual(StepViet3Border, StepViet3Badge, StepViet3Sub, StepViet3Title, stepIndex == 2, activeBg, activeBorder, activeBadgeBg, activeSubFg, activeTitleFg, inactiveBg, inactiveBorder, inactiveBadgeBg, inactiveSubFg, inactiveTitleFg);
        }

        private void SetVietStepVisual(Border border, Border badge, TextBlock sub, TextBlock title, bool isActive,
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

        private void BtnGoToVietStep2_Click(object sender, RoutedEventArgs e)
        {
            TabViet.SelectedIndex = 1;
        }

        private void BtnGoToVietStep3_Click(object sender, RoutedEventArgs e)
        {
            TabViet.SelectedIndex = 2;
            UpdateExportFolderHint();
            UpdatePaperPointsBadge();
        }

        private async void BtnGenerateVietExam_Click(object sender, RoutedEventArgs e)
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

            _vietGenCts?.Dispose();
            _vietGenCts = new System.Threading.CancellationTokenSource();
            var ct = _vietGenCts.Token;

            BtnGenerateVietExam.IsEnabled = false;
            BtnRewriteVietGuide.IsEnabled = false;
            BtnCancelVietGenerate.IsEnabled = true;
            BtnCancelVietGenerate.Visibility = Visibility.Visible;
            PbVietLoading.Visibility = Visibility.Visible;
            TxtVietAiStatus.Text = $"Đang gửi yêu cầu và phân tích ngữ liệu lên AI ({settings.ActiveProviderDisplayName})...";
            AppLogger.Info($"Bắt đầu sinh đề Tiếng Việt lớp {GetSelectedGrade()} bằng {settings.ActiveProviderDisplayName}/{settings.ActiveModel}.");

            try
            {
                ReadingPassage? customPassage = null;
                string theme = TxtPassageTheme.Text.Trim();

                if (RbCustomPassage.IsChecked == true)
                {
                    customPassage = new ReadingPassage
                    {
                        TieuDe = TxtCustomTitle.Text.Trim(),
                        TacGia = TxtCustomAuthor.Text.Trim(),
                        NoiDung = TxtCustomContent.Text.Trim(),
                        ChuDe = theme,
                        NguonGoc = "teacher-authored",
                        NguonChiTiet = TxtCustomSource.Text.Trim()
                    };

                    if (string.IsNullOrWhiteSpace(customPassage.NoiDung))
                    {
                        MessageBox.Show("Vui lòng nhập nội dung bài đọc trước khi sinh đề.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    var sourceCheck = MessageBox.Show("Thầy/cô xác nhận bài đọc này do mình biên soạn hoặc lấy từ nguồn ngoài, không chép từ SGK Kết nối tri thức, và nội dung thực sự bám chủ điểm đã chọn?", "Xác nhận nguồn bài đọc", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (sourceCheck != MessageBoxResult.Yes) return;
                }

                var gemini = new GeminiService();
                int grade = GetSelectedGrade();
                string period = GetSelectedPeriod();
                if (!CurriculumScopePolicy.IsVietReadingThemeAllowed(grade, period, theme))
                    throw new InvalidOperationException($"Chủ điểm '{theme}' chưa được xác nhận cho Tiếng Việt lớp {grade} kỳ {period}. Chọn chủ điểm đã rà soát; không thể tiếp tục bằng chủ điểm ngoài kỳ.");

                _currentPaper = await gemini.GenerateVietExamAsync(
                    settings,
                    grade,
                    35,
                    _matrixItems.ToList(),
                    customPassage,
                    theme,
                    period,
                    ct
                );

                _currentPaper.TieuDe = $"BÀI KIỂM TRA ĐỊNH KỲ {period.ToUpper()}";
                _currentPaper.KhoiLop = grade;
                _currentPaper.Period = period;
                _currentPaper.DanhSachMaTran = _matrixItems.ToList();
                UpdateExportFileName();

                if (_currentPaper.CanhBaoKiemTra.Count > 0)
                {
                    MessageBox.Show(
                        "Đề Tiếng Việt đã được sinh nhưng có điểm cần giáo viên rà soát:\n\n- " + string.Join("\n- ", _currentPaper.CanhBaoKiemTra) +
                        "\n\nBạn vẫn có thể xem trước và xuất Word; nên kiểm tra kỹ nội dung (mã mục, bài đọc, hình thức câu) trước khi dùng.",
                        "Đề sinh kèm cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                DisplayVietPreview(_currentPaper);
                TxtVietAiStatus.Text = "Đã sinh hoàn tất bài đọc hiểu và bộ 11 câu hỏi Tiếng Việt!";
                LicenseManager.IncrementTrialCount();
                if (Window.GetWindow(this) is MainWindow mwSuccess) mwSuccess.UpdateStatusInfo();
                AutoSaveDraft();
            }
            catch (OperationCanceledException)
            {
                TxtVietAiStatus.Text = "Đã hủy sinh đề theo yêu cầu.";
                AppLogger.Info("Người dùng đã hủy sinh đề Tiếng Việt.");
            }
            catch (AiServiceException ex)
            {
                AppLogger.Error("Lỗi dịch vụ AI khi sinh đề Tiếng Việt", ex);
                MessageBox.Show(ex.Message, "Không sinh được đề", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtVietAiStatus.Text = "Không sinh được đề do lỗi từ dịch vụ AI.";
            }
            catch (Exception ex)
            {
                AppLogger.Error("Lỗi sinh đề Tiếng Việt", ex);
                MessageBox.Show($"Lỗi trong quá trình sinh đề Tiếng Việt: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtVietAiStatus.Text = "Quá trình sinh đề gặp sự cố. Vui lòng kiểm tra lại.";
            }
            finally
            {
                BtnGenerateVietExam.IsEnabled = true;
                BtnRewriteVietGuide.IsEnabled = true;
                BtnCancelVietGenerate.IsEnabled = true;
                BtnCancelVietGenerate.Visibility = Visibility.Collapsed;
                PbVietLoading.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Nút "Viết lại Hướng dẫn chấm (AI)": giữ nguyên đề và ma trận hiện có, chỉ nhờ AI
        /// viết lại trường Hướng dẫn chấm chi tiết cho từng câu (Đáp án gợi ý + Biểu điểm /
        /// Đáp án đúng + Lý do) để giáo viên chấm được ngay.
        /// </summary>
        private async void BtnRewriteVietGuide_Click(object sender, RoutedEventArgs e)
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
                MessageBox.Show("Chưa có đề trên màn hình. Hãy sinh đề Tiếng Việt trước rồi mới viết lại hướng dẫn chấm.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (BtnCancelVietGenerate.Visibility == Visibility.Visible)
            {
                MessageBox.Show("Đang có tác vụ AI khác chạy. Vui lòng đợi hoàn tất hoặc bấm Hủy trước.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var settings = AppSettings.Load();
            if (string.IsNullOrWhiteSpace(settings.ActiveApiKey))
            {
                MessageBox.Show($"Chưa thiết lập {settings.ActiveProviderDisplayName} API Key!\nVui lòng vào mục 'Cài đặt' để nhập API Key của bạn trước.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _vietGenCts?.Dispose();
            _vietGenCts = new System.Threading.CancellationTokenSource();
            var ct = _vietGenCts.Token;

            BtnGenerateVietExam.IsEnabled = false;
            BtnRewriteVietGuide.IsEnabled = false;
            BtnCancelVietGenerate.IsEnabled = true;
            BtnCancelVietGenerate.Visibility = Visibility.Visible;
            PbVietLoading.Visibility = Visibility.Visible;
            TxtVietAiStatus.Text = $"Đang viết lại hướng dẫn chấm chi tiết cho {_currentPaper.DanhSachCauHoi.Count} câu bằng {settings.ActiveProviderDisplayName}...";
            AppLogger.Info($"Bắt đầu viết lại hướng dẫn chấm Tiếng Việt bằng {settings.ActiveProviderDisplayName}/{settings.ActiveModel}.");

            try
            {
                var gemini = new GeminiService();
                int updated = await gemini.RewriteVietGradingGuideAsync(settings, _currentPaper, ct);

                DisplayVietPreview(_currentPaper);
                AutoSaveDraft();
                TxtVietAiStatus.Text = $"Đã viết lại hướng dẫn chấm chi tiết cho {updated}/{_currentPaper.DanhSachCauHoi.Count} câu. Sang Bước 3 xuất Word để kiểm tra.";
                LicenseManager.IncrementTrialCount();
                if (Window.GetWindow(this) is MainWindow mwSuccess) mwSuccess.UpdateStatusInfo();
            }
            catch (OperationCanceledException)
            {
                TxtVietAiStatus.Text = "Đã hủy viết lại hướng dẫn chấm theo yêu cầu.";
                AppLogger.Info("Người dùng đã hủy viết lại hướng dẫn chấm Tiếng Việt.");
            }
            catch (AiServiceException ex)
            {
                AppLogger.Error("Lỗi dịch vụ AI khi viết lại hướng dẫn chấm Tiếng Việt", ex);
                MessageBox.Show(ex.Message, "Không viết lại được hướng dẫn chấm", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtVietAiStatus.Text = "Không viết lại được hướng dẫn chấm do lỗi từ dịch vụ AI.";
            }
            catch (Exception ex)
            {
                AppLogger.Error("Lỗi viết lại hướng dẫn chấm Tiếng Việt", ex);
                MessageBox.Show($"Lỗi trong quá trình viết lại hướng dẫn chấm: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtVietAiStatus.Text = "Quá trình viết lại hướng dẫn chấm gặp sự cố. Vui lòng kiểm tra lại.";
            }
            finally
            {
                BtnGenerateVietExam.IsEnabled = true;
                BtnRewriteVietGuide.IsEnabled = true;
                BtnCancelVietGenerate.IsEnabled = true;
                BtnCancelVietGenerate.Visibility = Visibility.Collapsed;
                PbVietLoading.Visibility = Visibility.Collapsed;
            }
        }

        private void DisplayVietPreview(VietExamPaper paper)
        {
            PnlVietPreview.Children.Clear();

            // Card Bài đọc thầm
            if (paper.BaiDoc != null && !string.IsNullOrEmpty(paper.BaiDoc.NoiDung))
            {
                var passageCard = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(240, 253, 250)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(153, 246, 228)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(18),
                    Margin = new Thickness(0, 0, 0, 18)
                };

                var passageSp = new StackPanel();

                passageSp.Children.Add(new TextBlock
                {
                    Text = "VĂN BẢN ĐỌC THẦM",
                    FontWeight = FontWeights.Bold,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(13, 148, 136)),
                    Margin = new Thickness(0, 0, 0, 6)
                });

                passageSp.Children.Add(new TextBlock
                {
                    Text = paper.BaiDoc.TieuDe.ToUpper(),
                    FontWeight = FontWeights.Bold,
                    FontSize = 15,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
                    Margin = new Thickness(0, 0, 0, 10)
                });

                passageSp.Children.Add(new TextBlock
                {
                    Text = paper.BaiDoc.NoiDung,
                    FontSize = 13.5,
                    LineHeight = 22,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                    Margin = new Thickness(0, 0, 0, 8)
                });

                if (!string.IsNullOrEmpty(paper.BaiDoc.TacGia))
                {
                    passageSp.Children.Add(new TextBlock
                    {
                        Text = $"Theo {paper.BaiDoc.TacGia}",
                        FontStyle = FontStyles.Italic,
                        FontSize = 12.5,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139))
                    });
                }

                passageCard.Child = passageSp;
                PnlVietPreview.Children.Add(passageCard);
            }

            // Danh sách 11 câu hỏi
            foreach (var q in paper.DanhSachCauHoi.OrderBy(x => x.SoThuTu))
            {
                var card = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(16),
                    Margin = new Thickness(0, 0, 0, 14)
                };

                var sp = new StackPanel();

                // Header line
                var headerSp = new StackPanel { Orientation = Orientation.Horizontal };
                headerSp.Children.Add(new TextBlock
                {
                    Text = $"Câu {q.SoThuTu}: ",
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59))
                });

                var badge = new Border
                {
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(8, 2, 8, 2),
                    Margin = new Thickness(6, 0, 8, 0),
                    Background = q.MucDo == "M3" ? new SolidColorBrush(Color.FromRgb(254, 226, 226)) :
                                 q.MucDo == "M2" ? new SolidColorBrush(Color.FromRgb(254, 243, 199)) :
                                                   new SolidColorBrush(Color.FromRgb(204, 251, 241))
                };
                badge.Child = new TextBlock
                {
                    Text = q.MucDo,
                    FontWeight = FontWeights.Bold,
                    FontSize = 11,
                    Foreground = q.MucDo == "M3" ? new SolidColorBrush(Color.FromRgb(220, 38, 38)) :
                                 q.MucDo == "M2" ? new SolidColorBrush(Color.FromRgb(217, 119, 6)) :
                                                   new SolidColorBrush(Color.FromRgb(15, 118, 110))
                };
                headerSp.Children.Add(badge);

                headerSp.Children.Add(new TextBlock
                {
                    Text = $"({WordExportMathService.FormatPoint(q.Diem)} điểm) - {q.MachKienThuc}",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                    VerticalAlignment = VerticalAlignment.Center
                });
                sp.Children.Add(headerSp);

                // Content
                sp.Children.Add(new TextBlock
                {
                    Text = q.NoiDung,
                    FontSize = 13.5,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 8, 0, 8),
                    Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42))
                });

                // Options for TN
                if (q.LoaiCauHoi == "TN_4LuaChon" && !string.IsNullOrEmpty(q.DapAnA))
                {
                    var optGrid = new Grid { Margin = new Thickness(10, 4, 0, 4) };
                    optGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    optGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    optGrid.RowDefinitions.Add(new RowDefinition());
                    optGrid.RowDefinitions.Add(new RowDefinition());

                    AddOption(optGrid, $"A. {q.DapAnA}", q.DapAnDung == "A", 0, 0);
                    AddOption(optGrid, $"B. {q.DapAnB}", q.DapAnDung == "B", 0, 1);
                    AddOption(optGrid, $"C. {q.DapAnC}", q.DapAnDung == "C", 1, 0);
                    AddOption(optGrid, $"D. {q.DapAnD}", q.DapAnDung == "D", 1, 1);

                    sp.Children.Add(optGrid);
                }

                // TL: Gợi ý chấm & số dòng ô ly
                if (q.LoaiCauHoi != "TN_4LuaChon")
                {
                    var guideBorder = new Border
                    {
                        Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(6),
                        Padding = new Thickness(12, 10, 12, 10),
                        Margin = new Thickness(0, 8, 0, 0)
                    };
                    var guideSp = new StackPanel();
                    if (!string.IsNullOrEmpty(q.HuongDanCham))
                    {
                        guideSp.Children.Add(new TextBlock
                        {
                            Text = $"Hướng dẫn chấm: {q.HuongDanCham}",
                            FontSize = 12.5,
                            TextWrapping = TextWrapping.Wrap,
                            Foreground = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
                            Margin = new Thickness(0, 0, 0, 8)
                        });
                    }

                    // Tùy chọn số dòng ô ly lớn
                    var olyRow = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                    olyRow.Children.Add(new TextBlock
                    {
                        Text = "Số dòng ô ly cho học sinh viết:",
                        FontWeight = FontWeights.SemiBold,
                        FontSize = 12,
                        Foreground = new SolidColorBrush(Color.FromRgb(15, 118, 110)),
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 8, 0)
                    });

                    var cboLines = new ComboBox
                    {
                        Width = 100,
                        Padding = new Thickness(6, 2, 6, 2),
                        VerticalContentAlignment = VerticalAlignment.Center,
                        FontSize = 12
                    };

                    int curLines = q.SoDongOly > 0 ? q.SoDongOly : (q.SoThuTu == 6 || q.SoThuTu == 11 ? 3 : 2);
                    q.SoDongOly = curLines;

                    for (int n = 2; n <= 8; n++)
                    {
                        var cbi = new ComboBoxItem
                        {
                            Content = $"{n} dòng",
                            Tag = n
                        };
                        cboLines.Items.Add(cbi);
                        if (n == curLines) cbi.IsSelected = true;
                    }

                    var lblLyDetail = new TextBlock
                    {
                        Text = $"({curLines * 4} hàng con - lưới 4 ô ly / 5 dòng kẻ chuẩn tiểu học)",
                        FontStyle = FontStyles.Italic,
                        FontSize = 11.5,
                        Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(8, 0, 0, 0)
                    };

                    cboLines.SelectionChanged += (s, ev) =>
                    {
                        if (cboLines.SelectedItem is ComboBoxItem item && item.Tag is int lines)
                        {
                            q.SoDongOly = lines;
                            lblLyDetail.Text = $"({lines * 4} hàng con - lưới 4 ô ly / 5 dòng kẻ chuẩn tiểu học)";
                        }
                    };

                    olyRow.Children.Add(cboLines);
                    olyRow.Children.Add(lblLyDetail);
                    guideSp.Children.Add(olyRow);

                    guideBorder.Child = guideSp;
                    sp.Children.Add(guideBorder);
                }
                else if (!string.IsNullOrEmpty(q.HuongDanCham))
                {
                    var guideBorder = new Border
                    {
                        Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                        CornerRadius = new CornerRadius(6),
                        Padding = new Thickness(12, 8, 12, 8),
                        Margin = new Thickness(0, 8, 0, 0)
                    };
                    var guideSp = new StackPanel();
                    guideSp.Children.Add(new TextBlock
                    {
                        Text = $"Hướng dẫn chấm: {q.HuongDanCham}",
                        FontSize = 12,
                        TextWrapping = TextWrapping.Wrap,
                        Foreground = new SolidColorBrush(Color.FromRgb(51, 65, 85))
                    });
                    guideBorder.Child = guideSp;
                    sp.Children.Add(guideBorder);
                }

                card.Child = sp;
                PnlVietPreview.Children.Add(card);
            }

            UpdatePaperPointsBadge();
        }

        private void AddOption(Grid grid, string text, bool isCorrect, int row, int col)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontSize = 13,
                FontWeight = isCorrect ? FontWeights.Bold : FontWeights.Normal,
                Foreground = isCorrect ? new SolidColorBrush(Color.FromRgb(22, 163, 74)) : new SolidColorBrush(Color.FromRgb(51, 65, 85)),
                Margin = new Thickness(0, 2, 10, 2)
            };
            Grid.SetRow(tb, row);
            Grid.SetColumn(tb, col);
            grid.Children.Add(tb);
        }

        // ========================= LƯU / MỞ / TỰ ĐỘNG LƯU ĐỀ =========================

        private ExamDraft BuildDraft()
        {
            return new ExamDraft
            {
                Kind = "Viet",
                Grade = GetSelectedGrade(),
                Period = GetSelectedPeriod(),
                ExportFileName = TxtExportVietFileName.Text.Trim(),
                RequiresCurriculumReview = CurriculumScopePolicy.NeedsVietCurriculumReview(GetSelectedGrade(), GetSelectedPeriod(), _matrixItems.ToList(), _currentPaper?.DanhSachCauHoi, _currentPaper?.BaiDoc),
                VietPaper = _currentPaper,
                VietMatrix = _matrixItems.ToList(),
                UseCustomPassage = RbCustomPassage.IsChecked == true,
                PassageTheme = TxtPassageTheme.Text.Trim(),
                CustomPassageTitle = TxtCustomTitle.Text.Trim(),
                CustomPassageAuthor = TxtCustomAuthor.Text.Trim(),
                CustomPassageSource = TxtCustomSource.Text.Trim(),
                CustomPassageContent = TxtCustomContent.Text.Trim()
            };
        }

        private void SaveDraftTo(string path)
        {
            if (DraftStore.Save(BuildDraft(), path))
            {
                AppLogger.Info($"Đã lưu đề Tiếng Việt vào: {path}");
            }
            else
            {
                AppLogger.Warn($"Không lưu được đề Tiếng Việt vào: {path}");
            }
        }

        private void AutoSaveDraft()
        {
            if (_isApplyingDraft) return; // Đang khôi phục thì không ghi đè bản nháp dở dang

            bool hasPaper = _currentPaper is { DanhSachCauHoi.Count: > 0 };
            bool hasMatrix = _matrixItems.Count > 0;
            if (!hasPaper && !hasMatrix) return;

            DraftStore.Save(BuildDraft(), DraftStore.AutoSavePath("Viet"));
        }

        private void TryRestoreAutoSave()
        {
            var draft = DraftStore.Load(DraftStore.AutoSavePath("Viet"));
            if (draft == null) return;

            bool hasPaper = draft.VietPaper is { DanhSachCauHoi.Count: > 0 };
            bool hasMatrix = draft.VietMatrix is { Count: > 0 };
            if (!hasPaper && !hasMatrix) return;

            ApplyDraft(draft);
            TxtVietAiStatus.Text = hasPaper
                ? $"Đã khôi phục đề Tiếng Việt soạn dở (lưu lúc {draft.SavedAt:HH:mm dd/MM}). Bấm 'Đề mới' nếu muốn bắt đầu lại."
                : $"Đã khôi phục ma trận Tiếng Việt soạn dở (lưu lúc {draft.SavedAt:HH:mm dd/MM}). Bấm 'Đề mới' nếu muốn bắt đầu lại.";
            if (draft.RequiresCurriculumReview)
                TxtVietAiStatus.Text += " ⚠ Bản nháp cũ/chưa ánh xạ chương trình: sinh đề và xuất bị khóa đến khi lập lại ma trận với mục đã xác nhận.";
            AppLogger.Info("Đã khôi phục bản nháp đề Tiếng Việt soạn dở.");
        }

        private void ApplyDraft(ExamDraft draft)
        {
            draft.RequiresCurriculumReview |= CurriculumScopePolicy.NeedsVietCurriculumReview(draft.Grade, draft.Period, draft.VietMatrix ?? new List<VietMatrixItem>(), draft.VietPaper?.DanhSachCauHoi, draft.VietPaper?.BaiDoc);
            _isApplyingDraft = true;
            try
            {
                CboGradeViet.SelectedIndex = draft.Grade == 5 ? 1 : 0;

                if (CboPeriodViet != null)
                {
                    foreach (ComboBoxItem item in CboPeriodViet.Items)
                    {
                        if (string.Equals(item.Content?.ToString(), draft.Period, StringComparison.OrdinalIgnoreCase))
                        {
                            CboPeriodViet.SelectedItem = item;
                            break;
                        }
                    }
                }

                if (draft.VietMatrix != null)
                {
                    _matrixItems = new ObservableCollection<VietMatrixItem>(draft.VietMatrix);
                    DgVietMatrix.ItemsSource = _matrixItems;
                    ClearPreAiSnapshot();
                    UpdateSummary();
                }

                // Khôi phục phần ngữ liệu bài đọc
                if (draft.UseCustomPassage == true)
                {
                    RbCustomPassage.IsChecked = true;
                    TxtCustomTitle.Text = draft.CustomPassageTitle ?? string.Empty;
                    TxtCustomAuthor.Text = draft.CustomPassageAuthor ?? string.Empty;
                    TxtCustomSource.Text = draft.CustomPassageSource ?? string.Empty;
                    TxtCustomContent.Text = draft.CustomPassageContent ?? string.Empty;
                }
                else
                {
                    RbAiGeneratePassage.IsChecked = true;
                }

                if (!string.IsNullOrWhiteSpace(draft.PassageTheme))
                {
                    TxtPassageTheme.Text = draft.PassageTheme;
                    TxtPassageTheme.Tag = "manual"; // Giữ nguyên chủ đề đã lưu, không bị ghi đè
                }

                _currentPaper = draft.VietPaper;
                if (_currentPaper != null) _currentPaper.Period = draft.Period;
                if (!string.IsNullOrWhiteSpace(draft.ExportFileName))
                {
                    TxtExportVietFileName.Text = draft.ExportFileName;
                }

                if (_currentPaper != null)
                {
                    DisplayVietPreview(_currentPaper);
                }
            }
            finally
            {
                _isApplyingDraft = false;
            }
        }

        private void BtnSaveVietDraft_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPaper == null || _currentPaper.DanhSachCauHoi.Count == 0)
            {
                MessageBox.Show("Chưa có đề nào để lưu. Hãy sinh đề trước.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Lưu đề Tiếng Việt để mở lại sau",
                FileName = $"DeTiengViet_Lop{GetSelectedGrade()}_{GetSelectedPeriod().Replace(' ', '_')}.exam.json",
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

        private void BtnOpenVietDraft_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Mở đề Tiếng Việt đã lưu",
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
            if (string.Equals(draft.Kind, "Math", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Tệp này là đề Toán. Hãy mở ở mục 'Toán'.", "Sai môn học", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (draft.VietPaper == null || draft.VietPaper.DanhSachCauHoi.Count == 0)
            {
                MessageBox.Show("Tệp này không có nội dung đề Tiếng Việt hợp lệ.", "Không mở được", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ApplyDraft(draft);
            TxtVietAiStatus.Text = $"Đã mở đề Tiếng Việt đã lưu (lúc {draft.SavedAt:HH:mm dd/MM}).";
            if (draft.RequiresCurriculumReview)
                TxtVietAiStatus.Text += " ⚠ Bản nháp cũ/chưa ánh xạ chương trình; sinh hoặc xuất sẽ bị chặn cho đến khi lập lại ma trận theo mục đã xác nhận.";
            AppLogger.Info($"Đã mở đề Tiếng Việt từ: {dlg.FileName}");
        }

        private void BtnNewVietExam_Click(object sender, RoutedEventArgs e)
        {
            var res = MessageBox.Show(
                "Bắt đầu đề mới?\n\nMa trận sẽ trở về mặc định và bản tự động lưu sẽ bị xóa. Đề đang soạn trên màn hình cũng sẽ bị bỏ.",
                "Đề mới", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res != MessageBoxResult.Yes) return;

            _currentPaper = null;
            PnlVietPreview.Children.Clear();
            PnlVietPreview.Children.Add(TxtVietEmptyNotice);

            // Xóa ngữ liệu của đề cũ để không lẫn sang đề mới
            RbAiGeneratePassage.IsChecked = true;
            TxtCustomTitle.Text = string.Empty;
            TxtCustomAuthor.Text = string.Empty;
            TxtCustomSource.Text = string.Empty;
            TxtCustomContent.Text = string.Empty;
            TxtPassageTheme.Tag = "auto"; // Cho phép nạp lại chủ đề mặc định theo khối/lớp

            LoadDefaultMatrix();
            UpdateExportFileName();
            DraftStore.Delete(DraftStore.AutoSavePath("Viet"));
            TxtVietAiStatus.Text = "Đã tạo đề mới. Ma trận đã trở về mặc định.";
        }

        private void BtnExportVietMatrixOnly_Click(object sender, RoutedEventArgs e)
        {
            if (_matrixItems.Count == 0)
            {
                MessageBox.Show("Ma trận chưa có câu hỏi nào.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var settings = AppSettings.Load();
            int grade = GetSelectedGrade();
            string period = GetSelectedPeriod();
            string safePeriod = period.Replace(" ", "_");
            string exportDir = ExportPaths.GetExportFolder(settings);
            string fullPath = Path.Combine(exportDir, $"Ma_Tran_Va_Dac_Ta_TiengViet_Lop_{grade}_{safePeriod}.docx");

            try
            {
                CurriculumScopePolicy.ValidateVietMatrix(grade, period, _matrixItems.ToList());
                string? saved = ExportHelper.RunGuarded(
                    fullPath,
                    p => WordExportVietService.ExportVietMatrixOnlyToDocx(_matrixItems.ToList(), settings, grade, p, period),
                    "Lưu Ma trận Tiếng Việt với tên khác");
                if (saved == null) return;

                var res = MessageBox.Show($"Đã xuất thành công tệp Ma trận & Bản đặc tả Tiếng Việt:\n{saved}\n\nBạn có muốn mở tệp ngay không?", "Thành công", MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (res == MessageBoxResult.Yes)
                {
                    Process.Start(new ProcessStartInfo { FileName = saved, UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("Lỗi khi xuất Ma trận Tiếng Việt", ex);
                MessageBox.Show($"Lỗi khi xuất Ma trận Tiếng Việt: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnExportVietWord_Click(object sender, RoutedEventArgs e)
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
            string fileName = TxtExportVietFileName.Text.Trim();
            if (string.IsNullOrEmpty(fileName))
            {
                int grade = _currentPaper.KhoiLop > 0 ? _currentPaper.KhoiLop : 4;
                string periodClean = GetSelectedPeriod().Replace(" ", "_");
                fileName = $"De_Kiem_Tra_TiengViet_Lop_{grade}_{periodClean}.docx";
            }
            if (!fileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            {
                fileName += ".docx";
            }

            string exportDir = ExportPaths.GetExportFolder(settings);
            string fullPath = Path.Combine(exportDir, fileName);

            // Sai tổng điểm bị chặn để các bảng trong DOCX luôn dùng cùng dữ liệu đã kiểm tra.
            double paperTotal = _currentPaper.DanhSachCauHoi.Sum(q => q.Diem) + 2.0;
            if (Math.Abs(paperTotal - 10.0) >= 0.05)
            {
                MessageBox.Show($"Không thể xuất: tổng điểm đề Tiếng Việt hiện tại là {paperTotal:0.#}, chưa khớp 10 điểm.", "Đề chưa hợp lệ", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                int scopeGrade = _currentPaper.KhoiLop > 0 ? _currentPaper.KhoiLop : GetSelectedGrade();
                bool skipValidation = false;
                try
                {
                    CurriculumScopePolicy.ValidateVietQuestions(scopeGrade, GetSelectedPeriod(), _currentPaper.DanhSachCauHoi, _matrixItems.ToList());
                    CurriculumScopePolicy.ValidateVietPassageMatchesMatrix(_currentPaper.BaiDoc, _matrixItems.ToList(), scopeGrade, GetSelectedPeriod());
                }
                catch (Exception vex) when (vex is InvalidOperationException || vex is ArgumentException)
                {
                    // Đề được sinh kèm cảnh báo (AI chưa khớp ma trận): giáo viên đã được báo; cho xuất sau khi xác nhận.
                    if (_currentPaper.CanhBaoKiemTra.Count == 0) throw;
                    var choice = MessageBox.Show(
                        "Đề Tiếng Việt còn điểm cần giáo viên rà soát:\n\n- " + string.Join("\n- ", _currentPaper.CanhBaoKiemTra) +
                        $"\n\nKiểm tra chặn xuất: {vex.Message}\n\nVẫn xuất file Word?",
                        "Đề có cảnh báo", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (choice != MessageBoxResult.Yes) return;
                    skipValidation = true;
                }
                _currentPaper.Period = GetSelectedPeriod();
                _currentPaper.DanhSachMaTran = _matrixItems.ToList();
                string? saved = ExportHelper.RunGuarded(
                    fullPath,
                    p =>
                    {
                        if (RbExportVietExamOnly.IsChecked == true)
                        {
                            WordExportVietService.ExportVietExamOnlyToDocx(_currentPaper!, settings, p, skipValidation);
                        }
                        else
                        {
                            WordExportVietService.ExportVietFullPackageToDocx(_currentPaper!, settings, p, skipValidation);
                        }
                    },
                    "Lưu đề Tiếng Việt với tên khác");
                if (saved == null) return;

                _lastExportedFilePath = saved;

                TxtExportVietStatus.Text = $"Đã xuất thành công tệp Word:\n{saved}";
                BtnOpenVietFolder.Visibility = Visibility.Visible;
                BtnOpenVietFile.Visibility = Visibility.Visible;

                AutoSaveDraft();
            }
            catch (Exception ex)
            {
                AppLogger.Error("Lỗi khi xuất Word (Tiếng Việt)", ex);
                MessageBox.Show($"Lỗi khi xuất Word: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnOpenVietFolder_Click(object sender, RoutedEventArgs e)
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

        private void BtnOpenVietFile_Click(object sender, RoutedEventArgs e)
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
