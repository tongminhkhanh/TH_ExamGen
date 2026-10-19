using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TH_ExamGen.Core.Config;
using TH_ExamGen.Core.MathModels;

namespace TH_ExamGen.Services
{
    /// <summary>
    /// Dịch vụ Họa sĩ TikZ chuyên trách: Đọc hiểu đề bài và sinh mã đồ họa TikZ chuẩn xác
    /// Kết hợp giữa Bộ phát sinh tham số chuẩn (Parametric Engine) và AI Họa sĩ chuyên biệt.
    /// </summary>
    public class TikzSpecialistService
    {
        /// <summary>
        /// Đọc hiểu đề bài và sinh mã TikZ: Ưu tiên bộ tham số chuẩn, nếu phức tạp thì gọi AI đọc đề.
        /// </summary>
        public static async Task<string> GenerateTikzForQuestionAsync(AppSettings settings, MathQuestion q, int khoiLop = 0, bool forceAi = false)
        {
            if (!forceAi)
            {
                // 1. Thử nhận diện bằng Bộ phát sinh tham số chuẩn (Deterministic Parametric Engine)
                string? parametricTikz = TryGenerateParametricTikz(q);
                if (!string.IsNullOrWhiteSpace(parametricTikz))
                {
                    q.TikzSource = "Template";
                    return parametricTikz;
                }
            }

            // 2. Nếu hình vẽ đặc thù/phức tạp hoặc bấm 'Vẽ lại từ đề (AI)', gọi AI Họa sĩ đọc đề và thiết kế TikZ
            try
            {
                string aiTikz = await GenerateTikzWithAiAsync(settings, q, khoiLop);
                if (!string.IsNullOrWhiteSpace(aiTikz))
                {
                    q.TikzSource = "AI";
                    return aiTikz;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Warn($"[TikzSpecialist] Lỗi sinh TikZ bằng AI: {ex.Message}");
            }

            // 3. Dự phòng cuối cùng: Hình mẫu thông minh theo chủ đề
            q.TikzSource = "Fallback";
            return GetSmartFallbackTikz(q);
        }

        #region 1. Bộ phát sinh tham số chuẩn (Deterministic Parametric Engine)

        public static string? TryGenerateParametricTikz(MathQuestion q, bool excludeStatisticalFigures = false)
        {
            string content = q.NoiDung ?? string.Empty;
            string fullContext = $"{content} {q.DapAnA} {q.DapAnB} {q.DapAnC} {q.DapAnD} {q.ChuDe}".ToLowerInvariant();

            // 1. HÌNH PHÂN SỐ (bánh tròn / băng giấy) — chỉ khi đề thực sự nói về phần tô màu / phân số.
            string? fraction = TryBuildFractionFigure(q, fullContext, content);
            if (fraction != null) return fraction;

            // 2. HÌNH CHỮ NHẬT / HÌNH VUÔNG có số đo.
            if (TryBuildRectangleFigure(fullContext, content, out string rectTikz)) return rectTikz;

            // 3. HÌNH TRÒN (bán kính / đường kính / chu vi / diện tích).
            if (TryBuildCircleFigure(fullContext, content, out string circleTikz)) return circleTikz;

            // 4. TAM GIÁC (phân loại + đường cao).
            if (TryBuildTriangleFigure(fullContext, content, out string triangleTikz)) return triangleTikz;

            // 5. GÓC (nhọn / vuông / tù / bẹt) và số đo góc.
            if (TryBuildAngleFigure(q, fullContext, content, out string angleTikz)) return angleTikz;

            // 5b. HAI ĐƯỜNG THẲNG VUÔNG GÓC / SONG SONG (Lớp 4).
            if (TryBuildPerpendicularParallelFigure(fullContext, content, out string lineTikz)) return lineTikz;

            // 6. HÌNH BÌNH HÀNH có đáy & chiều cao.
            if (TryBuildParallelogramFigure(fullContext, content, out string paraTikz)) return paraTikz;

            // 7. HÌNH THOI có hai đường chéo.
            if (TryBuildRhombusFigure(fullContext, content, out string rhombusTikz)) return rhombusTikz;

            // 8. HÌNH THANG CÓ CHIỀU CAO (Trapezoid with Altitude)
            if (fullContext.Contains("hình thang"))
            {
                ExtractTrapezoidDimensions(content, out double? dayLon, out double? dayBe, out double? chieuCao);
                return BuildTrapezoidTikz(dayLon, dayBe, chieuCao);
            }

            // 9. HÌNH HỘP CHỮ NHẬT / HÌNH LẬP PHƯƠNG 3D (3D Box / Cube)
            if (fullContext.Contains("hình hộp chữ nhật") || fullContext.Contains("thể tích hình hộp"))
            {
                Extract3DDimensions(content, out double? d, out double? r, out double? c);
                return BuildCuboid3DTikz(d, r, c);
            }
            if (fullContext.Contains("hình lập phương") || fullContext.Contains("thể tích hình lập phương"))
            {
                bool hasCanh = TryExtractNumber(content, @"cạnh", out double canh);
                return BuildCube3DTikz(hasCanh ? canh : (double?)null);
            }

            // 10. BIỂU ĐỒ HÌNH QUẠT TRÒN — phải xét TRƯỚC sơ đồ đoạn thẳng và biểu đồ cột.
            // Chỉ dựng hình khi đọc được đủ dữ liệu % từ đề; không đọc được thì trả null để AI đọc đề vẽ (tránh bịa số).
            bool isPieChart = fullContext.Contains("quạt tròn") || fullContext.Contains("biểu đồ quạt") || fullContext.Contains("hình quạt");
            if (isPieChart)
            {
                return TryBuildPieChartTikz(q);
            }

            // 11. SƠ ĐỒ ĐOẠN THẲNG (Toán có lời văn: Tổng - Tỉ, Hiệu - Tỉ, Tổng - Hiệu)
            bool isWordProblem = fullContext.Contains("sơ đồ")
                || fullContext.Contains("số lớn")
                || fullContext.Contains("số bé")
                || fullContext.Contains("tổng số")
                || fullContext.Contains("hiệu số")
                || ((fullContext.Contains("tổng") || fullContext.Contains("hiệu"))
                    && (fullContext.Contains("tỉ số") || fullContext.Contains("tỉ lệ") || fullContext.Contains("gấp")
                        || fullContext.Contains("số thứ nhất") || fullContext.Contains("số thứ hai")));
            if (!excludeStatisticalFigures && isWordProblem)
            {
                if (TryExtractRatioOrSum(content, out int partSmall, out int partLarge, out string labelTotal, out bool isDiff))
                {
                    return BuildSegmentDiagramTikz(partSmall, partLarge, labelTotal, isDiff);
                }
            }

            // 12. BIỂU ĐỒ CỘT THỐNG KÊ (Bar Chart)
            if (!excludeStatisticalFigures
                && (fullContext.Contains("biểu đồ") || q.ChuDe.Contains("Thống kê") || fullContext.Contains("số cây") || fullContext.Contains("trồng")))
            {
                return BuildBarChartTikz(q);
            }

            return null;
        }

        private static bool TryExtractFraction(MathQuestion q, out int numerator, out int denominator)
        {
            numerator = 3;
            denominator = 5;

            // Tìm phân số từ đáp án đúng hoặc nội dung câu hỏi
            string targetText = q.NoiDung ?? "";
            string correctLetter = q.DapAnDung?.Trim().ToUpperInvariant() ?? "A";
            string answerText = correctLetter switch
            {
                "B" => q.DapAnB ?? "",
                "C" => q.DapAnC ?? "",
                "D" => q.DapAnD ?? "",
                _ => q.DapAnA ?? ""
            };

            // Ưu tiên trích xuất từ đáp án đúng nếu đề hỏi "Phân số chỉ phần đã tô màu là:"
            string textToSearch = $"{targetText} {answerText}";

            // Regex dạng LaTeX: \frac{a}{b}
            var latexMatch = Regex.Match(textToSearch, @"\\frac\{(\d+)\}\{(\d+)\}");
            if (latexMatch.Success)
            {
                numerator = int.Parse(latexMatch.Groups[1].Value);
                denominator = int.Parse(latexMatch.Groups[2].Value);
                return denominator > 0 && numerator < denominator;
            }

            // Regex dạng thường: a/b
            var slashMatch = Regex.Match(textToSearch, @"(\d+)\s*/\s*(\d+)");
            if (slashMatch.Success)
            {
                numerator = int.Parse(slashMatch.Groups[1].Value);
                denominator = int.Parse(slashMatch.Groups[2].Value);
                return denominator > 0 && numerator < denominator;
            }

            // Regex dạng lời văn: chia n phần tô màu k phần
            var wordMatch = Regex.Match(textToSearch, @"chia.*?(\d+)\s*phần.*?tô.*?(\d+)\s*phần", RegexOptions.IgnoreCase);
            if (wordMatch.Success)
            {
                denominator = int.Parse(wordMatch.Groups[1].Value);
                numerator = int.Parse(wordMatch.Groups[2].Value);
                return denominator > 0 && numerator < denominator;
            }

            return false;
        }

        private static string BuildFractionCircleTikz(int k, int n)
        {
            double step = 360.0 / n;
            double endAngle = k * step;
            var sb = new StringBuilder();
            sb.AppendLine(@"\begin{tikzpicture}[scale=0.85]");
            sb.AppendLine(@"  % 1. Tô màu múi phân số trước");
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"  \fill[blue!35] (0,0) -- (0:1.5cm) arc (0:{0:0.#}:1.5cm) -- cycle;", endAngle));
            sb.AppendLine(@"  % 2. Vẽ nan hoa đè lên sau để vạch chia sắc nét");
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"  \foreach \ang in {{0, {0:0.#}, ..., 359}} {{", step));
            sb.AppendLine(@"    \draw[thick] (0,0) -- (\ang:1.5cm);");
            sb.AppendLine(@"  }");
            sb.AppendLine(@"  % 3. Viền ngoài hình tròn và tâm");
            sb.AppendLine(@"  \draw[thick] (0,0) circle (1.5cm);");
            sb.AppendLine(@"  \fill (0,0) circle (1.5pt);");
            sb.AppendLine(@"\end{tikzpicture}");
            return sb.ToString();
        }

        private static string BuildFractionBarTikz(int k, int n)
        {
            double totalWidth = 5.0;
            double filledWidth = (k * totalWidth) / n;
            double step = totalWidth / n;
            var sb = new StringBuilder();
            sb.AppendLine(@"\begin{tikzpicture}[scale=0.85]");
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"  \fill[blue!35] (0,0) rectangle ({0:0.##}, 1.2);", filledWidth));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"  \draw[thick] (0,0) rectangle ({0}, 1.2);", totalWidth));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"  \foreach \i in {{1, ..., {0}}} {{", n - 1));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"    \draw[thick] (\i * {0:0.##}, 0) -- (\i * {0:0.##}, 1.2);", step));
            sb.AppendLine(@"  }");
            sb.AppendLine(@"\end{tikzpicture}");
            return sb.ToString();
        }

        private static void ExtractDimensions(string text, out double day, out double cao, double defaultDay, double defaultCao)
        {
            day = defaultDay;
            cao = defaultCao;

            var dayMatch = Regex.Match(text, @"đáy\s*(?:là|dài)?\s*(\d+(?:[.,]\d+)?)", RegexOptions.IgnoreCase);
            if (dayMatch.Success && double.TryParse(dayMatch.Groups[1].Value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double dVal))
            {
                day = dVal;
            }

            var caoMatch = Regex.Match(text, @"(?:chiều cao|đường cao)\s*(?:là|dài)?\s*(\d+(?:[.,]\d+)?)", RegexOptions.IgnoreCase);
            if (caoMatch.Success && double.TryParse(caoMatch.Groups[1].Value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double cVal))
            {
                cao = cVal;
            }
        }

        private static string BuildTriangleTikz(string type, double? day, double? cao, bool withAltitude)
        {
            // Toạ độ đỉnh A cho từng loại tam giác (B, C cố định trên trục ngang).
            double ax, ay;
            switch (type)
            {
                case "vuông": ax = 0.0; ay = 3.0; break;
                case "đều":   ax = 2.5; ay = 4.33; break;
                case "cân":   ax = 2.5; ay = 4.0; break;
                case "tù":    ax = -1.1; ay = 2.3; break;
                case "nhọn":  ax = 2.2; ay = 3.4; break;
                default:      ax = 2.2; ay = 3.2; break;
            }

            bool showAltitude = withAltitude && type != "vuông";

            var sb = new StringBuilder();
            sb.AppendLine(@"\begin{tikzpicture}[scale=0.8]");
            sb.AppendLine(@"  \coordinate (B) at (0,0);");
            sb.AppendLine(@"  \coordinate (C) at (5,0);");
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"  \coordinate (A) at ({0:0.##}, {1:0.##});", ax, ay));
            sb.AppendLine(@"  \draw[thick] (A) -- (B) -- (C) -- cycle;");

            if (showAltitude)
            {
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"  \coordinate (H) at ({0:0.##}, 0);", ax));
                if (ax < 0)
                {
                    // Tam giác tù: chân đường cao nằm ngoài đáy -> kéo dài đáy bằng nét đứt đến H.
                    sb.AppendLine(@"  \draw[thick, dashed] (B) -- (H);");
                }
                string caoLabel = cao.HasValue ? TikzStyle.Len(cao.Value) : "h";
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"  \draw[thick, dashed, blue!80!black] (A) -- (H) node[midway, right, font=\footnotesize] {{{0}}};", caoLabel));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"  \draw ({0:0.##}, 0.35) -- ({1:0.##}, 0.35) -- ({1:0.##}, 0);", ax, ax + 0.35));
                sb.AppendLine(@"  \node[below left, font=\footnotesize] at (H) {H};");
            }
            else if (type == "vuông")
            {
                // Dấu góc vuông tại B (BA thẳng đứng, BC nằm ngang).
                sb.AppendLine(@"  \draw (0,0.4) -- (0.4,0.4) -- (0.4,0);");
            }

            // Nhãn đáy (khi biết số hoặc khi đang vẽ đường cao).
            if (showAltitude)
            {
                string dayLabel = day.HasValue ? TikzStyle.Len(day.Value) : "đáy";
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"  \draw[<->, >=stealth, gray] (0,-0.45) -- (5,-0.45) node[midway, below, font=\footnotesize, text=black] {{{0}}};", dayLabel));
            }

            sb.AppendLine(@"  \node[above, font=\footnotesize] at (A) {A};");
            sb.AppendLine(@"  \node[below left, font=\footnotesize] at (B) {B};");
            sb.AppendLine(@"  \node[below right, font=\footnotesize] at (C) {C};");
            sb.AppendLine(@"\end{tikzpicture}");
            return sb.ToString();
        }

        private static void ExtractTrapezoidDimensions(string text, out double? dayLon, out double? dayBe, out double? chieuCao)
        {
            dayLon = null;
            dayBe = null;
            chieuCao = null;

            var lonMatch = Regex.Match(text, @"đáy lớn\s*(?:là|dài)?\s*(\d+(?:[.,]\d+)?)", RegexOptions.IgnoreCase);
            if (lonMatch.Success && double.TryParse(lonMatch.Groups[1].Value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double lVal))
                dayLon = lVal;

            var beMatch = Regex.Match(text, @"đáy bé\s*(?:là|dài)?\s*(\d+(?:[.,]\d+)?)", RegexOptions.IgnoreCase);
            if (beMatch.Success && double.TryParse(beMatch.Groups[1].Value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double bVal))
                dayBe = bVal;

            var caoMatch = Regex.Match(text, @"chiều cao\s*(?:là|dài)?\s*(\d+(?:[.,]\d+)?)", RegexOptions.IgnoreCase);
            if (caoMatch.Success && double.TryParse(caoMatch.Groups[1].Value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double cVal))
                chieuCao = cVal;
        }

        private static string BuildTrapezoidTikz(double? dayLon, double? dayBe, double? chieuCao)
        {
            // Quy tắc SGK: đề không nêu số -> ghi nhãn chữ; đề có số -> in đúng số kèm đơn vị (ví dụ 6 cm).
            string caoLabel = chieuCao.HasValue ? TikzStyle.Len(chieuCao.Value) : "chiều cao";
            string dayBeLabel = dayBe.HasValue ? TikzStyle.Len(dayBe.Value) : "đáy bé";
            string dayLonLabel = dayLon.HasValue ? TikzStyle.Len(dayLon.Value) : "đáy lớn";

            return $@"\\begin{{tikzpicture}}[scale=0.8]
  \coordinate (A) at (1.5, 3.0);
  \coordinate (B) at (4.5, 3.0);
  \coordinate (C) at (6.0, 0);
  \coordinate (D) at (0, 0);
  \coordinate (H) at (1.5, 0);

  \draw[thick] (A) -- (B) -- (C) -- (D) -- cycle;
  \draw[thick, dashed, blue!80!black] (A) -- (H) node[midway, right, font=\footnotesize] {{{caoLabel}}};
  \draw (1.5, 0.3) -- (1.8, 0.3) -- (1.8, 0);

  \node[above left] at (A) {{A}};
  \node[above right] at (B) {{B}};
  \node[below right] at (C) {{C}};
  \node[below left] at (D) {{D}};
  \node[below] at (H) {{H}};

  \node[above, font=\footnotesize] at (3.0, 3.0) {{{dayBeLabel}}};
  \node[below, font=\footnotesize] at (3.0, 0) {{{dayLonLabel}}};
\end{{tikzpicture}}".Replace("\\\\begin", "\\begin");
        }

        private static string BuildParallelogramTikz(double? day, double? cao)
        {
            var sb = new StringBuilder();
            sb.AppendLine(@"\begin{tikzpicture}[scale=0.8]");
            sb.AppendLine(@"  \coordinate (D) at (0,0);");
            sb.AppendLine(@"  \coordinate (C) at (5,0);");
            sb.AppendLine(@"  \coordinate (A) at (1.6,2.6);");
            sb.AppendLine(@"  \coordinate (B) at (6.6,2.6);");
            sb.AppendLine(@"  \coordinate (H) at (1.6,0);");
            sb.AppendLine(@"  \draw[thick] (A) -- (B) -- (C) -- (D) -- cycle;");

            string caoLabel = cao.HasValue ? TikzStyle.Len(cao.Value) : "chiều cao";
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"  \draw[thick, dashed, blue!80!black] (A) -- (H) node[midway, right, font=\footnotesize] {{{0}}};", caoLabel));
            sb.AppendLine(@"  \draw (1.6,0.35) -- (1.95,0.35) -- (1.95,0);");

            string dayLabel = day.HasValue ? TikzStyle.Len(day.Value) : "đáy";
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"  \node[below, font=\footnotesize] at (3.3,0) {{{0}}};", dayLabel));

            sb.AppendLine(@"  \node[above left, font=\footnotesize] at (A) {A};");
            sb.AppendLine(@"  \node[above right, font=\footnotesize] at (B) {B};");
            sb.AppendLine(@"  \node[below right, font=\footnotesize] at (C) {C};");
            sb.AppendLine(@"  \node[below left, font=\footnotesize] at (D) {D};");
            sb.AppendLine(@"  \node[below left, font=\footnotesize] at (H) {H};");
            sb.AppendLine(@"\end{tikzpicture}");
            return sb.ToString();
        }

        private static string BuildRhombusTikz(double? duongCheo1, double? duongCheo2)
        {
            var sb = new StringBuilder();
            sb.AppendLine(@"\begin{tikzpicture}[scale=0.8]");
            sb.AppendLine(@"  \coordinate (A) at (0,2.0);");
            sb.AppendLine(@"  \coordinate (B) at (3.0,3.5);");
            sb.AppendLine(@"  \coordinate (C) at (6.0,2.0);");
            sb.AppendLine(@"  \coordinate (D) at (3.0,0.5);");
            sb.AppendLine(@"  \draw[thick] (A) -- (B) -- (C) -- (D) -- cycle;");
            sb.AppendLine(@"  \draw[thick, dashed, blue!80!black] (A) -- (C);");
            sb.AppendLine(@"  \draw[thick, dashed, blue!80!black] (B) -- (D);");
            sb.AppendLine(@"  \draw (3.3,2.0) -- (3.3,2.3) -- (3.0,2.3);");

            string mLabel = duongCheo1.HasValue ? TikzStyle.Len(duongCheo1.Value) : "m";
            string nLabel = duongCheo2.HasValue ? TikzStyle.Len(duongCheo2.Value) : "n";
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"  \node[above, font=\footnotesize] at (1.5,2.0) {{{0}}};", mLabel));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"  \node[right, font=\footnotesize] at (3.0,2.75) {{{0}}};", nLabel));

            sb.AppendLine(@"  \node[above left, font=\footnotesize] at (A) {A};");
            sb.AppendLine(@"  \node[above, font=\footnotesize] at (B) {B};");
            sb.AppendLine(@"  \node[above right, font=\footnotesize] at (C) {C};");
            sb.AppendLine(@"  \node[below, font=\footnotesize] at (D) {D};");
            sb.AppendLine(@"\end{tikzpicture}");
            return sb.ToString();
        }

        private static void Extract3DDimensions(string text, out double? d, out double? r, out double? c)
        {
            d = null;
            r = null;
            c = null;

            var daiMatch = Regex.Match(text, @"chiều dài\s*(?:là)?\s*(\d+(?:[.,]\d+)?)", RegexOptions.IgnoreCase);
            if (daiMatch.Success && double.TryParse(daiMatch.Groups[1].Value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double dv)) d = dv;

            var rongMatch = Regex.Match(text, @"chiều rộng\s*(?:là)?\s*(\d+(?:[.,]\d+)?)", RegexOptions.IgnoreCase);
            if (rongMatch.Success && double.TryParse(rongMatch.Groups[1].Value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double rv)) r = rv;

            var caoMatch = Regex.Match(text, @"chiều cao\s*(?:là)?\s*(\d+(?:[.,]\d+)?)", RegexOptions.IgnoreCase);
            if (caoMatch.Success && double.TryParse(caoMatch.Groups[1].Value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double cv)) c = cv;
        }

        private static string BuildCuboid3DTikz(double? dai, double? rong, double? cao)
        {
            // Quy tắc SGK: đề không nêu số -> ghi nhãn chữ; đề có số -> in đúng số kèm đơn vị.
            string daiLabel = dai.HasValue ? TikzStyle.Len(dai.Value) : "chiều dài";
            string rongLabel = rong.HasValue ? TikzStyle.Len(rong.Value) : "chiều rộng";
            string caoLabel = cao.HasValue ? TikzStyle.Len(cao.Value) : "chiều cao";

            return $@"\\begin{{tikzpicture}}[scale=0.75]
  % Các đỉnh phía trước
  \coordinate (A) at (0, 3);
  \coordinate (B) at (4.5, 3);
  \coordinate (C) at (4.5, 0);
  \coordinate (D) at (0, 0);
  % Các đỉnh phía sau
  \coordinate (A1) at (2.0, 4.2);
  \coordinate (B1) at (6.5, 4.2);
  \coordinate (C1) at (6.5, 1.2);
  \coordinate (D1) at (2.0, 1.2);

  % Cạnh khuất nét đứt
  \draw[thick, dashed] (D) -- (D1) -- (A1);
  \draw[thick, dashed] (D1) -- (C1);

  % Cạnh thấy nét liền
  \draw[thick] (A) -- (B) -- (C) -- (D) -- cycle;
  \draw[thick] (A) -- (A1) -- (B1) -- (B);
  \draw[thick] (B1) -- (C1) -- (C);

  \node[below, font=\footnotesize] at (2.25, 0) {{{daiLabel}}};
  \node[below right, font=\footnotesize] at (5.5, 0.6) {{{rongLabel}}};
  \node[left, font=\footnotesize] at (0, 1.5) {{{caoLabel}}};
\end{{tikzpicture}}".Replace("\\\\begin", "\\begin");
        }

        private static string BuildCube3DTikz(double? canh)
        {
            // Đề cho số đo cạnh -> in số; ngược lại ghi ký hiệu a.
            string aLabel = canh.HasValue ? TikzStyle.Len(canh.Value) : "a";

            return $@"\begin{{tikzpicture}}[scale=0.75]
  \coordinate (A) at (0, 3);
  \coordinate (B) at (3, 3);
  \coordinate (C) at (3, 0);
  \coordinate (D) at (0, 0);
  \coordinate (A1) at (1.5, 4.2);
  \coordinate (B1) at (4.5, 4.2);
  \coordinate (C1) at (4.5, 1.2);
  \coordinate (D1) at (1.5, 1.2);

  \draw[thick, dashed] (D) -- (D1) -- (A1);
  \draw[thick, dashed] (D1) -- (C1);
  \draw[thick] (A) -- (B) -- (C) -- (D) -- cycle;
  \draw[thick] (A) -- (A1) -- (B1) -- (B);
  \draw[thick] (B1) -- (C1) -- (C);

  \node[below, font=\footnotesize] at (1.5, 0) {{{aLabel}}};
  \node[below right, font=\footnotesize] at (3.75, 0.6) {{{aLabel}}};
  \node[left, font=\footnotesize] at (0, 1.5) {{{aLabel}}};
\end{{tikzpicture}}";
        }

        private static bool TryExtractRatioOrSum(string text, out int pSmall, out int pLarge, out string totalLabel, out bool isDiff)
        {
            pSmall = 2;
            pLarge = 3;
            totalLabel = "Tổng: ?";
            isDiff = false;

            // Tìm tỉ số
            var ratioMatch = Regex.Match(text, @"(?:bằng|tỉ số)\s*(?:là)?\s*\\frac\{(\d+)\}\{(\d+)\}");
            if (!ratioMatch.Success)
            {
                ratioMatch = Regex.Match(text, @"(?:bằng|tỉ số)\s*(?:là)?\s*(\d+)\s*/\s*(\d+)");
            }
            if (ratioMatch.Success)
            {
                pSmall = int.Parse(ratioMatch.Groups[1].Value);
                pLarge = int.Parse(ratioMatch.Groups[2].Value);
            }

            // Tìm tổng hoặc hiệu
            var sumMatch = Regex.Match(text, @"tổng\s*(?:là|hai số là)?\s*(\d+(?:[.,]\d+)?\s*(?:kg|tấn|m|học sinh|bông hoa|người)?)", RegexOptions.IgnoreCase);
            if (sumMatch.Success)
            {
                totalLabel = $"Tổng: {sumMatch.Groups[1].Value.Trim()}";
                isDiff = false;
                return true;
            }

            var diffMatch = Regex.Match(text, @"hiệu\s*(?:là|hai số là)?\s*(\d+(?:[.,]\d+)?\s*(?:kg|tấn|m|học sinh|bông hoa|người)?)", RegexOptions.IgnoreCase);
            if (diffMatch.Success)
            {
                totalLabel = $"Hiệu: {diffMatch.Groups[1].Value.Trim()}";
                isDiff = true;
                return true;
            }

            return false;
        }

        private static string BuildSegmentDiagramTikz(int pSmall, int pLarge, string totalLabel, bool isDiff)
        {
            double unitW = 1.0;
            double wSmall = pSmall * unitW;
            double wLarge = pLarge * unitW;

            var sb = new StringBuilder();
            sb.AppendLine(@"\begin{tikzpicture}[scale=0.75]");
            sb.AppendLine(@"  \node[anchor=east] at (0, 1.5) {Số bé:};");
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"  \draw[thick] (0.2, 1.5) -- ({0:0.##}, 1.5);", 0.2 + wSmall));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"  \foreach \x in {{0, 1, ..., {0}}} {{", pSmall));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"    \draw[thick] (0.2 + \x * {0:0.##}, 1.35) -- (0.2 + \x * {0:0.##}, 1.65);", unitW));
            sb.AppendLine(@"  }");

            sb.AppendLine(@"  \node[anchor=east] at (0, 0.4) {Số lớn:};");
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"  \draw[thick] (0.2, 0.4) -- ({0:0.##}, 0.4);", 0.2 + wLarge));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"  \foreach \x in {{0, 1, ..., {0}}} {{", pLarge));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"    \draw[thick] (0.2 + \x * {0:0.##}, 0.25) -- (0.2 + \x * {0:0.##}, 0.55);", unitW));
            sb.AppendLine(@"  }");

            if (isDiff)
            {
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"  \draw[thick, decoration={{brace, mirror, amplitude=5pt}}, decorate] ({0:0.##}, 0.1) -- ({1:0.##}, 0.1) node[midway, below=6pt, font=\footnotesize] {{{2}}};", 0.2 + wSmall, 0.2 + wLarge, totalLabel));
            }
            else
            {
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"  \draw[thick, decoration={{brace, mirror, amplitude=6pt}}, decorate] (0.2, -0.1) -- ({0:0.##}, -0.1) node[midway, below=8pt, font=\footnotesize] {{{1}}};", 0.2 + wLarge, totalLabel));
            }

            sb.AppendLine(@"\end{tikzpicture}");
            return sb.ToString();
        }

        private static string BuildBarChartTikz(MathQuestion q)
        {
            string content = q.NoiDung ?? "";
            string fullText = $"{content} {q.DapAnA} {q.DapAnB} {q.DapAnC} {q.DapAnD}";

            string g = "5";
            if (fullText.Contains("4A") || fullText.Contains("Lớp 4")) g = "4";
            else if (fullText.Contains("3A") || fullText.Contains("Lớp 3")) g = "3";

            string c1 = $"{g}A", c2 = $"{g}B", c3 = $"{g}C", c4 = $"{g}D";

            bool isMin = content.Contains("ít nhất") || content.Contains("bé nhất") || content.Contains("thấp nhất");
            string ans = (q.DapAnDung ?? "D").Trim().ToUpperInvariant();

            int v1 = 20, v2 = 25, v3 = 15, v4 = 30; // Mặc định 5D cao nhất

            if (isMin)
            {
                if (ans.Contains("A") || ans.Contains(c1)) { v1 = 10; v2 = 25; v3 = 20; v4 = 30; }
                else if (ans.Contains("B") || ans.Contains(c2)) { v1 = 25; v2 = 10; v3 = 20; v4 = 30; }
                else if (ans.Contains("C") || ans.Contains(c3)) { v1 = 25; v2 = 20; v3 = 10; v4 = 30; }
                else if (ans.Contains("D") || ans.Contains(c4)) { v1 = 25; v2 = 20; v3 = 30; v4 = 10; }
            }
            else
            {
                if (ans.Contains("A") || ans.Contains(c1)) { v1 = 35; v2 = 25; v3 = 20; v4 = 15; }
                else if (ans.Contains("B") || ans.Contains(c2)) { v1 = 20; v2 = 35; v3 = 15; v4 = 25; }
                else if (ans.Contains("C") || ans.Contains(c3)) { v1 = 20; v2 = 25; v3 = 35; v4 = 15; }
                else if (ans.Contains("D") || ans.Contains(c4)) { v1 = 20; v2 = 25; v3 = 15; v4 = 35; }
            }

            int maxVal = Math.Max(v1, Math.Max(v2, Math.Max(v3, v4)));
            int yMax = ((maxVal / 10) + 1) * 10;
            double yscale = 4.2 / yMax; // Biểu đồ luôn cao khoảng 4.2 cm

            string xLabel = content.Contains("lớp") || content.Contains("Lớp") ? "Lớp" : "Danh mục";
            string yLabel = content.Contains("cây") ? "Số cây" : (content.Contains("học sinh") ? "Số HS" : "Số lượng");

            var sb = new StringBuilder();
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"\begin{{tikzpicture}}[xscale=1.2, yscale={0:0.####}, font=\small]", yscale));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"  \draw[->, thick] (0, 0) -- (5.2, 0) node[below=2pt] {{{0}}};", xLabel));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"  \draw[->, thick] (0, 0) -- (0, {0}) node[left=2pt] {{{1}}};", yMax + (yMax / 10), yLabel));

            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"  \foreach \y in {{0, 5, ..., {0}}} {{", yMax));
            sb.AppendLine(@"    \draw (0, \y) -- (-0.1, \y) node[left] {\y};");
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"    \draw[dotted, gray!40] (0, \y) -- (4.8, \y);"));
            sb.AppendLine(@"  }");

            string barItems = string.Join(", ", new[]
            {
                $"{{{c1}}}/{v1}/1",
                $"{{{c2}}}/{v2}/2",
                $"{{{c3}}}/{v3}/3",
                $"{{{c4}}}/{v4}/4"
            });
            sb.AppendLine($@"  \foreach \class/\height/\pos in {{{barItems}}} {{");
            sb.AppendLine(@"    \fill[blue!35] (\pos - 0.32, 0) rectangle (\pos + 0.32, \height);");
            sb.AppendLine(@"    \draw[thick] (\pos - 0.32, 0) rectangle (\pos + 0.32, \height);");
            sb.AppendLine(@"    \node[below=4pt] at (\pos, 0) {\class};");
            sb.AppendLine(@"    \node[above=2pt] at (\pos, \height) {\height};");
            sb.AppendLine(@"  }");
            sb.AppendLine(@"\end{tikzpicture}");

            return sb.ToString();
        }

        /// <summary>
        /// Biểu đồ hình quạt tròn: chỉ dựng khi đọc được 2-6 tỉ số phần trăm hợp lệ từ đề bài
        /// (tổng xấp xỉ 100%). Không đọc được dữ liệu thì trả null để AI đọc đề vẽ, tránh bịa số.
        /// </summary>
        private static string? TryBuildPieChartTikz(MathQuestion q)
        {
            string content = q.NoiDung ?? string.Empty;
            var slices = ExtractPercentSlices(content);
            if (slices.Count < 2 || slices.Count > 6) return null;

            double sum = 0;
            foreach (var s in slices) sum += s.Value;
            if (sum < 98 || sum > 102) return null;

            return BuildPieChartTikz(slices);
        }

        /// <summary>
        /// Đọc các cặp "nhãn ... x%" trong đề (ví dụ: "Truyện thiếu nhi chiếm 25%").
        /// Nhãn lấy trong khoảng 34 ký tự trước dấu %, cắt tại dấu câu gần nhất và bỏ các từ đệm (chiếm, là, và...).
        /// </summary>
        private static List<KeyValuePair<string, double>> ExtractPercentSlices(string text)
        {
            var list = new List<KeyValuePair<string, double>>();
            var matches = Regex.Matches(text, @"(\d{1,3}(?:[.,]\d+)?)\s*%");
            foreach (Match m in matches)
            {
                if (!double.TryParse(m.Groups[1].Value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double value)) continue;
                if (value <= 0 || value > 100) continue;

                int winStart = Math.Max(0, m.Index - 34);
                string window = text.Substring(winStart, m.Index - winStart);
                int boundary = window.LastIndexOfAny(new[] { ',', ';', '.', '\n', '\r', ':', '(', ')' });
                if (boundary >= 0)
                {
                    string after = window.Substring(boundary + 1).Trim();
                    // Dấu ':' thường đứng NGAY TRƯỚC số % ("Sách giáo khoa: 30%") -> lấy phần trước dấu hai chấm.
                    if (window[boundary] == ':' && after.Length == 0)
                    {
                        string before = window.Substring(0, boundary);
                        int prev = before.LastIndexOfAny(new[] { ',', ';', '.', '\n', '\r', '(', ')' });
                        window = prev >= 0 ? before.Substring(prev + 1) : before;
                    }
                    else
                    {
                        window = after;
                    }
                }

                string label = window.Trim().Trim(':').Trim();
                string[] tailWords = { "chiếm khoảng", "chiếm gần", "chiếm", "khoảng", "gần", "ước", "là", "và" };
                bool changed = true;
                while (changed)
                {
                    changed = false;
                    foreach (var w in tailWords)
                    {
                        if (label.EndsWith(" " + w, StringComparison.OrdinalIgnoreCase) || label.Equals(w, StringComparison.OrdinalIgnoreCase))
                        {
                            label = label.Substring(0, Math.Max(0, label.Length - w.Length)).Trim();
                            changed = true;
                        }
                    }
                }

                if (label.Length > 28) label = label.Substring(label.Length - 28).TrimStart();
                if (label.Length == 0) label = $"Phần {list.Count + 1}";

                list.Add(new KeyValuePair<string, double>(label, value));
            }
            return list;
        }

        /// <summary>Dựng biểu đồ quạt tròn: miếng quạt màu theo tỉ số %, ghi % trong từng miếng và chú giải màu bên phải.</summary>
        private static string BuildPieChartTikz(List<KeyValuePair<string, double>> slices)
        {
            string[] colors = { "blue!40", "orange!45", "green!45", "red!35", "purple!35", "teal!35" };
            const double r = 2.2;

            var starts = new List<double>();
            double start = 0;
            var sb = new StringBuilder();
            sb.AppendLine(@"\begin{tikzpicture}[scale=0.85]");

            for (int i = 0; i < slices.Count; i++)
            {
                double sweep = 360.0 * slices[i].Value / 100.0;
                double end = start + sweep;
                starts.Add(start);
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    @"  \fill[{0}] (0,0) -- ({1:0.##}:{2:0.##}) arc ({1:0.##}:{3:0.##}:{2:0.##}) -- cycle;",
                    colors[i % colors.Length], start, r, end));
                start = end;
            }

            foreach (double a in starts)
            {
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"  \draw[thick] (0,0) -- ({0:0.##}:{1:0.##});", a, r));
            }
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"  \draw[thick] (0,0) circle ({0:0.##});", r));
            sb.AppendLine(@"  \fill (0,0) circle (1.2pt);");

            // Ghi số % ngay trong từng miếng quạt (tại tia phân giác).
            start = 0;
            for (int i = 0; i < slices.Count; i++)
            {
                double sweep = 360.0 * slices[i].Value / 100.0;
                double mid = start + sweep / 2.0;
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    @"  \node[font=\footnotesize] at ({0:0.##}:1.35) {{{1}\%}};", mid, TikzStyle.Num(slices[i].Value)));
                start += sweep;
            }

            // Chú giải màu bên phải.
            for (int i = 0; i < slices.Count; i++)
            {
                double y = 1.15 - i * 0.7;
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    @"  \fill[{0}] (2.6,{1:0.##}) rectangle (2.95,{2:0.##});", colors[i % colors.Length], y - 0.175, y + 0.175));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    @"  \node[anchor=west, font=\footnotesize] at (3.05,{0:0.##}) {{{1}}};", y, slices[i].Key));
            }

            sb.AppendLine(@"\end{tikzpicture}");
            return sb.ToString();
        }

        private static string GetSmartFallbackTikz(MathQuestion q)
        {
            string text = $"{q.NoiDung} {q.ChuDe}".ToLowerInvariant();

            if (text.Contains("phân số") || text.Contains("phần"))
            {
                return BuildFractionBarTikz(3, 5);
            }
            if (text.Contains("hình bình hành"))
            {
                return BuildParallelogramTikz(null, null);
            }
            if (text.Contains("hình thoi"))
            {
                return BuildRhombusTikz(null, null);
            }
            if (text.Contains("hình"))
            {
                return BuildRectangleTikz(null, null);
            }
            return BuildSegmentDiagramTikz(2, 3, "Tổng: ?", false);
        }

        #region 1b. Bộ dựng hình tiểu học theo mẫu chuẩn (không bịa số liệu)

        /// <summary>
        /// Chỉ dựng hình phân số khi đề thực sự nói về phần tô màu / phân số có hình.
        /// Tránh lỗi cũ: mọi câu có chữ "hình tròn"/"hình chữ nhật" đều bị vẽ thành hình phân số.
        /// </summary>
        private static string? TryBuildFractionFigure(MathQuestion q, string fullContext, string content)
        {
            bool fractionSignal = fullContext.Contains("phần đã tô màu")
                || fullContext.Contains("tô màu")
                || fullContext.Contains("băng giấy")
                || fullContext.Contains("thanh chữ nhật")
                || fullContext.Contains("phân số")
                || fullContext.Contains("\\frac");

            if (!fractionSignal) return null;
            if (!TryExtractFraction(q, out int tuSo, out int mauSo)) return null;
            if (mauSo < 2 || tuSo >= mauSo) return null;

            bool wantsBar = fullContext.Contains("băng giấy") || fullContext.Contains("thanh chữ nhật");
            if (wantsBar && mauSo <= 12) return BuildFractionBarTikz(tuSo, mauSo);

            if (mauSo <= 16) return BuildFractionCircleTikz(tuSo, mauSo);
            if (mauSo <= 12) return BuildFractionBarTikz(tuSo, mauSo);
            return null;
        }

        /// <summary>Trích một số đo đứng sau từ khoá, ví dụ "chiều dài 8 cm". False nếu đề không nêu số.</summary>
        private static bool TryExtractNumber(string text, string keywordPattern, out double value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(text)) return false;

            var m = Regex.Match(text, $@"(?:{keywordPattern})\s*(?:là|bằng|:)?\s*(\d+(?:[.,]\d+)?)", RegexOptions.IgnoreCase);
            if (m.Success && double.TryParse(m.Groups[1].Value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double v))
            {
                value = v;
                return true;
            }
            return false;
        }

        /// <summary>Trích số đo góc, ví dụ "60 độ" hoặc "60°".</summary>
        private static bool TryExtractAngleDegrees(string text, out int degrees)
        {
            degrees = 0;
            if (string.IsNullOrWhiteSpace(text)) return false;

            var m = Regex.Match(text, @"(\d+)\s*(?:°|độ)", RegexOptions.IgnoreCase);
            if (m.Success && int.TryParse(m.Groups[1].Value, out int d) && d > 0 && d <= 359)
            {
                degrees = d;
                return true;
            }
            return false;
        }

        /// <summary>Lấy hai số đo có đơn vị đứng đầu trong đề (dùng cho hai đường chéo hình thoi).</summary>
        private static void ExtractTwoNumbers(string text, out double? first, out double? second)
        {
            first = null;
            second = null;
            if (string.IsNullOrWhiteSpace(text)) return;

            var ms = Regex.Matches(text, @"(\d+(?:[.,]\d+)?)\s*(?:cm|m|dm|mm)", RegexOptions.IgnoreCase);
            if (ms.Count >= 1 && double.TryParse(ms[0].Groups[1].Value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double a)) first = a;
            if (ms.Count >= 2 && double.TryParse(ms[1].Groups[1].Value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double b)) second = b;
        }

        private static bool TryBuildRectangleFigure(string fullContext, string content, out string tikz)
        {
            tikz = string.Empty;
            bool isRect = fullContext.Contains("hình chữ nhật");
            bool isSquare = fullContext.Contains("hình vuông");
            if (!isRect && !isSquare) return false;

            // "thanh chữ nhật"/"băng giấy" là dạng phân số, không phải hình chữ nhật hình học.
            if (fullContext.Contains("băng giấy") || fullContext.Contains("thanh chữ nhật")) return false;

            if (isRect)
            {
                bool hasD = TryExtractNumber(content, @"chiều dài|dài", out double dai);
                bool hasR = TryExtractNumber(content, @"chiều rộng|rộng", out double rong);
                tikz = BuildRectangleTikz(hasD ? dai : (double?)null, hasR ? rong : (double?)null);
                return true;
            }

            bool hasA = TryExtractNumber(content, @"cạnh", out double canh);
            tikz = BuildSquareTikz(hasA ? canh : (double?)null);
            return true;
        }

        private static string BuildRectangleTikz(double? dai, double? rong)
        {
            // Giữ đúng tỉ lệ nếu biết số đo, chỉ giới hạn khung vẽ cho vừa trang.
            double realD = dai.GetValueOrDefault(5.0);
            double realR = rong.GetValueOrDefault(3.0);
            if (realD <= 0) realD = 5.0;
            if (realR <= 0) realR = 3.0;

            double k = Math.Min(5.2 / realD, 3.2 / realR);
            double w = realD * k;
            double h = realR * k;

            string labelD = dai.HasValue ? TikzStyle.Len(dai.Value) : "chiều dài";
            string labelR = rong.HasValue ? TikzStyle.Len(rong.Value) : "chiều rộng";

            var sb = new StringBuilder();
            sb.AppendLine(@"\begin{tikzpicture}[scale=0.9]");
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"  \draw[thick] (0,0) rectangle ({0:0.##},{1:0.##});", w, h));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"  \node[below, font=\footnotesize] at ({0:0.##},0) {{{1}}};", w / 2, labelD));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"  \node[left, font=\footnotesize] at (0,{0:0.##}) {{{1}}};", h / 2, labelR));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"  \node[above left, font=\footnotesize] at (0,{0:0.##}) {{A}};", h));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"  \node[above right, font=\footnotesize] at ({0:0.##},{1:0.##}) {{B}};", w, h));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"  \node[below right, font=\footnotesize] at ({0:0.##},0) {{C}};", w));
            sb.AppendLine(@"  \node[below left, font=\footnotesize] at (0,0) {D};");
            sb.AppendLine(@"\end{tikzpicture}");
            return sb.ToString();
        }

        private static string BuildSquareTikz(double? canh)
        {
            double real = canh.GetValueOrDefault(3.0);
            if (real <= 0) real = 3.0;
            double side = Math.Min(3.6, real);

            string label = canh.HasValue ? TikzStyle.Len(canh.Value) : "a";

            var sb = new StringBuilder();
            sb.AppendLine(@"\begin{tikzpicture}[scale=0.9]");
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"  \draw[thick] (0,0) rectangle ({0:0.##},{0:0.##});", side));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"  \node[below, font=\footnotesize] at ({0:0.##},0) {{{1}}};", side / 2, label));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"  \node[above left, font=\footnotesize] at (0,{0:0.##}) {{A}};", side));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"  \node[above right, font=\footnotesize] at ({0:0.##},{0:0.##}) {{B}};", side));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"  \node[below right, font=\footnotesize] at ({0:0.##},0) {{C}};", side));
            sb.AppendLine(@"  \node[below left, font=\footnotesize] at (0,0) {D};");
            sb.AppendLine(@"\end{tikzpicture}");
            return sb.ToString();
        }

        private static bool TryBuildCircleFigure(string fullContext, string content, out string tikz)
        {
            tikz = string.Empty;
            if (!fullContext.Contains("hình tròn")) return false;

            bool isDiameter = fullContext.Contains("đường kính");
            bool isRadius = fullContext.Contains("bán kính");

            double val;
            bool hasVal;
            if (isDiameter && !isRadius) hasVal = TryExtractNumber(content, @"đường kính", out val);
            else if (isRadius && !isDiameter) hasVal = TryExtractNumber(content, @"bán kính", out val);
            else hasVal = TryExtractNumber(content, @"bán kính|đường kính", out val);

            tikz = BuildCircleTikz(isDiameter && !isRadius, hasVal ? val : (double?)null);
            return true;
        }

        private static string BuildCircleTikz(bool showDiameter, double? r)
        {
            string label;
            if (showDiameter)
            {
                label = r.HasValue ? $"d = {TikzStyle.Len(r.Value)}" : "đường kính";
            }
            else
            {
                label = r.HasValue ? $"r = {TikzStyle.Len(r.Value)}" : "bán kính";
            }

            var sb = new StringBuilder();
            sb.AppendLine(@"\begin{tikzpicture}[scale=0.9]");
            sb.AppendLine(@"  \coordinate (O) at (0,0);");
            sb.AppendLine(@"  \draw[thick] (O) circle (2);");
            sb.AppendLine(@"  \fill (O) circle (1.2pt);");
            sb.AppendLine(@"  \node[below left, font=\footnotesize] at (O) {O};");

            if (showDiameter)
            {
                sb.AppendLine(@"  \draw[thick] (-2,0) -- (2,0);");
                sb.AppendLine(@"  \node[left, font=\footnotesize] at (-2,0) {B};");
                sb.AppendLine(@"  \node[right, font=\footnotesize] at (2,0) {A};");
            }
            else
            {
                sb.AppendLine(@"  \draw[thick] (O) -- (2,0);");
                sb.AppendLine(@"  \node[right, font=\footnotesize] at (2,0) {A};");
            }

            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"  \node[above, font=\footnotesize] at (1.0,0.08) {{{0}}};", label));
            sb.AppendLine(@"\end{tikzpicture}");
            return sb.ToString();
        }

        private static bool TryBuildTriangleFigure(string fullContext, string content, out string tikz)
        {
            tikz = string.Empty;
            if (!fullContext.Contains("tam giác")) return false;

            bool hasDay = TryExtractNumber(content, @"đáy", out double day);
            bool hasCao = TryExtractNumber(content, @"chiều cao|đường cao", out double cao);

            string type;
            if (fullContext.Contains("tam giác vuông")) type = "vuông";
            else if (fullContext.Contains("tam giác đều")) type = "đều";
            else if (fullContext.Contains("tam giác cân")) type = "cân";
            else if (fullContext.Contains("tam giác tù")) type = "tù";
            else if (fullContext.Contains("tam giác nhọn")) type = "nhọn";
            else type = string.Empty;

            // Mọi tam giác (kể cả đã phân loại, trừ tam giác vuông) đều hiển thị đường cao khi đề nói tới
            // chiều cao / đường cao / diện tích hoặc có số liệu đáy - chiều cao. Tam giác vuông hiển thị ô vuông góc.
            bool wantsAltitude = type != "vuông" &&
                (fullContext.Contains("chiều cao") || fullContext.Contains("đường cao") || fullContext.Contains("diện tích") || hasDay || hasCao);

            tikz = BuildTriangleTikz(type, hasDay ? day : (double?)null, hasCao ? cao : (double?)null, wantsAltitude);
            return true;
        }

        private static bool TryBuildAngleFigure(MathQuestion q, string fullContext, string content, out string tikz)
        {
            tikz = string.Empty;
            bool isAngle = fullContext.Contains("góc nhọn") || fullContext.Contains("góc vuông")
                || fullContext.Contains("góc tù") || fullContext.Contains("góc bẹt")
                || fullContext.Contains("số đo góc") || fullContext.Contains("đo góc")
                || fullContext.Contains("góc xoy") || fullContext.Contains("góc aob");
            if (!isAngle) return false;

            // Kiểm tra trường hợp quan sát 4 góc: "bốn góc", "các góc (1)", "góc (1)", "hình (1)"
            bool isMultipleAngles = fullContext.Contains("bốn góc") || fullContext.Contains("4 góc")
                || (fullContext.Contains("góc (1)") && fullContext.Contains("góc (2)"))
                || (fullContext.Contains("(1)") && fullContext.Contains("(2)") && fullContext.Contains("(3)") && fullContext.Contains("(4)"));

            if (isMultipleAngles)
            {
                tikz = BuildFourAnglesTikz(q, fullContext, content);
                return true;
            }

            // Xác định chính xác góc cần vẽ dựa trên đáp án đúng và hướng dẫn giải
            string correctAnsText = q.DapAnDung?.Trim().ToUpperInvariant() switch
            {
                "A" => q.DapAnA ?? "",
                "B" => q.DapAnB ?? "",
                "C" => q.DapAnC ?? "",
                "D" => q.DapAnD ?? "",
                _ => q.DapAnDung ?? ""
            };
            string solText = string.Join(" ", q.CacBuocGiai?.Select(b => b.NoiDung) ?? Enumerable.Empty<string>());
            string targetContext = $"{correctAnsText} {solText} {content}".ToLowerInvariant();

            int deg;
            if (TryExtractAngleDegrees(content, out int d)) deg = d;
            else if (TryExtractAngleDegrees(correctAnsText, out int cd)) deg = cd;
            else if (targetContext.Contains("góc bẹt") || targetContext.Contains("180")) deg = 180;
            else if (targetContext.Contains("góc tù") || targetContext.Contains("120")) deg = 120;
            else if (targetContext.Contains("góc vuông") || targetContext.Contains("vuông góc") || targetContext.Contains("90")) deg = 90;
            else if (targetContext.Contains("góc nhọn")) deg = 50;
            else if (fullContext.Contains("góc vuông")) deg = 90;
            else deg = 50;

            tikz = BuildAngleTikz(deg);
            return true;
        }

        private static string BuildFourAnglesTikz(MathQuestion q, string fullContext, string content)
        {
            // Xác định vị trí đáp án đúng: A -> 1, B -> 2, C -> 3, D -> 4. Mặc định là 3 (C)
            int correctIndex = q.DapAnDung?.Trim().ToUpperInvariant() switch
            {
                "A" => 1,
                "B" => 2,
                "C" => 3,
                "D" => 4,
                _ => 3
            };

            // Xác định loại góc được hỏi trong đề bài
            string targetKind = "obtuse";
            if (content.Contains("góc nhọn")) targetKind = "acute";
            else if (content.Contains("góc vuông")) targetKind = "right";
            else if (content.Contains("góc bẹt")) targetKind = "straight";
            else if (content.Contains("góc tù")) targetKind = "obtuse";

            var allKinds = new List<string> { "acute", "right", "obtuse", "straight" };
            allKinds.Remove(targetKind);

            var slotKinds = new string[4];
            slotKinds[correctIndex - 1] = targetKind;
            int otherIdx = 0;
            for (int i = 0; i < 4; i++)
            {
                if (i != correctIndex - 1)
                {
                    slotKinds[i] = allKinds[otherIdx++];
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine(@"\begin{tikzpicture}[scale=0.85]");
            for (int i = 0; i < 4; i++)
            {
                double shiftX = i * 3.2;
                string label = $"Góc ({i + 1})";
                string kind = slotKinds[i];

                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"  \begin{{scope}}[shift={{({0:0.##},0)}}]", shiftX));
                switch (kind)
                {
                    case "acute":
                        sb.AppendLine(@"    \coordinate (O) at (0,0);");
                        sb.AppendLine(@"    \draw[thick] (O) -- (1.8,0);");
                        sb.AppendLine(@"    \draw[thick] (O) -- (50:1.8);");
                        sb.AppendLine(@"    \draw (0.5,0) arc (0:50:0.5);");
                        sb.AppendLine(@"    \fill (O) circle (1.2pt);");
                        sb.AppendLine(@"    \node[below left, font=\scriptsize] at (O) {O};");
                        sb.AppendLine(string.Format(@"    \node[below=4pt, font=\footnotesize] at (0.9,-0.1) {{{0}}};", label));
                        break;

                    case "right":
                        sb.AppendLine(@"    \coordinate (O) at (0,0);");
                        sb.AppendLine(@"    \draw[thick] (O) -- (1.8,0);");
                        sb.AppendLine(@"    \draw[thick] (O) -- (0,1.8);");
                        sb.AppendLine(@"    \draw (0.35,0) -- (0.35,0.35) -- (0,0.35);");
                        sb.AppendLine(@"    \fill (O) circle (1.2pt);");
                        sb.AppendLine(@"    \node[below left, font=\scriptsize] at (O) {O};");
                        sb.AppendLine(string.Format(@"    \node[below=4pt, font=\footnotesize] at (0.9,-0.1) {{{0}}};", label));
                        break;

                    case "obtuse":
                        sb.AppendLine(@"    \coordinate (O) at (0.8,0);");
                        sb.AppendLine(@"    \draw[thick] (O) -- ++(1.5,0);");
                        sb.AppendLine(@"    \draw[thick] (O) -- ++(125:1.5);");
                        sb.AppendLine(@"    \draw (O) ++(0.5,0) arc (0:125:0.5);");
                        sb.AppendLine(@"    \fill (O) circle (1.2pt);");
                        sb.AppendLine(@"    \node[below, font=\scriptsize] at (O) {O};");
                        sb.AppendLine(string.Format(@"    \node[below=4pt, font=\footnotesize] at (0.9,-0.1) {{{0}}};", label));
                        break;

                    case "straight":
                    default:
                        sb.AppendLine(@"    \coordinate (O) at (1.0,0.3);");
                        sb.AppendLine(@"    \draw[thick] (O) -- ++(1.4,0);");
                        sb.AppendLine(@"    \draw[thick] (O) -- ++(-1.4,0);");
                        sb.AppendLine(@"    \draw (O) ++(0.4,0) arc (0:180:0.4);");
                        sb.AppendLine(@"    \fill (O) circle (1.2pt);");
                        sb.AppendLine(@"    \node[below, font=\scriptsize] at (O) {O};");
                        sb.AppendLine(string.Format(@"    \node[below=4pt, font=\footnotesize] at (1.0,-0.1) {{{0}}};", label));
                        break;
                }
                sb.AppendLine(@"  \end{scope}");
            }
            sb.AppendLine(@"\end{tikzpicture}");
            return sb.ToString();
        }

        private static string BuildAngleTikz(int deg)
        {
            var sb = new StringBuilder();
            sb.AppendLine(@"\begin{tikzpicture}[scale=1.0]");
            sb.AppendLine(@"  \coordinate (O) at (0,0);");
            sb.AppendLine(@"  \draw[thick] (O) -- (0:3.2) node[right, font=\footnotesize] {x};");
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"  \draw[thick] (O) -- ({0}:3.2) node[above right, font=\footnotesize] {{y}};", deg));

            if (deg == 90)
            {
                sb.AppendLine(@"  \draw (0.5,0) -- (0.5,0.5) -- (0,0.5);");
            }
            else
            {
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"  \draw (0.9,0) arc (0:{0}:0.9);", deg));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"  \node[font=\footnotesize] at ({0:0.#}:1.3) {{{1}$^\circ$}};", deg / 2.0, deg));
            }

            sb.AppendLine(@"  \fill (O) circle (1.2pt);");
            sb.AppendLine(@"  \node[below left, font=\footnotesize] at (O) {O};");
            sb.AppendLine(@"\end{tikzpicture}");
            return sb.ToString();
        }

        /// <summary>
        /// Hai đường thẳng vuông góc (có ô vuông góc tại giao điểm O) hoặc song song (hai đường không cắt nhau).
        /// Bỏ qua khi đề thuộc các dạng hình phẳng/góc đã có bộ dựng riêng.
        /// </summary>
        private static bool TryBuildPerpendicularParallelFigure(string fullContext, string content, out string tikz)
        {
            tikz = string.Empty;

            bool isPerpendicular = fullContext.Contains("vuông góc");
            bool isParallel = fullContext.Contains("song song");
            if (!isPerpendicular && !isParallel) return false;

            // Các bài về hình phẳng / đường chéo / góc đã có bộ dựng riêng -> bỏ qua.
            string[] exclude =
            {
                "hình thoi", "hình bình hành", "hình thang", "hình chữ nhật", "hình vuông", "tam giác",
                "đường chéo", "góc nhọn", "góc vuông", "góc tù", "góc bẹt"
            };
            foreach (var e in exclude)
            {
                if (fullContext.Contains(e)) return false;
            }
            if (!fullContext.Contains("đường thẳng") && !fullContext.Contains("hai đường")) return false;

            // Rút tên điểm từ các cặp chữ in hoa liền nhau trong đề (AB, CD, MN, PQ...).
            var pairs = Regex.Matches(content, @"\b([A-Z])([A-Z])\b");

            if (isPerpendicular)
            {
                string p1a = "A", p1b = "B", p2a = "C", p2b = "D";
                if (pairs.Count >= 1) { p1a = pairs[0].Groups[1].Value; p1b = pairs[0].Groups[2].Value; }
                if (pairs.Count >= 2) { p2a = pairs[1].Groups[1].Value; p2b = pairs[1].Groups[2].Value; }
                tikz = BuildPerpendicularLinesTikz(p1a, p1b, p2a, p2b);
                return true;
            }

            // Song song: ưu tiên tên đoạn AB, CD; nếu đề gọi tên a, b thì dùng chữ thường.
            string l1, r1, l2, r2;
            if (pairs.Count >= 2)
            {
                l1 = pairs[0].Groups[1].Value; r1 = pairs[0].Groups[2].Value;
                l2 = pairs[1].Groups[1].Value; r2 = pairs[1].Groups[2].Value;
            }
            else
            {
                var lows = Regex.Matches(content, @"(?:đường thẳng|đoạn thẳng)\s+([a-z])\b");
                if (lows.Count >= 2)
                {
                    l1 = lows[0].Groups[1].Value; r1 = string.Empty;
                    l2 = lows[1].Groups[1].Value; r2 = string.Empty;
                }
                else
                {
                    l1 = "a"; r1 = string.Empty; l2 = "b"; r2 = string.Empty;
                }
            }
            tikz = BuildParallelLinesTikz(l1, r1, l2, r2);
            return true;
        }

        private static string BuildPerpendicularLinesTikz(string p1a, string p1b, string p2a, string p2b)
        {
            var sb = new StringBuilder();
            sb.AppendLine(@"\begin{tikzpicture}[scale=0.85]");
            sb.AppendLine(@"  \draw[thick] (-0.6,0) -- (5.2,0);");
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"  \node[left, font=\footnotesize] at (-0.6,0) {{{0}}};", p1a));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"  \node[right, font=\footnotesize] at (5.2,0) {{{0}}};", p1b));
            sb.AppendLine(@"  \draw[thick] (2.2,-0.9) -- (2.2,3.3);");
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"  \node[below, font=\footnotesize] at (2.2,-0.9) {{{0}}};", p2a));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"  \node[above, font=\footnotesize] at (2.2,3.3) {{{0}}};", p2b));
            sb.AppendLine(@"  \draw (2.55,0) -- (2.55,0.35) -- (2.2,0.35);");
            sb.AppendLine(@"  \fill (2.2,0) circle (1.2pt);");
            sb.AppendLine(@"  \node[below left, font=\footnotesize] at (2.2,0) {O};");
            sb.AppendLine(@"\end{tikzpicture}");
            return sb.ToString();
        }

        private static string BuildParallelLinesTikz(string l1, string r1, string l2, string r2)
        {
            var sb = new StringBuilder();
            sb.AppendLine(@"\begin{tikzpicture}[scale=0.85]");
            sb.AppendLine(@"  \draw[thick] (0,0.3) -- (5.2,1.2);");
            if (!string.IsNullOrEmpty(l1))
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"  \node[left, font=\footnotesize] at (0,0.3) {{{0}}};", l1));
            if (!string.IsNullOrEmpty(r1))
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"  \node[right, font=\footnotesize] at (5.2,1.2) {{{0}}};", r1));
            sb.AppendLine(@"  \draw[thick] (0,2.0) -- (5.2,2.9);");
            if (!string.IsNullOrEmpty(l2))
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"  \node[left, font=\footnotesize] at (0,2.0) {{{0}}};", l2));
            if (!string.IsNullOrEmpty(r2))
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, @"  \node[right, font=\footnotesize] at (5.2,2.9) {{{0}}};", r2));
            sb.AppendLine(@"\end{tikzpicture}");
            return sb.ToString();
        }

        private static bool TryBuildParallelogramFigure(string fullContext, string content, out string tikz)
        {
            tikz = string.Empty;
            if (!fullContext.Contains("hình bình hành")) return false;

            bool hasDay = TryExtractNumber(content, @"đáy", out double day);
            bool hasCao = TryExtractNumber(content, @"chiều cao", out double cao);
            tikz = BuildParallelogramTikz(hasDay ? day : (double?)null, hasCao ? cao : (double?)null);
            return true;
        }

        private static bool TryBuildRhombusFigure(string fullContext, string content, out string tikz)
        {
            tikz = string.Empty;
            if (!fullContext.Contains("hình thoi")) return false;

            ExtractTwoNumbers(content, out double? cheo1, out double? cheo2);
            tikz = BuildRhombusTikz(cheo1, cheo2);
            return true;
        }

        #endregion

        #endregion

        #region 2. AI Họa sĩ chuyên biệt (Dedicated AI Painter)

        /// <summary>
        /// Gửi riêng câu hỏi này cho AI (Gemini / DeepSeek) để phân tích đề và vẽ hình TikZ
        /// </summary>
        public static async Task<string> GenerateTikzWithAiAsync(AppSettings settings, MathQuestion q, int khoiLop = 0)
        {
            var gemini = new GeminiService();
            string prompt = BuildDedicatedTikzPrompt(q, khoiLop);
            string rawOutput = await gemini.GenerateContentRawAsync(settings.AiProvider, settings.ActiveApiKey, settings.ActiveModel, prompt);

            return CleanTikzCode(rawOutput);
        }

        private static string BuildDedicatedTikzPrompt(MathQuestion q, int khoiLop)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Bạn là Họa sĩ và Chuyên gia lập trình đồ họa LaTeX TikZ hàng đầu cho Sách giáo khoa Tiểu học Việt Nam.");
            sb.AppendLine("HÃY ĐỌC KỸ CÂU HỎI DƯỚI ĐÂY ĐỂ VẼ HÌNH MINH HỌA TIKZ CHÍNH XÁC 100% THEO ĐỀ BÀI:");
            sb.AppendLine();
            sb.AppendLine($"CÂU HỎI {q.SoThuTu}: {q.NoiDung}");
            if (!string.IsNullOrWhiteSpace(q.DapAnA)) sb.AppendLine($"A. {q.DapAnA}");
            if (!string.IsNullOrWhiteSpace(q.DapAnB)) sb.AppendLine($"B. {q.DapAnB}");
            if (!string.IsNullOrWhiteSpace(q.DapAnC)) sb.AppendLine($"C. {q.DapAnC}");
            if (!string.IsNullOrWhiteSpace(q.DapAnD)) sb.AppendLine($"D. {q.DapAnD}");
            if (!string.IsNullOrWhiteSpace(q.DapAnDung)) sb.AppendLine($"Đáp án đúng: {q.DapAnDung}");
            sb.AppendLine();
            sb.AppendLine("QUY TẮC BẮT BUỘC KHI VIẾT MÃ TIKZ:");
            sb.AppendLine("1. Đọc hiểu chính xác từng số liệu, tỉ lệ, tên các đỉnh (A, B, C, D, H...) hoặc số phần tô màu được nêu trong đề.");
            sb.AppendLine("2. Sử dụng tọa độ cân đối, đẹp mắt, nằm trọn trong khung tỉ lệ vừa vặn (scale=0.8 đến scale=1.0) để chèn Word không bị tràn lề.");
            sb.AppendLine("3. Đường nét đứt [dashed] cho đường cao, đường chéo ẩn hoặc cạnh khuất 3D.");
            sb.AppendLine("4. Các chữ cái tên đỉnh hoặc nhãn số đo viết rõ ràng, có dấu tiếng Việt đầy đủ (ví dụ: 'Chiều dài', 'Số cây', 'Lớp 4A').");
            sb.AppendLine("5. Nếu là phân số hình tròn: BẮT BUỘC tô màu múi trước (\\fill[blue!35]), sau đó vẽ các nan hoa đè lên trên để không bị che khuất vạch chia.");
            sb.AppendLine("6. CHỈ TRẢ VỀ DUY NHẤT KHỐI MÃ TIKZ BẮT ĐẦU BẰNG \\begin{tikzpicture} VÀ KẾT THÚC BẰNG \\end{tikzpicture}. TUYỆT ĐỐI không có lời chào, giải thích hay markdown ```latex.");
            sb.AppendLine("7. Nếu đề bài yêu cầu quan sát nhiều hình/nhiều góc (như bốn góc (1), (2), (3), (4) hoặc các hình A, B, C, D): BẮT BUỘC phải vẽ đủ tất cả các hình/góc đó trong cùng một môi trường tikzpicture bằng cách đặt cạnh nhau theo hàng ngang (dùng scope shift={{(0,0)}}, shift={{(3.2,0)}}...), bên dưới mỗi hình có ghi rõ nhãn tương ứng (ví dụ: Góc (1), Góc (2)...).");
            sb.AppendLine("8. Nếu vẽ biểu đồ cột: BẮT BUỘC dùng [xscale=1.2, yscale=0.15] (hoặc yscale tương ứng) để chiều cao biểu đồ không vượt quá 6cm. TUYỆT ĐỐI không viết trên 1 dòng duy nhất, mỗi lệnh xuống 1 dòng.");
            sb.AppendLine("9. Nếu vẽ biểu đồ hình quạt tròn: vẽ các miếng quạt theo đúng tỉ số phần trăm ghi trong đề, ghi số % trong từng miếng và kèm chú giải màu (ô màu + tên loại) bên phải.");
            sb.AppendLine();
            sb.AppendLine(PromptBuilderService.GetMathFigureLibrary(khoiLop));

            return sb.ToString();
        }

        public static string CleanTikzCode(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

            string text = TikzRenderService.SanitizeTikzCode(raw);
            if (text.StartsWith("```latex", StringComparison.OrdinalIgnoreCase))
                text = text.Substring(8);
            else if (text.StartsWith("```tikz", StringComparison.OrdinalIgnoreCase))
                text = text.Substring(7);
            else if (text.StartsWith("```", StringComparison.OrdinalIgnoreCase))
                text = text.Substring(3);

            if (text.EndsWith("```"))
                text = text.Substring(0, text.Length - 3);

            text = text.Trim();

            // Đảm bảo lấy đúng từ \begin{tikzpicture} đến \end{tikzpicture}
            int startIdx = text.IndexOf("\\begin{tikzpicture}", StringComparison.OrdinalIgnoreCase);
            int endIdx = text.IndexOf("\\end{tikzpicture}", StringComparison.OrdinalIgnoreCase);

            if (startIdx >= 0 && endIdx >= startIdx)
            {
                text = text.Substring(startIdx, endIdx + "\\end{tikzpicture}".Length - startIdx);
            }

            return TikzRenderService.SanitizeTikzCode(text);
        }

        #endregion
    }
}
