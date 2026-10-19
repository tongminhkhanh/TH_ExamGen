using System.Collections.Generic;
using TH_ExamGen.Core.Curriculum;

namespace TH_ExamGen.Core.MathModels
{
    /// <summary>
    /// Một mục trong danh mục chủ đề/bài học Toán (SGK Kết nối tri thức với cuộc sống).
    /// Tuan: khoảng tuần dự kiến trong phân phối chương trình (bản nháp, có thể chỉnh sửa trực tiếp tại đây).
    /// </summary>
    public class MathTopicItem : ICurriculumTopicItem
    {
        public string MaMuc { get; set; } = "";
        public string MaPhienBan { get; set; } = "";
        public int KhoiLop { get; set; }
        public string HocKy { get; set; } = "";
        public int TuanBatDau { get; set; }
        public int TuanKetThuc { get; set; }
        public string NguonThamKhao { get; set; } = "";
        public string CanCuTuan { get; set; } = "";
        public bool DaXacNhan { get; set; }
        public string MachKienThuc { get; set; } = "";
        public string ChuDe { get; set; } = "";
        public string NoiDung { get; set; } = "";
        public string Tuan { get; set; } = "";
        /// <summary>Yêu cầu cần đạt theo mức độ nhận thức M1 (Thông tư 27); để trống nếu mục chưa tách theo mức.</summary>
        public string YeuCauCanDatM1 { get; set; } = "";
        /// <summary>Yêu cầu cần đạt theo mức độ nhận thức M2 (Thông tư 27); để trống nếu mục chưa tách theo mức.</summary>
        public string YeuCauCanDatM2 { get; set; } = "";
        /// <summary>Yêu cầu cần đạt theo mức độ nhận thức M3 (Thông tư 27); để trống nếu mục chưa tách theo mức.</summary>
        public string YeuCauCanDatM3 { get; set; } = "";

        /// <summary>True khi mục đã có đủ yêu cầu cần đạt riêng cho cả ba mức độ M1/M2/M3.</summary>
        public bool CoYeuCauTheoMuc =>
            !string.IsNullOrWhiteSpace(YeuCauCanDatM1) &&
            !string.IsNullOrWhiteSpace(YeuCauCanDatM2) &&
            !string.IsNullOrWhiteSpace(YeuCauCanDatM3);

        /// <summary>
        /// Yêu cầu cần đạt phải dùng cho một dòng ở mức độ chỉ định.
        /// Mục chưa tách theo mức (CoYeuCauTheoMuc = false) trả về NoiDung như hành vi cũ.
        /// </summary>
        public string GetYeuCauCanDat(string mucDo)
        {
            if (!CoYeuCauTheoMuc) return NoiDung;
            return mucDo switch
            {
                "M1" => YeuCauCanDatM1,
                "M2" => YeuCauCanDatM2,
                "M3" => YeuCauCanDatM3,
                _ => NoiDung
            };
        }
    }

    /// <summary>
    /// Danh mục chủ đề/bài học môn Toán lớp 4 - 5 theo từng đợt kiểm tra (SGK Kết nối tri thức).
    /// Dùng cho chế độ "AI tự chọn chủ đề": AI CHỈ được chọn chủ đề nằm trong danh mục này.
    /// Đây là bản nháp bám phân phối chương trình GDPT 2018 - giáo viên rà soát và chỉnh sửa nếu cần.
    /// </summary>
    public static class MathTopicCatalog
    {
        private const string S = "Số và phép tính";
        private const string H = "Hình học và đo lường";
        private const string T = "Thống kê và xác suất";

        private static MathTopicItem I(string mach, string chuDe, string noiDung, string tuan)
        {
            return new MathTopicItem { MachKienThuc = mach, ChuDe = chuDe, NoiDung = noiDung, Tuan = tuan };
        }

        /// <summary>Mục đã tách yêu cầu cần đạt theo ba mức độ nhận thức M1/M2/M3 (Thông tư 27).</summary>
        private static MathTopicItem I(string mach, string chuDe, string noiDung, string tuan, string m1, string m2, string m3)
        {
            return new MathTopicItem
            {
                MachKienThuc = mach,
                ChuDe = chuDe,
                NoiDung = noiDung,
                Tuan = tuan,
                YeuCauCanDatM1 = m1,
                YeuCauCanDatM2 = m2,
                YeuCauCanDatM3 = m3,
            };
        }

