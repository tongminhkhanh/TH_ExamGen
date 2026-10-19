using System.Collections.Generic;

namespace TH_ExamGen.Core.MathModels
{
    public class MathExamPaper
    {
        public string MonHoc { get; set; } = "Toán";
        public int KhoiLop { get; set; } = 4;
        public int ThoiGianPhut { get; set; } = 40;
        public string Period { get; set; } = "Cuối Học kỳ II";
        public string TieuDe { get; set; } = "BÀI KIỂM TRA ĐỊNH KỲ CUỐI HỌC KỲ II";
        public List<MathQuestion> DanhSachCauHoi { get; set; } = new();
        public List<MathMatrixItem> DanhSachMaTran { get; set; } = new();

        /// <summary>
        /// Cảnh báo sau khi sinh đề (ví dụ: AI chưa khớp ma trận sau nhiều lần thử) — hiển thị cho
        /// giáo viên rà soát nhưng KHÔNG chặn xem trước / xuất Word. Được lưu kèm bản nháp.
        /// </summary>
        public List<string> CanhBaoKiemTra { get; set; } = new();

        public static List<MathMatrixItem> GetDefaultGrade4Matrix()
        {
            return new List<MathMatrixItem>
            {
                new MathMatrixItem
                {
                    SoThuTu = 1,
                    ChuDe = "Số và phép tính",
                    NoiDungKienThuc = "Khái niệm phân số",
                    YeuCauCanDat = "Nhận biết được khái niệm phân số, phân số chỉ phần đã tô màu của hình.",
                    MucDo = "M1",
                    HinhThuc = "TN",
                    Diem = 1.0,
                    CoHinhTikz = true
                },
                new MathMatrixItem
                {
                    SoThuTu = 2,
                    ChuDe = "Số và phép tính",
                    NoiDungKienThuc = "Phân số bằng nhau, rút gọn phân số",
                    YeuCauCanDat = "Nhận biết được hai phân số bằng nhau; rút gọn được phân số về phân số tối giản.",
                    MucDo = "M1",
                    HinhThuc = "TN",
                    Diem = 1.0,
                    CoHinhTikz = false
                },
                new MathMatrixItem
                {
                    SoThuTu = 3,
                    ChuDe = "Số và phép tính",
                    NoiDungKienThuc = "Quy đồng và so sánh phân số",
                    YeuCauCanDat = "So sánh được hai phân số cùng hoặc khác mẫu số; so sánh phân số với 1.",
                    MucDo = "M1",
                    HinhThuc = "TN",
                    Diem = 1.0,
                    CoHinhTikz = false
                },
                new MathMatrixItem
                {
                    SoThuTu = 4,
                    ChuDe = "Hình học và đo lường",
                    NoiDungKienThuc = "Hình bình hành và hình thoi",
                    YeuCauCanDat = "Nhận biết được đặc điểm cạnh, góc của hình bình hành hoặc hình thoi qua hình vẽ trực quan.",
                    MucDo = "M1",
                    HinhThuc = "TN",
                    Diem = 1.0,
                    CoHinhTikz = true
                },
                new MathMatrixItem
                {
                    SoThuTu = 5,
                    ChuDe = "Số và phép tính",
                    NoiDungKienThuc = "Phép nhân, chia phân số",
                    YeuCauCanDat = "Thực hiện được phép tính nhân hoặc chia phân số với số tự nhiên.",
                    MucDo = "M2",
                    HinhThuc = "TN",
                    Diem = 1.0,
                    CoHinhTikz = false
                },
                new MathMatrixItem
                {
                    SoThuTu = 6,
                    ChuDe = "Hình học và đo lường",
                    NoiDungKienThuc = "Đại lượng và đơn vị đo",
                    YeuCauCanDat = "Chuyển đổi và tính toán được với số đo khối lượng (tấn, tạ, yến, kg) hoặc diện tích (m², dm², cm²) (Đúng/Sai 2 ý a, b).",
                    MucDo = "M2",
                    HinhThuc = "TN",
                    Diem = 1.0,
                    CoHinhTikz = false
                },
                new MathMatrixItem
                {
                    SoThuTu = 7,
                    ChuDe = "Thống kê và xác suất",
                    NoiDungKienThuc = "Bảng số liệu thống kê",
                    YeuCauCanDat = "Đọc, xử lý và phân tích được thông tin từ bảng số liệu thống kê đơn giản (2 ý a, b).",
                    MucDo = "M2",
                    HinhThuc = "TN",
                    Diem = 1.0,
                    CoHinhTikz = false
                },
                new MathMatrixItem
                {
                    SoThuTu = 8,
                    ChuDe = "Số và phép tính",
                    NoiDungKienThuc = "Đặt tính rồi tính (Phép tính phân số)",
                    YeuCauCanDat = "Thực hiện tính đúng 4 phép tính cộng, trừ, nhân, chia phân số (4 ý a, b, c, d; mỗi ý 0.25đ).",
                    MucDo = "M2",
                    HinhThuc = "TL",
                    Diem = 1.0,
                    CoHinhTikz = false
                },
                new MathMatrixItem
                {
                    SoThuTu = 9,
                    ChuDe = "Số và phép tính",
                    NoiDungKienThuc = "Giải bài toán có lời văn về phân số",
                    YeuCauCanDat = "Vận dụng bài toán tìm phân số của một số để giải và trình bày được bài toán thực tế có lời văn 2-3 bước tính có sơ đồ.",
                    MucDo = "M3",
                    HinhThuc = "TL",
                    Diem = 1.0,
                    CoHinhTikz = true
                },
                new MathMatrixItem
                {
                    SoThuTu = 10,
                    ChuDe = "Số và phép tính",
                    NoiDungKienThuc = "Giải bài toán có lời văn 2-3 bước tính",
                    YeuCauCanDat = "Vận dụng kiến thức phân số, hình học kết hợp để giải và trình bày được bài toán thực tế nâng cao 2-3 bước tính.",
                    MucDo = "M3",
                    HinhThuc = "TL",
                    Diem = 1.0,
                    CoHinhTikz = false
                }
            };
        }

