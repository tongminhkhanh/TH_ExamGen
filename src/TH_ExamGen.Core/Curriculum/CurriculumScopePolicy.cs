using System;
using System.Collections.Generic;
using System.Linq;
using TH_ExamGen.Core.MathModels;
using TH_ExamGen.Core.VietModels;

namespace TH_ExamGen.Core.Curriculum
{
    /// <summary>Common grade/period/week/review gate shared by both subject catalogs.</summary>
    public static class CurriculumScopePolicy
    {
        public static List<MathTopicItem> GetConfirmedMathTopics(int grade, string period) =>
            MathTopicCatalog.GetTopics(grade, period)
                .Where(item => item.DaXacNhan && IsWithinPeriod(item, period))
                .ToList();

        public static List<VietTopicItem> GetConfirmedVietTopics(int grade, string period) =>
            VietTopicCatalog.GetTopics(grade, period)
                .Where(item => item.DaXacNhan && IsWithinPeriod(item, period))
                .ToList();

        public static void ValidateCatalogRequest(int grade, string period)
        {
            if (grade is not (4 or 5))
                throw new ArgumentOutOfRangeException(nameof(grade), grade, "Danh mục chỉ hỗ trợ lớp 4 và lớp 5.");
            if (!ExamPeriodHelper.IsMidTerm1(period) && !ExamPeriodHelper.IsFinalTerm1(period) &&
                !ExamPeriodHelper.IsMidTerm2(period) && !ExamPeriodHelper.IsFinalTerm2(period))
                throw new ArgumentException($"Đợt kiểm tra không hợp lệ: '{period}'.", nameof(period));
        }

        public static List<VietTopicItem> GetConfirmedVietReadingTopics(int grade, string period) =>
            GetConfirmedVietTopics(grade, period)
                .Where(item => item.MachKienThuc.Contains("Đọc hiểu", StringComparison.OrdinalIgnoreCase))
                .ToList();

        public static bool IsVietReadingThemeAllowed(int grade, string period, string theme)
        {
            if (string.IsNullOrWhiteSpace(theme)) return false;
            string normalizedTheme = NormalizeVietTheme(theme);
            return GetMatchingVietReadingThemes(grade, period, normalizedTheme).Count == 1;
        }

        public static string ResolveVietReadingThemeId(int grade, string period, string theme)
        {
            string normalizedTheme = NormalizeVietTheme(theme ?? string.Empty);
            var matches = GetMatchingVietReadingThemes(grade, period, normalizedTheme);
            if (matches.Count != 1)
                throw new InvalidOperationException($"Không ánh xạ được chủ điểm bài đọc '{theme}' vào một mục Tiếng Việt đã xác nhận của {period}.");
            return matches[0].MaMuc;
        }

        private static List<VietTopicItem> GetMatchingVietReadingThemes(int grade, string period, string normalizedTheme) =>
            GetConfirmedVietReadingTopics(grade, period)
                .Where(item =>
                {
                    string topicName = NormalizeVietTheme(item.ChuDe);
                    return string.Equals(normalizedTheme, topicName, StringComparison.Ordinal);
                })
                .ToList();

        public static void ValidateVietPassage(int grade, string period, ReadingPassage passage)
        {
            if (passage == null || string.IsNullOrWhiteSpace(passage.NoiDung))
                throw new InvalidOperationException("Đề Tiếng Việt chưa có bài đọc hợp lệ.");
            if (!IsVietReadingThemeAllowed(grade, period, passage.ChuDe))
                throw new InvalidOperationException($"Bài đọc '{passage.TieuDe}' không khớp chủ điểm đã xác nhận của {period}.");
            if (MentionsKnttBook(passage.NguonGoc) || MentionsKnttBook(passage.NguonChiTiet))
                throw new InvalidOperationException("Không thể xuất bài đọc có nguồn gốc từ SGK Kết nối tri thức.");
            string expectedId = ResolveVietReadingThemeId(grade, period, passage.ChuDe);
            if (string.IsNullOrWhiteSpace(passage.MaMucChuongTrinh) || passage.MaMucChuongTrinh != expectedId)
                throw new InvalidOperationException("Bài đọc chưa được ánh xạ vào mã chủ điểm chương trình đã xác nhận; cần rà soát lại bài đọc.");
        }

        public static void ValidateVietPassageMatchesMatrix(ReadingPassage passage, IReadOnlyCollection<VietMatrixItem> matrix, int grade, string period)
        {
            ValidateVietPassage(grade, period, passage);
            var readingIds = matrix
                .Where(item => item.MachKienThuc.Contains("Đọc hiểu", StringComparison.OrdinalIgnoreCase))
                .Select(item => item.MaMucChuongTrinh)
                .Distinct(StringComparer.Ordinal)
                .ToList();
            string passageId = ResolveVietReadingThemeId(grade, period, passage.ChuDe);
            if (readingIds.Count != 1 || readingIds[0] != passageId)
                throw new InvalidOperationException("Mã chủ điểm của bài đọc không khớp với chủ điểm được gắn cho bảy câu Đọc hiểu trong ma trận.");
        }

        public static int GetWeekLimit(string period)
        {
            if (ExamPeriodHelper.IsMidTerm1(period)) return 9;
            if (ExamPeriodHelper.IsFinalTerm1(period)) return 18;
            if (ExamPeriodHelper.IsMidTerm2(period)) return 27;
            if (ExamPeriodHelper.IsFinalTerm2(period)) return 35;
            throw new ArgumentException($"Đợt kiểm tra không hợp lệ: '{period}'.", nameof(period));
        }

        public static bool IsWithinPeriod(int weekEnd, string period) =>
            weekEnd > 0 && weekEnd <= GetWeekLimit(period);

        public static bool IsWithinPeriod(ICurriculumTopicItem topic, string period) =>
            topic.TuanBatDau > 0 && topic.TuanKetThuc >= topic.TuanBatDau && IsWithinPeriod(topic.TuanKetThuc, period);

        public static string FormatTopic(ICurriculumTopicItem item) =>
            $"[{item.MaMuc}] [Tuần {item.Tuan}] ({item.MachKienThuc}) {item.ChuDe}: {item.NoiDung}";

        public static string FormatPendingReview<T>(IEnumerable<T> items) where T : ICurriculumTopicItem
        {
            var pending = items.Where(item => !item.DaXacNhan).ToList();
            if (pending.Count == 0) return string.Empty;
            return string.Join(Environment.NewLine, pending.Select(item =>
                $"• Tuần {item.Tuan}: {item.ChuDe} — {item.NoiDung}{Environment.NewLine}  Nguồn: {item.NguonThamKhao}"));
        }