        public static List<MathTopicItem> GetTopics(int khoiLop, string period)
        {
            CurriculumScopePolicy.ValidateCatalogRequest(khoiLop, period);
            List<MathTopicItem> topics;
            if (khoiLop == 5)
            {
                if (ExamPeriodHelper.IsMidTerm1(period)) topics = Grade5MidTerm1();
                else if (ExamPeriodHelper.IsFinalTerm1(period)) topics = Grade5FinalTerm1();
                else if (ExamPeriodHelper.IsMidTerm2(period)) topics = Grade5MidTerm2();
                else topics = Grade5FinalTerm2();
            }
            else if (ExamPeriodHelper.IsMidTerm1(period)) topics = Grade4MidTerm1();
            else if (ExamPeriodHelper.IsFinalTerm1(period)) topics = Grade4FinalTerm1();
            else if (ExamPeriodHelper.IsMidTerm2(period)) topics = Grade4MidTerm2();
            else topics = Grade4FinalTerm2();

            return CurriculumCatalogMetadata.Prepare(topics, khoiLop, period, GetSource(khoiLop, period));
        }

        private static string GetSource(int grade, string period)
        {
            bool firstTerm = ExamPeriodHelper.IsFirstTerm(period);
            if (grade == 4)
                return firstTerm
                    ? "https://sgk.olm.vn/thu-vien-so/doc-sach/shs-toan-tap-1.4491523128"
                    : "https://sgk.olm.vn/thu-vien-so/doc-sach/shs-toan-tap-2.4491525565";
            return firstTerm
                ? "https://sachdientu.nxbgd.vn/thu-vien-so/doc-sach/shs-toan-tap-1.4491508429"
                : "https://sachdientu.nxbgd.vn/thu-vien-so/doc-sach/shs-toan-tap-2.4491508600";
        }

        // ============================== LỚP 4 ==============================

        private static List<MathTopicItem> Grade4MidTerm1()
        {
            return new List<MathTopicItem>
            {
                I(S, "Số và phép tính trong phạm vi 100 000", "Các số đến 100 000 và các phép tính trong phạm vi 100 000", "1",
                    "Đọc, viết được các số đến 100 000; nêu được giá trị của chữ số theo vị trí hàng.",
                    "Thực hiện được phép cộng, phép trừ, phép nhân, phép chia trong phạm vi 100 000 (đặt tính, tính nhẩm).",
                    "Vận dụng bốn phép tính trong phạm vi 100 000 để giải quyết bài toán thực tế quen thuộc (1 - 2 bước tính)."),
                I(S, "Số chẵn, số lẻ và biểu thức chứa chữ", "Số chẵn, số lẻ; Biểu thức chứa một chữ (tính giá trị biểu thức)", "2",
                    "Nhận biết được số chẵn, số lẻ; nhận biết biểu thức chứa một chữ.",
                    "Tính được giá trị của biểu thức chứa một chữ với giá trị cho trước của chữ.",
                    "Vận dụng tính chất số chẵn, số lẻ và biểu thức chứa chữ để giải quyết tình huống thực tế."),
                I(S, "Giải bài toán có ba bước tính", "Giải bài toán có ba bước tính (bài toán thực tế)", "3",
                    "Nhận biết được các dữ kiện và câu hỏi trong bài toán có ba bước tính.",
                    "Thực hiện được các bước giải bài toán có ba bước tính dạng đơn giản.",
                    "Giải và trình bày được bài toán thực tế có ba bước tính liên quan đến các phép tính đã học."),
                I(H, "Góc và đơn vị đo góc", "Đo góc, đơn vị đo góc (độ); Góc nhọn, góc tù, góc bẹt (nhận biết và đo góc)", "3-4",
                    "Nhận biết được góc nhọn, góc vuông, góc tù, góc bẹt qua hình vẽ trực quan.",
                    "Sử dụng được thước đo góc để đo và đọc số đo các góc; phân loại được các góc theo số đo.",
                    "Vận dụng nhận biết góc để xác định loại góc trong các hình phẳng hoặc tình huống thực tế."),
                I(S, "Số có nhiều chữ số đến lớp triệu", "Số có sáu chữ số, số 1 000 000; Hàng và lớp; Các số trong phạm vi lớp triệu", "5-6",
                    "Đọc, viết được các số có nhiều chữ số đến lớp triệu; nhận biết được các hàng thuộc lớp đơn vị, lớp nghìn, lớp triệu.",
                    "Xác định được giá trị của chữ số theo hàng, lớp và viết được số thành tổng theo các hàng.",
                    "Vận dụng cấu tạo số và quan hệ hàng, lớp để tìm số thỏa mãn điều kiện cho trước."),
                I(S, "So sánh và làm tròn số", "Làm tròn số đến hàng trăm nghìn; So sánh các số có nhiều chữ số; Dãy số tự nhiên", "6-7",
                    "Nhận biết được thứ tự các số trong dãy số tự nhiên; nêu được quy tắc làm tròn số đến hàng trăm nghìn.",
                    "So sánh, sắp xếp được các số có nhiều chữ số theo thứ tự; làm tròn được số đến hàng trăm nghìn.",
                    "Vận dụng so sánh và làm tròn số để giải quyết tình huống thực tế (ước lượng, so sánh số liệu)."),
                I(H, "Đơn vị đo khối lượng", "Yến, tạ, tấn (chuyển đổi đơn vị và tính toán)", "8",
                    "Nhận biết được các đơn vị đo khối lượng yến, tạ, tấn và mối quan hệ giữa chúng với ki-lô-gam.",
                    "Chuyển đổi được số đo khối lượng giữa yến, tạ, tấn và ki-lô-gam; thực hiện được phép tính với số đo khối lượng.",
                    "Vận dụng chuyển đổi và tính toán với đơn vị yến, tạ, tấn để giải quyết bài toán thực tế 2 bước giải."),
                I(H, "Đơn vị đo diện tích và thời gian", "Đề-xi-mét vuông, mét vuông, mi-li-mét vuông; Giây, thế kỉ", "9",
                    "Nhận biết được các đơn vị đo diện tích dm2, m2, mm2 và đơn vị thời gian giây, thế kỉ.",
                    "Chuyển đổi được số đo diện tích (m2, dm2, mm2) và thời gian (giây với phút; năm với thế kỉ).",
                    "Vận dụng chuyển đổi và tính toán với số đo diện tích, thời gian để giải bài toán thực tế 2 - 3 bước tính.")
            };
        }

