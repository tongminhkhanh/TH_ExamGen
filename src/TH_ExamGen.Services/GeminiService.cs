using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TH_ExamGen.Core.Config;
using TH_ExamGen.Core.Curriculum;
using TH_ExamGen.Core.MathModels;
using TH_ExamGen.Core.VietModels;

namespace TH_ExamGen.Services
{
    public class GeminiService
    {
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };

        public static bool IsDeepSeek(string? provider, string? model)
        {
            if (!string.IsNullOrEmpty(provider) && provider.Equals("DeepSeek", StringComparison.OrdinalIgnoreCase))
                return true;
            if (!string.IsNullOrEmpty(model) && model.IndexOf("deepseek", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            return false;
        }

        // ==================== CƠ CHẾ THỬ LẠI (RETRY) + THÔNG BÁO LỖI THÂN THIỆN ====================

        // Chờ tăng dần giữa các lần thử lại: 1.5s -> 4s -> 8s
        private static readonly int[] RetryDelaysMs = { 1500, 4000, 8000 };

        private static bool IsTransientStatus(System.Net.HttpStatusCode code)
        {
            int c = (int)code;
            return c == 429 || c == 500 || c == 502 || c == 503 || c == 504;
        }

        private static int? GetRetryAfterMs(HttpResponseMessage response)
        {
            var retryAfter = response.Headers.RetryAfter;
            if (retryAfter == null) return null;

            if (retryAfter.Delta.HasValue)
            {
                return (int)Math.Min(retryAfter.Delta.Value.TotalMilliseconds, 30000);
            }
            if (retryAfter.Date.HasValue)
            {
                double ms = (retryAfter.Date.Value - DateTimeOffset.UtcNow).TotalMilliseconds;
                if (ms > 0) return (int)Math.Min(ms, 30000);
            }
            return null;
        }

        /// <summary>
        /// Gửi request qua HttpClient kèm cơ chế thử lại cho các lỗi tạm thời (429/5xx, lỗi mạng).
        /// Mỗi lần thử dùng một HttpRequestMessage mới (do request không tái sử dụng được).
        /// </summary>
        private async Task<HttpResponseMessage> SendWithRetryAsync(
            string provider,
            Func<HttpRequestMessage> requestFactory,
            CancellationToken ct)
        {
            Exception? lastNetworkError = null;

            for (int attempt = 0; attempt <= RetryDelaysMs.Length; attempt++)
            {
                HttpRequestMessage request = requestFactory();
                try
                {
                    HttpResponseMessage response = await _httpClient.SendAsync(
                        request, HttpCompletionOption.ResponseContentRead, ct);

                    if (IsTransientStatus(response.StatusCode) && attempt < RetryDelaysMs.Length)
                    {
                        int delay = GetRetryAfterMs(response) ?? RetryDelaysMs[attempt];
                        AppLogger.Warn($"{provider} tạm thời lỗi {(int)response.StatusCode}; thử lại sau {delay}ms (lần {attempt + 1}).");
                        response.Dispose();
                        await Task.Delay(delay, ct);
                        continue;
                    }

                    return response;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    // Người dùng chủ động hủy -> không thử lại
                    throw;
                }
                catch (Exception ex) when ((ex is HttpRequestException || ex is TaskCanceledException) && attempt < RetryDelaysMs.Length)
                {
                    lastNetworkError = ex;
                    AppLogger.Warn($"Lỗi mạng khi gọi {provider}: {ex.Message}; thử lại sau {RetryDelaysMs[attempt]}ms.");
                    await Task.Delay(RetryDelaysMs[attempt], ct);
                }
                catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
                {
                    lastNetworkError = ex;
                }
                finally
                {
                    request.Dispose();
                }
            }

            AppLogger.Error($"Không kết nối được {provider} sau {RetryDelaysMs.Length + 1} lần thử.", lastNetworkError ?? new Exception("unknown"));
            throw new AiServiceException(
                $"{provider}: Không kết nối được tới máy chủ AI. Vui lòng kiểm tra kết nối Internet rồi thử lại.",
                lastNetworkError?.ToString());
        }

        /// <summary>
        /// Chuyển mã lỗi HTTP của Gemini/DeepSeek thành thông báo tiếng Việt dễ hiểu cho giáo viên.
        /// </summary>
        private static AiServiceException BuildFriendlyAiError(string provider, int statusCode, string responseBody)
        {
            string friendly;
            switch (statusCode)
            {
                case 400:
                case 401:
                case 403:
                    friendly = $"{provider}: API Key sai hoặc không có quyền truy cập. Vui lòng vào mục 'Cài đặt' kiểm tra lại API Key.";
                    break;
                case 404:
                    friendly = $"{provider}: Mô hình (model) không tồn tại hoặc đã ngừng hỗ trợ. Vào 'Cài đặt' → bấm 'Làm mới danh sách' rồi chọn lại model.";
                    break;
                case 429:
                    friendly = $"{provider}: Đã tạm hết hạn mức (quota) sử dụng. Vui lòng chờ một lát rồi thử lại, hoặc đổi sang khóa/model khác.";
                    break;
                case 500:
                case 502:
                case 503:
                case 504:
                    friendly = $"{provider}: Máy chủ AI đang bận hoặc quá tải. Hệ thống đã tự thử lại nhưng chưa thành công, vui lòng thử lại sau ít phút.";
                    break;
                default:
                    friendly = $"{provider}: Dịch vụ AI trả về lỗi (mã {statusCode}). Vui lòng thử lại.";
                    break;
            }

            string detail = $"HTTP {statusCode}: {Truncate(responseBody, 1000)}";
            AppLogger.Error($"Lỗi {provider} - {detail}");
            return new AiServiceException(friendly, detail);
        }

        private static string Truncate(string? value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
        }

        /// <summary>
        /// Lấy danh sách model Gemini hiện có từ API (những model hỗ trợ generateContent).
        /// Trả về danh sách rỗng nếu không lấy được (offline / key sai) để giao diện dùng danh sách tĩnh dự phòng.
        /// </summary>
        public async Task<List<string>> GetGeminiModelsAsync(string apiKey, CancellationToken ct = default)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(apiKey)) return result;

            const string url = "https://generativelanguage.googleapis.com/v1beta/models?pageSize=100";
            try
            {
                using HttpResponseMessage response = await SendWithRetryAsync("Gemini", () =>
                {
                    var req = new HttpRequestMessage(HttpMethod.Get, url);
                    req.Headers.Add("x-goog-api-key", apiKey.Trim());
                    return req;
                }, ct);

                string body = await response.Content.ReadAsStringAsync(ct);
                if (!response.IsSuccessStatusCode) return result;

                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("models", out var models))
                {
                    foreach (var m in models.EnumerateArray())
                    {
                        if (!m.TryGetProperty("name", out var nameProp)) continue;

                        string name = nameProp.GetString() ?? string.Empty;
                        if (name.StartsWith("models/")) name = name.Substring("models/".Length);
                        if (!name.StartsWith("gemini", StringComparison.OrdinalIgnoreCase)) continue;

                        bool supportsGenerate = true;
                        if (m.TryGetProperty("supportedGenerationMethods", out var methods))
                        {
                            supportsGenerate = methods.EnumerateArray()
                                .Any(x => string.Equals(x.GetString(), "generateContent", StringComparison.OrdinalIgnoreCase));
                        }

                        if (supportsGenerate) result.Add(name);
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Warn($"Không lấy được danh sách model Gemini: {ex.Message}");
            }

            result.Sort(StringComparer.OrdinalIgnoreCase);
            return result;
        }


        public async Task<string> GenerateContentRawAsync(string apiKey, string model, string promptText, CancellationToken ct = default)
        {
            string provider = IsDeepSeek(null, model) ? "DeepSeek" : "Gemini";
            return await GenerateContentRawAsync(provider, apiKey, model, promptText, ct);
        }

        public async Task<string> GenerateContentRawAsync(string provider, string apiKey, string model, string promptText, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                string providerName = IsDeepSeek(provider, model) ? "DeepSeek" : "Gemini";
                throw new ArgumentException($"{providerName} API Key chưa được thiết lập. Vui lòng vào mục Cài đặt để nhập API Key.", nameof(apiKey));
            }

            if (IsDeepSeek(provider, model))
            {
                return await GenerateDeepSeekRawAsync(apiKey, model, promptText, ct);
            }
            else
            {
                return await GenerateGeminiRawAsync(apiKey, model, promptText, ct);
            }
        }