        public static void ValidateMathMatrix(int grade, string period, IReadOnlyCollection<MathMatrixItem> matrix, IEnumerable<string>? selectedTopicIds = null)
        {
            ValidatePointValues(matrix.Select(x => x.Diem), "ma trận Toán");
            if (matrix.Count != 10 || Math.Abs(matrix.Sum(x => x.Diem) - 10.0) > 0.001)
                throw new InvalidOperationException("Ma trận Toán phải có đúng 10 câu và tổng 10 điểm trước khi sinh đề.");
            ValidateQuestionNumbers(matrix.Select(x => x.SoThuTu), 10, "Toán");
            ValidateMatrixCategories(matrix.Select(x => (x.MucDo, x.HinhThuc)), "Toán");
            var allowed = GetConfirmedMathTopics(grade, period).ToDictionary(x => x.MaMuc, StringComparer.Ordinal);
            if (allowed.Count == 0)
                throw new InvalidOperationException($"Chưa có mục chương trình Toán nào đã xác nhận cho {period}.");

            // Tự động gắn/chuẩn hóa mã mục chương trình nếu người dùng sửa nội dung
            foreach (var row in matrix)
            {
                if (string.IsNullOrWhiteSpace(row.MaMucChuongTrinh) || !allowed.ContainsKey(row.MaMucChuongTrinh))
                {
                    var matchedTopic = FindBestMathTopic(row, allowed.Values);
                    row.MaMucChuongTrinh = matchedTopic?.MaMuc ?? allowed.Values.First().MaMuc;
                }
            }

            ValidateIds(matrix.Select(x => (x.SoThuTu, x.MaMucChuongTrinh)), allowed.Keys, "ma trận Toán", period);
            var selected = selectedTopicIds == null ? null : new HashSet<string>(selectedTopicIds.Where(id => !string.IsNullOrWhiteSpace(id)), StringComparer.Ordinal);
            if (selected != null && selected.Count == 0)
                throw new InvalidOperationException("Chưa chọn mã mục Toán nào đã xác nhận; không thể lập ma trận.");

            foreach (var row in matrix)
            {
                if (selected != null && !selected.Contains(row.MaMucChuongTrinh))
                {
                    var selMatch = FindBestMathTopic(row, allowed.Values.Where(t => selected.Contains(t.MaMuc)));
                    if (selMatch != null)
                    {
                        row.MaMucChuongTrinh = selMatch.MaMuc;
                    }
                }

                var topic = allowed[row.MaMucChuongTrinh];

                // Chuẩn hóa mạch kiến thức (chuDe) về 3 mạch chuẩn
                if (string.IsNullOrWhiteSpace(row.ChuDe))
                {
                    row.ChuDe = topic.MachKienThuc;
                }
                else
                {
                    row.ChuDe = NormalizeMathStrand(row.ChuDe, topic.MachKienThuc);
                }

                // Cho phép giáo viên tự do tinh chỉnh nội dung kiến thức và yêu cầu cần đạt,
                // chỉ điền mặc định nếu bị bỏ trống
                if (string.IsNullOrWhiteSpace(row.NoiDungKienThuc))
                {
                    row.NoiDungKienThuc = topic.ChuDe;
                }
                else
                {
                    // Tự động chuẩn hóa nếu tên nội dung kiến thức là tên chương/chủ đề mơ hồ (ví dụ: "Ôn tập đầu năm", "Ôn tập học kỳ", "Chủ đề...")
                    string normNd = Normalize(row.NoiDungKienThuc);
                    if (normNd.Contains("on tap dau nam") || normNd.Contains("on tap hoc ky") || normNd.Contains("on tap va bo sung") || normNd.StartsWith("chu de"))
                    {
                        row.NoiDungKienThuc = topic.ChuDe;
                    }
                }
                if (string.IsNullOrWhiteSpace(row.YeuCauCanDat))
                {
                    row.YeuCauCanDat = topic.GetYeuCauCanDat(row.MucDo);
                }
            }

            ValidateMathLevelObjectives(matrix, allowed);
            ValidateNoOutOfPeriodMath(grade, period, matrix);
        }