        private static List<MathTopicItem> Grade4FinalTerm1()
        {
            return new List<MathTopicItem>
            {
                I(H, "Đo đại lượng", "Thực hành và trải nghiệm sử dụng một số đơn vị đo đại lượng; Giây, thế kỉ", "10",
                    "Nhận biết được các đơn vị đo đại lượng đã học trong tình huống thực tế.",
                    "Thực hiện chuyển đổi và tính toán chính xác với các số đo đại lượng.",
                    "Vận dụng đo đại lượng để giải quyết bài toán thực tế có lời văn."),
                I(S, "Cộng, trừ số có nhiều chữ số", "Phép cộng, phép trừ các số có nhiều chữ số (đặt tính và tính)", "11",
                    "Thực hiện được phép cộng, phép trừ các số có nhiều chữ số không nhớ hoặc có nhớ 1 lần.",
                    "Đặt tính và tính đúng phép cộng, phép trừ các số có nhiều chữ số; tìm thành phần chưa biết.",
                    "Vận dụng phép cộng, phép trừ số có nhiều chữ số để giải quyết bài toán thực tế 2 - 3 bước tính."),
                I(S, "Tính chất phép cộng", "Tính chất giao hoán và kết hợp của phép cộng (tính bằng cách thuận tiện)", "12",
                    "Nhận biết được tính chất giao hoán và kết hợp của phép cộng.",
                    "Vận dụng tính chất giao hoán, kết hợp của phép cộng để tính giá trị biểu thức bằng cách thuận tiện.",
                    "Vận dụng linh hoạt tính chất phép cộng để tính nhanh và giải quyết bài toán thực tế."),
                I(S, "Tìm hai số biết tổng và hiệu", "Tìm hai số khi biết tổng và hiệu của hai số đó (bài toán có lời văn)", "12",
                    "Nhận biết được bài toán tìm hai số khi biết tổng và hiệu của hai số đó; nêu được công thức tìm số lớn, số bé.",
                    "Giải được bài toán tìm hai số khi biết tổng và hiệu với các số liệu trực tiếp.",
                    "Vận dụng bài toán tìm hai số khi biết tổng và hiệu để giải quyết tình huống thực tế phức hợp."),
                I(H, "Đường thẳng vuông góc và song song", "Hai đường thẳng vuông góc; Hai đường thẳng song song (nhận biết, vẽ hình bằng ê-ke)", "13-14",
                    "Nhận biết được hai đường thẳng vuông góc, hai đường thẳng song song qua hình vẽ trực quan.",
                    "Dùng ê-ke và thước thẳng để kiểm tra hoặc vẽ được hai đường thẳng vuông góc, hai đường thẳng song song.",
                    "Xác định được các cặp cạnh vuông góc, song song trong các hình phẳng thực tế."),
                I(H, "Hình bình hành và hình thoi", "Hình bình hành, hình thoi: nhận biết đặc điểm cạnh và góc", "15",
                    "Nhận biết được hình bình hành, hình thoi qua hình ảnh trực quan.",
                    "Nêu được các đặc điểm cơ bản về cạnh và góc của hình bình hành, hình thoi; vẽ hình trên lưới ô vuông.",
                    "Phân biệt được hình bình hành, hình thoi với các hình phẳng khác và vận dụng giải quyết bài toán thực tế."),
                I(S, "Bốn phép tính với số có nhiều chữ số", "Cộng, trừ, nhân, chia số có nhiều chữ số và đo lường tổng hợp", "16-18",
                    "Đọc, viết, so sánh số đến lớp triệu; tính cộng, trừ số tự nhiên mức độ đơn giản.",
                    "Giải được bài toán tổng hợp học kỳ I gồm phép cộng, trừ, hình học phẳng và số đo đại lượng.",
                    "Vận dụng kiến thức tổng hợp học kì I để giải quyết tình huống thực tế 3 bước tính.")
            };
        }

