using System;
using System.IO;
using System.Xml.Linq;
using Xceed.Document.NET;
using Xceed.Words.NET;

namespace TH_ExamGen.Services
{
    public static class DocxContentRenderer
    {
        private static readonly XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

        /// <summary>
        /// Chuẩn hóa dấu ngoặc kép dạng thẳng "..." hoặc \"...\" thành cặp dấu ngoặc kép cong typographic Việt Nam: “...”.
        /// </summary>
        public static string NormalizeVietnameseQuotes(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            var sb = new System.Text.StringBuilder();
            bool inQuote = false;
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (c == '\\' && i + 1 < input.Length && input[i + 1] == '"')
                {
                    c = '"';
                    i++;
                }

                if (c == '"')
                {
                    if (!inQuote)
                    {
                        sb.Append('“');
                        inQuote = true;
                    }
                    else
                    {
                        sb.Append('”');
                        inQuote = false;
                    }
                }
                else
                {
                    if (c == '“') inQuote = true;
                    else if (c == '”') inQuote = false;
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private static void AppendFormattedText(Paragraph paragraph, string text, string fontName, double fontSize, bool isBold, bool isItalic)
        {
            if (string.IsNullOrEmpty(text)) return;
            var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                if (i > 0)
                {
                    paragraph.Xml.Add(new XElement(w + "r", new XElement(w + "br")));
                }
                if (!string.IsNullOrEmpty(lines[i]))
                {
                    AppendInlineFormattedLine(paragraph, lines[i], fontName, fontSize, isBold, isItalic);
                }
            }
        }

        private static void AppendInlineFormattedLine(Paragraph paragraph, string line, string fontName, double fontSize, bool baseBold, bool baseItalic)
        {
            if (string.IsNullOrEmpty(line)) return;

            // Tách các đoạn Markdown: ***bold-italic***, **bold**, *italic*, ___bold-italic___, __bold__, _italic_
            var tokens = System.Text.RegularExpressions.Regex.Split(line, @"(\*\*\*[^*]+?\*\*\*|\*\*[^*]+?\*\*|\*[^*]+?\*|___[^_]+?___|__[^_]+?__|_[^_]+?_)");

            foreach (var token in tokens)
            {
                if (string.IsNullOrEmpty(token)) continue;

                string innerText = token;
                bool runBold = baseBold;
                bool runItalic = baseItalic;

                if (token.StartsWith("***") && token.EndsWith("***") && token.Length > 6)
                {
                    innerText = token.Substring(3, token.Length - 6);
                    runBold = true;
                    runItalic = true;
                }
                else if (token.StartsWith("___") && token.EndsWith("___") && token.Length > 6)
                {
                    innerText = token.Substring(3, token.Length - 6);
                    runBold = true;
                    runItalic = true;
                }
                else if (token.StartsWith("**") && token.EndsWith("**") && token.Length > 4)
                {
                    innerText = token.Substring(2, token.Length - 4);
                    runBold = true;
                }
                else if (token.StartsWith("__") && token.EndsWith("__") && token.Length > 4)
                {
                    innerText = token.Substring(2, token.Length - 4);
                    runBold = true;
                }
                else if (token.StartsWith("*") && token.EndsWith("*") && token.Length > 2)
                {
                    innerText = token.Substring(1, token.Length - 2);
                    runItalic = true;
                }
                else if (token.StartsWith("_") && token.EndsWith("_") && token.Length > 2)
                {
                    innerText = token.Substring(1, token.Length - 2);
                    runItalic = true;
                }

                // Chuẩn hóa dấu ngoặc kép typographic
                innerText = NormalizeVietnameseQuotes(innerText);

                // Tách các đoạn trong dấu ngoặc kép “...” hoặc "..." để tự động in nghiêng theo chuẩn sư phạm
                var quoteParts = System.Text.RegularExpressions.Regex.Split(innerText, @"(“[^\r\n”]+?”|""[^\r\n""]+?"")");
                foreach (var qp in quoteParts)
                {
                    if (string.IsNullOrEmpty(qp)) continue;
                    bool isQuoted = (qp.Length >= 2) && ((qp.StartsWith("“") && qp.EndsWith("”")) || (qp.StartsWith("\"") && qp.EndsWith("\"")));
                    var run = paragraph.Append(qp).Font(fontName).FontSize(fontSize);
                    if (runBold) run.Bold();
                    if (runItalic || isQuoted) run.Italic();
                }
            }
        }

        /// <summary>
        /// Chèn văn bản có chứa công thức toán LaTeX (phân số, phép tính) vào paragraph Word.
        /// Công thức sẽ được chuyển thành Word Equation chuẩn (OMML - Office Math Markup Language),
        /// cho phép giáo viên CLICK VÀO CHỈNH SỬA TRỰC TIẾP TRONG MICROSOFT WORD.
        /// </summary>
        public static void AppendTextWithFractions(
            DocX doc, 
            Paragraph paragraph, 
            string text, 
            string fontName = "Times New Roman", 
            double fontSize = 14.0, 
            bool isBold = false, 
            bool isItalic = false)
        {
            if (string.IsNullOrEmpty(text)) return;

            var parts = LatexToOmmlService.SplitContent(text);
            if (parts.Count == 0) return;
            if (parts.Count == 1 && !parts[0].IsMath)
            {
                AppendFormattedText(paragraph, parts[0].Text, fontName, fontSize, isBold, isItalic);
                return;
            }

            foreach (var part in parts)
            {
                if (!part.IsMath)
                {
                    AppendFormattedText(paragraph, part.Text, fontName, fontSize, isBold, isItalic);
                }
                else
                {
                    try
                    {
                        // Chuyển mã LaTeX sang Word Equation (OMML) với cỡ chữ tương ứng
                        XElement oMath = LatexToOmmlService.ConvertToOMath(part.Text, fontSize);
                        paragraph.Xml.Add(oMath);
                    }
                    catch
                    {
                        // Fallback nếu có lỗi XML: chèn text
                        AppendFormattedText(paragraph, part.Text, fontName, fontSize, isBold, isItalic);
                    }
                }
            }
        }
    }
}
