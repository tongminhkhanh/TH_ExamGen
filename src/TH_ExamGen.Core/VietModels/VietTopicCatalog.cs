using System.Collections.Generic;
using TH_ExamGen.Core.Curriculum;

namespace TH_ExamGen.Core.VietModels
{
    /// <summary>
    /// Một mục trong danh mục chủ điểm/đơn vị kiến thức Tiếng Việt (SGK Kết nối tri thức với cuộc sống).
    /// Tuan: khoảng tuần dự kiến trong phân phối chương trình (bản nháp, có thể chỉnh sửa trực tiếp tại đây).
    /// </summary>
    public class VietTopicItem : ICurriculumTopicItem
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
    }

    /// <summary>
    /// Danh mục chủ điểm đọc hiểu + đơn vị kiến thức Tiếng Việt lớp 4 - 5 theo từng đợt kiểm tra (SGK Kết nối tri thức).
    /// Dùng cho chế độ "AI tự chọn chủ đề": AI CHỈ được chọn nội dung nằm trong danh mục này.
    /// Chủ điểm đọc hiểu được đối chiếu theo các nguồn chính thức của Nhà xuất bản Giáo dục Việt Nam:
    /// https://nxbgd.vn/ và https://hanhtrangso.nxbgd.vn/.
    /// Các tên chủ điểm theo từng học kỳ là danh sách đã được xác nhận; các khoảng tuần bên dưới được giữ
    /// theo catalog hiện có và chỉ là tạm tính, chờ rà soát lại phân phối bài học theo từng trường.
    /// Việc chia các chủ điểm trong cùng một học kỳ giữa kỳ/cuối kỳ được xếp theo thứ tự danh sách;
    /// cần đối chiếu lại với phân phối bài học khi có bảng tuần chính thức.
    /// </summary>
    public static class VietTopicCatalog
    {
        private const string D = "Đọc hiểu văn bản";
        private const string K = "Kiến thức Tiếng Việt";

        private static VietTopicItem I(string mach, string chuDe, string noiDung, string tuan)
        {
            return new VietTopicItem { MachKienThuc = mach, ChuDe = chuDe, NoiDung = noiDung, Tuan = tuan };
        }

        public static List<VietTopicItem> GetTopics(int khoiLop, string period)
        {
            CurriculumScopePolicy.ValidateCatalogRequest(khoiLop, period);
            List<VietTopicItem> topics;
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
                    ? "https://sachdientu.nxbgd.vn/thu-vien-so/doc-sach/sgv-tieng-viet-4-tap-1.4505203485"
                    : "https://sgk.olm.vn/thu-vien-so/doc-sach/shs-tieng-viet-4-tap-2.4505249180";
            return firstTerm
                ? "https://sachdientu.nxbgd.vn/thu-vien-so/doc-sach/sgv-tieng-viet-tap-1.4502984966"
                : "https://sachdientu.nxbgd.vn/thu-vien-so/doc-sach/sgv-tieng-viet-tap-2.4502985144";
        }

        // ============================== LỚP 4 ==============================

        private static List<VietTopicItem> Grade4MidTerm1()
        {
            return new List<VietTopicItem>
            {
                I(D, "Chủ điểm \"Mỗi người một vẻ\"", "Đọc hiểu truyện/thơ về nét riêng của mỗi người; nhận biết chi tiết, hình ảnh", "1-3"),
                I(D, "Chủ điểm \"Trải nghiệm và khám phá\"", "Đọc hiểu văn bản về trải nghiệm, khám phá thế giới xung quanh; hiểu ý chính", "3-6"),
                I(K, "Danh từ", "Danh từ (từ chỉ người, vật, thời gian, hiện tượng)", "1-4"),
                I(K, "Danh từ", "Danh từ chung và danh từ riêng", "4-6"),
                I(K, "Danh từ", "Quy tắc viết hoa danh từ riêng (tên người, tên địa lý Việt Nam và nước ngoài)", "6-7"),
                I(K, "Dấu câu", "Dấu gạch ngang (công dụng) và vận dụng đặt câu", "7-9"),
            };
        }

        private static List<VietTopicItem> Grade4FinalTerm1()
        {
            return new List<VietTopicItem>
            {
                I(D, "Chủ điểm \"Niềm vui sáng tạo\"", "Đọc hiểu văn bản về niềm vui sáng tạo, khám phá ý tưởng và cách thể hiện", "10-12"),
                I(D, "Chủ điểm \"Chắp cánh ước mơ\"", "Đọc hiểu văn bản về ước mơ, khát vọng", "13-15"),
                I(K, "Động từ", "Động từ (từ chỉ hoạt động, trạng thái)", "10-11"),
                I(K, "Tính từ", "Tính từ (đặc điểm, tính chất, màu sắc, hình dáng)", "11-13"),
                I(K, "Từ loại", "Phân loại danh từ - động từ - tính từ", "13-14"),
                I(K, "Nhân hóa", "Biện pháp nhân hóa (các cách) và vận dụng viết câu", "14-16"),
                I(K, "Dấu câu", "Dấu ngoặc kép (công dụng) và vận dụng", "16-18"),
            };
        }

        private static List<VietTopicItem> Grade4MidTerm2()
        {
            return new List<VietTopicItem>
            {
                I(D, "Chủ điểm \"Sống để yêu thương\"", "Đọc hiểu văn bản về yêu thương, sẻ chia", "19-21"),
                I(D, "Chủ điểm \"Uống nước nhớ nguồn\"", "Đọc hiểu văn bản về lòng biết ơn, truyền thống", "21-24"),
                I(K, "Câu", "Câu kể; dấu kết thúc câu", "19-20"),
                I(K, "Thành phần câu", "Chủ ngữ - Vị ngữ: khái niệm, đặc điểm (Ai? Làm gì? Thế nào? Là gì?)", "20-22"),
                I(K, "Thành phần câu", "Xác định Chủ ngữ - Vị ngữ trong câu; đặt câu có đủ CN - VN", "22-24"),
                I(K, "Viết hoa", "Quy tắc viết hoa tên cơ quan, tổ chức", "24-25"),
                I(K, "Tu từ", "So sánh, nhân hóa (ôn luyện, vận dụng)", "25-27"),
            };
        }

        private static List<VietTopicItem> Grade4FinalTerm2()
        {
            return new List<VietTopicItem>
            {
                I(D, "Chủ điểm \"Quê hương trong tôi\"", "Đọc hiểu văn bản về tình yêu quê hương", "28-30"),
                I(D, "Chủ điểm \"Vì một thế giới bình yên\"", "Đọc hiểu văn bản về hòa bình, bảo vệ môi trường", "30-32"),
                I(K, "Trạng ngữ", "Trạng ngữ chỉ nơi chốn, thời gian", "28-29"),
                I(K, "Trạng ngữ", "Trạng ngữ chỉ nguyên nhân, mục đích, phương tiện", "29-31"),
                I(K, "Câu", "Phân tích cấu tạo câu: Trạng ngữ - Chủ ngữ - Vị ngữ", "31-32"),
                I(K, "Dấu câu", "Dấu hai chấm, dấu ngoặc đơn (ôn cả gạch ngang, ngoặc kép)", "32-33"),
                I(K, "Ôn tập", "Ôn từ loại (danh từ, động từ, tính từ); so sánh, nhân hóa; vận dụng viết câu", "33-35"),
            };
        }

        // ============================== LỚP 5 ==============================

        private static List<VietTopicItem> Grade5MidTerm1()
        {
            return new List<VietTopicItem>
            {
                I(D, "Chủ điểm \"Thế giới tuổi thơ\"", "Đọc hiểu văn bản về thế giới tuổi thơ, kỉ niệm và tình bạn", "1-3"),
                I(D, "Chủ điểm \"Thiên nhiên kì thú\"", "Đọc hiểu văn bản về vẻ đẹp, điều kì thú của thiên nhiên", "3-6"),
                I(K, "Từ đồng nghĩa", "Từ đồng nghĩa (hoàn toàn, không hoàn toàn); lựa chọn từ đồng nghĩa thích hợp", "1-3"),
                I(K, "Từ trái nghĩa", "Từ trái nghĩa; đặt câu với cặp từ trái nghĩa", "3-5"),
                I(K, "Từ đồng âm", "Từ đồng âm (nhận diện)", "5-7"),
                I(K, "Từ đa nghĩa", "Từ đa nghĩa (khái niệm ban đầu)", "7-9"),
                I(K, "Dấu câu / Ngữ pháp", "Tác dụng của dấu gạch ngang, dấu ngoặc kép; xác định thành phần câu (Chủ ngữ - Vị ngữ)", "7-9"),
                I(K, "Hoạt động giao tiếp", "Vận dụng dùng từ đặt câu, viết câu theo chủ đề ước mơ, tuổi thơ", "7-9"),
            };
        }

        private static List<VietTopicItem> Grade5FinalTerm1()
        {
            return new List<VietTopicItem>
            {
                I(D, "Chủ điểm \"Hương sắc trăm miền\"", "Đọc hiểu văn bản về vẻ đẹp văn hóa, cảnh sắc các miền quê", "10-12"),
                I(D, "Chủ điểm \"Trên con đường học tập\"", "Đọc hiểu văn bản về hành trình học tập, rèn luyện và trưởng thành", "12-15"),
                I(D, "Chủ điểm \"Nghệ thuật muôn màu\"", "Đọc hiểu văn bản về nghệ thuật, cảm xúc và cách thể hiện cái đẹp", "15-18"),
                I(K, "Từ nhiều nghĩa", "Từ nhiều nghĩa: phân biệt nghĩa gốc - nghĩa chuyển; đặt câu", "10-12"),
                I(K, "Đại từ", "Đại từ xưng hô, đại từ nghi vấn, đại từ thay thế", "12-14"),
                I(K, "Kết từ", "Kết từ (quan hệ từ); cặp kết từ biểu thị quan hệ", "14-16"),
                I(K, "Ôn từ loại", "Danh từ, động từ, tính từ, đại từ, kết từ (ôn tập)", "16-17"),
                I(K, "Tu từ", "So sánh, nhân hóa (ôn luyện, vận dụng)", "17-18"),
            };
        }

        private static List<VietTopicItem> Grade5MidTerm2()
        {
            return new List<VietTopicItem>
            {
                I(D, "Chủ điểm \"Vẻ đẹp cuộc sống\"", "Đọc hiểu văn bản về những vẻ đẹp trong cuộc sống quanh em", "19-21"),
                I(D, "Chủ điểm \"Hương sắc trăm miền\"", "Đọc hiểu văn bản về vẻ đẹp văn hóa, cảnh sắc các miền quê", "21-24"),
                I(K, "Câu ghép", "Câu đơn và câu ghép; nhận biết câu ghép trong đoạn văn", "19-20"),
                I(K, "Nối vế câu ghép", "Nối các vế câu ghép trực tiếp bằng dấu câu (phẩy, chấm phẩy, hai chấm)", "20-22"),
                I(K, "Nối vế câu ghép", "Nối bằng kết từ/cặp kết từ (nguyên nhân - kết quả, điều kiện - kết quả, tương phản, tăng tiến)", "22-24"),
                I(K, "Câu ghép", "Phân tích cấu tạo câu ghép (CN - VN từng vế); đặt câu ghép", "24-25"),
                I(K, "Tu từ", "Điệp từ, điệp ngữ", "25-27"),
            };
        }

        private static List<VietTopicItem> Grade5FinalTerm2()
        {
            return new List<VietTopicItem>
            {
                I(D, "Chủ điểm \"Tiếp bước cha ông\"", "Đọc hiểu văn bản về truyền thống lịch sử, tình yêu đất nước", "28-30"),
                I(D, "Chủ điểm \"Thế giới của chúng ta\"", "Đọc hiểu văn bản về thế giới chung, trách nhiệm và khát vọng hòa bình", "30-32"),
                I(K, "Câu ghép", "Câu ghép; nối vế câu bằng cặp từ hô ứng (vừa... đã..., càng... càng...)", "28-29"),
                I(K, "Liên kết câu", "Phép lặp từ ngữ; phép thay thế từ ngữ", "29-31"),
                I(K, "Liên kết câu", "Phép dùng từ ngữ nối", "31-32"),
                I(K, "Dấu câu", "Dấu gạch ngang, dấu ngoặc kép, dấu hai chấm, dấu ngoặc đơn (ôn tập)", "32-33"),
                I(K, "Ôn tập tổng hợp", "Ôn từ loại và biện pháp tu từ (so sánh, nhân hóa, điệp từ/ngữ); viết đoạn văn 3-4 câu có liên kết", "33-35"),
            };
        }
    }
}