        private static List<MathTopicItem> Grade4MidTerm2()
        {
            return new List<MathTopicItem>
            {
                I(S, "Nhân, chia với số có một chữ số", "Nhân với số có một chữ số; Chia cho số có một chữ số; Tính chất giao hoán, kết hợp của phép nhân", "19-20",
                    "Thực hiện được phép nhân, chia số có nhiều chữ số với số có một chữ số.",
                    "Vận dụng tính chất giao hoán, kết hợp của phép nhân để tính nhẩm, tính thuận tiện.",
                    "Giải bài toán thực tế liên quan đến nhân, chia với số có một chữ số."),
                I(S, "Nhân, chia với 10, 100, 1000 & Nhân số hai chữ số", "Nhân, chia với 10, 100, 1000...; Tính chất phân phối của phép nhân với phép cộng; Nhân với số có hai chữ số", "20-21",
                    "Tính nhẩm nhân, chia với 10, 100, 1000...; nhận biết tính chất nhân một số với một tổng.",
                    "Đặt tính và thực hiện đúng phép nhân với số có hai chữ số; tính thuận tiện.",
                    "Vận dụng phép nhân với số có hai chữ số vào bài toán thực tế có lời văn."),
                I(S, "Chia cho số có hai chữ số", "Chia cho số có hai chữ số; Thực hành và trải nghiệm ước lượng trong tính toán", "22",
                    "Nêu được các bước chia cho số có hai chữ số.",
                    "Đặt tính và thực hiện đúng phép chia cho số có hai chữ số (chia hết và chia có dư).",
                    "Vận dụng ước lượng thương và phép chia cho số có hai chữ số để giải bài toán thực tế."),
                I(S, "Số trung bình cộng và rút về đơn vị", "Tìm số trung bình cộng; Bài toán liên quan đến rút về đơn vị", "22-23",
                    "Nêu được quy tắc tìm số trung bình cộng của nhiều số; nhận biết bài toán rút về đơn vị.",
                    "Giải được bài toán tìm số trung bình cộng và bài toán rút về đơn vị dạng cơ bản.",
                    "Giải quyết được bài toán thực tế phức hợp kết hợp tìm số trung bình cộng hoặc rút về đơn vị."),
                I(T, "Thống kê và xác suất", "Dãy số liệu thống kê; Biểu đồ cột; Số lần xuất hiện của một sự kiện", "24-25",
                    "Đọc được số liệu trên dãy số liệu thống kê và biểu đồ cột đơn giản; nhận biết sự kiện xảy ra.",
                    "Sắp xếp, phân loại số liệu thống kê; đếm số lần xuất hiện của sự kiện trong thực tế.",
                    "Đưa ra nhận xét, giải quyết vấn đề đơn giản dựa trên số liệu thống kê hoặc xác suất thực nghiệm."),
                I(S, "Khái niệm và tính chất phân số", "Khái niệm phân số; Phân số và phép chia; Tính chất cơ bản của phân số; Rút gọn phân số", "25-27",
                    "Đọc, viết được phân số; nhận biết tử số, mẫu số; nhận biết phân số chỉ phần đã tô màu.",
                    "Rút gọn được phân số về phân số tối giản; nhận biết hai phân số bằng nhau.",
                    "Vận dụng phân số để biểu thị số đo hoặc giải quyết tình huống thực tế."),
                I(S, "Quy đồng và so sánh phân số", "Quy đồng mẫu số các phân số; So sánh hai phân số cùng và khác mẫu số", "27",
                    "Nhận biết phân số cùng mẫu, khác mẫu; so sánh phân số với 1.",
                    "Quy đồng được mẫu số hai phân số; so sánh và sắp xếp các phân số theo thứ tự.",
                    "Vận dụng so sánh phân số để giải quyết bài toán thực tế so sánh phần ăn, số lượng.")
            };
        }