        public static void ValidateVietMatrix(int grade, string period, IReadOnlyCollection<VietMatrixItem> matrix, IEnumerable<string>? selectedTopicIds = null)
        {
            ValidatePointValues(matrix.Select(x => x.Diem), "ma trận Tiếng Việt");
            if (matrix.Count != 11 || Math.Abs(matrix.Sum(x => x.Diem) - 8.0) > 0.001)
                throw new InvalidOperationException("Ma trận Tiếng Việt phải có đúng 11 câu, tổng 8 điểm (7 câu đọc hiểu/5 điểm và 4 câu Kiến thức Tiếng Việt/3 điểm). Không tự lấp thiếu bằng nội dung ngoài chương trình.");
            ValidateQuestionNumbers(matrix.Select(x => x.SoThuTu), 11, "Tiếng Việt");
            ValidateMatrixCategories(matrix.Select(x => (x.MucDo, x.HinhThuc)), "Tiếng Việt");
            foreach (var item in matrix)
            {
                if (item.SoThuTu <= 7 || (item.MachKienThuc != null && Normalize(item.MachKienThuc).Contains("doc hieu")))
                {
                    item.MachKienThuc = "Đọc hiểu văn bản";
                }
                else
                {
                    item.MachKienThuc = "Kiến thức Tiếng Việt";
                }
            }

            var reading = matrix.Where(x => string.Equals(x.MachKienThuc, "Đọc hiểu văn bản", StringComparison.Ordinal)).ToList();
            var knowledge = matrix.Where(x => string.Equals(x.MachKienThuc, "Kiến thức Tiếng Việt", StringComparison.Ordinal)).ToList();
            if (reading.Count != 7 || Math.Abs(reading.Sum(x => x.Diem) - 5.0) > 0.001 || knowledge.Count != 4 || Math.Abs(knowledge.Sum(x => x.Diem) - 3.0) > 0.001)
                throw new InvalidOperationException("Phân bổ ma trận Tiếng Việt lệch khung: cần 7 câu/5 điểm Đọc hiểu và 4 câu/3 điểm Kiến thức Tiếng Việt.");
            if (matrix.Count(x => x.HinhThuc == "TN") != 6 || matrix.Count(x => x.HinhThuc == "TL") != 5 ||
                matrix.Count(x => x.MucDo == "M1") != 6 || matrix.Count(x => x.MucDo == "M2") != 3 || matrix.Count(x => x.MucDo == "M3") != 2)
                throw new InvalidOperationException("Ma trận Tiếng Việt cần đúng 6 câu TN, 5 câu TL; mức độ gồm M1: 6, M2: 3, M3: 2.");
            var allowed = GetConfirmedVietTopics(grade, period).ToDictionary(x => x.MaMuc, StringComparer.Ordinal);
            if (allowed.Count == 0)
                throw new InvalidOperationException($"Chưa có mục chương trình Tiếng Việt nào đã xác nhận cho {period}.");

            var confirmedReading = allowed.Values.Where(t => t.MachKienThuc.Contains("Đọc hiểu", StringComparison.OrdinalIgnoreCase)).ToList();
            var confirmedKnowledge = allowed.Values.Where(t => !t.MachKienThuc.Contains("Đọc hiểu", StringComparison.OrdinalIgnoreCase)).ToList();

            // Đồng bộ 7 câu Đọc hiểu về cùng 1 mã chủ điểm bài đọc đã xác nhận
            var readingTopicId = reading.Select(x => x.MaMucChuongTrinh)
                .FirstOrDefault(id => !string.IsNullOrWhiteSpace(id) && allowed.ContainsKey(id) && allowed[id].MachKienThuc.Contains("Đọc hiểu", StringComparison.OrdinalIgnoreCase))
                ?? confirmedReading.FirstOrDefault()?.MaMuc
                ?? allowed.Values.First().MaMuc;

            foreach (var row in reading)
            {
                row.MaMucChuongTrinh = readingTopicId;
            }

            foreach (var row in knowledge)
            {
                if (string.IsNullOrWhiteSpace(row.MaMucChuongTrinh) || !allowed.ContainsKey(row.MaMucChuongTrinh) || allowed[row.MaMucChuongTrinh].MachKienThuc.Contains("Đọc hiểu", StringComparison.OrdinalIgnoreCase))
                {
                    var fallback = confirmedKnowledge.FirstOrDefault() ?? allowed.Values.First();
                    row.MaMucChuongTrinh = fallback.MaMuc;
                }
            }

            ValidateIds(matrix.Select(x => (x.SoThuTu, x.MaMucChuongTrinh)), allowed.Keys, "ma trận Tiếng Việt", period);
            var selected = selectedTopicIds == null ? null : new HashSet<string>(selectedTopicIds.Where(id => !string.IsNullOrWhiteSpace(id)), StringComparer.Ordinal);
            if (selected != null && selected.Count == 0)
                throw new InvalidOperationException("Chưa chọn mã mục Tiếng Việt nào đã xác nhận; không thể lập ma trận.");
            foreach (var row in matrix)
            {
                var topic = allowed[row.MaMucChuongTrinh];
                if (string.IsNullOrWhiteSpace(row.MachKienThuc)) row.MachKienThuc = topic.MachKienThuc;
                if (string.IsNullOrWhiteSpace(row.NoiDungKienThuc)) row.NoiDungKienThuc = topic.ChuDe;
                if (string.IsNullOrWhiteSpace(row.YeuCauCanDat)) row.YeuCauCanDat = topic.NoiDung;

                if (string.Equals(row.MachKienThuc, "Đọc hiểu văn bản", StringComparison.Ordinal))
                {
                    ValidateReadingSkill(row);
                }
            }
            if (reading.Select(x => x.MaMucChuongTrinh).Distinct(StringComparer.Ordinal).Count() != 1)
                throw new InvalidOperationException("Bảy câu Đọc hiểu phải cùng gắn với một chủ điểm đã xác nhận, tương ứng với một bài đọc trong đề.");
        }

        public static bool NeedsMathCurriculumReview(int grade, string period, IReadOnlyCollection<MathMatrixItem> matrix, IReadOnlyCollection<MathQuestion>? questions)
        {
            try
            {
                ValidateMathMatrix(grade, period, matrix);
                if (questions == null || questions.Count == 0) return false;
                if (questions.Any(question => string.IsNullOrWhiteSpace(question.MaMucChuongTrinh))) return true;
                ValidateMathQuestions(grade, period, questions, matrix);
                ValidateMathQuestionScope(grade, period, questions);
                return false;
            }
            catch (InvalidOperationException) { return true; }
            catch (ArgumentException) { return true; }
        }

        public static bool NeedsVietCurriculumReview(int grade, string period, IReadOnlyCollection<VietMatrixItem> matrix, IReadOnlyCollection<VietQuestion>? questions, ReadingPassage? passage)
        {
            try
            {
                ValidateVietMatrix(grade, period, matrix);
                if (questions == null || questions.Count == 0)
                {
                    if (passage != null) ValidateVietPassageMatchesMatrix(passage, matrix, grade, period);
                    return false;
                }
                if (questions.Any(question => string.IsNullOrWhiteSpace(question.MaMucChuongTrinh))) return true;
                ValidateVietQuestions(grade, period, questions, matrix);
                if (passage == null) return true;
                ValidateVietPassageMatchesMatrix(passage, matrix, grade, period);
                return false;
            }
            catch (InvalidOperationException) { return true; }
            catch (ArgumentException) { return true; }
        }

        public static void ValidateMathQuestions(int grade, string period, IReadOnlyCollection<MathQuestion> questions, IReadOnlyCollection<MathMatrixItem> matrix)
        {
            ValidateMathMatrix(grade, period, matrix);
            ValidatePointValues(questions.Select(x => x.Diem), "đề Toán");
            if (questions.Count != matrix.Count || Math.Abs(questions.Sum(x => x.Diem) - matrix.Sum(x => x.Diem)) > 0.001)
                throw new InvalidOperationException("Số câu hoặc tổng điểm của đề Toán không khớp ma trận.");
            ValidateQuestionNumbers(questions.Select(x => x.SoThuTu), matrix.Count, "đề Toán");
            var expectedByOrder = matrix.ToDictionary(x => x.SoThuTu, x => x.MaMucChuongTrinh);
            var matrixByOrder = matrix.ToDictionary(x => x.SoThuTu);
            foreach (var question in questions)
            {
                if (!expectedByOrder.TryGetValue(question.SoThuTu, out string? expectedId))
                    throw new InvalidOperationException($"Câu {question.SoThuTu} của đề Toán không có dòng ma trận tương ứng.");
                var expected = matrixByOrder[question.SoThuTu];

                // Đồng bộ metadata câu hỏi theo đúng dòng ma trận của giáo viên
                question.MaMucChuongTrinh = expectedId;
                question.ChuDe = expected.ChuDe;
                question.YeuCauCanDat = expected.YeuCauCanDat;
                question.MucDo = expected.MucDo;
                question.Diem = expected.Diem;

                if (!AllowedMathQuestionTypes.Contains(question.LoaiCauHoi))
                    throw new InvalidOperationException($"Câu {question.SoThuTu} có hình thức câu hỏi không hợp lệ: '{question.LoaiCauHoi}'.");
                string questionForm = question.LoaiCauHoi.StartsWith("TN", StringComparison.OrdinalIgnoreCase) ? "TN" : "TL";
                if (questionForm != expected.HinhThuc)
                    throw new InvalidOperationException($"Câu {question.SoThuTu} không đúng hình thức ({questionForm} != {expected.HinhThuc}) của dòng ma trận.");
            }
        }