        public static MathExamPaper GetSampleGrade4Paper()
        {
            var paper = new MathExamPaper
            {
                MonHoc = "Toán",
                KhoiLop = 4,
                ThoiGianPhut = 40,
                TieuDe = "BÀI KIỂM TRA ĐỊNH KỲ CUỐI HỌC KỲ II",
                DanhSachMaTran = GetDefaultGrade4Matrix()
            };

            paper.DanhSachCauHoi = new List<MathQuestion>
            {
                new MathQuestion
                {
                    SoThuTu = 1,
                    ChuDe = "Số và phép tính",
                    YeuCauCanDat = "Nhận biết khái niệm phân số, phân số chỉ phần đã tô màu của hình.",
                    MucDo = "M1",
                    LoaiCauHoi = "TN_4LuaChon",
                    Diem = 1.0,
                    NoiDung = "Phân số chỉ phần đã tô màu của hình tròn bên là:",
                    DapAnA = @"\frac{3}{8}",
                    DapAnB = @"\frac{5}{8}",
                    DapAnC = @"\frac{1}{2}",
                    DapAnD = @"\frac{3}{5}",
                    DapAnDung = "A",
                    CoHinhTikz = true,
                    MaTikz = @"\begin{tikzpicture}[scale=0.85]
  % 1. Tô màu các múi phân số trước (3/8)
  \fill[blue!30] (0,0) -- (0:1.5cm) arc (0:135:1.5cm) -- cycle;
  % 2. Vẽ nan hoa chia 8 phần (vẽ sau để nét nan hoa luôn sắc nét đè lên múi màu)
  \foreach \ang in {0, 45, 90, 135, 180, 225, 270, 315} {
    \draw[thick] (0,0) -- (\ang:1.5cm);
  }
  % 3. Đường tròn viền ngoài
  \draw[thick] (0,0) circle (1.5cm);
  % 4. Tâm hình tròn
  \fill (0,0) circle (1.5pt);
\end{tikzpicture}"
                },
                new MathQuestion
                {
                    SoThuTu = 2,
                    ChuDe = "Số và phép tính",
                    YeuCauCanDat = "Nhận biết hai phân số bằng nhau, rút gọn phân số.",
                    MucDo = "M1",
                    LoaiCauHoi = "TN_4LuaChon",
                    Diem = 1.0,
                    NoiDung = @"Trong các phân số sau, phân số nào bằng phân số \frac{2}{3}?",
                    DapAnA = @"\frac{4}{6}",
                    DapAnB = @"\frac{5}{6}",
                    DapAnC = @"\frac{6}{8}",
                    DapAnD = @"\frac{3}{2}",
                    DapAnDung = "A"
                },
                new MathQuestion
                {
                    SoThuTu = 3,
                    ChuDe = "Số và phép tính",
                    YeuCauCanDat = "So sánh hai phân số cùng mẫu số.",
                    MucDo = "M1",
                    LoaiCauHoi = "TN_4LuaChon",
                    Diem = 1.0,
                    NoiDung = @"So sánh hai phân số \frac{4}{7} và \frac{5}{7}:",
                    DapAnA = @"\frac{4}{7} < \frac{5}{7}",
                    DapAnB = @"\frac{4}{7} > \frac{5}{7}",
                    DapAnC = @"\frac{4}{7} = \frac{5}{7}",
                    DapAnD = "Không so sánh được",
                    DapAnDung = "A"
                },
                new MathQuestion
                {
                    SoThuTu = 4,
                    ChuDe = "Hình học và đo lường",
                    YeuCauCanDat = "Nhận biết đặc điểm cạnh của hình bình hành.",
                    MucDo = "M1",
                    LoaiCauHoi = "TN_4LuaChon",
                    Diem = 1.0,
                    NoiDung = "Cho hình bình hành ABCD như hình vẽ bên. Cặp cạnh song song và bằng nhau là:",
                    DapAnA = "AB và CD",
                    DapAnB = "AB và BC",
                    DapAnC = "AD và AB",
                    DapAnD = "BC và CD",
                    DapAnDung = "A",
                    CoHinhTikz = true,
                    MaTikz = @"\begin{tikzpicture}[scale=0.8]
  \coordinate (A) at (1.5, 2.5);
  \coordinate (B) at (5.5, 2.5);
  \coordinate (C) at (4.0, 0);
  \coordinate (D) at (0, 0);
  \draw[thick] (A) -- (B) -- (C) -- (D) -- cycle;
  \node[above left] at (A) {A};
  \node[above right] at (B) {B};
  \node[below right] at (C) {C};
  \node[below left] at (D) {D};
\end{tikzpicture}"
                },
                new MathQuestion
                {
                    SoThuTu = 5,
                    ChuDe = "Số và phép tính",
                    YeuCauCanDat = "Nhân phân số với số tự nhiên.",
                    MucDo = "M2",
                    LoaiCauHoi = "TN_4LuaChon",
                    Diem = 1.0,
                    NoiDung = @"Kết quả của phép tính \frac{3}{5} \times 4 là:",
                    DapAnA = @"\frac{12}{5}",
                    DapAnB = @"\frac{3}{20}",
                    DapAnC = @"\frac{7}{5}",
                    DapAnD = @"\frac{12}{20}",
                    DapAnDung = "A"
                },
                new MathQuestion
                {
                    SoThuTu = 6,
                    ChuDe = "Hình học và đo lường",
                    YeuCauCanDat = "Chuyển đổi số đo khối lượng, diện tích.",
                    MucDo = "M2",
                    LoaiCauHoi = "TN_DungSai",
                    Diem = 1.0,
                    NoiDung = "Đúng ghi Đ, sai ghi S vào ô vuông:\na) 3 tấn 5 tạ = 3500 kg\nb) 4 m² 25 dm² = 425 dm²",
                    DapAnDung = "a) Đ ; b) Đ"
                },
                new MathQuestion
                {
                    SoThuTu = 7,
                    ChuDe = "Thống kê và xác suất",
                    YeuCauCanDat = "Đọc và xử lý số liệu từ bảng thống kê.",
                    MucDo = "M2",
                    LoaiCauHoi = "TN_TraLoiNgan",
                    Diem = 1.0,
                    NoiDung = "Bảng số liệu sau đây cho biết số cuốn sách cửa hàng bán được qua các tháng:\n| Tháng | Tháng Ba | Tháng Tư | Tháng Năm | Tháng Sáu | Tháng Bảy |\n|---|---|---|---|---|---|\n| Số sách | 250 cuốn | 180 cuốn | 400 cuốn | 350 cuốn | 420 cuốn |\na) Cửa hàng bán được nhiều sách nhất vào tháng nào?\nb) Tháng Ba và tháng Sáu bán được tất cả bao nhiêu cuốn sách?",
                    DapAnDung = "a) Tháng Bảy (420 cuốn) ; b) 250 + 350 = 600 cuốn sách"
                },
                new MathQuestion
                {
                    SoThuTu = 8,
                    ChuDe = "Số và phép tính",
                    YeuCauCanDat = "Thực hiện phép tính cộng, trừ, nhân, chia với phân số.",
                    MucDo = "M2",
                    LoaiCauHoi = "TL_TinhToan",
                    Diem = 1.0,
                    NoiDung = @"Tính:
a) \frac{3}{4} + \frac{2}{5}
b) \frac{7}{8} - \frac{1}{4}
c) \frac{4}{9} \times \frac{3}{8}
d) \frac{5}{6} : \frac{2}{3}",
                    SoDongOly = 4,
                    CacBuocGiai = new List<MathStepSolution>
                    {
                        new MathStepSolution { Buoc = 1, NoiDung = @"a) \frac{3}{4} + \frac{2}{5} = \frac{15}{20} + \frac{8}{20} = \frac{23}{20}", Diem = 0.25 },
                        new MathStepSolution { Buoc = 2, NoiDung = @"b) \frac{7}{8} - \frac{1}{4} = \frac{7}{8} - \frac{2}{8} = \frac{5}{8}", Diem = 0.25 },
                        new MathStepSolution { Buoc = 3, NoiDung = @"c) \frac{4}{9} \times \frac{3}{8} = \frac{12}{72} = \frac{1}{6}", Diem = 0.25 },
                        new MathStepSolution { Buoc = 4, NoiDung = @"d) \frac{5}{6} : \frac{2}{3} = \frac{5}{6} \times \frac{3}{2} = \frac{15}{12} = \frac{5}{4}", Diem = 0.25 }
                    }
                },
                new MathQuestion
                {
                    SoThuTu = 9,
                    ChuDe = "Số và phép tính",
                    YeuCauCanDat = "Giải bài toán tìm hai số khi biết tổng và tỉ số của hai số đó.",
                    MucDo = "M3",
                    LoaiCauHoi = "TL_LoiVan",
                    Diem = 1.0,
                    NoiDung = @"Một cửa hàng có 150 kg gạo tẻ và gạo nếp. Biết số gạo nếp bằng \frac{2}{3} số gạo tẻ. Hỏi cửa hàng có bao nhiêu ki-lô-gam gạo mỗi loại?",
                    CoHinhTikz = true,
                    MaTikz = @"\begin{tikzpicture}[scale=0.75]
  \node[anchor=east] at (0, 1.5) {Gạo nếp:};
  \draw[thick] (0.2, 1.5) -- (4.2, 1.5);
  \foreach \x in {0.2, 2.2, 4.2} {
    \draw[thick] (\x, 1.35) -- (\x, 1.65);
  }
  \node[anchor=east] at (0, 0.4) {Gạo tẻ:};
  \draw[thick] (0.2, 0.4) -- (6.2, 0.4);
  \foreach \x in {0.2, 2.2, 4.2, 6.2} {
    \draw[thick] (\x, 0.25) -- (\x, 0.55);
  }
  \draw[thick, decoration={brace, mirror, amplitude=6pt}, decorate] (0.2, 0.05) -- (6.2, 0.05) node[midway, below=8pt] {Tổng: 150 kg};
\end{tikzpicture}",
                    CacBuocGiai = new List<MathStepSolution>
                    {
                        new MathStepSolution { Buoc = 1, NoiDung = "Tổng số phần bằng nhau là: 2 + 3 = 5 (phần)", Diem = 0.2 },
                        new MathStepSolution { Buoc = 2, NoiDung = "Số ki-lô-gam gạo nếp là: 150 : 5 × 2 = 60 (kg)", Diem = 0.4 },
                        new MathStepSolution { Buoc = 3, NoiDung = "Số ki-lô-gam gạo tẻ là: 150 - 60 = 90 (kg)", Diem = 0.3 },
                        new MathStepSolution { Buoc = 4, NoiDung = "Đáp số: Gạo nếp: 60 kg; Gạo tẻ: 90 kg", Diem = 0.1 }
                    }
                },
                new MathQuestion
                {
                    SoThuTu = 10,
                    ChuDe = "Số và phép tính",
                    YeuCauCanDat = "Vận dụng kiến thức phân số và hình học giải bài toán thực tế.",
                    MucDo = "M3",
                    LoaiCauHoi = "TL_LoiVan",
                    Diem = 1.0,
                    NoiDung = @"Một mảnh đất hình chữ nhật có chu vi là 160 m, chiều rộng bằng \frac{3}{5} chiều dài. Tính diện tích của mảnh đất đó.",
                    CacBuocGiai = new List<MathStepSolution>
                    {
                        new MathStepSolution { Buoc = 1, NoiDung = "Nửa chu vi mảnh đất là: 160 : 2 = 80 (m)", Diem = 0.2 },
                        new MathStepSolution { Buoc = 2, NoiDung = "Tổng số phần bằng nhau là: 3 + 5 = 8 (phần)", Diem = 0.2 },
                        new MathStepSolution { Buoc = 3, NoiDung = "Chiều rộng mảnh đất là: 80 : 8 × 3 = 30 (m)", Diem = 0.2 },
                        new MathStepSolution { Buoc = 4, NoiDung = "Chiều dài mảnh đất là: 80 - 30 = 50 (m)", Diem = 0.2 },
                        new MathStepSolution { Buoc = 5, NoiDung = "Diện tích mảnh đất là: 50 × 30 = 1500 (m²)\nĐáp số: 1500 m²", Diem = 0.2 }
                    }
                }
            };

            return paper;
        }

        public static MathExamPaper GetSampleGrade5Paper()
        {
            var paper = new MathExamPaper
            {
                MonHoc = "Toán",
                KhoiLop = 5,
                ThoiGianPhut = 40,
                TieuDe = "BÀI KIỂM TRA ĐỊNH KỲ CUỐI HỌC KỲ II",
                DanhSachMaTran = MathCurriculumBank.GetGrade5FinalTerm2Matrix()
            };

            paper.DanhSachCauHoi = new List<MathQuestion>
            {
                new MathQuestion
                {
                    SoThuTu = 1,
                    ChuDe = "Số và phép tính",
                    YeuCauCanDat = "Nhận biết hàng và giá trị của chữ số trong số thập phân.",
                    MucDo = "M1",
                    LoaiCauHoi = "TN_4LuaChon",
                    Diem = 1.0,
                    NoiDung = "Chữ số 7 trong số thập phân 12,574 có giá trị là:",
                    DapAnA = @"\frac{7}{10}",
                    DapAnB = @"\frac{7}{100}",
                    DapAnC = @"\frac{7}{1000}",
                    DapAnD = "70",
                    DapAnDung = "B"
                },
                new MathQuestion
                {
                    SoThuTu = 2,
                    ChuDe = "Số và phép tính",
                    YeuCauCanDat = "Chuyển đổi phân số thành tỉ số phần trăm.",
                    MucDo = "M1",
                    LoaiCauHoi = "TN_4LuaChon",
                    Diem = 1.0,
                    NoiDung = @"Phân số \frac{3}{4} viết dưới dạng tỉ số phần trăm là:",
                    DapAnA = "75%",
                    DapAnB = "34%",
                    DapAnC = "43%",
                    DapAnD = "30%",
                    DapAnDung = "A"
                },
                new MathQuestion
                {
                    SoThuTu = 3,
                    ChuDe = "Hình học và đo lường",
                    YeuCauCanDat = "Tính thể tích của hình lập phương.",
                    MucDo = "M1",
                    LoaiCauHoi = "TN_4LuaChon",
                    Diem = 1.0,
                    NoiDung = "Một hình lập phương có cạnh 4 cm. Thể tích của hình lập phương đó là:",
                    DapAnA = "16 cm³",
                    DapAnB = "64 cm²",
                    DapAnC = "64 cm³",
                    DapAnD = "96 cm³",
                    DapAnDung = "C"
                },
                new MathQuestion
                {
                    SoThuTu = 4,
                    ChuDe = "Hình học và đo lường",
                    YeuCauCanDat = "Tính diện tích hình tam giác khi biết độ dài đáy và chiều cao.",
                    MucDo = "M1",
                    LoaiCauHoi = "TN_4LuaChon",
                    Diem = 1.0,
                    NoiDung = "Cho hình tam giác ABC có độ dài đáy BC = 8 cm và chiều cao AH = 5 cm như hình vẽ bên. Diện tích hình tam giác ABC là:",
                    DapAnA = "20 cm²",
                    DapAnB = "40 cm²",
                    DapAnC = "26 cm²",
                    DapAnD = "13 cm²",
                    DapAnDung = "A",
                    CoHinhTikz = true,
                    MaTikz = @"\begin{tikzpicture}[scale=0.8]
  \coordinate (B) at (0, 0);
  \coordinate (C) at (5, 0);
  \coordinate (H) at (2, 0);
  \coordinate (A) at (2, 3);
  \draw[thick] (A) -- (B) -- (C) -- cycle;
  \draw[dashed, thick] (A) -- (H);
  \draw (1.8, 0) -- (1.8, 0.2) -- (2, 0.2);
  \node[above] at (A) {A};
  \node[below left] at (B) {B};
  \node[below right] at (C) {C};
  \node[below] at (H) {H};
  \node[left] at (2, 1.5) {5 cm};
  \node[below] at (2.5, -0.3) {8 cm};
\end{tikzpicture}"
                },
                new MathQuestion
                {
                    SoThuTu = 5,
                    ChuDe = "Số và phép tính",
                    YeuCauCanDat = "Tính quãng đường trong bài toán chuyển động đều.",
                    MucDo = "M2",
                    LoaiCauHoi = "TN_4LuaChon",
                    Diem = 1.0,
                    NoiDung = "Một người đi xe máy với vận tốc 42 km/giờ trong thời gian 2 giờ 30 phút. Quãng đường người đó đi được là:",
                    DapAnA = "84 km",
                    DapAnB = "105 km",
                    DapAnC = "95 km",
                    DapAnD = "126 km",
                    DapAnDung = "B"
                },
                new MathQuestion
                {
                    SoThuTu = 6,
                    ChuDe = "Hình học và đo lường",
                    YeuCauCanDat = "Chuyển đổi đơn vị đo thể tích, thời gian.",
                    MucDo = "M2",
                    LoaiCauHoi = "TN_DungSai",
                    Diem = 1.0,
                    NoiDung = "Đúng ghi Đ, sai ghi S vào ô vuông:\na) 2,5 m³ = 2500 dm³\nb) 3 giờ 15 phút = 3,15 giờ",
                    DapAnDung = "a) Đ ; b) S"
                },
                new MathQuestion
                {
                    SoThuTu = 7,
                    ChuDe = "Thống kê và xác suất",
                    YeuCauCanDat = "Giải bài toán liên quan đến tỉ số phần trăm và bảng số liệu.",
                    MucDo = "M2",
                    LoaiCauHoi = "TN_TraLoiNgan",
                    Diem = 1.0,
                    NoiDung = "Một lớp học có 40 học sinh, trong đó có 24 học sinh nữ.\na) Tỉ số phần trăm của số học sinh nữ và số học sinh cả lớp là bao nhiêu?\nb) Số học sinh nam chiếm bao nhiêu phần trăm số học sinh cả lớp?",
                    DapAnDung = "a) 60% ; b) 40%"
                },
                new MathQuestion
                {
                    SoThuTu = 8,
                    ChuDe = "Số và phép tính",
                    YeuCauCanDat = "Thực hiện phép tính cộng, trừ, nhân, chia số thập phân.",
                    MucDo = "M2",
                    LoaiCauHoi = "TL_TinhToan",
                    Diem = 1.0,
                    NoiDung = "Đặt tính rồi tính:\na) 146,38 + 59,7\nb) 260,74 - 85,39\nc) 34,5 × 2,8\nd) 17,55 : 3,9",
                    SoDongOly = 4,
                    CacBuocGiai = new List<MathStepSolution>
                    {
                        new MathStepSolution { Buoc = 1, NoiDung = "a) 146,38 + 59,7 = 206,08", Diem = 0.25 },
                        new MathStepSolution { Buoc = 2, NoiDung = "b) 260,74 - 85,39 = 175,35", Diem = 0.25 },
                        new MathStepSolution { Buoc = 3, NoiDung = "c) 34,5 × 2,8 = 96,6", Diem = 0.25 },
                        new MathStepSolution { Buoc = 4, NoiDung = "d) 17,55 : 3,9 = 4,5", Diem = 0.25 }
                    }
                },
                new MathQuestion
                {
                    SoThuTu = 9,
                    ChuDe = "Hình học và đo lường",
                    YeuCauCanDat = "Giải bài toán thực tế liên quan đến diện tích hình thang.",
                    MucDo = "M3",
                    LoaiCauHoi = "TL_LoiVan",
                    Diem = 1.0,
                    NoiDung = @"Một thửa ruộng hình thang có đáy lớn 40 m, đáy bé bằng \frac{3}{5} đáy lớn và chiều cao là 25 m. Người ta trồng lúa trên thửa ruộng đó, trung bình cứ 100 m² thu hoạch được 65 kg thóc. Hỏi trên cả thửa ruộng người ta thu hoạch được bao nhiêu tạ thóc?",
                    CoHinhTikz = true,
                    MaTikz = @"\begin{tikzpicture}[scale=0.8]
  \coordinate (D) at (0, 0);
  \coordinate (C) at (5, 0);
  \coordinate (H) at (1.5, 0);
  \coordinate (A) at (1.5, 2.5);
  \coordinate (B) at (4, 2.5);
  \draw[thick] (A) -- (B) -- (C) -- (D) -- cycle;
  \draw[dashed, thick] (A) -- (H);
  \draw (1.3, 0) -- (1.3, 0.2) -- (1.5, 0.2);
  \node[above left] at (A) {A};
  \node[above right] at (B) {B};
  \node[below right] at (C) {C};
  \node[below left] at (D) {D};
  \node[below] at (H) {H};
  \node[above] at (2.75, 2.5) {24 m};
  \node[below] at (2.5, -0.3) {40 m};
  \node[left] at (1.5, 1.25) {25 m};
\end{tikzpicture}",
                    CacBuocGiai = new List<MathStepSolution>
                    {
                        new MathStepSolution { Buoc = 1, NoiDung = "Đáy bé của thửa ruộng là: 40 × 3/5 = 24 (m)", Diem = 0.25 },
                        new MathStepSolution { Buoc = 2, NoiDung = "Diện tích thửa ruộng là: (40 + 24) × 25 : 2 = 800 (m²)", Diem = 0.25 },
                        new MathStepSolution { Buoc = 3, NoiDung = "Số ki-lô-gam thóc thu hoạch được là: 800 : 100 × 65 = 520 (kg)", Diem = 0.25 },
                        new MathStepSolution { Buoc = 4, NoiDung = "Đổi 520 kg = 5,2 tạ. Đáp số: 5,2 tạ thóc", Diem = 0.25 }
                    }
                },
                new MathQuestion
                {
                    SoThuTu = 10,
                    ChuDe = "Số và phép tính",
                    YeuCauCanDat = "Giải bài toán chuyển động đều hai vật chuyển động ngược chiều.",
                    MucDo = "M3",
                    LoaiCauHoi = "TL_LoiVan",
                    Diem = 1.0,
                    NoiDung = "Hai tỉnh A và B cách nhau 180 km. Cùng một lúc, một ô tô đi từ A đến B với vận tốc 54 km/giờ và một xe máy đi từ B về A với vận tốc 36 km/giờ.\na) Sau bao lâu hai xe gặp nhau?\nb) Chỗ gặp nhau cách A bao nhiêu ki-lô-mét?",
                    CacBuocGiai = new List<MathStepSolution>
                    {
                        new MathStepSolution { Buoc = 1, NoiDung = "Tổng vận tốc của hai xe là: 54 + 36 = 90 (km/giờ)", Diem = 0.25 },
                        new MathStepSolution { Buoc = 2, NoiDung = "Thời gian để hai xe gặp nhau là: 180 : 90 = 2 (giờ)", Diem = 0.25 },
                        new MathStepSolution { Buoc = 3, NoiDung = "Chỗ gặp nhau cách A là: 54 × 2 = 108 (km)", Diem = 0.25 },
                        new MathStepSolution { Buoc = 4, NoiDung = "Đáp số: a) 2 giờ ; b) 108 km", Diem = 0.25 }
                    }
                }
            };

            return paper;
        }
    }
}
