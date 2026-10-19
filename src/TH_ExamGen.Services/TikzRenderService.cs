using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TH_ExamGen.Services
{
    public class TikzRenderService
    {
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };

        public static string ResolveMiktexBinPath(string? customBinPath)
        {
            // 1. Thử đường dẫn do người dùng cấu hình
            if (!string.IsNullOrWhiteSpace(customBinPath))
            {
                string cleaned = customBinPath.Trim().Replace(@"\\", @"\").TrimEnd('\\', '/');
                if (File.Exists(Path.Combine(cleaned, "pdflatex.exe")))
                {
                    return cleaned;
                }

                // Kiểm tra bundle/binpath.txt
                string txt1 = Path.Combine(cleaned, "bundle", "binpath.txt");
                if (File.Exists(txt1))
                {
                    try
                    {
                        string line = File.ReadAllText(txt1).Trim().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0].Trim().Replace(@"\\", @"\");
                        if (File.Exists(Path.Combine(line, "pdflatex.exe"))) return line;
                    }
                    catch { }
                }

                string txt2 = Path.Combine(cleaned, "binpath.txt");
                if (File.Exists(txt2))
                {
                    try
                    {
                        string line = File.ReadAllText(txt2).Trim().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0].Trim().Replace(@"\\", @"\");
                        if (File.Exists(Path.Combine(line, "pdflatex.exe"))) return line;
                    }
                    catch { }
                }

                // Kiểm tra các thư mục con quen thuộc của MiKTeX Portable
                string[] subDirs = new[]
                {
                    Path.Combine(cleaned, "bundle", "miktex", "texmfs", "install", "miktex", "bin", "x64"),
                    Path.Combine(cleaned, "miktex", "texmfs", "install", "miktex", "bin", "x64"),
                    Path.Combine(cleaned, "texmfs", "install", "miktex", "bin", "x64"),
                    Path.Combine(cleaned, "bin", "x64"),
                    Path.Combine(cleaned, "bin")
                };
                foreach (var dir in subDirs)
                {
                    if (File.Exists(Path.Combine(dir, "pdflatex.exe"))) return dir;
                }
            }

            // 2. Tự động dò tìm từ thư mục ứng dụng hoặc thư mục gốc dự án
            try
            {
                string[] candidateRoots = new[]
                {
                    AppDomain.CurrentDomain.BaseDirectory,
                    Directory.GetCurrentDirectory(),
                    Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..")),
                    Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..")),
                    Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..")),
                    @"C:\Users\Administrator\Desktop\D"
                };

                foreach (var root in candidateRoots)
                {
                    if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) continue;

                    string candidateTxt = Path.Combine(root, "tools", "TikzOffline", "bundle", "binpath.txt");
                    if (File.Exists(candidateTxt))
                    {
                        try
                        {
                            string line = File.ReadAllText(candidateTxt).Trim().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0].Trim().Replace(@"\\", @"\");
                            if (File.Exists(Path.Combine(line, "pdflatex.exe"))) return line;
                        }
                        catch { }
                    }

                    string candidateBin = Path.Combine(root, "tools", "TikzOffline", "bundle", "miktex", "texmfs", "install", "miktex", "bin", "x64");
                    if (File.Exists(Path.Combine(candidateBin, "pdflatex.exe"))) return candidateBin;
                }
            }
            catch { }

            return customBinPath ?? string.Empty;
        }

        public static bool IsLatexAvailable(string? customBinPath = null)
        {
            string pdflatexExe = GetExecutablePath("pdflatex", customBinPath);
            return File.Exists(pdflatexExe) || CanRunCommand(pdflatexExe, "--version");
        }

        // Ghi nhớ kết quả kiểm tra pdflatex để không phải dò (spawn tiến trình) lại nhiều lần trong cùng phiên.
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, bool> _latexAvailableCache = new();

        public static bool IsLatexAvailableCached(string? customBinPath)
        {
            string key = ResolveMiktexBinPath(customBinPath);
            return _latexAvailableCache.GetOrAdd(key, _ => IsLatexAvailable(customBinPath));
        }

        /// <summary>
        /// Tên file cache theo nội dung TikZ (hash) để tái sử dụng ảnh đã render, tránh gọi lại mạng/pdflatex.
        /// </summary>
        public static string GetCacheFileName(string tikzCode)
        {
            string normalized = SanitizeTikzCode(tikzCode) ?? string.Empty;
            using var sha = System.Security.Cryptography.SHA1.Create();
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(normalized));
            var sb = new StringBuilder("tikz_");
            for (int i = 0; i < 10; i++)
            {
                sb.Append(hash[i].ToString("x2"));
            }
            sb.Append(".png");
            return sb.ToString();
        }

        /// <summary>
        /// Xóa các ảnh cache cũ hơn số ngày chỉ định để tránh thư mục TikzCache phình to mãi.
        /// </summary>
        public static void CleanupCache(string cacheDir, int olderThanDays = 14)
        {
            try
            {
                if (!Directory.Exists(cacheDir)) return;
                DateTime cutoff = DateTime.Now.AddDays(-olderThanDays);
                foreach (string file in Directory.GetFiles(cacheDir, "*.png"))
                {
                    try
                    {
                        if (File.GetLastWriteTime(file) < cutoff) File.Delete(file);
                    }
                    catch
                    {
                        // Bỏ qua từng file lỗi
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Warn($"Dọn cache TikZ lỗi: {ex.Message}");
            }
        }

        public static string? RenderTikzToPng(string tikzCode, string outputPngPath, string? customBinPath = null)
        {
            // Bọc trong Task.Run để chắc chắn không deadlock nếu vô tình gọi trên UI thread.
            return Task.Run(() => RenderTikzToPngAsync(tikzCode, outputPngPath, customBinPath)).GetAwaiter().GetResult();
        }

        public static async Task<string?> RenderTikzToPngAsync(string tikzCode, string outputPngPath, string? customBinPath = null)
        {
            var result = await RenderTikzToPngResultAsync(tikzCode, outputPngPath, customBinPath);
            return result.Success ? result.Path : null;
        }

        /// <summary>
        /// Render TikZ và trả về kết quả chi tiết (thành công/thất bại + lý do) để giao diện
        /// cảnh báo rõ ràng thay vì âm thầm bỏ qua hình.
        /// </summary>
        public static async Task<TikzRenderResult> RenderTikzToPngResultAsync(string tikzCode, string outputPngPath, string? customBinPath = null)
        {
            var result = new TikzRenderResult();

            if (string.IsNullOrWhiteSpace(tikzCode))
            {
                result.Error = "Mã TikZ rỗng.";
                return result;
            }

            // 1. Thử biên dịch với dấu Tiếng Việt chuẩn LaTeX
            string fullLatex = NormalizeTikzDocument(tikzCode, useVietnameseAccents: true);

            // Thử pdflatex cục bộ trước nếu có
            if (IsLatexAvailableCached(customBinPath))
            {
                string? localResult = RenderLocalPdflatex(fullLatex, outputPngPath, customBinPath);
                if (localResult != null && File.Exists(localResult))
                {
                    result.Path = localResult;
                    return result;
                }

                // Thử lại cục bộ với bản bỏ dấu nếu bản có dấu gặp lỗi ký tự
                string fallbackLatexLocal = NormalizeTikzDocument(tikzCode, useVietnameseAccents: false);
                localResult = RenderLocalPdflatex(fallbackLatexLocal, outputPngPath, customBinPath);
                if (localResult != null && File.Exists(localResult))
                {
                    result.Path = localResult;
                    return result;
                }

                result.Error = "Biên dịch cục bộ (pdflatex) thất bại.";
            }
            else
            {
                result.Error = "Không tìm thấy MiKTeX/pdflatex trên máy.";
            }

            // 2. Gọi Kroki API (có thử lại) với bản có dấu
            try
            {
                string? onlineResult = await RenderViaKrokiWithRetryAsync(fullLatex, outputPngPath);
                if (onlineResult != null && File.Exists(onlineResult))
                {
                    result.Path = onlineResult;
                    result.UsedOnline = true;
                    result.Error = null;
                    return result;
                }
                result.Error += " Dịch vụ Kroki không trả về kết quả.";
            }
            catch (Exception ex)
            {
                AppLogger.Warn($"Lỗi Kroki TikZ API (có dấu): {ex.Message}");
                result.Error += " Không kết nối được Kroki (kiểm tra Internet).";
            }

            // 3. Fallback an toàn: Thử lại với bản bỏ dấu nếu TeX báo lỗi
            try
            {
                string fallbackLatex = NormalizeTikzDocument(tikzCode, useVietnameseAccents: false);
                string? onlineResult = await RenderViaKrokiWithRetryAsync(fallbackLatex, outputPngPath);
                if (onlineResult != null && File.Exists(onlineResult))
                {
                    AppLogger.Warn("TikZ: bản có dấu không biên dịch được, đã dùng bản BỎ DẤU (nhãn trong hình có thể mất dấu tiếng Việt).");
                    result.Path = onlineResult;
                    result.UsedOnline = true;
                    result.Error = null;
                    return result;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Warn($"Lỗi Kroki TikZ API (fallback không dấu): {ex.Message}");
            }

            if (string.IsNullOrWhiteSpace(result.Error))
            {
                result.Error = "Không thể render hình TikZ.";
            }
            return result;
        }

        /// <summary>
        /// Làm sạch mã TikZ: Chuyển các ký tự literal \r\n, \n thành ký tự xuống dòng thực tế,
        /// gỡ bỏ lỗi double-escape \\draw, \\def, \\node... về dạng lệnh \ đơn chuẩn LaTeX.
        /// </summary>
        public static string SanitizeTikzCode(string tikzCode)
        {
            if (string.IsNullOrWhiteSpace(tikzCode)) return string.Empty;

            // 1. Chuyển đổi các chuỗi ký tự xuống dòng literal (\r\n hoặc \n) thành ký tự xuống dòng thực tế.
            //    Lưu ý: CHỈ thay khi \n không đứng đầu một lệnh LaTeX — tránh cắt nát \node, \newcommand, \newline... thành xuống dòng + "ode".
            string s = tikzCode.Replace(@"\r\n", "\n");
            s = Regex.Replace(s, @"\\n(?![a-zA-Z])", "\n");

            // 2. Gỡ bỏ double-escape cho các lệnh LaTeX: \\draw -> \draw, \\def -> \def, \\foreach -> \foreach, \\small -> \small, \\node -> \node...
            s = Regex.Replace(s, @"\\\\([a-zA-Z]+)", @"\$1");

            // 3. Nếu các thẻ trong \foreach có dạng {"5A"}, chuẩn hóa bỏ ngoặc kép không cần thiết nếu có
            s = Regex.Replace(s, @"\{""([^""]+)""\}", "{$1}");

            return s.Trim();
        }

        private static string NormalizeTikzDocument(string tikzCode, bool useVietnameseAccents = true)
        {
            string trimmed = SanitizeTikzCode(tikzCode);
            if (useVietnameseAccents)
            {
                trimmed = ConvertVietnameseToLatexAccents(trimmed);
            }
            else
            {
                trimmed = RemoveVietnameseDiacritics(trimmed);
            }

            // Nếu người dùng/AI đã bao gồm full document thì giữ nguyên
            if (trimmed.Contains(@"\begin{document}"))
            {
                return trimmed;
            }

            // Nếu chưa có \begin{tikzpicture}, tự động bọc lại
            if (!trimmed.Contains(@"\begin{tikzpicture}"))
            {
                trimmed = trimmed.Replace(@"\end{tikzpicture}", "").Trim();
                trimmed = $"\\begin{{tikzpicture}}[scale=0.85]\n{trimmed}\n\\end{{tikzpicture}}";
            }
            else if (!trimmed.Contains(@"\end{tikzpicture}"))
            {
                trimmed = $"{trimmed}\n\\end{{tikzpicture}}";
            }

            return $@"
\documentclass[tikz,border=4pt]{{standalone}}
\usepackage[T1]{{fontenc}}
\usepackage[utf8]{{inputenc}}
\usepackage{{tikz}}
\usepackage{{graphicx}}
\usepackage{{amsmath,amssymb}}
\usetikzlibrary{{calc,shapes.geometric,arrows,patterns,positioning,decorations.pathreplacing}}
\providecommand{{\ohorn}}{{o\kern-0.08em\raisebox{{0.5ex}}{{\scalebox{{0.6}}{{'}}}}}}
\providecommand{{\Ohorn}}{{O\kern-0.08em\raisebox{{0.5ex}}{{\scalebox{{0.6}}{{'}}}}}}
\providecommand{{\uhorn}}{{u\kern-0.08em\raisebox{{0.5ex}}{{\scalebox{{0.6}}{{'}}}}}}
\providecommand{{\Uhorn}}{{U\kern-0.08em\raisebox{{0.5ex}}{{\scalebox{{0.6}}{{'}}}}}}
\begin{{document}}
{trimmed}
\end{{document}}
".Trim();
        }

        public static string ConvertVietnameseToLatexAccents(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            // Chuẩn hoá NFC trước: nếu chuỗi ở dạng tổ hợp (NFD) thì bảng thay thế bên dưới
            // sẽ không khớp ký tự, khiến hình bị chuyển sang bản bỏ dấu. NFC giữ nguyên dấu tiếng Việt.
            text = text.Normalize(NormalizationForm.FormC);

            var map = new (string From, string To)[]
            {
                // Consonants
                ("đ", @"\dj{}"), ("Đ", @"\DJ{}"),

                // ă
                ("ắ", @"\'{\u{a}}"), ("ằ", @"\`{\u{a}}"), ("ẳ", @"\v{\u{a}}"), ("ẵ", @"\~{\u{a}}"), ("ặ", @"\d{\u{a}}"), ("ă", @"\u{a}"),
                ("Ắ", @"\'{\u{A}}"), ("Ằ", @"\`{\u{A}}"), ("Ẳ", @"\v{\u{A}}"), ("Ẵ", @"\~{\u{A}}"), ("Ặ", @"\d{\u{A}}"), ("Ă", @"\u{A}"),

                // â
                ("ấ", @"\'{\^{a}}"), ("ầ", @"\`{\^{a}}"), ("ẩ", @"\v{\^{a}}"), ("ẫ", @"\~{\^{a}}"), ("ậ", @"\d{\^{a}}"), ("â", @"\^{a}"),
                ("Ấ", @"\'{\^{A}}"), ("Ầ", @"\`{\^{A}}"), ("Ẩ", @"\v{\^{A}}"), ("Ẫ", @"\~{\^{A}}"), ("Ậ", @"\d{\^{A}}"), ("Â", @"\^{A}"),

                // a
                ("á", @"\'{a}"), ("à", @"\`{a}"), ("ả", @"\v{a}"), ("ã", @"\~{a}"), ("ạ", @"\d{a}"),
                ("Á", @"\'{A}"), ("À", @"\`{A}"), ("Ả", @"\v{A}"), ("Ã", @"\~{A}"), ("Ạ", @"\d{A}"),

                // ê
                ("ế", @"\'{\^{e}}"), ("ề", @"\`{\^{e}}"), ("ể", @"\v{\^{e}}"), ("ễ", @"\~{\^{e}}"), ("ệ", @"\d{\^{e}}"), ("ê", @"\^{e}"),
                ("Ế", @"\'{\^{E}}"), ("Ề", @"\`{\^{E}}"), ("Ể", @"\v{\^{E}}"), ("Ễ", @"\~{\^{E}}"), ("Ệ", @"\d{\^{E}}"), ("Ê", @"\^{E}"),

                // e
                ("é", @"\'{e}"), ("è", @"\`{e}"), ("ẻ", @"\v{e}"), ("ẽ", @"\~{e}"), ("ẹ", @"\d{e}"),
                ("É", @"\'{E}"), ("È", @"\`{E}"), ("Ẻ", @"\v{E}"), ("Ẽ", @"\~{E}"), ("Ẹ", @"\d{E}"),

                // i
                ("í", @"\'{i}"), ("ì", @"\`{i}"), ("ỉ", @"\v{i}"), ("ĩ", @"\~{i}"), ("ị", @"\d{i}"),
                ("Í", @"\'{I}"), ("Ì", @"\`{I}"), ("Ỉ", @"\v{I}"), ("Ĩ", @"\~{I}"), ("Ị", @"\d{I}"),

                // ô
                ("ố", @"\'{\^{o}}"), ("ồ", @"\`{\^{o}}"), ("ổ", @"\v{\^{o}}"), ("ỗ", @"\~{\^{o}}"), ("ộ", @"\d{\^{o}}"), ("ô", @"\^{o}"),
                ("Ố", @"\'{\^{O}}"), ("Ồ", @"\`{\^{O}}"), ("Ổ", @"\v{\^{O}}"), ("Ỗ", @"\~{\^{O}}"), ("Ộ", @"\d{\^{O}}"), ("Ô", @"\^{O}"),

                // ơ
                ("ớ", @"\'{\ohorn{}}"), ("ờ", @"\`{\ohorn{}}"), ("ở", @"\v{\ohorn{}}"), ("ỡ", @"\~{\ohorn{}}"), ("ợ", @"\d{\ohorn{}}"), ("ơ", @"\ohorn{}"),
                ("Ớ", @"\'{\Ohorn{}}"), ("Ờ", @"\`{\Ohorn{}}"), ("Ở", @"\v{\Ohorn{}}"), ("Ỡ", @"\~{\Ohorn{}}"), ("Ợ", @"\d{\Ohorn{}}"), ("Ơ", @"\Ohorn{}"),

                // o
                ("ó", @"\'{o}"), ("ò", @"\`{o}"), ("ỏ", @"\v{o}"), ("õ", @"\~{o}"), ("ọ", @"\d{o}"),
                ("Ó", @"\'{O}"), ("Ò", @"\`{O}"), ("Ỏ", @"\v{O}"), ("Õ", @"\~{O}"), ("Ọ", @"\d{O}"),

                // ư
                ("ứ", @"\'{\uhorn{}}"), ("ừ", @"\`{\uhorn{}}"), ("ử", @"\v{\uhorn{}}"), ("ữ", @"\~{\uhorn{}}"), ("ự", @"\d{\uhorn{}}"), ("ư", @"\uhorn{}"),
                ("Ứ", @"\'{\Uhorn{}}"), ("Ừ", @"\`{\Uhorn{}}"), ("Ử", @"\v{\Uhorn{}}"), ("Ữ", @"\~{\Uhorn{}}"), ("Ự", @"\d{\Uhorn{}}"), ("Ư", @"\Uhorn{}"),

                // u
                ("ú", @"\'{u}"), ("ù", @"\`{u}"), ("ủ", @"\v{u}"), ("ũ", @"\~{u}"), ("ụ", @"\d{u}"),
                ("Ú", @"\'{U}"), ("Ù", @"\`{U}"), ("Ủ", @"\v{U}"), ("Ũ", @"\~{U}"), ("Ụ", @"\d{U}"),

                // y
                ("ý", @"\'{y}"), ("ỳ", @"\`{y}"), ("ỷ", @"\v{y}"), ("ỹ", @"\~{y}"), ("ỵ", @"\d{y}"),
                ("Ý", @"\'{Y}"), ("Ỳ", @"\`{Y}"), ("Ỷ", @"\v{Y}"), ("Ỹ", @"\~{Y}"), ("Ỵ", @"\d{Y}")
            };

            var sb = new StringBuilder(text);
            foreach (var item in map)
            {
                sb.Replace(item.From, item.To);
            }
            return sb.ToString();
        }

        private static string RemoveVietnameseDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            // Chuẩn hoá NFC trước để việc bỏ dấu khớp đúng từng ký tự dựng sẵn.
            text = text.Normalize(NormalizationForm.FormC);

            string[] vietnameseSigns = new string[]
            {
                "aAeEoOuUiIdDyY",
                "áàạảãâấầậẩẫăắằặẳẵ",
                "ÁÀẠẢÃÂẤẦẬẨẪĂẮẰẶẲẴ",
                "éèẹẻẽêếềệểễ",
                "ÉÈẸẺẼÊẾỀỆỂỄ",
                "óòọỏõôốồộổỗơớờợởỡ",
                "ÓÒỌỎÕÔỐỒỘỔỖƠỚỜỢỞỠ",
                "úùụủũưứừựửữ",
                "ÚÙỤỦŨƯỨỪỰỬỮ",
                "íìịỉĩ",
                "ÍÌỊỈĨ",
                "đ",
                "Đ",
                "ýỳỵỷỹ",
                "ÝỲỴỶỸ"
            };

            for (int i = 1; i < vietnameseSigns.Length; i++)
            {
                for (int j = 0; j < vietnameseSigns[i].Length; j++)
                {
                    text = text.Replace(vietnameseSigns[i][j], vietnameseSigns[0][i - 1]);
                }
            }
            return text;
        }

        private static async Task<string?> RenderViaKrokiWithRetryAsync(string fullLatexDoc, string outputPngPath)
        {
            Exception? last = null;
            const int attempts = 2;
            for (int attempt = 0; attempt < attempts; attempt++)
            {
                try
                {
                    string? res = await RenderViaKrokiApiAsync(fullLatexDoc, outputPngPath);
                    if (res != null) return res;
                }
                catch (Exception ex)
                {
                    last = ex;
                    AppLogger.Warn($"Kroki lần {attempt + 1}/{attempts} lỗi: {ex.Message}");
                }

                if (attempt < attempts - 1)
                {
                    await Task.Delay(1200);
                }
            }

            if (last != null) throw last;
            return null;
        }

        private static async Task<string?> RenderViaKrokiApiAsync(string fullLatexDoc, string outputPngPath)
        {
            var content = new StringContent(fullLatexDoc, Encoding.UTF8, "text/plain");
            var response = await _httpClient.PostAsync("https://kroki.io/tikz/png", content);

            if (response.IsSuccessStatusCode)
            {
                byte[] bytes = await response.Content.ReadAsByteArrayAsync();
                if (bytes != null && bytes.Length > 0)
                {
                    string dir = Path.GetDirectoryName(outputPngPath) ?? string.Empty;
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    await File.WriteAllBytesAsync(outputPngPath, bytes);
                    return outputPngPath;
                }
            }
            else
            {
                // Ghi lại thông báo lỗi của Kroki để chẩn đoán (vd thiếu gói LaTeX, lỗi cú pháp TikZ).
                try
                {
                    string err = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(err))
                    {
                        if (err.Length > 300) err = err.Substring(0, 300) + "...";
                        AppLogger.Warn($"Kroki trả về {(int)response.StatusCode} {response.StatusCode}: {err}");
                    }
                }
                catch
                {
                    // Bỏ qua: chỉ là log chẩn đoán.
                }
            }

            return null;
        }

        private static string? RenderLocalPdflatex(string fullLatex, string outputPngPath, string? customBinPath)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "TH_ExamGen_TikZ", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                string texFile = Path.Combine(tempDir, "figure.tex");
                string pdfFile = Path.Combine(tempDir, "figure.pdf");

                File.WriteAllText(texFile, fullLatex, Encoding.UTF8);

                string pdflatexExe = GetExecutablePath("pdflatex", customBinPath);
                var psi = new ProcessStartInfo
                {
                    FileName = pdflatexExe,
                    Arguments = "--disable-installer -interaction=nonstopmode figure.tex",
                    WorkingDirectory = tempDir,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true
                };

                using (var process = Process.Start(psi))
                {
                    if (process == null) return null;

                    // Đọc bất đồng bộ cả hai luồng để tránh deadlock khi pdflatex in nhiều cảnh báo (ống pipe đầy),
                    // đồng thời đóng stdin để không bao giờ bị chặn chờ nhập liệu.
                    try { process.StandardInput.Close(); } catch { }
                    var stdoutTask = process.StandardOutput.ReadToEndAsync();
                    var stderrTask = process.StandardError.ReadToEndAsync();

                    if (!process.WaitForExit(15000))
                    {
                        try { process.Kill(true); } catch { }
                        return null;
                    }

                    Task.WaitAll(new[] { stdoutTask, stderrTask }, 2000);
                }

                if (!File.Exists(pdfFile))
                {
                    return null;
                }

                string pdftoppmExe = GetExecutablePath("pdftoppm", customBinPath);
                string pngPrefix = Path.Combine(tempDir, "img");
                var ppmPsi = new ProcessStartInfo
                {
                    FileName = pdftoppmExe,
                    Arguments = $"-png -r 300 figure.pdf \"{pngPrefix}\"",
                    WorkingDirectory = tempDir,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var ppmProc = Process.Start(ppmPsi))
                {
                    if (ppmProc != null)
                    {
                        ppmProc.WaitForExit(10000);
                    }
                }

                string[] files = Directory.GetFiles(tempDir, "img*.png");
                if (files.Length > 0)
                {
                    string targetDir = Path.GetDirectoryName(outputPngPath) ?? string.Empty;
                    if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }

                    File.Copy(files[0], outputPngPath, true);
                    return outputPngPath;
                }

                return null;
            }
            catch (Exception ex)
            {
                AppLogger.Warn($"Lỗi biên dịch TikZ cục bộ (pdflatex): {ex.Message}");
                return null;
            }
            finally
            {
                try
                {
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Warn($"Không xóa được thư mục tạm TikZ: {ex.Message}");
                }
            }
        }

        private static string GetExecutablePath(string exeName, string? customBinPath)
        {
            string resolved = ResolveMiktexBinPath(customBinPath);
            if (!string.IsNullOrWhiteSpace(resolved) && Directory.Exists(resolved))
            {
                string target = Path.Combine(resolved, exeName + ".exe");
                if (File.Exists(target)) return target;
            }

            return exeName;
        }

        private static bool CanRunCommand(string command, string args)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = args,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using var p = Process.Start(psi);
                p?.WaitForExit(3000);
                return p?.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