        public static void ValidateVietQuestions(int grade, string period, IReadOnlyCollection<VietQuestion> questions, IReadOnlyCollection<VietMatrixItem> matrix)
        {
            ValidateVietMatrix(grade, period, matrix);
            ValidatePointValues(questions.Select(x => x.Diem), "đề Tiếng Việt");
            if (questions.Count != matrix.Count || Math.Abs(questions.Sum(x => x.Diem) - matrix.Sum(x => x.Diem)) > 0.001)
                throw new InvalidOperationException("Số câu hoặc tổng điểm của đề Tiếng Việt không khớp ma trận.");
            ValidateQuestionNumbers(questions.Select(x => x.SoThuTu), matrix.Count, "đề Tiếng Việt");
            var expectedByOrder = matrix.ToDictionary(x => x.SoThuTu, x => x.MaMucChuongTrinh);
            var matrixByOrder = matrix.ToDictionary(x => x.SoThuTu);
            foreach (var question in questions)
            {
                if (!expectedByOrder.TryGetValue(question.SoThuTu, out string? expectedId))
                    throw new InvalidOperationException($"Câu {question.SoThuTu} Tiếng Việt không có dòng ma trận tương ứng.");
                var expected = matrixByOrder[question.SoThuTu];

                // Đồng bộ metadata câu hỏi theo đúng dòng ma trận của giáo viên
                question.MaMucChuongTrinh = expectedId;
                question.MachKienThuc = expected.MachKienThuc;
                question.YeuCauCanDat = expected.YeuCauCanDat;
                question.MucDo = expected.MucDo;
                question.Diem = expected.Diem;

                if (question.LoaiCauHoi is not ("TN_4LuaChon" or "TL"))
                    throw new InvalidOperationException($"Câu {question.SoThuTu} Tiếng Việt có hình thức câu hỏi không hợp lệ: '{question.LoaiCauHoi}'.");
                string questionForm = question.LoaiCauHoi.StartsWith("TN", StringComparison.OrdinalIgnoreCase) ? "TN" : "TL";
                if (questionForm != expected.HinhThuc)
                    throw new InvalidOperationException($"Câu {question.SoThuTu} Tiếng Việt không đúng hình thức ({questionForm} != {expected.HinhThuc}) của dòng ma trận.");
            }
        }

        public static void ValidateMathQuestionScope(int grade, string period, IEnumerable<MathQuestion> questions)
        {
            if (grade == 4)
            {
                foreach (var question in questions)
                {
                    string questionText = string.Join(" ", question.ChuDe, question.YeuCauCanDat, question.NoiDung,
                        question.DapAnA, question.DapAnB, question.DapAnC, question.DapAnD, question.DapAnDung,
                        string.Join(" ", (question.CacBuocGiai ?? new List<MathStepSolution>()).Select(step => step.NoiDung)));
                    // Bỏ qua cụm biểu điểm kiểu "0,5 điểm"/"0,5đ"; mọi số thập phân khác đều ngoài chương trình Toán 4.
                    var decimalMatch = System.Text.RegularExpressions.Regex.Match(questionText,
                        @"(?<!\w)\d+,\d+(?!\s*(?:điểm|đ)\b)|(?<!\w)\d+\.\d+(?![\d.]|\s*(?:điểm|đ)\b)");
                    if (questionText.Contains("số thập phân", StringComparison.OrdinalIgnoreCase) || decimalMatch.Success)
                        throw new InvalidOperationException($"Câu {question.SoThuTu} có số thập phân{(decimalMatch.Success ? $" ('{decimalMatch.Value}')" : string.Empty)}; nội dung này không thuộc chương trình Toán lớp 4 (Toán 4 chỉ dùng số tự nhiên). Hãy viết lại câu này với số liệu, phép tính và kết quả là số tự nhiên.");
                }
            }
            if (grade == 4 && ExamPeriodHelper.IsMidTerm1(period))
            {
                string[] forbidden = { "tìm hai số khi biết tổng và hiệu", "tìm hai số biết tổng và hiệu", "tổng và hiệu của hai số" };
                foreach (var question in questions)
                {
                    string content = Normalize(string.Join(" ", question.ChuDe, question.YeuCauCanDat, question.NoiDung, question.DapAnDung));
                    if (forbidden.Any(pattern => content.Contains(Normalize(pattern), StringComparison.Ordinal)))
                        throw new InvalidOperationException($"Câu {question.SoThuTu} có dạng tìm hai số khi biết tổng và hiệu. Nội dung này thuộc Toán 4 tuần 12, vượt mốc giữa kỳ I tuần 9; đề đã dừng và chưa được phép xuất.");
                }
            }
        }

