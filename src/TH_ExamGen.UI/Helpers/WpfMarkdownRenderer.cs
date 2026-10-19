using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace TH_ExamGen.UI.Helpers
{
    public static class WpfMarkdownRenderer
    {
        private static readonly Regex InlineRegex = new Regex(
            @"(\*\*\*(?:[^*]+)\*\*\*|\*\*(?:[^*]+)\*\*|\*(?:[^*]+)\*|___(?:[^_]+)___|__(?:[^_]+)__|_(?:[^_]+)_|`([^`]+)`|[^`*_]+|[*_`])",
            RegexOptions.Compiled);

        public static FlowDocument RenderToFlowDocument(string markdown)
        {
            var doc = new FlowDocument
            {
                PagePadding = new Thickness(14, 10, 14, 10),
                FontFamily = new FontFamily("Segoe UI, Arial"),
                FontSize = 13.5,
                Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                Background = Brushes.White
            };

            if (string.IsNullOrWhiteSpace(markdown))
            {
                return doc;
            }

            var lines = markdown.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');

            foreach (var rawLine in lines)
            {
                string line = rawLine.TrimEnd();

                if (string.IsNullOrWhiteSpace(line))
                {
                    // Dòng trống -> tạo khoảng cách nhẹ
                    var emptyP = new Paragraph { Margin = new Thickness(0, 0, 0, 6), FontSize = 6 };
                    doc.Blocks.Add(emptyP);
                    continue;
                }

                // Tiêu đề cấp 1 (# Title)
                if (line.StartsWith("# "))
                {
                    var p = new Paragraph
                    {
                        FontSize = 16,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
                        Margin = new Thickness(0, 10, 0, 4)
                    };
                    AppendInlineMarkdown(p, line.Substring(2).Trim());
                    doc.Blocks.Add(p);
                    continue;
                }

                // Tiêu đề cấp 2 (## Title)
                if (line.StartsWith("## "))
                {
                    var p = new Paragraph
                    {
                        FontSize = 14.5,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(13, 148, 136)),
                        Margin = new Thickness(0, 8, 0, 4)
                    };
                    AppendInlineMarkdown(p, line.Substring(3).Trim());
                    doc.Blocks.Add(p);
                    continue;
                }

                // Tiêu đề cấp 3 (### Title)
                if (line.StartsWith("### "))
                {
                    var p = new Paragraph
                    {
                        FontSize = 14,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                        Margin = new Thickness(0, 6, 0, 2)
                    };
                    AppendInlineMarkdown(p, line.Substring(4).Trim());
                    doc.Blocks.Add(p);
                    continue;
                }

                // Tiêu đề cấp 4 (#### Title)
                if (line.StartsWith("#### "))
                {
                    var p = new Paragraph
                    {
                        FontSize = 13.5,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
                        Margin = new Thickness(0, 4, 0, 2)
                    };
                    AppendInlineMarkdown(p, line.Substring(5).Trim());
                    doc.Blocks.Add(p);
                    continue;
                }

                // Gạch ngang phân cách (--- hoặc ***)
                if (line.Trim() == "---" || line.Trim() == "***")
                {
                    var hr = new BlockUIContainer(new System.Windows.Shapes.Rectangle
                    {
                        Height = 1,
                        Fill = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                        Margin = new Thickness(0, 6, 0, 6)
                    });
                    doc.Blocks.Add(hr);
                    continue;
                }

                // Gạch đầu dòng (- item, * item, + item)
                string trimmedStart = line.TrimStart();
                if (trimmedStart.StartsWith("- ") || trimmedStart.StartsWith("* ") || trimmedStart.StartsWith("+ "))
                {
                    int indentSpaces = line.Length - trimmedStart.Length;
                    double leftMargin = 16 + (indentSpaces * 4);

                    var p = new Paragraph
                    {
                        Margin = new Thickness(leftMargin, 2, 0, 2),
                        LineHeight = 22
                    };

                    var bulletRun = new Run("• ")
                    {
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(13, 148, 136))
                    };
                    p.Inlines.Add(bulletRun);

                    AppendInlineMarkdown(p, trimmedStart.Substring(2).Trim());
                    doc.Blocks.Add(p);
                    continue;
                }

                // Danh sách đánh số (1. item, 2. item...)
                var numMatch = Regex.Match(trimmedStart, @"^(\d+\.)\s+(.*)$");
                if (numMatch.Success)
                {
                    int indentSpaces = line.Length - trimmedStart.Length;
                    double leftMargin = 16 + (indentSpaces * 4);

                    var p = new Paragraph
                    {
                        Margin = new Thickness(leftMargin, 2, 0, 2),
                        LineHeight = 22
                    };

                    var numRun = new Run(numMatch.Groups[1].Value + " ")
                    {
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42))
                    };
                    p.Inlines.Add(numRun);

                    AppendInlineMarkdown(p, numMatch.Groups[2].Value.Trim());
                    doc.Blocks.Add(p);
                    continue;
                }

                // Trích dẫn (> quote)
                if (line.StartsWith("> "))
                {
                    var p = new Paragraph
                    {
                        Margin = new Thickness(12, 3, 0, 3),
                        BorderThickness = new Thickness(3, 0, 0, 0),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(13, 148, 136)),
                        Padding = new Thickness(8, 2, 0, 2),
                        FontStyle = FontStyles.Italic,
                        LineHeight = 22
                    };
                    AppendInlineMarkdown(p, line.Substring(2).Trim());
                    doc.Blocks.Add(p);
                    continue;
                }

                // Đoạn văn thông thường
                var normalP = new Paragraph
                {
                    Margin = new Thickness(0, 2, 0, 3),
                    LineHeight = 22
                };
                AppendInlineMarkdown(normalP, line);
                doc.Blocks.Add(normalP);
            }

            return doc;
        }

        private static void AppendInlineMarkdown(Paragraph p, string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            var matches = InlineRegex.Matches(text);
            foreach (Match match in matches)
            {
                string val = match.Value;

                // ***Bold Italic***
                if (val.Length >= 6 && val.StartsWith("***") && val.EndsWith("***"))
                {
                    p.Inlines.Add(new Run(val.Substring(3, val.Length - 6))
                    {
                        FontWeight = FontWeights.Bold,
                        FontStyle = FontStyles.Italic
                    });
                }
                // ___Bold Italic___
                else if (val.Length >= 6 && val.StartsWith("___") && val.EndsWith("___"))
                {
                    p.Inlines.Add(new Run(val.Substring(3, val.Length - 6))
                    {
                        FontWeight = FontWeights.Bold,
                        FontStyle = FontStyles.Italic
                    });
                }
                // **Bold**
                else if (val.Length >= 4 && val.StartsWith("**") && val.EndsWith("**"))
                {
                    p.Inlines.Add(new Run(val.Substring(2, val.Length - 4))
                    {
                        FontWeight = FontWeights.Bold
                    });
                }
                // __Bold__
                else if (val.Length >= 4 && val.StartsWith("__") && val.EndsWith("__"))
                {
                    p.Inlines.Add(new Run(val.Substring(2, val.Length - 4))
                    {
                        FontWeight = FontWeights.Bold
                    });
                }
                // *Italic*
                else if (val.Length >= 2 && val.StartsWith("*") && val.EndsWith("*"))
                {
                    p.Inlines.Add(new Run(val.Substring(1, val.Length - 2))
                    {
                        FontStyle = FontStyles.Italic
                    });
                }
                // _Italic_
                else if (val.Length >= 2 && val.StartsWith("_") && val.EndsWith("_"))
                {
                    p.Inlines.Add(new Run(val.Substring(1, val.Length - 2))
                    {
                        FontStyle = FontStyles.Italic
                    });
                }
                // `Code`
                else if (val.Length >= 2 && val.StartsWith("`") && val.EndsWith("`"))
                {
                    p.Inlines.Add(new Run(val.Substring(1, val.Length - 2))
                    {
                        FontFamily = new FontFamily("Consolas"),
                        Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                        Foreground = new SolidColorBrush(Color.FromRgb(190, 18, 60))
                    });
                }
                // Plain Text
                else
                {
                    p.Inlines.Add(new Run(val));
                }
            }
        }
    }
}