        private static string NormalizeGeminiModel(string? model)
        {
            if (string.IsNullOrWhiteSpace(model)) return "gemini-2.5-flash";
            var m = model.Trim();
            if (m == "gemini-1.5-flash" || m == "gemini-1.5-pro" || m == "gemini-2.0-flash") return "gemini-2.5-flash";
            return m;
        }

        private async Task<string> GenerateGeminiRawAsync(string apiKey, string model, string promptText, CancellationToken ct = default)
        {
            model = NormalizeGeminiModel(model);

            string url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";

            var requestObj = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = promptText }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.5,
                    responseMimeType = "application/json"
                }
            };

            string jsonPayload = JsonSerializer.Serialize(requestObj);

            using HttpResponseMessage response = await SendWithRetryAsync("Gemini", () =>
            {
                var req = new HttpRequestMessage(HttpMethod.Post, url);
                req.Headers.Add("x-goog-api-key", apiKey.Trim());
                req.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                return req;
            }, ct);

            string responseBody = await response.Content.ReadAsStringAsync(ct);

            // Tự động chuyển dự phòng sang gemini-2.5-flash nếu model hiện tại trả về 404 Not Found hoặc 503/502 Service Unavailable
            if ((response.StatusCode == System.Net.HttpStatusCode.NotFound ||
                 response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
                 response.StatusCode == System.Net.HttpStatusCode.BadGateway) && model != "gemini-2.5-flash")
            {
                AppLogger.Warn($"Model {model} gặp sự cố (HTTP {(int)response.StatusCode}), tự động chuyển sang gemini-2.5-flash...");
                return await GenerateGeminiRawAsync(apiKey, "gemini-2.5-flash", promptText, ct);
            }

            if (!response.IsSuccessStatusCode)
            {
                throw BuildFriendlyAiError("Gemini", (int)response.StatusCode, responseBody);
            }

            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;
            if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                var firstCandidate = candidates[0];
                if (firstCandidate.TryGetProperty("content", out var cObj) &&
                    cObj.TryGetProperty("parts", out var parts) &&
                    parts.GetArrayLength() > 0)
                {
                    string text = parts[0].GetProperty("text").GetString() ?? string.Empty;
                    return CleanJsonOutput(text);
                }
            }

            throw new Exception("Không nhận được nội dung hợp lệ từ Gemini API.");
        }

        private async Task<string> GenerateDeepSeekRawAsync(string apiKey, string model, string promptText, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(model))
            {
                model = "deepseek-chat";
            }

            string url = "https://api.deepseek.com/chat/completions";

            var requestObj = new
            {
                model = model,
                messages = new object[]
                {
                    new
                    {
                        role = "system",
                        content = "Bạn là chuyên gia sư phạm tiểu học hàng đầu theo Chương trình GDPT 2018. Hãy trả về kết quả dưới định dạng JSON thuần túy, hợp lệ, không bọc markdown fences (không dùng ```json)."
                    },
                    new
                    {
                        role = "user",
                        content = promptText
                    }
                },
                response_format = new { type = "json_object" },
                temperature = 0.5
            };

            string jsonPayload = JsonSerializer.Serialize(requestObj);

            using HttpResponseMessage response = await SendWithRetryAsync("DeepSeek", () =>
            {
                var req = new HttpRequestMessage(HttpMethod.Post, url);
                req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey.Trim());
                req.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                return req;
            }, ct);

            string responseBody = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                throw BuildFriendlyAiError("DeepSeek", (int)response.StatusCode, responseBody);
            }

            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;
            if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
            {
                var firstChoice = choices[0];
                if (firstChoice.TryGetProperty("message", out var msgObj) &&
                    msgObj.TryGetProperty("content", out var contentProp))
                {
                    string text = contentProp.GetString() ?? string.Empty;
                    return CleanJsonOutput(text);
                }
            }

            throw new Exception("Không nhận được nội dung JSON hợp lệ từ DeepSeek API.");
        }

        public async Task<string> GenerateContentTextAsync(string apiKey, string model, string promptText, CancellationToken ct = default)
        {
            string provider = IsDeepSeek(null, model) ? "DeepSeek" : "Gemini";
            return await GenerateContentTextAsync(provider, apiKey, model, promptText, ct);
        }

        public async Task<string> GenerateContentTextAsync(string provider, string apiKey, string model, string promptText, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                string providerName = IsDeepSeek(provider, model) ? "DeepSeek" : "Gemini";
                throw new ArgumentException($"{providerName} API Key chưa được thiết lập. Vui lòng vào mục Cài đặt để nhập API Key.", nameof(apiKey));
            }

            if (IsDeepSeek(provider, model))
            {
                return await GenerateDeepSeekTextAsync(apiKey, model, promptText, ct);
            }
            else
            {
                return await GenerateGeminiTextAsync(apiKey, model, promptText, ct);
            }
        }

        private async Task<string> GenerateGeminiTextAsync(string apiKey, string model, string promptText, CancellationToken ct = default)
        {
            model = NormalizeGeminiModel(model);

            string url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";

            var requestObj = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = promptText }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.4
                }
            };

            string jsonPayload = JsonSerializer.Serialize(requestObj);

            using HttpResponseMessage response = await SendWithRetryAsync("Gemini", () =>
            {
                var req = new HttpRequestMessage(HttpMethod.Post, url);
                req.Headers.Add("x-goog-api-key", apiKey.Trim());
                req.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                return req;
            }, ct);

            string responseBody = await response.Content.ReadAsStringAsync(ct);

            // Tự động chuyển dự phòng sang gemini-2.5-flash nếu model hiện tại trả về 404 Not Found hoặc 503/502 Service Unavailable
            if ((response.StatusCode == System.Net.HttpStatusCode.NotFound ||
                 response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
                 response.StatusCode == System.Net.HttpStatusCode.BadGateway) && model != "gemini-2.5-flash")
            {
                AppLogger.Warn($"Model {model} gặp sự cố (HTTP {(int)response.StatusCode}), tự động chuyển sang gemini-2.5-flash...");
                return await GenerateGeminiTextAsync(apiKey, "gemini-2.5-flash", promptText, ct);
            }

            if (!response.IsSuccessStatusCode)
            {
                throw BuildFriendlyAiError("Gemini", (int)response.StatusCode, responseBody);
            }

            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;
            if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                var firstCandidate = candidates[0];
                if (firstCandidate.TryGetProperty("content", out var cObj) &&
                    cObj.TryGetProperty("parts", out var parts) &&
                    parts.GetArrayLength() > 0)
                {
                    return parts[0].GetProperty("text").GetString() ?? string.Empty;
                }
            }

            throw new Exception("Không nhận được nội dung phản hồi từ Gemini API.");
        }

        private async Task<string> GenerateDeepSeekTextAsync(string apiKey, string model, string promptText, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(model))
            {
                model = "deepseek-chat";
            }

            string url = "https://api.deepseek.com/chat/completions";

            var requestObj = new
            {
                model = model,
                messages = new object[]
                {
                    new
                    {
                        role = "system",
                        content = "Bạn là chuyên gia thẩm định đề kiểm tra tiểu học hàng đầu theo Chương trình GDPT 2018."
                    },
                    new
                    {
                        role = "user",
                        content = promptText
                    }
                },
                temperature = 0.4
            };

            string jsonPayload = JsonSerializer.Serialize(requestObj);

            using HttpResponseMessage response = await SendWithRetryAsync("DeepSeek", () =>
            {
                var req = new HttpRequestMessage(HttpMethod.Post, url);
                req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey.Trim());
                req.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                return req;
            }, ct);

            string responseBody = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                throw BuildFriendlyAiError("DeepSeek", (int)response.StatusCode, responseBody);
            }

            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;
            if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
            {
                var firstChoice = choices[0];
                if (firstChoice.TryGetProperty("message", out var msgObj) &&
                    msgObj.TryGetProperty("content", out var contentProp))
                {
                    return contentProp.GetString() ?? string.Empty;
                }
            }

            throw new Exception("Không nhận được nội dung phản hồi từ DeepSeek API.");
        }

        public async Task<string> TestConnectionAsync(string provider, string apiKey, string model, CancellationToken ct = default)
        {
            if (IsDeepSeek(provider, model))
            {
                return await GenerateDeepSeekTextAsync(apiKey, model, "Chào bạn, hãy trả lời ngắn gọn đúng 1 câu: 'Kết nối DeepSeek API thành công!'", ct);
            }
            else
            {
                return await GenerateGeminiTextAsync(apiKey, model, "Chào bạn, hãy trả lời ngắn gọn đúng 1 câu: 'Kết nối Gemini API thành công!'", ct);
            }
        }

        // ==================== CÁC PHƯƠNG THỨC TẠO ĐỀ & MA TRẬN ====================

        public async Task<MathExamPaper> GenerateMathExamAsync(AppSettings settings, int khoiLop, int thoiGianPhut, List<MathMatrixItem> matrix, string period = "Cuối Học kỳ II", CancellationToken ct = default)
        {
            return await GenerateMathExamAsync(settings.AiProvider, settings.ActiveApiKey, settings.ActiveModel, khoiLop, thoiGianPhut, matrix, period, ct);
        }

        public async Task<MathExamPaper> GenerateMathExamAsync(string apiKey, string model, int khoiLop, int thoiGianPhut, List<MathMatrixItem> matrix, string period = "Cuối Học kỳ II", CancellationToken ct = default)
        {
            string provider = IsDeepSeek(null, model) ? "DeepSeek" : "Gemini";
            return await GenerateMathExamAsync(provider, apiKey, model, khoiLop, thoiGianPhut, matrix, period, ct);
        }

        public async Task<MathExamPaper> GenerateMathExamAsync(string provider, string apiKey, string model, int khoiLop, int thoiGianPhut, List<MathMatrixItem> matrix, string period = "Cuối Học kỳ II", CancellationToken ct = default)
        {
            CurriculumScopePolicy.ValidateMathMatrix(khoiLop, period, matrix);
            string prompt = PromptBuilderService.BuildMathPrompt(khoiLop, thoiGianPhut, matrix, period);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            Exception? lastValidationError = null;
            MathExamPaper? lastParsedPaper = null;
            string numberDomainReminder = khoiLop == 4
                ? " Với Khối 4: mọi số liệu, phép tính và kết quả chỉ được là SỐ TỰ NHIÊN — tuyệt đối không dùng số thập phân."
                : string.Empty;
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                ct.ThrowIfCancellationRequested();
                string retryPrompt = attempt == 1
                    ? prompt
                    : prompt + $"\n\nLẦN TẠO LẠI {attempt}/3: Phản hồi trước bị từ chối vì: {lastValidationError?.Message}. Hãy sửa đúng lỗi này, giữ nguyên mã mục và dòng ma trận, rồi trả lại JSON hợp lệ. Nhắc lại: trường loaiCauHoi chỉ được nhận đúng một trong các giá trị: TN_4LuaChon, TN_DungSai, TN_TraLoiNgan, TL_TinhToan, TL_LoiVan (câu tự luận dùng TL_TinhToan hoặc TL_LoiVan, tuyệt đối không dùng 'TuLuan'/'Tuluan'/'TL')." + numberDomainReminder;
                string jsonResult = await GenerateContentRawAsync(provider, apiKey, model, retryPrompt, ct);
                try
                {
                    var paper = JsonSerializer.Deserialize<MathExamPaper>(jsonResult, options);
                    if (paper == null)
                        throw new InvalidOperationException("Không thể chuyển đổi dữ liệu đề Toán từ AI sang cấu trúc dữ liệu.");

                    paper.DanhSachCauHoi ??= new List<MathQuestion>();
                    if (paper.DanhSachCauHoi.Count > 0)
                    {
                        lastParsedPaper = paper;
                    }

                    paper.DanhSachMaTran = matrix;
                    paper.Period = period;
                    NormalizeMathQuestionTypes(paper, matrix);

                    // Đồng bộ metadata câu hỏi theo đúng dòng ma trận đã xác nhận của giáo viên
                    var matrixByOrder = matrix.ToDictionary(m => m.SoThuTu);
                    foreach (var q in paper.DanhSachCauHoi)
                    {
                        if (matrixByOrder.TryGetValue(q.SoThuTu, out var exp))
                        {
                            q.MaMucChuongTrinh = exp.MaMucChuongTrinh;
                            q.ChuDe = exp.ChuDe;
                            q.YeuCauCanDat = exp.YeuCauCanDat;
                            q.MucDo = exp.MucDo;
                            q.Diem = exp.Diem;
                        }
                    }

                    CurriculumScopePolicy.ValidateMathQuestions(khoiLop, period, paper.DanhSachCauHoi, matrix);
                    CurriculumScopePolicy.ValidateMathQuestionScope(khoiLop, period, paper.DanhSachCauHoi);
                    return paper;
                }
                catch (Exception ex) when (ex is JsonException || ex is InvalidOperationException || ex is FormatException)
                {
                    lastValidationError = ex;
                    AppLogger.Warn($"Lần {attempt}/3 đề Toán chưa đạt kiểm tra: {ex.Message}");
                }
            }

            // Không chặn cứng làm mất cả đề đã sinh: nếu đã có đề đọc được thì trả về kèm cảnh báo để giáo viên rà lại.
            if (lastParsedPaper != null)
            {
                lastParsedPaper.CanhBaoKiemTra.Add($"Đề Toán chưa khớp hoàn toàn danh mục/ma trận sau 3 lần sinh. Lỗi cuối: {lastValidationError?.Message}");
                AppLogger.Warn($"Đề Toán sinh kèm cảnh báo, vẫn trả về cho giáo viên rà soát: {lastValidationError?.Message}");
                return lastParsedPaper;
            }

            throw new InvalidOperationException($"Đã thử tạo đề Toán 3 lần nhưng vẫn vi phạm danh mục hoặc ma trận; đã dừng, không xuất đề. Lỗi cuối: {lastValidationError?.Message}", lastValidationError);
        }

        public async Task<VietExamPaper> GenerateVietExamAsync(AppSettings settings, int khoiLop, int thoiGianPhut, List<VietMatrixItem> matrix, ReadingPassage? passage, string chuDe, string period = "Cuối Học kỳ II", CancellationToken ct = default)
        {
            return await GenerateVietExamAsync(settings.AiProvider, settings.ActiveApiKey, settings.ActiveModel, khoiLop, thoiGianPhut, matrix, passage, chuDe, period, ct);
        }

        public async Task<VietExamPaper> GenerateVietExamAsync(string apiKey, string model, int khoiLop, int thoiGianPhut, List<VietMatrixItem> matrix, ReadingPassage? passage, string chuDe, string period = "Cuối Học kỳ II", CancellationToken ct = default)
        {
            string provider = IsDeepSeek(null, model) ? "DeepSeek" : "Gemini";
            return await GenerateVietExamAsync(provider, apiKey, model, khoiLop, thoiGianPhut, matrix, passage, chuDe, period, ct);
        }

        public async Task<VietExamPaper> GenerateVietExamAsync(string provider, string apiKey, string model, int khoiLop, int thoiGianPhut, List<VietMatrixItem> matrix, ReadingPassage? passage, string chuDe, string period = "Cuối Học kỳ II", CancellationToken ct = default)
        {
            CurriculumScopePolicy.ValidateVietMatrix(khoiLop, period, matrix);
            if (CurriculumScopePolicy.GetConfirmedVietReadingTopics(khoiLop, period).Count == 0)
                throw new InvalidOperationException($"Tiếng Việt lớp {khoiLop} kỳ {period} chưa có chủ điểm đọc hiểu nào được xác nhận; không thể tạo bài đọc ngoài danh mục.");
            if (!CurriculumScopePolicy.IsVietReadingThemeAllowed(khoiLop, period, chuDe))
                throw new InvalidOperationException($"Chủ điểm bài đọc '{chuDe}' chưa được xác nhận cho Tiếng Việt lớp {khoiLop} kỳ {period}. Chọn chủ điểm KNTT đã xác nhận; không tự thay bằng chủ đề khác.");
            if (passage != null)
            {
                if (!CurriculumScopePolicy.IsVietReadingThemeAllowed(khoiLop, period, passage.ChuDe))
                    throw new InvalidOperationException($"Bài đọc '{passage.TieuDe}' chưa gắn với chủ điểm đã xác nhận của kỳ {period}; đã dừng trước khi tạo đề.");
                if ((passage.NguonGoc ?? string.Empty).Contains("KNTT", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Không được dùng nguyên văn bài đọc từ SGK Kết nối tri thức. Hãy thay bằng bài tự viết hoặc nguồn ngoài SGK và giữ đúng chủ điểm của kỳ.");
                passage.MaMucChuongTrinh = CurriculumScopePolicy.ResolveVietReadingThemeId(khoiLop, period, passage.ChuDe);
            }

            string prompt = PromptBuilderService.BuildVietPrompt(khoiLop, thoiGianPhut, matrix, passage, chuDe, period);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            Exception? lastValidationError = null;
            VietExamPaper? lastParsedPaper = null;
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                ct.ThrowIfCancellationRequested();
                string retryPrompt = attempt == 1
                    ? prompt
                    : prompt + $"\n\nLẦN TẠO LẠI {attempt}/3: Phản hồi trước bị từ chối vì: {lastValidationError?.Message}. Hãy sửa theo đúng các mã mục đã xác nhận, không tự thêm nội dung hoặc đổi chủ điểm, rồi trả lại JSON hợp lệ. Nhắc lại: trường loaiCauHoi chỉ được nhận đúng \"TN_4LuaChon\" hoặc \"TL\" (tuyệt đối không dùng 'TuLuan'/'Tuluan').";
                string jsonResult = await GenerateContentRawAsync(provider, apiKey, model, retryPrompt, ct);
                try
                {
                    var paper = JsonSerializer.Deserialize<VietExamPaper>(jsonResult, options);
                    if (paper == null)
                        throw new InvalidOperationException("Không thể chuyển đổi dữ liệu đề Tiếng Việt từ AI sang cấu trúc dữ liệu.");

                    paper.DanhSachCauHoi ??= new List<VietQuestion>();
                    if (paper.DanhSachCauHoi.Count > 0 && (passage != null || paper.BaiDoc != null))
                    {
                        lastParsedPaper = paper;
                    }

                    paper.DanhSachMaTran = matrix;
                    paper.Period = period;
                    NormalizeVietQuestionTypes(paper, matrix);
                    if (passage == null)
                    {
                        if (paper.BaiDoc == null ||
                            !CurriculumScopePolicy.IsVietReadingThemeAllowed(khoiLop, period, paper.BaiDoc.ChuDe) ||
                            !string.IsNullOrWhiteSpace(paper.BaiDoc.ChuDe) && !NormalizeTheme(chuDe).Contains(NormalizeTheme(paper.BaiDoc.ChuDe), StringComparison.Ordinal))
                            throw new InvalidOperationException($"Bài đọc do AI tạo không gắn với chủ điểm KNTT đã xác nhận của kỳ {period}.");
                        string expectedPassageId = CurriculumScopePolicy.ResolveVietReadingThemeId(khoiLop, period, paper.BaiDoc.ChuDe);
                        if (!string.IsNullOrWhiteSpace(paper.BaiDoc.MaMucChuongTrinh) && paper.BaiDoc.MaMucChuongTrinh != expectedPassageId)
                            throw new InvalidOperationException("Mã chủ điểm bài đọc do AI trả về không khớp chủ điểm đã chọn.");
                        paper.BaiDoc.MaMucChuongTrinh = expectedPassageId;
                        paper.BaiDoc.NguonGoc = "AI-sáng-tác";
                        paper.BaiDoc.NguonChiTiet = "AI tự sáng tác";
                    }
                    else
                    {
                        // Preserve the exact teacher-provided text and its source metadata.
                        paper.BaiDoc = passage;
                    }
                    CurriculumScopePolicy.ValidateVietPassage(khoiLop, period, paper.BaiDoc);
                    CurriculumScopePolicy.ValidateVietPassageMatchesMatrix(paper.BaiDoc, matrix, khoiLop, period);
                    CurriculumScopePolicy.ValidateVietQuestions(khoiLop, period, paper.DanhSachCauHoi, matrix);

                    ApplyVietDefaultOlyLines(paper);
                    return paper;
                }
                catch (Exception ex) when (ex is JsonException || ex is InvalidOperationException || ex is FormatException)
                {
                    lastValidationError = ex;
                    AppLogger.Warn($"Lần {attempt}/3 đề Tiếng Việt chưa đạt kiểm tra: {ex.Message}");
                }
            }

            // Không chặn cứng làm mất cả đề đã sinh: nếu đã có đề đọc được thì trả về kèm cảnh báo để giáo viên rà lại.
            if (lastParsedPaper != null)
            {
                ApplyVietDefaultOlyLines(lastParsedPaper);
                lastParsedPaper.CanhBaoKiemTra.Add($"Đề Tiếng Việt chưa khớp hoàn toàn danh mục/ma trận sau 3 lần sinh. Lỗi cuối: {lastValidationError?.Message}");
                AppLogger.Warn($"Đề Tiếng Việt sinh kèm cảnh báo, vẫn trả về cho giáo viên rà soát: {lastValidationError?.Message}");
                return lastParsedPaper;
            }

            throw new InvalidOperationException($"Đã thử tạo đề Tiếng Việt 3 lần nhưng vẫn vi phạm danh mục hoặc ma trận; đã dừng, không xuất đề. Lỗi cuối: {lastValidationError?.Message}", lastValidationError);
        }

        private static string NormalizeTheme(string value)
        {
            string decomposed = (value ?? string.Empty).Normalize(System.Text.NormalizationForm.FormD);
            var chars = decomposed.Where(ch => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) != System.Globalization.UnicodeCategory.NonSpacingMark).ToArray();
            return new string(chars).Normalize(System.Text.NormalizationForm.FormC).ToLowerInvariant();
        }

        /// <summary>
        /// Chuẩn hóa hình thức câu hỏi Toán từ phản hồi AI (ví dụ 'Tuluan' -> 'TL_TinhToan') để
        /// validator không từ chối oan. Nếu không nhận dạng được thì lấy theo hình thức của dòng ma trận.
        /// </summary>
        private static void NormalizeMathQuestionTypes(MathExamPaper paper, List<MathMatrixItem> matrix)
        {
            var formByOrder = matrix
                .GroupBy(m => m.SoThuTu)
                .ToDictionary(g => g.Key, g => g.First().HinhThuc);
            foreach (var q in paper.DanhSachCauHoi)
            {
                string? normalized = CurriculumScopePolicy.NormalizeMathQuestionType(q.LoaiCauHoi);
                if (normalized == null && formByOrder.TryGetValue(q.SoThuTu, out var form))
                {
                    normalized = form switch
                    {
                        "TN" => "TN_4LuaChon",
                        "TL" => "TL_TinhToan",
                        _ => null
                    };
                }
                if (!string.IsNullOrEmpty(normalized) && normalized != q.LoaiCauHoi)
                {
                    q.LoaiCauHoi = normalized;
                }
            }
        }

        /// <summary>Chuẩn hóa hình thức câu hỏi Tiếng Việt từ phản hồi AI về đúng "TN_4LuaChon" hoặc "TL".</summary>
        private static void NormalizeVietQuestionTypes(VietExamPaper paper, List<VietMatrixItem> matrix)
        {
            var formByOrder = matrix
                .GroupBy(m => m.SoThuTu)
                .ToDictionary(g => g.Key, g => g.First().HinhThuc);
            foreach (var q in paper.DanhSachCauHoi)
            {
                string? normalized = CurriculumScopePolicy.NormalizeVietQuestionType(q.LoaiCauHoi);
                if (normalized == null && formByOrder.TryGetValue(q.SoThuTu, out var form))
                {
                    normalized = form switch
                    {
                        "TN" => "TN_4LuaChon",
                        "TL" => "TL",
                        _ => null
                    };
                }
                if (!string.IsNullOrEmpty(normalized) && normalized != q.LoaiCauHoi)
                {
                    q.LoaiCauHoi = normalized;
                }
            }
        }

        /// <summary>Gán mặc định số dòng ô ly cho các câu tự luận Tiếng Việt khi AI chưa gán (dùng cả khi trả đề kèm cảnh báo).</summary>
        private static void ApplyVietDefaultOlyLines(VietExamPaper paper)
        {
            foreach (var q in paper.DanhSachCauHoi)
            {
                if (q.LoaiCauHoi != "TN_4LuaChon")
                {
                    if (q.SoDongOly <= 0) q.SoDongOly = q.SoThuTu == 7 || q.SoThuTu == 11 || (q.SoThuTu == 6 && q.MucDo == "M3") ? 3 : 2;
                }
                else
                {
                    q.SoDongOly = 0;
                }
            }
        }

        /// <summary>
        /// Chuẩn hóa ma trận Tiếng Việt do AI đề xuất theo đúng khung chuẩn 11 câu / 8 điểm
        /// (6 TN / 5 TL; M1: 6, M2: 3, M3: 2) quy định trong Ma trận đặc tả môn Tiếng Việt 4, 5.
        /// </summary>
        private static void NormalizeVietSuggestedMatrix(int khoiLop, string period, List<VietMatrixItem> items, IEnumerable<string>? selectedTopics)
        {
            if (items == null || items.Count == 0) return;

            var allowed = CurriculumScopePolicy.GetConfirmedVietTopics(khoiLop, period).ToDictionary(x => x.MaMuc, StringComparer.Ordinal);
            if (allowed.Count == 0) return;

            var confirmedReadingTopics = allowed.Values.Where(t => t.MachKienThuc.Contains("Đọc hiểu", StringComparison.OrdinalIgnoreCase)).ToList();
            var confirmedKnowledgeTopics = allowed.Values.Where(t => !t.MachKienThuc.Contains("Đọc hiểu", StringComparison.OrdinalIgnoreCase)).ToList();

            // Nếu danh sách thừa hoặc thiếu 11 câu
            if (items.Count > 11)
            {
                items.RemoveRange(11, items.Count - 11);
            }
            else if (items.Count < 11)
            {
                var defaultMatrix = VietCurriculumBank.GetMatrixByGradeAndPeriod(khoiLop, period);
                for (int i = items.Count; i < 11 && i < defaultMatrix.Count; i++)
                {
                    items.Add(defaultMatrix[i]);
                }
            }

            // Đồng bộ 1 mã chủ điểm bài đọc cho 7 câu Đọc hiểu
            string readingTopicId = items.Take(7)
                .Select(x => x.MaMucChuongTrinh)
                .FirstOrDefault(id => !string.IsNullOrWhiteSpace(id) && allowed.ContainsKey(id) && allowed[id].MachKienThuc.Contains("Đọc hiểu", StringComparison.OrdinalIgnoreCase))
                ?? confirmedReadingTopics.FirstOrDefault()?.MaMuc
                ?? allowed.Values.First().MaMuc;

            for (int i = 0; i < items.Count && i < 11; i++)
            {
                var item = items[i];
                item.SoThuTu = i + 1;
                item.IsAiNew = true;

                if (i < 7)
                {
                    item.MachKienThuc = "Đọc hiểu văn bản";
                    item.MaMucChuongTrinh = readingTopicId;

                    if (i < 4)
                    {
                        item.HinhThuc = "TN";
                        item.MucDo = "M1";
                        item.Diem = 0.5;
                        if (string.IsNullOrWhiteSpace(item.NoiDungKienThuc) || !item.NoiDungKienThuc.Contains("nội dung", StringComparison.OrdinalIgnoreCase))
                            item.NoiDungKienThuc = "Đọc hiểu nội dung";
                    }
                    else if (i is 4 or 5)
                    {
                        item.HinhThuc = "TL";
                        item.MucDo = "M2";
                        item.Diem = 1.0;
                        if (string.IsNullOrWhiteSpace(item.NoiDungKienThuc) || !item.NoiDungKienThuc.Contains("hình thức", StringComparison.OrdinalIgnoreCase))
                            item.NoiDungKienThuc = "Đọc hiểu hình thức";
                    }
                    else // i == 6
                    {
                        item.HinhThuc = "TL";
                        item.MucDo = "M3";
                        item.Diem = 1.0;
                        if (string.IsNullOrWhiteSpace(item.NoiDungKienThuc) || !item.NoiDungKienThuc.Contains("liên hệ", StringComparison.OrdinalIgnoreCase))
                            item.NoiDungKienThuc = "Liên hệ, so sánh, kết nối";
                    }
                }
                else
                {
                    item.MachKienThuc = "Kiến thức Tiếng Việt";

                    if (i is 7 or 8)
                    {
                        item.HinhThuc = "TN";
                        item.MucDo = "M1";
                        item.Diem = 0.5;
                    }
                    else if (i == 9)
                    {
                        item.HinhThuc = "TL";
                        item.MucDo = "M2";
                        item.Diem = 1.0;
                    }
                    else // i == 10
                    {
                        item.HinhThuc = "TL";
                        item.MucDo = "M3";
                        item.Diem = 1.0;
                    }

                    if (string.IsNullOrWhiteSpace(item.MaMucChuongTrinh) || !allowed.ContainsKey(item.MaMucChuongTrinh) || allowed[item.MaMucChuongTrinh].MachKienThuc.Contains("Đọc hiểu", StringComparison.OrdinalIgnoreCase))
                    {
                        var fallbackKnowledge = confirmedKnowledgeTopics.FirstOrDefault() ?? allowed.Values.First();
                        item.MaMucChuongTrinh = fallbackKnowledge.MaMuc;
                        if (string.IsNullOrWhiteSpace(item.NoiDungKienThuc)) item.NoiDungKienThuc = fallbackKnowledge.ChuDe;
                        if (string.IsNullOrWhiteSpace(item.YeuCauCanDat)) item.YeuCauCanDat = fallbackKnowledge.NoiDung;
                    }
                }
            }
        }

        /// <summary>
        /// Viết lại RIÊNG trường huongDanCham cho toàn bộ câu hỏi của đề hiện có (giữ nguyên đề,
        /// không sinh lại câu hỏi) theo chuẩn chi tiết "Đáp án gợi ý + Biểu điểm" / "Đáp án đúng + Lý do".
        /// Trả về số câu đã cập nhật được hướng dẫn chấm.
        /// </summary>
        public async Task<int> RewriteVietGradingGuideAsync(AppSettings settings, VietExamPaper paper, CancellationToken ct = default)
        {
            string prompt = PromptBuilderService.BuildVietGuideRewritePrompt(paper);
            string jsonResult = await GenerateContentRawAsync(settings.AiProvider, settings.ActiveApiKey, settings.ActiveModel, prompt, ct);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            VietGuideRewriteResponse? response = null;
            try
            {
                response = JsonSerializer.Deserialize<VietGuideRewriteResponse>(jsonResult, options);
            }
            catch (JsonException)
            {
                // Xử lý thông báo thống nhất ở dưới.
            }

            if (response?.DanhSach == null || response.DanhSach.Count == 0)
            {
                throw new Exception("Không thể chuyển đổi dữ liệu hướng dẫn chấm viết lại từ AI.");
            }

            int updated = 0;
            foreach (var item in response.DanhSach)
            {
                if (string.IsNullOrWhiteSpace(item.HuongDanCham)) continue;

                var question = paper.DanhSachCauHoi.FirstOrDefault(x => x.SoThuTu == item.SoThuTu);
                if (question == null) continue;

                question.HuongDanCham = item.HuongDanCham.Trim();
                updated++;
            }

            if (updated == 0)
            {
                throw new Exception("AI trả về hướng dẫn chấm nhưng không khớp số thứ tự câu nào của đề. Vui lòng thử lại.");
            }

            return updated;
        }

        public async Task<List<MathMatrixItem>> SuggestMathMatrixAsync(AppSettings settings, int khoiLop, string period, string userRequirement, bool autoSelectTopics = false, List<string>? selectedTopics = null)
        {
            return await SuggestMathMatrixAsync(settings.AiProvider, settings.ActiveApiKey, settings.ActiveModel, khoiLop, period, userRequirement, autoSelectTopics, selectedTopics);
        }

        public async Task<List<MathMatrixItem>> SuggestMathMatrixAsync(string apiKey, string model, int khoiLop, string period, string userRequirement, bool autoSelectTopics = false, List<string>? selectedTopics = null)
        {
            string provider = IsDeepSeek(null, model) ? "DeepSeek" : "Gemini";
            return await SuggestMathMatrixAsync(provider, apiKey, model, khoiLop, period, userRequirement, autoSelectTopics, selectedTopics);
        }

        public async Task<List<MathMatrixItem>> SuggestMathMatrixAsync(string provider, string apiKey, string model, int khoiLop, string period, string userRequirement, bool autoSelectTopics = false, List<string>? selectedTopics = null)
        {
            if (CurriculumScopePolicy.GetConfirmedMathTopics(khoiLop, period).Count == 0)
                throw new InvalidOperationException($"Danh mục Toán lớp {khoiLop} kỳ {period} chưa có mục nào được xác nhận. Hãy rà soát ít nhất một mục trước khi sinh ma trận.");
            CurriculumScopePolicy.ValidateMathUserRequirement(khoiLop, period, userRequirement);
            string prompt = PromptBuilderService.BuildMatrixSuggestionPrompt(khoiLop, period, userRequirement, autoSelectTopics, selectedTopics);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            Exception? lastValidationError = null;
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                string retryPrompt = attempt == 1
                    ? prompt
                    : prompt + $"\n\nLẦN TẠO LẠI {attempt}/3: Ma trận trước bị từ chối vì: {lastValidationError?.Message}. Hãy sửa đúng lỗi, giữ nguyên phạm vi mã mục đã xác nhận, rồi trả về JSON hợp lệ.";
                string jsonResult = await GenerateContentRawAsync(provider, apiKey, model, retryPrompt);
                try
                {
                    var items = JsonSerializer.Deserialize<List<MathMatrixItem>>(jsonResult, options);
                    if (items == null || items.Count == 0)
                        throw new InvalidOperationException("Không thể chuyển đổi danh sách ma trận gợi ý từ AI.");

                    for (int i = 0; i < items.Count; i++) items[i].SoThuTu = i + 1;
                    // Ba cột văn bản (mạch / tên mục / yêu cầu cần đạt) là dữ liệu nguyên văn của danh mục:
                    // AI chỉ chọn mã mục + mức độ, điền lại từ danh mục đã xác nhận theo (mã, mức) trước khi kiểm nghiêm ngặt.
                    CurriculumMatrixMapper.FillCatalogTexts(khoiLop, period, items);
                    CurriculumScopePolicy.ValidateMathMatrix(khoiLop, period, items, selectedTopics);
                    return items;
                }
                catch (Exception ex) when (ex is JsonException || ex is InvalidOperationException || ex is FormatException)
                {
                    lastValidationError = ex;
                }
            }
            throw new InvalidOperationException($"Đã thử lập ma trận Toán 3 lần nhưng vẫn vi phạm danh mục hoặc khung điểm; đã dừng. Lỗi cuối: {lastValidationError?.Message}", lastValidationError);
        }

        public async Task<string> AuditMathMatrixAsync(AppSettings settings, int khoiLop, string period, List<MathMatrixItem> matrix)
        {
            return await AuditMathMatrixAsync(settings.AiProvider, settings.ActiveApiKey, settings.ActiveModel, khoiLop, period, matrix);
        }

        public async Task<string> AuditMathMatrixAsync(string apiKey, string model, int khoiLop, string period, List<MathMatrixItem> matrix)
        {
            string provider = IsDeepSeek(null, model) ? "DeepSeek" : "Gemini";
            return await AuditMathMatrixAsync(provider, apiKey, model, khoiLop, period, matrix);
        }

        public async Task<string> AuditMathMatrixAsync(string provider, string apiKey, string model, int khoiLop, string period, List<MathMatrixItem> matrix)
        {
            string prompt = PromptBuilderService.BuildMatrixAuditPrompt(khoiLop, period, matrix);
            return await GenerateContentTextAsync(provider, apiKey, model, prompt);
        }

        public async Task<List<VietMatrixItem>> SuggestVietMatrixAsync(AppSettings settings, int khoiLop, string period, string userRequirement, bool autoSelectTopics = false, List<string>? selectedTopics = null)
        {
            return await SuggestVietMatrixAsync(settings.AiProvider, settings.ActiveApiKey, settings.ActiveModel, khoiLop, period, userRequirement, autoSelectTopics, selectedTopics);
        }

        public async Task<List<VietMatrixItem>> SuggestVietMatrixAsync(string apiKey, string model, int khoiLop, string period, string userRequirement, bool autoSelectTopics = false, List<string>? selectedTopics = null)
        {
            string provider = IsDeepSeek(null, model) ? "DeepSeek" : "Gemini";
            return await SuggestVietMatrixAsync(provider, apiKey, model, khoiLop, period, userRequirement, autoSelectTopics, selectedTopics);
        }

        public async Task<List<VietMatrixItem>> SuggestVietMatrixAsync(string provider, string apiKey, string model, int khoiLop, string period, string userRequirement, bool autoSelectTopics = false, List<string>? selectedTopics = null)
        {
            if (CurriculumScopePolicy.GetConfirmedVietTopics(khoiLop, period).Count == 0)
                throw new InvalidOperationException($"Danh mục Tiếng Việt lớp {khoiLop} kỳ {period} chưa có mục nào được xác nhận. Hãy rà soát ít nhất một mục trước khi sinh ma trận.");
            CurriculumScopePolicy.ValidateVietUserRequirement(khoiLop, period, userRequirement);
            string prompt = PromptBuilderService.BuildVietMatrixSuggestionPrompt(khoiLop, period, userRequirement, autoSelectTopics, selectedTopics);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            Exception? lastValidationError = null;
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                string retryPrompt = attempt == 1
                    ? prompt
                    : prompt + $"\n\nLẦN TẠO LẠI {attempt}/3: Ma trận trước bị từ chối vì: {lastValidationError?.Message}. Hãy sửa đúng lỗi, chỉ dùng mã mục và kỹ năng đã xác nhận, rồi trả về JSON hợp lệ.";
                string jsonResult = await GenerateContentRawAsync(provider, apiKey, model, retryPrompt);
                try
                {
                    var items = JsonSerializer.Deserialize<List<VietMatrixItem>>(jsonResult, options);
                    if (items == null || items.Count == 0)
                        throw new InvalidOperationException("Không thể chuyển đổi danh sách ma trận Tiếng Việt gợi ý từ AI.");

                    for (int i = 0; i < items.Count; i++) items[i].SoThuTu = i + 1;
                    NormalizeVietSuggestedMatrix(khoiLop, period, items, selectedTopics);
                    CurriculumScopePolicy.ValidateVietMatrix(khoiLop, period, items, selectedTopics);
                    return items;
                }
                catch (Exception ex) when (ex is JsonException || ex is InvalidOperationException || ex is FormatException)
                {
                    lastValidationError = ex;
                }
            }
            throw new InvalidOperationException($"Đã thử lập ma trận Tiếng Việt 3 lần nhưng vẫn vi phạm danh mục hoặc khung đề; đã dừng. Lỗi cuối: {lastValidationError?.Message}", lastValidationError);
        }

        public async Task<string> AuditVietMatrixAsync(AppSettings settings, int khoiLop, string period, List<VietMatrixItem> matrix)
        {
            return await AuditVietMatrixAsync(settings.AiProvider, settings.ActiveApiKey, settings.ActiveModel, khoiLop, period, matrix);
        }

        public async Task<string> AuditVietMatrixAsync(string apiKey, string model, int khoiLop, string period, List<VietMatrixItem> matrix)
        {
            string provider = IsDeepSeek(null, model) ? "DeepSeek" : "Gemini";
            return await AuditVietMatrixAsync(provider, apiKey, model, khoiLop, period, matrix);
        }

        public async Task<string> AuditVietMatrixAsync(string provider, string apiKey, string model, int khoiLop, string period, List<VietMatrixItem> matrix)
        {
            string prompt = PromptBuilderService.BuildVietMatrixAuditPrompt(khoiLop, period, matrix);
            return await GenerateContentTextAsync(provider, apiKey, model, prompt);
        }

        private static string CleanJsonOutput(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            string text = raw.Trim();
            if (text.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            {
                text = text.Substring(7);
            }
            else if (text.StartsWith("```"))
            {
                text = text.Substring(3);
            }

            if (text.EndsWith("```"))
            {
                text = text.Substring(0, text.Length - 3);
            }

            text = text.Trim();

            // Sửa lỗi LaTeX \frac trong chuỗi JSON trước khi Deserialize:
            // Trong chuẩn JSON, "\f" bị giải mã thành FormFeed (\u000c).
            // Cần chuyển "\frac" thành "\\frac" nếu chưa được escape hai dấu gạch chéo.
            text = System.Text.RegularExpressions.Regex.Replace(text, @"(?<!\\)\\frac\{", @"\\frac{");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"(?<![a-zA-Z\\])rac\{", @"\\frac{");

            return text;
        }
    }

    /// <summary>DTO hứng kết quả AI cho chức năng "Viết lại Hướng dẫn chấm" môn Tiếng Việt.</summary>
    internal sealed class VietGuideRewriteResponse
    {
        public List<VietGuideRewriteItem>? DanhSach { get; set; }
    }

    internal sealed class VietGuideRewriteItem
    {
        public int SoThuTu { get; set; }
        public string? HuongDanCham { get; set; }
    }
}