        private static List<MathTopicItem> Grade4FinalTerm2()
        {
            return new List<MathTopicItem>
            {
                I(S, "Phép cộng và phép trừ phân số", "Phép cộng phân số; Phép trừ phân số (cùng mẫu và khác mẫu)", "28-30",
                    "Nêu được quy tắc cộng, trừ hai phân số cùng mẫu và khác mẫu số.",
                    "Thực hiện thành thạo phép cộng, phép trừ hai phân số; tính giá trị biểu thức phân số.",
                    "Vận dụng phép cộng, phép trừ phân số để giải bài toán thực tế 2 bước tính."),
                I(S, "Phép nhân và phép chia phân số", "Phép nhân phân số; Phép chia phân số; Tìm phân số của một số", "30-32",
                    "Nêu được quy tắc nhân, chia hai phân số; nhận biết cách tìm phân số của một số.",
                    "Thực hiện thành thạo phép nhân, phép chia phân số; tìm phân số của một số cho trước.",
                    "Vận dụng bốn phép tính phân số và tìm phân số của một số để giải bài toán thực tế có lời văn."),
                I(S, "Bốn phép tính với số tự nhiên và phân số", "Số tự nhiên và bốn phép tính; Phân số và phép tính với phân số", "33-34",
                    "Thực hiện được phép tính với số tự nhiên và phân số mức độ cơ bản.",
                    "Tính giá trị biểu thức phối hợp; tính bằng cách thuận tiện với số tự nhiên và phân số.",
                    "Giải bài toán thực tế tổng hợp nhiều bước tính liên quan đến số và phép tính."),
                I(H, "Chu vi, diện tích và đơn vị đo đại lượng", "Diện tích, chu vi hình phẳng; Đơn vị đo khối lượng, diện tích, thời gian", "34-35",
                    "Nhận biết và nêu công thức tính chu vi, diện tích các hình đã học; bảng đơn vị đo.",
                    "Chuyển đổi số đo và tính chu vi, diện tích hình phẳng quen thuộc.",
                    "Vận dụng kiến thức hình học và đo lường để giải bài toán thực tế tổng hợp."),
                I(T, "Biểu đồ cột và khả năng xảy ra sự kiện", "Biểu đồ cột; Dãy số liệu; Khả năng xảy ra của một sự kiện", "35",
                    "Đọc hiểu bảng số liệu, biểu đồ cột; xác định khả năng xảy ra của sự kiện.",
                    "Thu thập, phân tích và biểu diễn số liệu trên biểu đồ cột đơn giản.",
                    "Giải quyết vấn đề thực tế dựa trên bảng số liệu hoặc biểu đồ thống kê."),
                I(S, "Giải bài toán có lời văn 2-3 bước tính", "Giải bài toán có lời văn tổng hợp cuối năm", "35",
                    "Nhận biết và phân tích được các dữ kiện trong bài toán thực tế.",
                    "Trình bày lời giải bài toán tổng hợp cuối năm 2 - 3 bước tính.",
                    "Vận dụng sáng tạo các kiến thức toán lớp 4 để giải bài toán thực tế nâng cao.")
            };
        }

        // ============================== LỚP 5 ==============================