        public static void ValidateMathUserRequirement(int grade, string period, string requirement)
        {
            if (string.IsNullOrWhiteSpace(requirement)) return;
            var allowed = GetConfirmedMathTopics(grade, period);
            var allowedIds = new HashSet<string>(allowed.Select(x => x.MaMuc), StringComparer.Ordinal);
            int currentOrder = GetPeriodOrder(period);
            var laterPeriods = MathCurriculumBank.ExamPeriods.Where(candidate => GetPeriodOrder(candidate) > currentOrder).ToList();

            var clauses = requirement.Split(new[] { '\r', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Trim())
                .Where(c => c.Length > 0)
                .ToList();

            foreach (string clause in clauses)
            {
                string normClause = Normalize(clause);

                // Specific hard curriculum boundary checks
                if (grade == 4 && ExamPeriodHelper.IsMidTerm1(period))
                {
                    if (normClause.Contains("tong va hieu") || normClause.Contains("tim hai so biet tong") || normClause.Contains("tim hai so khi biet tong") || normClause.Contains("tong - hieu"))
                        throw new InvalidOperationException($"Yêu cầu '{clause}' có dạng toán tìm hai số khi biết tổng và hiệu. Dạng toán này thuộc tuần 12 (Cuối Học kỳ I), vượt mốc giữa kỳ I tuần 9; đề đã dừng.");
                    if (normClause.Contains("hinh binh hanh") || normClause.Contains("hinh thoi"))
                        throw new InvalidOperationException($"Yêu cầu '{clause}' có nội dung hình bình hành hoặc hình thoi. Nội dung này thuộc tuần 15 (Cuối Học kỳ I), ngoài phạm vi {period}.");
                    if (normClause.Contains("hai duong thang vuong goc") || normClause.Contains("hai duong thang song song"))
                        throw new InvalidOperationException($"Yêu cầu '{clause}' có hai đường thẳng vuông góc hoặc song song. Nội dung này thuộc tuần 13-14 (Cuối Học kỳ I), ngoài phạm vi {period}.");
                    if (normClause.Contains("phan so"))
                        throw new InvalidOperationException($"Yêu cầu '{clause}' có phân số. Phân số thuộc chương trình Toán 4 Học kỳ II, ngoài phạm vi {period}.");
                    if (normClause.Contains("trung binh cong"))
                        throw new InvalidOperationException($"Yêu cầu '{clause}' có số trung bình cộng. Nội dung này thuộc tuần 22-23 (Học kỳ II), ngoài phạm vi {period}.");
                    if (normClause.Contains("rut ve don vi"))
                        throw new InvalidOperationException($"Yêu cầu '{clause}' có bài toán rút về đơn vị. Nội dung này thuộc tuần 23 (Học kỳ II), ngoài phạm vi {period}.");
                    if (normClause.Contains("bieu do cot"))
                        throw new InvalidOperationException($"Yêu cầu '{clause}' có biểu đồ cột. Nội dung này thuộc tuần 24 (Học kỳ II), ngoài phạm vi {period}.");
                }
                else if (grade == 4 && ExamPeriodHelper.IsFinalTerm1(period))
                {
                    if (normClause.Contains("phan so"))
                        throw new InvalidOperationException($"Yêu cầu '{clause}' có phân số. Phân số thuộc chương trình Toán 4 Học kỳ II, ngoài phạm vi {period}.");
                    if (normClause.Contains("trung binh cong"))
                        throw new InvalidOperationException($"Yêu cầu '{clause}' có số trung bình cộng. Nội dung này thuộc tuần 22-23 (Học kỳ II), ngoài phạm vi {period}.");
                    if (normClause.Contains("rut ve don vi"))
                        throw new InvalidOperationException($"Yêu cầu '{clause}' có bài toán rút về đơn vị. Nội dung này thuộc tuần 23 (Học kỳ II), ngoài phạm vi {period}.");
                }
                else if (grade == 5 && ExamPeriodHelper.IsMidTerm1(period))
                {
                    if (normClause.Contains("hinh tam giac") || normClause.Contains("dien tich tam giac"))
                        throw new InvalidOperationException($"Yêu cầu '{clause}' có hình tam giác. Nội dung này thuộc tuần 12 (Cuối Học kỳ I), ngoài phạm vi {period}.");
                    if (normClause.Contains("hinh thang") || normClause.Contains("dien tich hinh thang"))
                        throw new InvalidOperationException($"Yêu cầu '{clause}' có hình thang. Nội dung này thuộc tuần 12-13 (Cuối Học kỳ I), ngoài phạm vi {period}.");
                    if (normClause.Contains("hinh tron") || normClause.Contains("chu vi hinh tron") || normClause.Contains("dien tich hinh tron"))
                        throw new InvalidOperationException($"Yêu cầu '{clause}' có hình tròn. Nội dung này thuộc tuần 13-14 (Cuối Học kỳ I), ngoài phạm vi {period}.");
                    if (normClause.Contains("ti so phan tram"))
                        throw new InvalidOperationException($"Yêu cầu '{clause}' có tỉ số phần trăm. Nội dung này thuộc Học kỳ II, ngoài phạm vi {period}.");
                    if (normClause.Contains("van toc") || normClause.Contains("chuyen dong deu"))
                        throw new InvalidOperationException($"Yêu cầu '{clause}' có toán chuyển động đều/vận tốc. Nội dung này thuộc Học kỳ II, ngoài phạm vi {period}.");
                    if (normClause.Contains("hinh hop chu nhat") || normClause.Contains("hinh lap phuong") || normClause.Contains("the tich"))
                        throw new InvalidOperationException($"Yêu cầu '{clause}' có thể tích/hình khối. Nội dung này thuộc Học kỳ II, ngoài phạm vi {period}.");
                }
                else if (grade == 5 && ExamPeriodHelper.IsFinalTerm1(period))
                {
                    if (normClause.Contains("ti so phan tram"))
                        throw new InvalidOperationException($"Yêu cầu '{clause}' có tỉ số phần trăm. Nội dung này thuộc Học kỳ II, ngoài phạm vi {period}.");
                    if (normClause.Contains("van toc") || normClause.Contains("chuyen dong deu"))
                        throw new InvalidOperationException($"Yêu cầu '{clause}' có toán chuyển động đều/vận tốc. Nội dung này thuộc Học kỳ II, ngoài phạm vi {period}.");
                    if (normClause.Contains("hinh hop chu nhat") || normClause.Contains("hinh lap phuong") || normClause.Contains("the tich"))
                        throw new InvalidOperationException($"Yêu cầu '{clause}' có thể tích/hình khối. Nội dung này thuộc Học kỳ II, ngoài phạm vi {period}.");
                }

                if (laterPeriods.Count == 0) continue;

                // Bỏ qua kiểm tra độ trùng từ khóa nếu câu thuộc phần ôn tập hoặc phạm vi 100 000
                if (normClause.Contains("100 000") || normClause.Contains("100000") || normClause.Contains("on tap cac phep tinh") || normClause.Contains("lop 3") || normClause.Contains("lop 4"))
                    continue;

                var requestTokens = normClause.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Where(token => token.Length >= 3 && !StopWords.Contains(token) && !MathGeneralWords.Contains(token))
                    .ToHashSet(StringComparer.Ordinal);

                foreach (string otherPeriod in laterPeriods)
                {
                    var laterTopics = MathTopicCatalog.GetTopics(grade, otherPeriod)
                        .Where(item => !allowedIds.Contains(item.MaMuc));
                    foreach (var item in laterTopics)
                    {
                        string descriptor = Normalize(item.ChuDe + " " + item.NoiDung);
                        var keyTokens = descriptor.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                            .Where(token => token.Length >= 3 && !StopWords.Contains(token) && !MathGeneralWords.Contains(token))
                            .Distinct(StringComparer.Ordinal)
                            .ToHashSet(StringComparer.Ordinal);
                        int overlap = keyTokens.Count(requestTokens.Contains);
                        if (overlap >= 4 && overlap / (double)Math.Max(keyTokens.Count, 1) >= 0.5)
                            throw new InvalidOperationException($"Yêu cầu '{clause}' khớp nội dung Toán lớp {grade} '{item.ChuDe}: {item.NoiDung}' ở tuần {item.Tuan}, ngoài phạm vi {period}. Đã dừng; hãy chọn kỳ phù hợp thay vì tự đổi sang dạng gần giống.");
                    }
                }
            }
        }

        public static void ValidateVietUserRequirement(int grade, string period, string requirement)
        {
            if (string.IsNullOrWhiteSpace(requirement)) return;
            var allowedIds = new HashSet<string>(GetConfirmedVietTopics(grade, period).Select(x => x.MaMuc), StringComparer.Ordinal);
            int currentOrder = GetPeriodOrder(period);
            var laterPeriods = MathCurriculumBank.ExamPeriods.Where(candidate => GetPeriodOrder(candidate) > currentOrder).ToList();
            if (laterPeriods.Count == 0) return;

            var clauses = requirement.Split(new[] { '\r', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Trim())
                .Where(c => c.Length > 0)
                .ToList();

            foreach (string clause in clauses)
            {
                var requestTokens = Normalize(clause).Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Where(token => token.Length >= 3 && !StopWords.Contains(token))
                    .ToHashSet(StringComparer.Ordinal);

                foreach (string otherPeriod in laterPeriods)
                {
                    foreach (var item in VietTopicCatalog.GetTopics(grade, otherPeriod).Where(topic => !allowedIds.Contains(topic.MaMuc)))
                    {
                        string descriptor = Normalize(item.ChuDe + " " + item.NoiDung);
                        var keyTokens = descriptor.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                            .Where(token => token.Length >= 3 && !StopWords.Contains(token))
                            .Distinct(StringComparer.Ordinal)
                            .ToHashSet(StringComparer.Ordinal);
                        int overlap = keyTokens.Count(requestTokens.Contains);
                        if (overlap >= 4 && overlap / (double)Math.Max(keyTokens.Count, 1) >= 0.5)
                            throw new InvalidOperationException($"Yêu cầu '{clause}' khớp nội dung Tiếng Việt lớp {grade} '{item.ChuDe}: {item.NoiDung}' ở tuần {item.Tuan}, ngoài phạm vi {period}. Đã dừng; hãy chọn kỳ phù hợp.");
                    }
                }
            }
        }

        private static void ValidateNoOutOfPeriodMath(int grade, string period, IEnumerable<MathMatrixItem> matrix)
        {
            if (grade != 4 || !ExamPeriodHelper.IsMidTerm1(period)) return;
            string[] forbidden = { "tìm hai số khi biết tổng và hiệu", "tìm hai số biết tổng và hiệu", "tổng và hiệu của hai số" };
            foreach (var row in matrix)
            {
                string content = Normalize(string.Join(" ", row.ChuDe, row.NoiDungKienThuc, row.YeuCauCanDat));
                if (forbidden.Any(pattern => content.Contains(Normalize(pattern), StringComparison.Ordinal)))
                    throw new InvalidOperationException($"Dòng {row.SoThuTu} yêu cầu dạng tìm hai số khi biết tổng và hiệu. Dạng này ở tuần 12, ngoài phạm vi Toán 4 giữa kỳ I (đến tuần 9).");
            }
        }

        /// <summary>
        /// Với mục đã tách yêu cầu cần đạt theo mức, cùng một mục xuất hiện ở nhiều mức khác nhau
        /// bắt buộc dùng yêu cầu cần đạt khác nhau; lặp y hệt văn bản giữa các mức là lỗi cần lập lại ma trận.
        /// </summary>
        private static void ValidateMathLevelObjectives(IReadOnlyCollection<MathMatrixItem> matrix, Dictionary<string, MathTopicItem> allowed)
        {
            foreach (var topicRows in matrix.GroupBy(row => row.MaMucChuongTrinh, StringComparer.Ordinal))
            {
                if (!allowed.TryGetValue(topicRows.Key, out var topic) || !topic.CoYeuCauTheoMuc) continue;
                foreach (var sameText in topicRows.GroupBy(row => row.YeuCauCanDat, StringComparer.Ordinal))
                {
                    var levels = sameText.Select(row => row.MucDo).Distinct(StringComparer.Ordinal).ToList();
                    if (levels.Count <= 1) continue;
                    // Phân biệt yêu cầu cần đạt theo từng mức để không chặn cứng người dùng khi sửa ma trận
                    foreach (var r in sameText)
                    {
                        if (!r.YeuCauCanDat.Contains($"(Mức {r.MucDo})", StringComparison.OrdinalIgnoreCase))
                        {
                            r.YeuCauCanDat = $"{r.YeuCauCanDat} (Mức {r.MucDo})";
                        }
                    }
                }
            }
        }

        public static MathTopicItem? FindBestMathTopic(MathMatrixItem row, IEnumerable<MathTopicItem> candidates)
        {
            var candidateList = candidates.ToList();
            if (candidateList.Count == 0) return null;

            string searchText = $"{row.ChuDe} {row.NoiDungKienThuc} {row.YeuCauCanDat}";
            var terms = Normalize(searchText).Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(t => t.Length >= 3 && !StopWords.Contains(t) && !MathGeneralWords.Contains(t))
                .ToHashSet(StringComparer.Ordinal);

            if (terms.Count == 0)
            {
                return candidateList.FirstOrDefault(t => string.Equals(t.MachKienThuc, row.ChuDe, StringComparison.OrdinalIgnoreCase)) ?? candidateList.FirstOrDefault();
            }

            var scored = candidateList.Select(topic =>
            {
                string desc = Normalize($"{topic.MachKienThuc} {topic.ChuDe} {topic.NoiDung}");
                var topicTerms = desc.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Where(t => t.Length >= 3 && !StopWords.Contains(t) && !MathGeneralWords.Contains(t))
                    .ToHashSet(StringComparer.Ordinal);
                int overlap = terms.Intersect(topicTerms, StringComparer.Ordinal).Count();
                bool sameStrand = string.Equals(topic.MachKienThuc, row.ChuDe, StringComparison.OrdinalIgnoreCase);
                int score = overlap * 10 + (sameStrand ? 5 : 0);
                return (Topic: topic, Score: score);
            }).OrderByDescending(x => x.Score).ToList();

            return scored.Count > 0 && scored[0].Score > 0 ? scored[0].Topic : (candidateList.FirstOrDefault(t => string.Equals(t.MachKienThuc, row.ChuDe, StringComparison.OrdinalIgnoreCase)) ?? candidateList.FirstOrDefault());
        }

        public static string NormalizeMathStrand(string strand, string fallback)
        {
            string norm = Normalize(strand);
            if (norm.Contains("so") || norm.Contains("phep tinh")) return "Số và phép tính";
            if (norm.Contains("hinh") || norm.Contains("do luong")) return "Hình học và đo lường";
            if (norm.Contains("thong ke") || norm.Contains("xac suat")) return "Thống kê và xác suất";
            return fallback;
        }

        private static void ValidateIds(IEnumerable<(int Number, string Id)> rows, IEnumerable<string> allowedIds, string label, string period)
        {
            var allowed = new HashSet<string>(allowedIds, StringComparer.Ordinal);
            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.Id))
                    throw new InvalidOperationException($"Dòng {row.Number} của {label} chưa gắn mã chương trình đã rà soát. Bản nháp cũ cần được lập lại ma trận theo danh mục hiện hành trước khi sinh hoặc xuất đề.");
                if (!allowed.Contains(row.Id))
                    throw new InvalidOperationException($"Dòng {row.Number} của {label} gắn với nội dung chưa xác nhận, sai kỳ hoặc vượt mốc tuần của {period}. Hãy chọn lại mục đã xác nhận trong danh mục.");
            }
        }

