using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace TH_ExamGen.UI.Helpers
{
    /// <summary>
    /// Helper hiển thị trực quan các công thức Toán học (phân số, hỗn số, số mũ đơn vị đo, toán tử)
    /// Thay thế mã LaTeX thô (\frac{a}{b}) bằng giao diện trực quan 100% Native WPF không cần thư viện ngoài.
    /// </summary>
    public static class MathTextHelper
    {
        // Regex phát hiện phân số \frac{num}{den}
        private static readonly Regex FractionRegex = new Regex(
            @"(\\frac\{([^{}]+)\}\{([^{}]+)\})",
            RegexOptions.Compiled);

        /// <summary>
        /// Chuẩn hóa chuỗi công thức: sửa lỗi escape JSON FormFeed (\u000c / rac{),
        /// chuyển số mũ đơn vị đo (m^2 -> m²), và các toán tử LaTeX sang ký hiệu Unicode.
        /// </summary>
        public static string NormalizeMathText(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            // 1. Khắc phục FormFeed (\u000c) hoặc lỗi mất dấu \ từ JSON deserializer
            string s = input.Replace("\u000crac{", @"\frac{").Replace("\u000c", "");
            s = Regex.Replace(s, @"(?<![a-zA-Z\\])rac\{", @"\frac{");

            // 2. Dấu chấm lửng
            s = s.Replace(@"\dots", "...").Replace(@"\ldots", "...");

            // 3. Đơn vị đo diện tích và thể tích có số mũ
            s = Regex.Replace(s, @"\b(km|hm|dam|m|dm|cm|mm)\^2\b", "$1²", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"\b(km|hm|dam|m|dm|cm|mm)\^3\b", "$1³", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"\b(km|hm|dam|m|dm|cm|mm)\^\{2\}\b", "$1²", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"\b(km|hm|dam|m|dm|cm|mm)\^\{3\}\b", "$1³", RegexOptions.IgnoreCase);

            // 4. Toán tử LaTeX thông dụng
            s = s.Replace(@"\times", "×")
                 .Replace(@"\div", "÷")
                 .Replace(@"\pm", "±")
                 .Replace(@"\le", "≤")
                 .Replace(@"\leq", "≤")
                 .Replace(@"\ge", "≥")
                 .Replace(@"\geq", "≥")
                 .Replace(@"\ne", "≠")
                 .Replace(@"\neq", "≠")
                 .Replace(@"\cdot", "·");

            return s;
        }

        /// <summary>
        /// Kiểm tra xem văn bản có chứa thành phần toán học cần render đặc biệt không
        /// </summary>
        public static bool ContainsMath(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            return text.Contains(@"\frac") || text.Contains("rac{") || text.Contains("\u000c") ||
                   text.Contains("$") || text.Contains("²") || text.Contains("³") ||
                   text.Contains(@"\times") || text.Contains(@"\div") || text.Contains("^");
        }

        /// <summary>
        /// Phân tích cú pháp và nạp các Run / InlineUIContainer (phân số trực quan) vào InlineCollection
        /// </summary>
        public static void PopulateInlines(InlineCollection inlines, string rawText, double fontSize = 13.0, Brush? foreground = null)
        {
            inlines.Clear();
            if (string.IsNullOrEmpty(rawText)) return;

            var fg = foreground ?? new SolidColorBrush(Color.FromRgb(30, 41, 59));
            string text = NormalizeMathText(rawText);

            int lastIdx = 0;
            var matches = FractionRegex.Matches(text);

            if (matches.Count == 0)
            {
                AppendTextWithMarkdown(inlines, text, fontSize, fg);
                return;
            }

            foreach (Match match in matches)
            {
                if (match.Index > lastIdx)
                {
                    string before = text.Substring(lastIdx, match.Index - lastIdx);
                    AppendTextWithMarkdown(inlines, before, fontSize, fg);
                }

                string num = match.Groups[2].Value.Trim();
                string den = match.Groups[3].Value.Trim();

                // Tạo visual fraction inline (tử số / đường kẻ ngang / mẫu số)
                inlines.Add(CreateFractionInline(num, den, fontSize, fg));

                lastIdx = match.Index + match.Length;
            }

            if (lastIdx < text.Length)
            {
                string after = text.Substring(lastIdx);
                AppendTextWithMarkdown(inlines, after, fontSize, fg);
            }
        }

        private static InlineUIContainer CreateFractionInline(string numerator, string denominator, double fontSize, Brush foreground)
        {
            var grid = new Grid
            {
                Margin = new Thickness(3, 0, 3, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            double fSize = Math.Max(10.0, fontSize * 0.88);

            // 1. Tử số
            var txtNum = new TextBlock
            {
                Text = numerator,
                FontSize = fSize,
                FontFamily = new FontFamily("Times New Roman, Cambria Math, Segoe UI"),
                FontWeight = FontWeights.Normal,
                Foreground = foreground,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(2, 0, 2, 0)
            };
            Grid.SetRow(txtNum, 0);
            grid.Children.Add(txtNum);

            // 2. Đường gạch ngang phân số
            var line = new Border
            {
                Height = 1.2,
                Background = foreground,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 1, 0, 1)
            };
            Grid.SetRow(line, 1);
            grid.Children.Add(line);

            // 3. Mẫu số
            var txtDen = new TextBlock
            {
                Text = denominator,
                FontSize = fSize,
                FontFamily = new FontFamily("Times New Roman, Cambria Math, Segoe UI"),
                FontWeight = FontWeights.Normal,
                Foreground = foreground,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(2, 0, 2, 0)
            };
            Grid.SetRow(txtDen, 2);
            grid.Children.Add(txtDen);

            return new InlineUIContainer(grid)
            {
                BaselineAlignment = BaselineAlignment.Center
            };
        }

        private static void AppendTextWithMarkdown(InlineCollection inlines, string text, double fontSize, Brush foreground)
        {
            if (string.IsNullOrEmpty(text)) return;

            // Xóa dấu $ nếu có
            text = text.Replace("$", "");

            var parts = Regex.Split(text, @"(\*\*[^*]+?\*\*)");
            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part)) continue;

                if (part.StartsWith("**") && part.EndsWith("**") && part.Length > 4)
                {
                    string inner = part.Substring(2, part.Length - 4);
                    inlines.Add(new Run(inner)
                    {
                        FontFamily = new FontFamily("Times New Roman, Segoe UI"),
                        FontSize = fontSize,
                        FontWeight = FontWeights.Bold,
                        Foreground = foreground
                    });
                }
                else
                {
                    inlines.Add(new Run(part)
                    {
                        FontFamily = new FontFamily("Times New Roman, Segoe UI"),
                        FontSize = fontSize,
                        Foreground = foreground
                    });
                }
            }
        }

        /// <summary>
        /// Tạo một TextBlock hoàn chỉnh đã được render công thức toán
        /// </summary>
        public static TextBlock CreateMathTextBlock(string text, double fontSize = 13.0, Brush? foreground = null)
        {
            var tb = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                LineHeight = fontSize * 1.55
            };
            PopulateInlines(tb.Inlines, text, fontSize, foreground);
            return tb;
        }

        /// <summary>
        /// Tạo Card hiển thị xem trước trực tiếp (Live Preview) công thức toán đồng bộ với TextBox nhập liệu
        /// </summary>
        public static Border CreateMathPreviewCard(string label, TextBox sourceTextBox, double fontSize = 13.5)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var sp = new StackPanel();

            var headerSp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
            headerSp.Children.Add(new TextBlock
            {
                Text = label,
                FontWeight = FontWeights.Bold,
                FontSize = 11.5,
                Foreground = new SolidColorBrush(Color.FromRgb(79, 70, 229))
            });
            headerSp.Children.Add(new TextBlock
            {
                Text = " (Xem trước công thức phân số, hỗn số, số mũ):",
                FontSize = 11,
                FontStyle = FontStyles.Italic,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139))
            });
            sp.Children.Add(headerSp);

            var previewTb = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                LineHeight = fontSize * 1.55,
                FontSize = fontSize
            };
            PopulateInlines(previewTb.Inlines, sourceTextBox.Text, fontSize);
            sp.Children.Add(previewTb);

            border.Child = sp;

            sourceTextBox.TextChanged += (s, ev) =>
            {
                PopulateInlines(previewTb.Inlines, sourceTextBox.Text, fontSize);
            };

            return border;
        }

        /// <summary>
        /// Tạo khung xem trước công thức cho từng phương án trắc nghiệm A, B, C, D
        /// Tự động hiển thị khi phương án chứa phân số/số mũ và ẩn khi là chữ thường để giao diện gọn gàng.
        /// </summary>
        public static FrameworkElement CreateOptionPreviewBlock(TextBox sourceTextBox, bool isCorrect)
        {
            var border = new Border
            {
                Background = isCorrect ? new SolidColorBrush(Color.FromRgb(240, 253, 244)) : new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                BorderBrush = isCorrect ? new SolidColorBrush(Color.FromRgb(187, 247, 208)) : new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 3, 6, 3),
                Margin = new Thickness(22, 2, 0, 2)
            };

            var tb = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12.0
            };
            border.Child = tb;

            void UpdateVisibility()
            {
                string text = sourceTextBox.Text ?? "";
                if (ContainsMath(text))
                {
                    PopulateInlines(tb.Inlines, text, 12.0);
                    border.Visibility = Visibility.Visible;
                }
                else
                {
                    border.Visibility = Visibility.Collapsed;
                }
            }

            UpdateVisibility();
            sourceTextBox.TextChanged += (s, ev) => UpdateVisibility();

            return border;
        }
    }
}