        private static List<MathTopicItem> Grade5MidTerm1()
        {
            return new List<MathTopicItem>
            {
                I(S, "Phân số, phân số thập phân và phép tính", "Số tự nhiên và các phép tính với số tự nhiên; Phân số và phân số thập phân", "1-2",
                    "Đọc, viết số tự nhiên, phân số, phân số thập phân; xác định tử số và mẫu số.",
                    "Thực hiện được phép tính với số tự nhiên và phân số; chuyển phân số thành phân số thập phân.",
                    "Vận dụng các phép tính với số tự nhiên và phân số để giải quyết bài toán thực tế."),
                I(S, "Hỗn số và phép tính phân số", "Cộng, trừ hai phân số khác mẫu số; Hỗn số (khái niệm và chuyển đổi); Ôn tập hình học và đo lường", "3-4",
                    "Nhận biết khái niệm hỗn số (phần nguyên và phần phân số); quy tắc cộng trừ phân số khác mẫu số.",
                    "Thực hiện cộng, trừ hai phân số khác mẫu số; chuyển hỗn số thành phân số và ngược lại.",
                    "Vận dụng hỗn số và phép tính phân số để giải quyết bài toán thực tế quen thuộc."),
                I(S, "Khái niệm và cấu tạo số thập phân", "Khái niệm số thập phân; Cấu tạo phần nguyên, phần thập phân; Hàng của số thập phân", "4-5",
                    "Đọc, viết đúng số thập phân; nêu được cấu tạo phần nguyên, phần thập phân và tên các hàng.",
                    "Xác định được giá trị của từng chữ số theo hàng trong số thập phân; viết số thập phân bằng nhau.",
                    "Vận dụng cấu tạo số thập phân để viết số thập phân thỏa mãn điều kiện cho trước."),
                I(S, "So sánh và làm tròn số thập phân", "So sánh các số thập phân; Xếp thứ tự số thập phân; Làm tròn số thập phân", "5-6",
                    "Nhận biết số thập phân lớn hơn, bé hơn; nêu được quy tắc làm tròn số thập phân.",
                    "So sánh, sắp xếp được các số thập phân theo thứ tự; làm tròn được số thập phân theo yêu cầu.",
                    "Vận dụng so sánh và làm tròn số thập phân để giải quyết tình huống thực tế (ước lượng số đo, so sánh số liệu)."),
                I(S, "Viết số đo dưới dạng số thập phân", "Viết số đo độ dài, khối lượng, diện tích dưới dạng số thập phân", "5-6",
                    "Nhận biết quan hệ giữa các đơn vị đo đại lượng quen thuộc để viết dưới dạng số thập phân.",
                    "Viết được số đo độ dài, khối lượng, diện tích dưới dạng số thập phân và ngược lại.",
                    "Vận dụng chuyển đổi số đo dưới dạng số thập phân để giải quyết bài toán thực tế."),
                I(H, "Đơn vị đo diện tích", "Ki-lô-mét vuông, Héc-ta; Bảng đơn vị đo diện tích; Thực hành đo đại lượng", "7-8",
                    "Nhận biết các đơn vị đo diện tích ki-lô-mét vuông (km2), héc-ta (ha) và bảng đơn vị đo diện tích.",
                    "Chuyển đổi được giữa các đơn vị đo diện tích km2, ha, m2; thực hiện phép tính với số đo diện tích.",
                    "Vận dụng chuyển đổi và tính toán với số đo diện tích ha, km2 để giải bài toán thực tế."),
                I(S, "Phép cộng và phép trừ số thập phân", "Phép cộng số thập phân; Phép trừ số thập phân (đặt tính và tính)", "8-9",
                    "Nêu được quy tắc đặt tính rồi tính phép cộng, phép trừ số thập phân.",
                    "Đặt tính và tính đúng phép cộng, phép trừ hai hay nhiều số thập phân; tìm thành phần chưa biết.",
                    "Vận dụng phép cộng, phép trừ số thập phân và tính chất giao hoán, kết hợp để tính thuận tiện và giải bài toán thực tế."),
                I(S, "Phép nhân số thập phân", "Phép nhân số thập phân với số tự nhiên, nhân số thập phân với số thập phân", "9",
                    "Nêu được quy tắc nhân một số thập phân với số tự nhiên, nhân số thập phân với số thập phân.",
                    "Đặt tính và thực hiện đúng phép nhân số thập phân; tính nhẩm nhân với 10, 100, 1000...",
                    "Vận dụng phép nhân số thập phân để giải bài toán thực tế 2 - 3 bước tính.")
            };
        }

        private static List<MathTopicItem> Grade5FinalTerm1()
        {
            return new List<MathTopicItem>
            {
                I(S, "Phép chia số thập phân", "Chia số thập phân cho số tự nhiên; Chia STN cho STN thương là STP; Chia cho STP", "10",
                    "Nêu được quy tắc chia số thập phân cho số tự nhiên, chia cho số thập phân.",
                    "Đặt tính và thực hiện đúng phép chia số thập phân; tính nhẩm chia cho 10, 100, 1000... hoặc chia cho 0,1; 0,01...",
                    "Vận dụng phép chia số thập phân để giải quyết bài toán thực tế có lời văn."),
                I(S, "Bốn phép tính với số thập phân", "Nhân, chia STP với 10, 100, 1000... hoặc 0,1; 0,01...; Tính giá trị biểu thức thuận tiện", "11",
                    "Nhận biết thứ tự thực hiện các phép tính trong biểu thức chứa số thập phân.",
                    "Tính được giá trị của biểu thức số thập phân; vận dụng các tính chất để tính bằng cách thuận tiện.",
                    "Vận dụng bốn phép tính với số thập phân để giải quyết bài toán thực tế 2 - 3 bước tính."),
                I(H, "Hình tam giác và diện tích", "Hình tam giác: đáy, chiều cao; Công thức tính diện tích hình tam giác", "12",
                    "Nhận biết được đỉnh, đáy và đường cao tương ứng của hình tam giác; nêu công thức tính diện tích hình tam giác.",
                    "Tính được diện tích hình tam giác khi biết độ dài đáy và chiều cao cùng đơn vị đo.",
                    "Vận dụng công thức tính diện tích hình tam giác để giải bài toán thực tế (mảnh đất tam giác, cắt ghép hình)."),
                I(H, "Hình thang và diện tích", "Hình thang: hai đáy, cạnh bên, chiều cao; Công thức tính diện tích hình thang", "12-13",
                    "Nhận biết được hình thang, đáy lớn, đáy bé, cạnh bên và chiều cao hình thang; nêu công thức tính diện tích hình thang.",
                    "Tính được diện tích hình thang khi biết độ dài hai đáy và chiều cao cùng đơn vị đo.",
                    "Vận dụng công thức tính diện tích hình thang để giải bài toán thực tế (thửa ruộng hình thang, thiết kế hình thang)."),
                I(H, "Đường tròn, chu vi và diện tích hình tròn", "Đường tròn, hình tròn: tâm, bán kính, đường kính; Chu vi và diện tích hình tròn", "13-14",
                    "Nhận biết được tâm, bán kính, đường kính của hình tròn; nêu công thức tính chu vi và diện tích hình tròn.",
                    "Tính được chu vi và diện tích hình tròn khi biết bán kính hoặc đường kính.",
                    "Vận dụng công thức chu vi, diện tích hình tròn để giải bài toán thực tế (bánh xe lăn, bồn hoa hình tròn)."),
                I(H, "Chu vi và diện tích các hình phẳng", "Thực hành đo, vẽ, lắp ghép tạo hình; Chu vi và diện tích các hình phẳng", "14, 17",
                    "Nhận dạng được các hình phẳng đã học (tam giác, thang, tròn, chữ nhật, vuông).",
                    "Vẽ và tính được chu vi, diện tích của các hình ghép từ các hình phẳng quen thuộc.",
                    "Giải quyết được vấn đề thực tế liên quan đến chu vi, diện tích hình phẳng phức hợp."),
                I(S, "Bốn phép tính với số thập phân và đo lường", "Số thập phân, các phép tính với số thập phân và đo lường tổng hợp", "15-18",
                    "Đọc, viết, so sánh số thập phân; thực hiện tính nhẩm, đặt tính đơn giản.",
                    "Giải bài toán tổng hợp học kỳ I liên quan đến bốn phép tính số thập phân và đo lường.",
                    "Vận dụng kiến thức tổng hợp học kì I để giải bài toán thực tế nhiều bước tính.")
            };
        }