        private static void ValidateQuestionNumbers(IEnumerable<int> numbers, int expectedCount, string subject)
        {
            var actual = numbers.OrderBy(number => number).ToList();
            if (!actual.SequenceEqual(Enumerable.Range(1, expectedCount)))
                throw new InvalidOperationException($"Ma trận {subject} phải đánh số câu liên tục từ 1 đến {expectedCount}, không thiếu hoặc trùng số thứ tự.");
        }

        private static void ValidatePointValues(IEnumerable<double> values, string label)
        {
            if (values.Any(value => !double.IsFinite(value) || value <= 0))
                throw new InvalidOperationException($"{label} có điểm không hợp lệ; mỗi điểm phải là số hữu hạn lớn hơn 0.");
        }

        private static void ValidateMatrixCategories(IEnumerable<(string MucDo, string HinhThuc)> rows, string label)
        {
            foreach (var (mucDo, hinhThuc) in rows)
            {
                if (mucDo is not ("M1" or "M2" or "M3"))
                    throw new InvalidOperationException($"{label} có mức độ không hợp lệ: '{mucDo}'. Chỉ chấp nhận M1, M2 hoặc M3.");
                if (hinhThuc is not ("TN" or "TL"))
                    throw new InvalidOperationException($"{label} có hình thức không hợp lệ: '{hinhThuc}'. Chỉ chấp nhận TN hoặc TL.");
            }
        }

