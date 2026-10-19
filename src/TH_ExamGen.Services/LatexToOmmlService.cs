using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace TH_ExamGen.Services
{
    public class TextOrMathPart
    {
        public bool IsMath { get; set; }
        public string Text { get; set; } = string.Empty;
    }

    /// <summary>
    /// Bộ chuyển đổi công thức Toán LaTeX sang Word Equation (OMML - Office Math Markup Language)
    /// Giúp công thức (phân số, phép tính) hiển thị chuẩn và CÓ THỂ CHỈNH SỬA TRỰC TIẾP trong Microsoft Word.
    /// </summary>
    public static class LatexToOmmlService
    {
        private static readonly XNamespace m = "http://schemas.openxmlformats.org/officeDocument/2006/math";
        private static readonly XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

        // Regex phát hiện phân số \frac{a}{b}, hỗn số 5\frac{a}{b} hoặc biểu thức math $...$
        private static readonly Regex MathTokenRegex = new Regex(
            @"(\$[^$]+\$|(?:\b\d+\s*(?:[\+\-\*x\:\=\/]|\\times|\\div|×)?\s*)?\\frac\{[^{}]+\}\{[^{}]+\}(?:\s*(?:[\+\-\*x\:\=\/]|\\times|\\div|\\cdot|\\pm|×)\s*(?:\\frac\{[^{}]+\}\{[^{}]+\}|\d+))*)",
            RegexOptions.Compiled);

        private static readonly Regex SingleFractionRegex = new Regex(@"\\frac\{([^{}]+)\}\{([^{}]+)\}", RegexOptions.Compiled);

        /// <summary>
        /// Tiền xử lý chuỗi văn bản: khắc phục lỗi FormFeed (\u000c), chuyển đổi các ký hiệu đặc biệt, dấu chấm lửng, đơn vị đo diện tích
        /// </summary>
        public static string NormalizeLatex(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            // 0. Khắc phục lỗi FormFeed (\f -> \u000c) hoặc mất backslash (\frac -> rac) khi deserialization JSON
            string s = input.Replace("\u000crac{", @"\frac{").Replace("\u000c", "");
            s = Regex.Replace(s, @"(?<![a-zA-Z\\])rac\{", @"\frac{");

            // 1. Chuyển đổi các dấu chấm lửng
            s = s.Replace(@"\dots", "...").Replace(@"\ldots", "...");

            // 2. Chuyển đổi các đơn vị đo diện tích và thể tích có số mũ dạng m^2, cm^2, dm^2...
            s = Regex.Replace(s, @"\b(km|hm|dam|m|dm|cm|mm)\^2\b", "$1²", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"\b(km|hm|dam|m|dm|cm|mm)\^3\b", "$1³", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"\b(km|hm|dam|m|dm|cm|mm)\^\{2\}\b", "$1²", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"\b(km|hm|dam|m|dm|cm|mm)\^\{3\}\b", "$1³", RegexOptions.IgnoreCase);

            return s;
        }

        /// <summary>
        /// Làm sạch các lệnh LaTeX toán tử cơ bản còn sót trong đoạn văn bản thường
        /// </summary>
        public static string CleanPlainText(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return text.Replace(@"\times", "×")
                       .Replace(@"\div", ":")
                       .Replace(@"\cdot", "·")
                       .Replace(@"\pm", "±")
                       .Replace(@"\le", "≤")
                       .Replace(@"\leq", "≤")
                       .Replace(@"\ge", "≥")
                       .Replace(@"\geq", "≥")
                       .Replace(@"\ne", "≠")
                       .Replace(@"\neq", "≠");
        }

        /// <summary>
        /// Tách chuỗi câu hỏi thành các phần văn bản thường và các phần công thức toán
        /// </summary>
        public static List<TextOrMathPart> SplitContent(string input)
        {
            var result = new List<TextOrMathPart>();
            if (string.IsNullOrEmpty(input)) return result;

            input = NormalizeLatex(input);

            // Xử lý các dạng:
            // 1) $...$ (LaTeX inline math)
            // 2) \frac{...}{...} (phân số đơn lẻ hoặc chuỗi phép tính phân số)
            var matches = MathTokenRegex.Matches(input);
            if (matches.Count == 0)
            {
                result.Add(new TextOrMathPart { IsMath = false, Text = CleanPlainText(input) });
                return result;
            }

            int lastIdx = 0;
            foreach (Match match in matches)
            {
                if (match.Index > lastIdx)
                {
                    result.Add(new TextOrMathPart
                    {
                        IsMath = false,
                        Text = CleanPlainText(input.Substring(lastIdx, match.Index - lastIdx))
                    });
                }

                string mathText = match.Value;
                // Bỏ dấu $ nếu có
                if (mathText.StartsWith("$") && mathText.EndsWith("$") && mathText.Length >= 2)
                {
                    mathText = mathText.Substring(1, mathText.Length - 2).Trim();
                }

                result.Add(new TextOrMathPart
                {
                    IsMath = true,
                    Text = mathText
                });

                lastIdx = match.Index + match.Length;
            }

            if (lastIdx < input.Length)
            {
                result.Add(new TextOrMathPart
                {
                    IsMath = false,
                    Text = CleanPlainText(input.Substring(lastIdx))
                });
            }

            return result;
        }

        /// <summary>
        /// Chuyển đổi mã LaTeX toán học sang XElement <m:oMath> chuẩn OpenXML
        /// </summary>
        public static XElement ConvertToOMath(string latexMath, double fontSize = 14.0)
        {
            var oMath = new XElement(m + "oMath");

            if (string.IsNullOrWhiteSpace(latexMath)) return oMath;

            // Tiền xử lý các toán tử LaTeX thông dụng
            string formula = latexMath.Trim();

            // Phân tích cú pháp: hỗ trợ phân số \frac{a}{b}, các phép tính +, -, \times, \div, =, số, biến
            ParseExpression(oMath, formula, fontSize);

            return oMath;
        }

        private static void ParseExpression(XElement parent, string expression, double fontSize = 14.0)
        {
            int i = 0;
            int n = expression.Length;

            while (i < n)
            {
                // Bỏ qua khoảng trắng dư thừa
                if (char.IsWhiteSpace(expression[i]))
                {
                    i++;
                    continue;
                }

                // 1. Phân số: \frac{num}{den}
                if (i + 5 <= n && expression.Substring(i, 5) == @"\frac")
                {
                    int pos = i + 5;
                    while (pos < n && char.IsWhiteSpace(expression[pos])) pos++;

                    if (pos < n && expression[pos] == '{')
                    {
                        string num = ExtractGroup(expression, ref pos);
                        while (pos < n && char.IsWhiteSpace(expression[pos])) pos++;

                        if (pos < n && expression[pos] == '{')
                        {
                            string den = ExtractGroup(expression, ref pos);
                            parent.Add(CreateFractionElement(num, den, fontSize));
                            i = pos;
                            continue;
                        }
                    }
                }

                // 2. Toán tử LaTeX: \times, \div, \pm, \le, \ge, \cdot
                if (expression[i] == '\\')
                {
                    int startCmd = i;
                    i++;
                    while (i < n && char.IsLetter(expression[i])) i++;
                    string cmd = expression.Substring(startCmd, i - startCmd);

                    string sym = cmd switch
                    {
                        @"\times" => "×",
                        @"\cdot" => "·",
                        @"\div" => ":",
                        @"\pm" => "±",
                        @"\le" => "≤",
                        @"\leq" => "≤",
                        @"\ge" => "≥",
                        @"\geq" => "≥",
                        @"\ne" => "≠",
                        @"\neq" => "≠",
                        _ => cmd
                    };

                    parent.Add(CreateRunElement(sym, fontSize));
                    continue;
                }

                // 3. Số mũ / chỉ số: ví dụ cm^2, x^2
                if (expression[i] == '^')
                {
                    i++;
                    string expText = "";
                    if (i < n && expression[i] == '{')
                    {
                        expText = ExtractGroup(expression, ref i);
                    }
                    else if (i < n)
                    {
                        expText = expression[i].ToString();
                        i++;
                    }

                    // Nếu phần tử trước là một run, chuyển thành m:sSup
                    var lastChild = parent.Elements().LastOrDefault();
                    if (lastChild != null)
                    {
                        lastChild.Remove();
                        var sSup = new XElement(m + "sSup",
                            new XElement(m + "e", lastChild),
                            new XElement(m + "sup", CreateRunElement(expText, fontSize))
                        );
                        parent.Add(sSup);
                    }
                    else
                    {
                        parent.Add(CreateRunElement("^" + expText, fontSize));
                    }
                    continue;
                }

                // 4. Các ký tự toán tử đơn lẻ: +, -, *, /, :, =, <, >, (, ), ×, ÷, ·
                char c = expression[i];
                if (c == '+' || c == '-' || c == '*' || c == '/' || c == ':' || c == '=' || c == '<' || c == '>' || c == '(' || c == ')' || c == '[' || c == ']' || c == '×' || c == '÷' || c == '·')
                {
                    string opStr = c switch
                    {
                        '-' => "−", // Unicode minus đẹp hơn dấu gạch nối
                        '*' => "×",
                        '/' => ":",
                        '÷' => ":",
                        '×' => "×",
                        '·' => "·",
                        _ => c.ToString()
                    };
                    parent.Add(CreateRunElement(" " + opStr + " ", fontSize));
                    i++;
                    continue;
                }

                // 5. Chuỗi số hoặc chữ cái liền kề
                int startToken = i;
                while (i < n && !char.IsWhiteSpace(expression[i]) && expression[i] != '\\' &&
                       expression[i] != '+' && expression[i] != '-' && expression[i] != '*' &&
                       expression[i] != '/' && expression[i] != ':' && expression[i] != '=' &&
                       expression[i] != '<' && expression[i] != '>' && expression[i] != '^' &&
                       expression[i] != '(' && expression[i] != ')' && expression[i] != '[' && expression[i] != ']' &&
                       expression[i] != '×' && expression[i] != '÷' && expression[i] != '·')
                {
                    i++;
                }

                if (i > startToken)
                {
                    string token = expression.Substring(startToken, i - startToken);
                    parent.Add(CreateRunElement(token, fontSize));
                }
            }
        }

        private static string ExtractGroup(string text, ref int pos)
        {
            if (pos >= text.Length || text[pos] != '{') return "";
            pos++; // bỏ qua '{'
            int start = pos;
            int depth = 1;

            while (pos < text.Length && depth > 0)
            {
                if (text[pos] == '{') depth++;
                else if (text[pos] == '}') depth--;
                pos++;
            }

            int end = depth == 0 ? pos - 1 : pos;
            return text.Substring(start, end - start);
        }

        private static XElement CreateFractionElement(string numText, string denText, double fontSize = 14.0)
        {
            var fraction = new XElement(m + "f",
                new XElement(m + "fPr",
                    new XElement(m + "type", new XAttribute(m + "val", "bar"))
                ),
                new XElement(m + "num"),
                new XElement(m + "den")
            );

            var numElem = fraction.Element(m + "num")!;
            var denElem = fraction.Element(m + "den")!;

            // Đệ quy phân tích tử số và mẫu số nếu bên trong có phép tính hoặc phân số lồng
            ParseExpression(numElem, numText, fontSize);
            ParseExpression(denElem, denText, fontSize);

            return fraction;
        }

        private static XElement CreateRunElement(string text, double fontSize = 14.0)
        {
            int halfPt = (int)Math.Round(fontSize * 2);
            return new XElement(m + "r",
                new XElement(w + "rPr",
                    new XElement(w + "rFonts",
                        new XAttribute(w + "ascii", "Cambria Math"),
                        new XAttribute(w + "hAnsi", "Cambria Math")
                    ),
                    new XElement(w + "sz", new XAttribute(w + "val", halfPt.ToString())),
                    new XElement(w + "szCs", new XAttribute(w + "val", halfPt.ToString()))
                ),
                new XElement(m + "t", text)
            );
        }
    }
}