        private static List<MathTopicItem> Grade5MidTerm2()
        {
            return new List<MathTopicItem>
            {
                I(S, "Tỉ số và tỉ số phần trăm", "Tỉ số; Tỉ số phần trăm; Tỉ lệ bản đồ và ứng dụng", "19",
                    "Nhận biết khái niệm tỉ số, tỉ số phần trăm và tỉ lệ bản đồ.",
                    "Tìm được tỉ số của hai số; đọc và tính khoảng cách thực tế dựa vào tỉ lệ bản đồ.",
                    "Giải bài toán thực tế liên quan đến tỉ số và tỉ lệ bản đồ."),
                I(S, "Bài toán về tỉ số", "Tìm hai số khi biết tổng và tỉ số; Tìm hai số khi biết hiệu và tỉ số", "19-20",
                    "Nhận biết bài toán tìm hai số khi biết tổng - tỉ hoặc hiệu - tỉ.",
                    "Giải thành thạo bài toán tìm hai số khi biết tổng - tỉ, hiệu - tỉ với các số liệu quen thuộc.",
                    "Vận dụng bài toán tổng - tỉ, hiệu - tỉ để giải quyết tình huống thực tế phức hợp."),
                I(S, "Giải toán về tỉ số phần trăm", "Tìm tỉ số phần trăm của hai số; Tìm giá trị phần trăm của một số; Máy tính cầm tay", "20-21",
                    "Nêu được cách tìm tỉ số phần trăm của hai số; cách tìm giá trị phần trăm của một số.",
                    "Thực hiện tìm tỉ số phần trăm của hai số, tìm giá trị phần trăm; sử dụng máy tính cầm tay để tính toán.",
                    "Vận dụng tỉ số phần trăm để giải bài toán thực tế về mua bán, giảm giá, lãi suất."),
                I(H, "Thể tích và đơn vị đo thể tích", "Thể tích của một hình; Xăng-ti-mét khối, đề-xi-mét khối, mét khối; Hình khai triển", "22-23",
                    "Nhận biết khái niệm thể tích; nhận biết các đơn vị đo thể tích cm3, dm3, m3 và hình khai triển.",
                    "Chuyển đổi đúng giữa các đơn vị đo thể tích; thực hiện phép tính với số đo thể tích.",
                    "Vận dụng chuyển đổi đơn vị đo thể tích để giải quyết tình huống thực tế."),
                I(H, "Diện tích hình hộp chữ nhật và hình lập phương", "Diện tích xung quanh và diện tích toàn phần hình hộp chữ nhật, hình lập phương", "24",
                    "Nêu công thức tính diện tích xung quanh, toàn phần hình hộp chữ nhật và hình lập phương.",
                    "Tính được diện tích xung quanh, toàn phần của hình hộp chữ nhật, hình lập phương theo kích thước cho trước.",
                    "Vận dụng tính diện tích xung quanh, toàn phần để giải bài toán thực tế (sơn tường, dán hộp)."),
                I(H, "Thể tích hình hộp chữ nhật và hình lập phương", "Thể tích hình hộp chữ nhật, hình lập phương; Thực hành ước lượng thể tích", "25",
                    "Nêu công thức tính thể tích hình hộp chữ nhật và hình lập phương.",
                    "Tính được thể tích hình hộp chữ nhật, hình lập phương khi biết kích thước.",
                    "Vận dụng tính thể tích để giải bài toán thực tế liên quan đến dung tích bể nước, thùng chứa."),
                I(S, "Số đo thời gian", "Các đơn vị đo thời gian; Cộng, trừ, nhân, chia số đo thời gian", "26-27",
                    "Nhận biết các đơn vị đo thời gian và mối quan hệ giữa chúng.",
                    "Thực hiện thành thạo phép cộng, trừ, nhân, chia số đo thời gian với một số.",
                    "Vận dụng phép tính số đo thời gian để giải bài toán thực tế có lời văn.")
            };
        }