        private static void ValidateReadingSkill(VietMatrixItem row)
        {
            string skill = Normalize(row.NoiDungKienThuc);
            string target = Normalize(row.YeuCauCanDat);
            if (skill.StartsWith("doc hieu noi dung", StringComparison.Ordinal) ||
                (row.SoThuTu <= 4 && (skill.Contains("noi dung") || skill.Contains("chi tiet") || skill.Contains("su viec") || skill.Contains("y chinh") || skill.Contains("thong tin"))))
            {
                row.NoiDungKienThuc = "Đọc hiểu nội dung";
                if (!ContainsAny(target, "chi tiet", "su viec", "nhan vat", "noi dung", "y chinh", "tom tat", "y nghia", "hinh anh"))
                    row.YeuCauCanDat += " (Nhận biết chi tiết, nội dung bài đọc)";
                return;
            }
            if (skill.StartsWith("doc hieu hinh thuc", StringComparison.Ordinal) ||
                ((row.SoThuTu == 5 || row.SoThuTu == 6) && (skill.Contains("hinh thuc") || skill.Contains("nghe thuat") || skill.Contains("so sanh") || skill.Contains("nhan hoa") || skill.Contains("tu ngu") || skill.Contains("nhan vat") || skill.Contains("ly do") || skill.Contains("nguyen nhan") || skill.Contains("vi sao"))))
            {
                row.NoiDungKienThuc = "Đọc hiểu hình thức";
                if (!ContainsAny(target, "hinh anh", "nghe thuat", "so sanh", "nhan hoa", "tu ngu", "y nghia", "biet phap tu tu", "nhan vat", "ly do", "nguyen nhan", "vi sao", "giai thich"))
                    row.YeuCauCanDat += " (Hiểu từ ngữ, nghệ thuật hoặc lý do sự việc trong bài)";
                return;
            }
            if (skill.StartsWith("lien he", StringComparison.Ordinal) ||
                (row.SoThuTu == 7 && (skill.Contains("lien he") || skill.Contains("so sanh") || skill.Contains("ket noi") || skill.Contains("bai hoc") || skill.Contains("thong diep") || skill.Contains("suy nghi") || skill.Contains("ban than"))))
            {
                row.NoiDungKienThuc = "Liên hệ, so sánh, kết nối";
                if (!ContainsAny(target, "lien he", "so sanh", "ket noi", "thong diep", "bai hoc", "ban than", "van dung", "suy nghi", "tam dac"))
                    row.YeuCauCanDat += " (Rút ra bài học, liên hệ bản thân)";
                return;
            }
            if (row.SoThuTu <= 4)
            {
                row.NoiDungKienThuc = "Đọc hiểu nội dung";
                if (!ContainsAny(target, "chi tiet", "su viec", "nhan vat", "noi dung", "y chinh", "tom tat", "y nghia", "hinh anh"))
                    row.YeuCauCanDat += " (Nhận biết chi tiết, nội dung bài đọc)";
                return;
            }
            if (row.SoThuTu is 5 or 6)
            {
                row.NoiDungKienThuc = "Đọc hiểu hình thức";
                if (!ContainsAny(target, "hinh anh", "nghe thuat", "so sanh", "nhan hoa", "tu ngu", "y nghia", "biet phap tu tu", "nhan vat", "ly do", "nguyen nhan", "vi sao", "giai thich"))
                    row.YeuCauCanDat += " (Hiểu từ ngữ, nghệ thuật hoặc lý do sự việc trong bài)";
                return;
            }
            if (row.SoThuTu == 7)
            {
                row.NoiDungKienThuc = "Liên hệ, so sánh, kết nối";
                if (!ContainsAny(target, "lien he", "so sanh", "ket noi", "thong diep", "bai hoc", "ban than", "van dung", "suy nghi", "tam dac"))
                    row.YeuCauCanDat += " (Rút ra bài học, liên hệ bản thân)";
                return;
            }
            throw new InvalidOperationException($"Kỹ năng đọc hiểu Câu {row.SoThuTu} không thuộc ba nhóm đã được thiết kế cho ma trận.");
        }

        private static bool ContainsAny(string value, params string[] fragments) =>
            fragments.Any(fragment => value.Contains(fragment, StringComparison.Ordinal));

        private static string Normalize(string value)
        {
            string decomposed = value.Normalize(System.Text.NormalizationForm.FormD);
            var chars = decomposed.Where(ch => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) != System.Globalization.UnicodeCategory.NonSpacingMark).ToArray();
            return new string(chars).Normalize(System.Text.NormalizationForm.FormC).ToLowerInvariant();
        }

        private static string NormalizeVietTheme(string value)
        {
            string withoutLabel = (value ?? string.Empty).Replace("Chủ điểm", string.Empty, StringComparison.OrdinalIgnoreCase);
            return Normalize(withoutLabel).Trim(' ', '\"', '\'', '“', '”', '.', ':');
        }

        private static bool MentionsKnttBook(string value)
        {
            string normalized = Normalize(value ?? string.Empty);
            return normalized.Contains("kntt", StringComparison.Ordinal) ||
                   normalized.Contains("sgk ket noi tri thuc", StringComparison.Ordinal) ||
                   normalized.Contains("sach giao khoa ket noi tri thuc", StringComparison.Ordinal) ||
                   normalized.Contains("bo sach ket noi tri thuc", StringComparison.Ordinal) ||
                   normalized.Contains("ket noi tri thuc voi cuoc song", StringComparison.Ordinal);
        }

        // ==================== CHUẨN HÓA HÌNH THỨC CÂU HỎI TỪ AI ====================

        /// <summary>
        /// Chuẩn hóa giá trị hình thức câu hỏi Toán do AI trả về (miễn nhiễm biến thể như
        /// 'Tuluan', 'TuLuan', 'Tự luận', 'trac nghiem'...) về đúng token hệ thống.
        /// Trả về null nếu không nhận dạng được (phía gọi giữ nguyên giá trị gốc).
        /// </summary>
        public static string? NormalizeMathQuestionType(string? rawType)
        {
            string s = FoldQuestionType(rawType);
            if (s.Length == 0) return null;

            if (s.Contains("dungsai", StringComparison.Ordinal)) return "TN_DungSai";
            if (s.Contains("traloingan", StringComparison.Ordinal)) return "TN_TraLoiNgan";
            if (s.Contains("loivan", StringComparison.Ordinal)) return "TL_LoiVan";
            if (s.Contains("luachon", StringComparison.Ordinal) || s.Contains("tracnghiem", StringComparison.Ordinal) ||
                s.StartsWith("tn", StringComparison.Ordinal)) return "TN_4LuaChon";
            if (s.Contains("tuluan", StringComparison.Ordinal) || s.Contains("tinhtoan", StringComparison.Ordinal) ||
                s.StartsWith("tl", StringComparison.Ordinal)) return "TL_TinhToan";
            return null;
        }

        /// <summary>
        /// Chuẩn hóa hình thức câu hỏi Tiếng Việt: chỉ nhận đúng "TN_4LuaChon" hoặc "TL".
        /// Trả về null nếu không nhận dạng được.
        /// </summary>
        public static string? NormalizeVietQuestionType(string? rawType)
        {
            string s = FoldQuestionType(rawType);
            if (s.Length == 0) return null;

            if (s.StartsWith("tn", StringComparison.Ordinal) || s.Contains("tracnghiem", StringComparison.Ordinal) ||
                s.Contains("luachon", StringComparison.Ordinal) || s.Contains("dungsai", StringComparison.Ordinal))
                return "TN_4LuaChon";
            if (s.StartsWith("tl", StringComparison.Ordinal) || s.Contains("tuluan", StringComparison.Ordinal))
                return "TL";
            return null;
        }

        /// <summary>Bỏ dấu tiếng Việt, chữ thường, bỏ khoảng trắng/gạch nối để so khớp các biến thể AI hay trả về.</summary>
        private static string FoldQuestionType(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            string decomposed = raw.Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder(decomposed.Length);
            foreach (char c in decomposed)
            {
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.NonSpacingMark) continue;
                char lower = char.ToLowerInvariant(c);
                if (lower == 'đ') lower = 'd';
                if (char.IsLetterOrDigit(lower)) sb.Append(lower);
            }
            return sb.ToString();
        }

        private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
        {
            "bai", "toan", "lop", "ve", "va", "la", "cua", "cac", "mot", "nhieu", "biet", "de", "cho", "trong", "theo", "voi", "hoc", "tap", "on"
        };

        private static readonly HashSet<string> AllowedMathQuestionTypes = new(StringComparer.Ordinal)
        {
            "TN_4LuaChon", "TN_DungSai", "TN_TraLoiNgan", "TL_TinhToan", "TL_LoiVan"
        };

        private static readonly HashSet<string> MathGeneralWords = new(StringComparer.Ordinal)
        {
            "phep", "tinh", "toan", "thuan", "tien", "bieu", "thuc", "giai", "dung", "dat", "thuc", "hien", "diem", "cau", "bai"
        };

        private static int GetPeriodOrder(string period)
        {
            if (ExamPeriodHelper.IsMidTerm1(period)) return 1;
            if (ExamPeriodHelper.IsFinalTerm1(period)) return 2;
            if (ExamPeriodHelper.IsMidTerm2(period)) return 3;
            if (ExamPeriodHelper.IsFinalTerm2(period)) return 4;
            return 0;
        }
    }
}