        private static List<MathTopicItem> Grade5FinalTerm2()
        {
            return new List<MathTopicItem>
            {
                I(S, "Vận tốc của chuyển động đều", "Khái niệm vận tốc; Công thức tính vận tốc (v = s : t)", "28",
                    "Nhận biết khái niệm vận tốc và đơn vị đo vận tốc (km/h, m/s, m/phút).",
                    "Tính được vận tốc của một chuyển động đều khi biết quãng đường và thời gian.",
                    "Giải bài toán thực tế tìm vận tốc của người hoặc phương tiện giao thông."),
                I(S, "Quãng đường và thời gian trong chuyển động đều", "Quãng đường (s = v x t); Thời gian (t = s : v); Hai chuyển động ngược chiều, cùng chiều", "28-29",
                    "Nêu được công thức tính quãng đường và thời gian trong chuyển động đều.",
                    "Tính được quãng đường, thời gian; giải bài toán hai chuyển động ngược chiều gặp nhau, cùng chiều đuổi nhau.",
                    "Vận dụng chuyển động đều để giải quyết tình huống giao thông thực tế phức hợp."),
                I(T, "Thống kê và xác suất", "Thu thập, phân loại số liệu; Biểu đồ hình quạt tròn; Tỉ số số lần lặp sự kiện", "30-31",
                    "Đọc và phân tích thông tin từ biểu đồ hình quạt tròn; nhận biết kết quả sự kiện.",
                    "Thu thập, phân loại, sắp xếp số liệu; tính tỉ số số lần lặp của sự kiện.",
                    "Đưa ra nhận xét, giải quyết vấn đề đơn giản dựa trên biểu đồ hình quạt tròn."),
                I(S, "Bốn phép tính phối hợp và tính thuận tiện", "Số tự nhiên, phân số, số thập phân; Bốn phép tính phối hợp; Tính thuận tiện", "31-32",
                    "Thực hiện thành thạo các phép tính với số tự nhiên, phân số, số thập phân.",
                    "Tính giá trị biểu thức phối hợp; vận dụng tính chất để tính bằng cách thuận tiện.",
                    "Giải bài toán thực tế nhiều bước tính liên quan đến số và phép tính."),
                I(S, "Giải bài toán về tỉ số và tỉ số phần trăm", "Tỉ số, tỉ số phần trăm và các bài toán thực tế", "32-33",
                    "Nhận biết và giải các bài toán về tỉ số, tỉ số phần trăm quen thuộc.",
                    "Giải bài toán thực tế về tỉ số phần trăm (tiền vốn, tiền lãi, giá bán, giảm giá).",
                    "Vận dụng linh hoạt tỉ số và tỉ số phần trăm vào bài toán thực tế tổng hợp nâng cao."),
                I(H, "Diện tích, thể tích hình khối và bảng đơn vị đo", "Chu vi, diện tích hình phẳng; Diện tích, thể tích hình khối; Bảng đơn vị đo", "33-34",
                    "Nắm vững các công thức hình học và bảng đơn vị đo đại lượng đã học.",
                    "Tính chu vi, diện tích hình phẳng; diện tích, thể tích hình khối theo yêu cầu.",
                    "Giải quyết vấn đề thực tế phức hợp về hình học và đo lường."),
                I(S, "Giải bài toán về chuyển động đều", "Toán chuyển động đều và các bài toán thực tế tổng hợp", "34-35",
                    "Hệ thống hóa toàn bộ kiến thức toán lớp 5 và phương pháp giải các dạng toán điển hình.",
                    "Giải bài toán chuyển động đều và các bài toán tổng hợp cuối năm 3 bước tính.",
                    "Vận dụng sáng tạo các kiến thức toán tiểu học để giải quyết bài toán thực tế nâng cao.")
            };
        }
    }
}
