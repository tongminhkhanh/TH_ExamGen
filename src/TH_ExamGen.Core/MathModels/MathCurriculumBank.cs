using System;
using System.Collections.Generic;
using System.Linq;
using TH_ExamGen.Core.Curriculum;

namespace TH_ExamGen.Core.MathModels
{
    public static class MathCurriculumBank
    {
        public static readonly string[] ExamPeriods = new[]
        {
            "Giữa Học kỳ I",
            "Cuối Học kỳ I",
            "Giữa Học kỳ II",
            "Cuối Học kỳ II"
        };

        public static readonly string[] Topics = new[]
        {
            "Số và phép tính",
            "Hình học và đo lường",
            "Thống kê và xác suất"
        };

        public static List<MathMatrixItem> GetMatrixByGradeAndPeriod(int grade, string period)
        {
            List<MathMatrixItem> matrix;
            if (grade == 5)
            {
                matrix = period switch
                {
                    "Giữa Học kỳ I" => GetGrade5MidTerm1Matrix(),
                    "Cuối Học kỳ I" => GetGrade5FinalTerm1Matrix(),
                    "Giữa Học kỳ II" => GetGrade5MidTerm2Matrix(),
                    _ => GetGrade5FinalTerm2Matrix()
                };
            }

            // Mặc định Khối 4
            else matrix = period switch
            {
                "Giữa Học kỳ I" => GetGrade4MidTerm1Matrix(),
                "Cuối Học kỳ I" => GetGrade4FinalTerm1Matrix(),
                "Giữa Học kỳ II" => GetGrade4MidTerm2Matrix(),
                _ => GetGrade4FinalTerm2Matrix()
            };

            return CurriculumMatrixMapper.AttachMathIds(grade, period, matrix);
        }

        #region Ma trận chuẩn Khối 4 (SGK Kết nối tri thức)

        public static List<MathMatrixItem> GetGrade4MidTerm1Matrix()
        {
            return new List<MathMatrixItem>
            {
                new MathMatrixItem { SoThuTu = 1, ChuDe = "Số và phép tính", NoiDungKienThuc = "Số có nhiều chữ số đến lớp triệu", YeuCauCanDat = "Đọc, viết được các số có nhiều chữ số đến lớp triệu; nhận biết được các hàng thuộc lớp đơn vị, lớp nghìn, lớp triệu.", MucDo = "M1", HinhThuc = "TN", Diem = 1.0, CoHinhTikz = false },
                new MathMatrixItem { SoThuTu = 2, ChuDe = "Số và phép tính", NoiDungKienThuc = "So sánh và làm tròn số tự nhiên", YeuCauCanDat = "Nhận biết được thứ tự các số trong dãy số tự nhiên; nêu được quy tắc làm tròn số đến hàng trăm nghìn.", MucDo = "M1", HinhThuc = "TN", Diem = 1.0, CoHinhTikz = false },
                new MathMatrixItem { SoThuTu = 3, ChuDe = "Số và phép tính", NoiDungKienThuc = "Số chẵn, số lẻ và biểu thức chứa chữ", YeuCauCanDat = "Nhận biết được số chẵn, số lẻ; nhận biết biểu thức chứa một chữ.", MucDo = "M1", HinhThuc = "TN", Diem = 1.0, CoHinhTikz = false },
                new MathMatrixItem { SoThuTu = 4, ChuDe = "Hình học và đo lường", NoiDungKienThuc = "Góc nhọn, góc tù, góc bẹt", YeuCauCanDat = "Nhận biết được góc nhọn, góc vuông, góc tù, góc bẹt qua hình vẽ trực quan.", MucDo = "M1", HinhThuc = "TN", Diem = 1.0, CoHinhTikz = true },
                new MathMatrixItem { SoThuTu = 5, ChuDe = "Hình học và đo lường", NoiDungKienThuc = "Đơn vị đo khối lượng (yến, tạ, tấn)", YeuCauCanDat = "Chuyển đổi được số đo khối lượng giữa yến, tạ, tấn và ki-lô-gam; thực hiện được phép tính với số đo khối lượng.", MucDo = "M2", HinhThuc = "TN", Diem = 1.0, CoHinhTikz = false },
                new MathMatrixItem { SoThuTu = 6, ChuDe = "Hình học và đo lường", NoiDungKienThuc = "Đơn vị đo thời gian (giây, thế kỉ)", YeuCauCanDat = "Chuyển đổi được số đo diện tích (m2, dm2, mm2) và thời gian (giây với phút; năm với thế kỉ).", MucDo = "M2", HinhThuc = "TN", Diem = 1.0, CoHinhTikz = false },
                new MathMatrixItem { SoThuTu = 7, ChuDe = "Số và phép tính", NoiDungKienThuc = "So sánh và làm tròn số tự nhiên", YeuCauCanDat = "So sánh, sắp xếp được các số có nhiều chữ số theo thứ tự; làm tròn được số đến hàng trăm nghìn.", MucDo = "M2", HinhThuc = "TN", Diem = 1.0, CoHinhTikz = false },
                new MathMatrixItem { SoThuTu = 8, ChuDe = "Số và phép tính", NoiDungKienThuc = "Đặt tính rồi tính (Cộng, trừ, nhân, chia)", YeuCauCanDat = "Đặt tính và tính được đúng 4 phép tính cộng, trừ, nhân, chia trong phạm vi đã học (nhân, chia với số có một chữ số) (4 ý a, b, c, d; mỗi ý 0.25đ).", MucDo = "M2", HinhThuc = "TL", Diem = 1.0, CoHinhTikz = false },
                new MathMatrixItem { SoThuTu = 9, ChuDe = "Hình học và đo lường", NoiDungKienThuc = "Đơn vị đo diện tích và thời gian thực tế", YeuCauCanDat = "Vận dụng chuyển đổi và tính toán với số đo diện tích, thời gian để giải bài toán thực tế 2 - 3 bước tính.", MucDo = "M3", HinhThuc = "TL", Diem = 1.0, CoHinhTikz = true },
                new MathMatrixItem { SoThuTu = 10, ChuDe = "Số và phép tính", NoiDungKienThuc = "Giải bài toán có ba bước tính", YeuCauCanDat = "Giải và trình bày được bài toán thực tế có ba bước tính liên quan đến các phép tính đã học.", MucDo = "M3", HinhThuc = "TL", Diem = 1.0, CoHinhTikz = false }
            };
        }

        public static List<MathMatrixItem> GetGrade4FinalTerm1Matrix()
        {
            return new List<MathMatrixItem>
            {
                new MathMatrixItem { SoThuTu = 1, ChuDe = "Số và phép tính", NoiDungKienThuc = "Cộng, trừ số có nhiều chữ số", YeuCauCanDat = "Nhận biết và thực hiện được phép cộng, phép trừ các số có nhiều chữ số không nhớ hoặc có nhớ 1 lần.", MucDo = "M1", HinhThuc = "TN", Diem = 1.0, CoHinhTikz = false },
                new MathMatrixItem { SoThuTu = 2, ChuDe = "Số và phép tính", NoiDungKienThuc = "Tính chất phép cộng", YeuCauCanDat = "Nhận biết được tính chất giao hoán và kết hợp của phép cộng số tự nhiên.", MucDo = "M1", HinhThuc = "TN", Diem = 1.0, CoHinhTikz = false },
                new MathMatrixItem { SoThuTu = 3, ChuDe = "Hình học và đo lường", NoiDungKienThuc = "Hai đường thẳng vuông góc, song song", YeuCauCanDat = "Nhận biết được hai đường thẳng vuông góc, hai đường thẳng song song qua hình vẽ trực quan.", MucDo = "M1", HinhThuc = "TN", Diem = 1.0, CoHinhTikz = true },
                new MathMatrixItem { SoThuTu = 4, ChuDe = "Hình học và đo lường", NoiDungKienThuc = "Hình bình hành và hình thoi", YeuCauCanDat = "Nhận biết được hình bình hành, hình thoi qua hình ảnh trực quan.", MucDo = "M1", HinhThuc = "TN", Diem = 1.0, CoHinhTikz = true },
                new MathMatrixItem { SoThuTu = 5, ChuDe = "Hình học và đo lường", NoiDungKienThuc = "Đo đại lượng", YeuCauCanDat = "Chuyển đổi và tính toán chính xác được với các số đo đại lượng khối lượng, diện tích.", MucDo = "M2", HinhThuc = "TN", Diem = 1.0, CoHinhTikz = false },
                new MathMatrixItem { SoThuTu = 6, ChuDe = "Số và phép tính", NoiDungKienThuc = "Tìm hai số khi biết tổng và hiệu", YeuCauCanDat = "Giải được bài toán tìm hai số khi biết tổng và hiệu với các số liệu trực tiếp.", MucDo = "M2", HinhThuc = "TN", Diem = 1.0, CoHinhTikz = false },
                new MathMatrixItem { SoThuTu = 7, ChuDe = "Thống kê và xác suất", NoiDungKienThuc = "Biểu đồ cột", YeuCauCanDat = "Đọc, xử lý và phân tích được thông tin từ biểu đồ cột đơn giản (2 ý a, b).", MucDo = "M2", HinhThuc = "TN", Diem = 1.0, CoHinhTikz = false },
                new MathMatrixItem { SoThuTu = 8, ChuDe = "Số và phép tính", NoiDungKienThuc = "Đặt tính rồi tính (Cộng, trừ, nhân, chia)", YeuCauCanDat = "Đặt tính và tính được đúng 4 phép tính cộng, trừ, nhân, chia các số tự nhiên đã học (4 ý a, b, c, d; mỗi ý 0.25đ).", MucDo = "M2", HinhThuc = "TL", Diem = 1.0, CoHinhTikz = false },
                new MathMatrixItem { SoThuTu = 9, ChuDe = "Số và phép tính", NoiDungKienThuc = "Tìm hai số khi biết tổng và hiệu", YeuCauCanDat = "Vận dụng bài toán tìm hai số khi biết tổng và hiệu để giải quyết tình huống thực tế phức hợp 2-3 bước tính.", MucDo = "M3", HinhThuc = "TL", Diem = 1.0, CoHinhTikz = true },
                new MathMatrixItem { SoThuTu = 10, ChuDe = "Hình học và đo lường", NoiDungKienThuc = "Diện tích hình bình hành, hình thoi", YeuCauCanDat = "Vận dụng công thức tính diện tích hình bình hành, hình thoi để giải bài toán thực tế có lời văn.", MucDo = "M3", HinhThuc = "TL", Diem = 1.0, CoHinhTikz = false }
            };
        }

        public static List<MathMatrixItem> GetGrade4MidTerm2Matrix()
        {
            return new List<MathMatrixItem>
            {
                new MathMatrixItem { SoThuTu = 1, ChuDe = "Số và phép tính", NoiDungKienThuc = "Khái niệm phân số", YeuCauCanDat = "Nhận biết được khái niệm phân số, phân số chỉ phần đã tô màu của hình.", MucDo = "M1", HinhThuc = "TN", Diem = 1.0, CoHinhTikz = true },
                new MathMatrixItem { SoThuTu = 2, ChuDe = "Số và phép tính", NoiDungKienThuc = "Phân số bằng nhau, rút gọn phân số", YeuCauCanDat = "Nhận biết được hai phân số bằng nhau; rút gọn được phân số về phân số tối giản.", MucDo = "M1", HinhThuc = "TN", Diem = 1.0, CoHinhTikz = false },
                new MathMatrixItem { SoThuTu = 3, ChuDe = "Số và phép tính", NoiDungKienThuc = "Quy đồng và so sánh phân số", YeuCauCanDat = "Quy đồng được mẫu số và so sánh hai phân số có cùng hoặc khác mẫu số.", MucDo = "M1", HinhThuc = "TN", Diem = 1.0, CoHinhTikz = false },
                new MathMatrixItem { SoThuTu = 4, ChuDe = "Hình học và đo lường", NoiDungKienThuc = "Hình bình hành và hình thoi", YeuCauCanDat = "Nhận biết được đặc điểm các cạnh song song, đối diện của hình bình hành hoặc hình thoi.", MucDo = "M1", HinhThuc = "TN", Diem = 1.0, CoHinhTikz = true },
                new MathMatrixItem { SoThuTu = 5, ChuDe = "Số và phép tính", NoiDungKienThuc = "Phép cộng và phép trừ phân số", YeuCauCanDat = "Thực hiện được phép cộng, phép trừ hai phân số cùng hoặc khác mẫu số.", MucDo = "M2", HinhThuc = "TN", Diem = 1.0, CoHinhTikz = false },
                new MathMatrixItem { SoThuTu = 6, ChuDe = "Số và phép tính", NoiDungKienThuc = "Phép nhân, chia phân số", YeuCauCanDat = "Thực hiện được phép nhân hoặc chia phân số với số tự nhiên (Đúng/Sai 2 ý a, b).", MucDo = "M2", HinhThuc = "TN", Diem = 1.0, CoHinhTikz = false },
                new MathMatrixItem { SoThuTu = 7, ChuDe = "Thống kê và xác suất", NoiDungKienThuc = "Bảng số liệu thống kê", YeuCauCanDat = "Đọc, xử lý và phân tích được số liệu từ bảng thống kê đơn giản (2 ý a, b).", MucDo = "M2", HinhThuc = "TN", Diem = 1.0, CoHinhTikz = false },
                new MathMatrixItem { SoThuTu = 8, ChuDe = "Số và phép tính", NoiDungKienThuc = "Đặt tính rồi tính (Phép tính phân số)", YeuCauCanDat = "Thực hiện tính đúng 4 phép tính cộng, trừ, nhân, chia phân số (4 ý a, b, c, d; mỗi ý 0.25đ).", MucDo = "M2", HinhThuc = "TL", Diem = 1.0, CoHinhTikz = false },
                new MathMatrixItem { SoThuTu = 9, ChuDe = "Số và phép tính", NoiDungKienThuc = "Giải bài toán có lời văn về phân số", YeuCauCanDat = "Vận dụng phép tính phân số để giải và trình bày được bài toán thực tế có lời văn 2-3 bước tính có sơ đồ minh họa.", MucDo = "M3", HinhThuc = "TL", Diem = 1.0, CoHinhTikz = true },
                new MathMatrixItem { SoThuTu = 10, ChuDe = "Số và phép tính", NoiDungKienThuc = "Tính giá trị biểu thức thuận tiện", YeuCauCanDat = "Vận dụng tính chất kết hợp, giao hoán của phép tính phân số để tính giá trị biểu thức bằng cách thuận tiện.", MucDo = "M3", HinhThuc = "TL", Diem = 1.0, CoHinhTikz = false }
            };
        }

        public static List<MathMatrixItem> GetGrade4FinalTerm2Matrix()
        {
            return MathExamPaper.GetDefaultGrade4Matrix();
        }

        #endregion

        #region Ma trận chuẩn Khối 5 (SGK Kết nối tri thức)

        public static List<MathMatrixItem> GetGrade5MidTerm1Matrix()
        {
            return new List<MathMatrixItem>
            {
                new MathMatrixItem { SoThuTu = 1, ChuDe = "Số và phép tính", NoiDungKienThuc = "Khái niệm số thập phân", YeuCauCanDat = "Nhận biết được khái niệm, cấu tạo số thập phân (phần nguyên, phần thập phân).", MucDo = "M1", HinhThuc = "TN", Diem = 1.0, CoHinhTikz = false },
                new MathMatrixItem { SoThuTu = 2, ChuDe = "Số và phép tính", NoiDungKienThuc = "Hàng của số thập phân", YeuCauCanDat = "Xác định được đúng tên hàng và giá trị theo vị trí của các chữ số trong số thập phân.", MucDo = "M1", HinhThuc = "TN", Diem = 1.0, CoHinhTikz = false },
                new MathMatrixItem { SoThuTu = 3, ChuDe = "Số và phép tính", NoiDungKienThuc = "So sánh và làm tròn số thập phân", YeuCauCanDat = "So sánh được hai số thập phân; làm tròn được số thập phân đến hàng đơn vị, hàng phần mười.", MucDo = "M1", HinhThuc = "TN", Diem = 1.0, CoHinhTikz = false },
                new MathMatrixItem { SoThuTu = 4, ChuDe = "Hình học và đo lường", NoiDungKienThuc = "Đơn vị đo diện tích (ha, km²)", YeuCauCanDat = "Nhận biết và chuyển đổi được giữa các đơn vị đo diện tích héc-ta, ki-lô-mét vuông, mét vuông.", MucDo = "M1", HinhThuc = "TN", Diem = 1.0, CoHinhTikz = false },
                new MathMatrixItem { SoThuTu = 5, ChuDe = "Số và phép tính", NoiDungKienThuc = "Cộng, trừ số thập phân", YeuCauCanDat = "Thực hiện được phép tính cộng hoặc trừ hai số thập phân.", MucDo = "M2", HinhThuc = "TN", Diem = 1.0, CoHinhTikz = false },
                new MathMatrixItem { SoThuTu = 6, ChuDe = "Số và phép tính", NoiDungKienThuc = "Số thập phân và số đo đại lượng", YeuCauCanDat = "Chuyển đổi và viết được số đo độ dài, khối lượng, diện tích dưới dạng số thập phân (Đúng/Sai 2 ý a, b).", MucDo = "M2", HinhThuc = "TN", Diem = 1.0, CoHinhTikz = false },
                new MathMatrixItem { SoThuTu = 7, ChuDe = "Thống kê và xác suất", NoiDungKienThuc = "Bảng số liệu thống kê", YeuCauCanDat = "Đọc, xử lý và phân tích được số liệu thống kê số thập phân từ bảng số liệu đơn giản (2 ý a, b).", MucDo = "M2", HinhThuc = "TN", Diem = 1.0, CoHinhTikz = false },
                new MathMatrixItem { SoThuTu = 8, ChuDe = "Số và phép tính", NoiDungKienThuc = "Đặt tính rồi tính (Cộng, trừ số thập phân)", YeuCauCanDat = "Đặt tính và tính được đúng 4 phép tính cộng, trừ số thập phân (4 ý a, b, c, d; mỗi ý 0.25đ).", MucDo = "M2", HinhThuc = "TL", Diem = 1.0, CoHinhTikz = false },
                new MathMatrixItem { SoThuTu = 9, ChuDe = "Số và phép tính", NoiDungKienThuc = "Giải bài toán có lời văn về số thập phân", YeuCauCanDat = "Giải và trình bày được bài toán có lời văn thực tế 2 đến 3 bước giải liên quan đến số thập phân và đơn vị đo có sơ đồ.", MucDo = "M3", HinhThuc = "TL", Diem = 1.0, CoHinhTikz = true },
                new MathMatrixItem { SoThuTu = 10, ChuDe = "Số và phép tính", NoiDungKienThuc = "Tính giá trị biểu thức thuận tiện", YeuCauCanDat = "Vận dụng tính chất kết hợp, giao hoán để tính giá trị biểu thức số thập phân bằng cách thuận tiện.", MucDo = "M3", HinhThuc = "TL", Diem = 1.0, CoHinhTikz = false }
            };
        }

        public static List<MathMatrixItem> GetGrade5FinalTerm1Matrix()
        {
            return new List<MathMatrixItem>
            {
                new MathMatrixItem { SoThuTu = 1, ChuDe = "Số và phép tính", NoiDungKienThuc = "Phép nhân, phép chia số thập phân", YeuCauCanDat = "Nêu được quy tắc và nhận biết thứ tự thực hiện nhân, chia số thập phân.", MucDo = "M1", HinhThuc = "TN", Diem = 1.0, CoHinhTikz = false },
                new MathMatrixItem { SoThuTu = 2, ChuDe = "Số và phép tính", NoiDungKienThuc = "Tỉ số phần trăm", YeuCauCanDat = "Nhận biết được khái niệm và ý nghĩa thực tế của tỉ số phần trăm.", MucDo = "M1", HinhThuc = "TN", Diem = 1.0, CoHinhTikz = false },
                new MathMatrixItem { SoThuTu = 3, ChuDe = "Hình học và đo lường", NoiDungKienThuc = "Hình tam giác và diện tích hình tam giác", YeuCauCanDat = "Nhận biết được đỉnh, đáy và đường cao tương ứng của hình tam giác; nêu công thức tính diện tích hình tam giác.", MucDo = "M1", HinhThuc = "TN", Diem = 1.0, CoHinhTikz = true },
                new MathMatrixItem { SoThuTu = 4, ChuDe = "Hình học và đo lường", NoiDungKienThuc = "Hình thang và diện tích hình thang", YeuCauCanDat = "Nhận biết được hình thang, đáy lớn, đáy bé, cạnh bên và chiều cao hình thang; nêu công thức tính diện tích hình thang.", MucDo = "M1", HinhThuc = "TN", Diem = 1.0, CoHinhTikz = true },
                new MathMatrixItem { SoThuTu = 5, ChuDe = "Hình học và đo lường", NoiDungKienThuc = "Chu vi và diện tích hình tròn", YeuCauCanDat = "Tính được chu vi và diện tích hình tròn khi biết bán kính hoặc đường kính.", MucDo = "M2", HinhThuc = "TN", Diem = 1.0, CoHinhTikz = true },
                new MathMatrixItem { SoThuTu = 6, ChuDe = "Số và phép tính", NoiDungKienThuc = "Giải bài toán về tỉ số phần trăm", YeuCauCanDat = "Tìm được tỉ số phần trăm của hai số hoặc tìm giá trị phần trăm của một số.", MucDo = "M2", HinhThuc = "TN", Diem = 1.0, CoHinhTikz = false },
                new MathMatrixItem { SoThuTu = 7, ChuDe = "Thống kê và xác suất", NoiDungKienThuc = "Bảng số liệu và biểu đồ thống kê", YeuCauCanDat = "Đọc, xử lý và rút ra được nhận xét từ bảng số liệu hoặc biểu đồ thống kê (2 ý a, b).", MucDo = "M2", HinhThuc = "TN", Diem = 1.0, CoHinhTikz = false },
                new MathMatrixItem { SoThuTu = 8, ChuDe = "Số và phép tính", NoiDungKienThuc = "Đặt tính rồi tính (Cộng, trừ, nhân, chia số thập phân)", YeuCauCanDat = "Đặt tính và tính được đúng 4 phép tính cộng, trừ, nhân, chia số thập phân (4 ý a, b, c, d; mỗi ý 0.25đ).", MucDo = "M2", HinhThuc = "TL", Diem = 1.0, CoHinhTikz = false },
                new MathMatrixItem { SoThuTu = 9, ChuDe = "Số và phép tính", NoiDungKienThuc = "Giải bài toán thực tế về tỉ số phần trăm", YeuCauCanDat = "Vận dụng tỉ số phần trăm để giải và trình bày được bài toán thực tế (tiền vốn, tiền lãi, giảm giá...) 2-3 bước tính.", MucDo = "M3", HinhThuc = "TL", Diem = 1.0, CoHinhTikz = false },
                new MathMatrixItem { SoThuTu = 10, ChuDe = "Hình học và đo lường", NoiDungKienThuc = "Giải bài toán thực tế về diện tích hình phẳng", YeuCauCanDat = "Vận dụng công thức tính diện tích hình tam giác hoặc hình thang để giải bài toán thực tế 2-3 bước tính.", MucDo = "M3", HinhThuc = "TL", Diem = 1.0, CoHinhTikz = true }
            };
        }

        public static List<MathMatrixItem> GetGrade5MidTerm2Matrix()
        {
            return new List<MathMatrixItem>
            {
                new MathMatrixItem { SoThuTu = 1, ChuDe = "Hình học và đo lường", NoiDungKienThuc = "Hình tròn và chu vi, diện tích", YeuCauCanDat = "Nhận biết được bán kính, đường kính hình tròn; nêu công thức tính chu vi và diện tích hình tròn.", MucDo = "M1", HinhThuc = "TN", Diem = 1.0, CoHinhTikz = true },
                new MathMatrixItem { SoThuTu = 2, ChuDe = "Hình học và đo lường", NoiDungKienThuc = "Hình hộp chữ nhật và hình lập phương", YeuCauCanDat = "Nhận biết được các yếu tố đỉnh, cạnh, mặt của hình hộp chữ nhật và hình lập phương.", MucDo = "M1", HinhThuc = "TN", Diem = 1.0, CoHinhTikz = true },
                new MathMatrixItem { SoThuTu = 3, ChuDe = "Hình học và đo lường", NoiDungKienThuc = "Đơn vị đo thể tích (cm³, dm³, m³)", YeuCauCanDat = "Nhận biết và chuyển đổi được đúng giữa các đơn vị đo thể tích cm³, dm³, m³.", MucDo = "M1", HinhThuc = "TN", Diem = 1.0, CoHinhTikz = false },
                new MathMatrixItem { SoThuTu = 4, ChuDe = "Hình học và đo lường", NoiDungKienThuc = "Diện tích xung quanh và toàn phần hình hộp", YeuCauCanDat = "Nhận biết và nêu được công thức tính diện tích xung quanh, diện tích toàn phần của hình hộp chữ nhật, hình lập phương.", MucDo = "M1", HinhThuc = "TN", Diem = 1.0, CoHinhTikz = false },
                new MathMatrixItem { SoThuTu = 5, ChuDe = "Hình học và đo lường", NoiDungKienThuc = "Thể tích hình hộp chữ nhật và hình lập phương", YeuCauCanDat = "Tính được thể tích của hình hộp chữ nhật hoặc hình lập phương theo kích thước cho trước.", MucDo = "M2", HinhThuc = "TN", Diem = 1.0, CoHinhTikz = false },
                new MathMatrixItem { SoThuTu = 6, ChuDe = "Hình học và đo lường", NoiDungKienThuc = "Vận tốc, quãng đường, thời gian", YeuCauCanDat = "Tính toán và giải thích được mối quan hệ giữa vận tốc, quãng đường và thời gian trong chuyển động đều (Đúng/Sai 2 ý a, b).", MucDo = "M2", HinhThuc = "TN", Diem = 1.0, CoHinhTikz = false },
                new MathMatrixItem { SoThuTu = 7, ChuDe = "Thống kê và xác suất", NoiDungKienThuc = "Biểu đồ hình quạt tròn", YeuCauCanDat = "Đọc và phân tích được số liệu từ biểu đồ hình quạt tròn đơn giản (2 ý a, b).", MucDo = "M2", HinhThuc = "TN", Diem = 1.0, CoHinhTikz = true },
                new MathMatrixItem { SoThuTu = 8, ChuDe = "Số và phép tính", NoiDungKienThuc = "Đặt tính rồi tính (Số thập phân và thời gian)", YeuCauCanDat = "Đặt tính và tính được đúng 4 phép tính với số thập phân và số đo thời gian (4 ý a, b, c, d; mỗi ý 0.25đ).", MucDo = "M2", HinhThuc = "TL", Diem = 1.0, CoHinhTikz = false },
                new MathMatrixItem { SoThuTu = 9, ChuDe = "Số và phép tính", NoiDungKienThuc = "Giải bài toán có lời văn về chuyển động đều", YeuCauCanDat = "Giải và trình bày được bài toán thực tế về chuyển động đều của một động tử (tìm vận tốc, quãng đường, thời gian) 2-3 bước tính có sơ đồ.", MucDo = "M3", HinhThuc = "TL", Diem = 1.0, CoHinhTikz = true },
                new MathMatrixItem { SoThuTu = 10, ChuDe = "Hình học và đo lường", NoiDungKienThuc = "Giải bài toán có lời văn về thể tích hình khối", YeuCauCanDat = "Vận dụng tính thể tích hình hộp chữ nhật/hình lập phương để giải bài toán thực tế (bể nước, thùng hàng) 2-3 bước tính.", MucDo = "M3", HinhThuc = "TL", Diem = 1.0, CoHinhTikz = false }
            };
        }

        public static List<MathMatrixItem> GetGrade5FinalTerm2Matrix()
        {
            return new List<MathMatrixItem>
            {
                new MathMatrixItem { SoThuTu = 1, ChuDe = "Số và phép tính", NoiDungKienThuc = "Số thập phân và tỉ số phần trăm", YeuCauCanDat = "Nhận biết được số thập phân, phân số thập phân và tỉ số phần trăm.", MucDo = "M1", HinhThuc = "TN", Diem = 1.0, CoHinhTikz = false },
                new MathMatrixItem { SoThuTu = 2, ChuDe = "Hình học và đo lường", NoiDungKienThuc = "Diện tích hình tam giác, hình thang, hình tròn", YeuCauCanDat = "Nhận biết được hình dạng và công thức tính diện tích hình tam giác, hình thang, hình tròn.", MucDo = "M1", HinhThuc = "TN", Diem = 1.0, CoHinhTikz = true },
                new MathMatrixItem { SoThuTu = 3, ChuDe = "Hình học và đo lường", NoiDungKienThuc = "Thể tích hình hộp chữ nhật, hình lập phương", YeuCauCanDat = "Nhận biết được hình khối và công thức tính thể tích của hình hộp chữ nhật, hình lập phương.", MucDo = "M1", HinhThuc = "TN", Diem = 1.0, CoHinhTikz = true },
                new MathMatrixItem { SoThuTu = 4, ChuDe = "Hình học và đo lường", NoiDungKienThuc = "Bảng đơn vị đo đại lượng", YeuCauCanDat = "Nhận biết được các đơn vị đo và mối quan hệ giữa các đơn vị đo đại lượng đã học.", MucDo = "M1", HinhThuc = "TN", Diem = 1.0, CoHinhTikz = false },
                new MathMatrixItem { SoThuTu = 5, ChuDe = "Số và phép tính", NoiDungKienThuc = "Bốn phép tính với số thập phân", YeuCauCanDat = "Thực hiện được phép tính nhân hoặc chia hai số thập phân trong tình huống tính toán chuẩn.", MucDo = "M2", HinhThuc = "TN", Diem = 1.0, CoHinhTikz = false },
                new MathMatrixItem { SoThuTu = 6, ChuDe = "Số và phép tính", NoiDungKienThuc = "Mối quan hệ vận tốc, quãng đường, thời gian", YeuCauCanDat = "Tính toán và giải thích được mối quan hệ giữa vận tốc, quãng đường và thời gian (Đúng/Sai 2 ý a, b).", MucDo = "M2", HinhThuc = "TN", Diem = 1.0, CoHinhTikz = false },
                new MathMatrixItem { SoThuTu = 7, ChuDe = "Thống kê và xác suất", NoiDungKienThuc = "Biểu đồ quạt tròn và bảng số liệu", YeuCauCanDat = "Đọc, xử lý và giải quyết được vấn đề từ biểu đồ hình quạt tròn hoặc bảng số liệu thống kê (2 ý a, b).", MucDo = "M2", HinhThuc = "TN", Diem = 1.0, CoHinhTikz = true },
                new MathMatrixItem { SoThuTu = 8, ChuDe = "Số và phép tính", NoiDungKienThuc = "Đặt tính rồi tính (Bốn phép tính)", YeuCauCanDat = "Đặt tính và tính được đúng 4 phép tính cộng, trừ, nhân, chia (4 ý a, b, c, d; mỗi ý 0.25đ).", MucDo = "M2", HinhThuc = "TL", Diem = 1.0, CoHinhTikz = false },
                new MathMatrixItem { SoThuTu = 9, ChuDe = "Số và phép tính", NoiDungKienThuc = "Giải bài toán có lời văn về hai chuyển động", YeuCauCanDat = "Giải và trình bày được bài toán về chuyển động đều hai động tử ngược chiều gặp nhau hoặc cùng chiều đuổi nhau 2-3 bước tính có sơ đồ.", MucDo = "M3", HinhThuc = "TL", Diem = 1.0, CoHinhTikz = true },
                new MathMatrixItem { SoThuTu = 10, ChuDe = "Hình học và đo lường", NoiDungKienThuc = "Giải bài toán thực tế hình học tổng hợp", YeuCauCanDat = "Vận dụng kiến thức hình học (diện tích, thể tích) và tỉ số phần trăm để giải bài toán thực tế nâng cao 2-3 bước tính.", MucDo = "M3", HinhThuc = "TL", Diem = 1.0, CoHinhTikz = false }
            };
        }

        #endregion

        #region Từ điển gợi ý Nội dung kiến thức & Yêu cầu cần đạt chuẩn SGK Kết nối

        public static List<string> GetKnowledgeContents(int grade, string period, string topic)
        {
            if (string.IsNullOrWhiteSpace(period) || ExamPeriodHelper.IsFinalTerm2(period))
            {
                return GetKnowledgeContents(grade, topic);
            }

            if (grade == 5)
            {
                if (ExamPeriodHelper.IsMidTerm1(period))
                {
                    return topic switch
                    {
                        "Số và phép tính" => new List<string>
                        {
                            "Khái niệm & Hàng số thập phân",
                            "So sánh & Làm tròn số thập phân",
                            "Phép cộng, trừ số thập phân",
                            "Phân số và phân số thập phân",
                            "Tính giá trị biểu thức thuận tiện"
                        },
                        "Hình học và đo lường" => new List<string>
                        {
                            "Đơn vị đo diện tích (ha, km²)",
                            "Bảng đơn vị đo diện tích"
                        },
                        _ => new List<string>
                        {
                            "Thu thập, phân loại số liệu",
                            "Bảng số liệu thống kê"
                        }
                    };
                }

                if (ExamPeriodHelper.IsFinalTerm1(period))
                {
                    return topic switch
                    {
                        "Số và phép tính" => new List<string>
                        {
                            "Khái niệm & Hàng số thập phân",
                            "So sánh & Làm tròn số thập phân",
                            "Phép cộng, trừ số thập phân",
                            "Nhân, chia số thập phân",
                            "Tỉ số phần trăm",
                            "Giải toán tỉ số phần trăm",
                            "Tính giá trị biểu thức thuận tiện"
                        },
                        "Hình học và đo lường" => new List<string>
                        {
                            "Đơn vị đo diện tích (ha, km²)",
                            "Hình tam giác, hình thang",
                            "Hình tròn & Chu vi, diện tích",
                            "Vận dụng tính diện tích thực tế"
                        },
                        _ => new List<string>
                        {
                            "Thu thập, phân loại số liệu",
                            "Bảng số liệu thống kê"
                        }
                    };
                }

                if (ExamPeriodHelper.IsMidTerm2(period))
                {
                    return topic switch
                    {
                        "Số và phép tính" => new List<string>
                        {
                            "Tỉ số, tỉ lệ bản đồ",
                            "Tìm hai số khi biết tổng-tỉ / hiệu-tỉ",
                            "Phép tính số đo thời gian",
                            "Vận tốc cơ bản",
                            "Tính giá trị biểu thức thuận tiện"
                        },
                        "Hình học và đo lường" => new List<string>
                        {
                            "Hình hộp chữ nhật, hình lập phương",
                            "Đơn vị đo thể tích (cm³, dm³, m³)",
                            "Diện tích xung quanh & toàn phần",
                            "Thể tích hình hộp chữ nhật, lập phương",
                            "Vận dụng thể tích hình khối"
                        },
                        _ => new List<string>
                        {
                            "Biểu đồ hình quạt tròn"
                        }
                    };
                }
            }
            else // Khối 4
            {
                if (ExamPeriodHelper.IsMidTerm1(period))
                {
                    return topic switch
                    {
                        "Số và phép tính" => new List<string>
                        {
                            "Số có nhiều chữ số",
                            "Làm tròn số",
                            "Cộng, trừ số có nhiều chữ số",
                            "Giải bài toán có lời văn về số tự nhiên"
                        },
                        "Hình học và đo lường" => new List<string>
                        {
                            "Góc nhọn, góc tù, góc bẹt",
                            "Hai đường thẳng vuông góc, song song",
                            "Đo khối lượng (yến, tạ, tấn)",
                            "Đo thời gian (giây, thế kỉ)"
                        },
                        _ => new List<string>
                        {
                            "Dãy số liệu thống kê"
                        }
                    };
                }

                if (ExamPeriodHelper.IsFinalTerm1(period))
                {
                    return topic switch
                    {
                        "Số và phép tính" => new List<string>
                        {
                            "Nhân với số có một, hai chữ số",
                            "Chia cho số có một, hai chữ số",
                            "Số trung bình cộng",
                            "Tìm hai số khi biết tổng và hiệu",
                            "Tính chất phép tính & Biểu thức chữ",
                            "Bốn phép tính số tự nhiên"
                        },
                        "Hình học và đo lường" => new List<string>
                        {
                            "Hình bình hành, hình thoi",
                            "Đo diện tích (dm², m², mm²)",
                            "Vận dụng diện tích hình học"
                        },
                        _ => new List<string>
                        {
                            "Biểu đồ cột"
                        }
                    };
                }

                if (ExamPeriodHelper.IsMidTerm2(period))
                {
                    return topic switch
                    {
                        "Số và phép tính" => new List<string>
                        {
                            "Khái niệm phân số",
                            "Phân số bằng nhau, rút gọn phân số",
                            "Quy đồng & so sánh phân số",
                            "Phép cộng phân số",
                            "Phép trừ phân số",
                            "Tính giá trị biểu thức thuận tiện"
                        },
                        "Hình học và đo lường" => new List<string>
                        {
                            "Hình bình hành, hình thoi"
                        },
                        _ => new List<string>
                        {
                            "Bảng số liệu thống kê"
                        }
                    };
                }
            }

            return GetKnowledgeContents(grade, topic);
        }

        public static List<string> GetKnowledgeContents(int grade, string topic)
        {
            if (grade == 5)
            {
                return topic switch
                {
                    "Số và phép tính" => new List<string>
                    {
                        "Khái niệm & Hàng số thập phân",
                        "So sánh & Làm tròn số thập phân",
                        "Phép cộng, trừ số thập phân",
                        "Nhân, chia số thập phân",
                        "Tỉ số phần trăm",
                        "Giải toán tỉ số phần trăm",
                        "Toán chuyển động đều",
                        "Chuyển động cùng chiều / ngược chiều",
                        "Bốn phép tính phối hợp",
                        "Tính giá trị biểu thức thuận tiện"
                    },
                    "Hình học và đo lường" => new List<string>
                    {
                        "Đơn vị đo diện tích (ha, km²)",
                        "Hình tam giác, hình thang",
                        "Hình tròn & Chu vi, diện tích",
                        "Hình hộp chữ nhật, hình lập phương",
                        "Đơn vị đo thể tích (cm³, dm³, m³)",
                        "Diện tích xung quanh & toàn phần",
                        "Thể tích hình hộp chữ nhật, lập phương",
                        "Vận dụng tính diện tích thực tế",
                        "Vận dụng thể tích hình khối",
                        "Toán thực tế tổng hợp nâng cao"
                    },
                    _ => new List<string>
                    {
                        "Thu thập, phân loại số liệu",
                        "Bảng số liệu thống kê",
                        "Biểu đồ hình quạt tròn",
                        "Xác suất thực nghiệm cơ bản"
                    }
                };
            }

            // Khối 4
            return topic switch
            {
                "Số và phép tính" => new List<string>
                {
                    "Số có nhiều chữ số",
                    "Làm tròn số",
                    "Cộng, trừ số có nhiều chữ số",
                    "Nhân với số có một, hai chữ số",
                    "Chia cho số có một, hai chữ số",
                    "Số trung bình cộng",
                    "Tìm hai số khi biết tổng và hiệu",
                    "Khái niệm phân số",
                    "Phân số bằng nhau, rút gọn phân số",
                    "Quy đồng & so sánh phân số",
                    "Bốn phép tính phân số",
                    "Giải bài toán có lời văn về phân số",
                    "Vận dụng giải toán thực tiễn"
                },
                "Hình học và đo lường" => new List<string>
                {
                    "Góc nhọn, góc tù, góc bẹt",
                    "Hai đường thẳng vuông góc, song song",
                    "Đo khối lượng (yến, tạ, tấn)",
                    "Đo thời gian (giây, thế kỉ)",
                    "Đo diện tích (dm², m², mm²)",
                    "Hình bình hành, hình thoi",
                    "Vận dụng diện tích hình học"
                },
                _ => new List<string>
                {
                    "Dãy số liệu thống kê",
                    "Bảng số liệu thống kê",
                    "Biểu đồ cột",
                    "Khả năng xảy ra của một sự kiện"
                }
            };
        }

        public static string SuggestObjective(string topic, string content, string level)
        {
            if (string.IsNullOrWhiteSpace(content)) return "Thực hiện được yêu cầu theo chuẩn chương trình.";

            if (content.Contains("phân số", StringComparison.OrdinalIgnoreCase))
            {
                if (level == "M1") return "Nhận biết khái niệm phân số, phân số chỉ phần đã tô màu của hình hoặc hai phân số bằng nhau.";
                if (level == "M2") return "Thực hiện thành thạo các phép tính cộng, trừ, nhân, chia với phân số.";
                return "Vận dụng giải bài toán thực tế có lời văn liên quan đến tìm phân số của một số hoặc tỉ số.";
            }

            if (content.Contains("thập phân", StringComparison.OrdinalIgnoreCase))
            {
                if (level == "M1") return "Nhận biết khái niệm, hàng của số thập phân; so sánh và sắp xếp thứ tự các số thập phân.";
                if (level == "M2") return "Thực hiện đặt tính và tính đúng phép tính cộng, trừ, nhân hoặc chia các số thập phân.";
                return "Vận dụng giải bài toán thực tế liên quan đến số thập phân nhiều bước tính hoặc tính thuận tiện.";
            }

            if (content.Contains("tam giác", StringComparison.OrdinalIgnoreCase) || content.Contains("thang", StringComparison.OrdinalIgnoreCase))
            {
                if (level == "M1") return "Nhận biết đặc điểm đỉnh, cạnh, đáy và đường cao của hình tam giác hoặc hình thang.";
                if (level == "M2") return "Tính được diện tích hình tam giác hoặc hình thang theo công thức đã học.";
                return "Vận dụng tính diện tích hình tam giác hoặc hình thang để giải quyết vấn đề thực tế.";
            }

            if (content.Contains("thể tích", StringComparison.OrdinalIgnoreCase) || content.Contains("hộp chữ nhật", StringComparison.OrdinalIgnoreCase))
            {
                if (level == "M1") return "Nhận biết các yếu tố đỉnh, cạnh, mặt của hình khối và đơn vị đo thể tích xăng-ti-mét khối, mét khối.";
                if (level == "M2") return "Tính được diện tích xung quanh, toàn phần hoặc thể tích hình hộp chữ nhật, hình lập phương.";
                return "Vận dụng công thức tính thể tích giải quyết bài toán bể chứa, thùng hàng thực tế.";
            }

            if (content.Contains("chuyển động", StringComparison.OrdinalIgnoreCase) || content.Contains("vận tốc", StringComparison.OrdinalIgnoreCase))
            {
                if (level == "M1") return "Nhận biết công thức và mối liên hệ giữa các đại lượng vận tốc, quãng đường và thời gian.";
                if (level == "M2") return "Thực hiện tính vận tốc, quãng đường hoặc thời gian của một chuyển động đều đơn giản.";
                return "Giải bài toán chuyển động đều hai động tử chuyển động cùng chiều hoặc ngược chiều gặp nhau.";
            }

            if (content.Contains("thống kê", StringComparison.OrdinalIgnoreCase) || content.Contains("biểu đồ", StringComparison.OrdinalIgnoreCase))
            {
                if (level == "M1") return "Đọc và nhận biết các thông tin cơ bản từ biểu đồ cột hoặc bảng số liệu thống kê.";
                if (level == "M2") return "Xử lý, so sánh và trả lời các câu hỏi dựa trên số liệu từ biểu đồ hoặc bảng thống kê.";
                return "Nhận xét, phân tích xu hướng và giải quyết bài toán thực tế rút ra từ số liệu thống kê.";
            }

            return level switch
            {
                "M1" => $"Nhận biết kiến thức cơ bản về {content.ToLower()}.",
                "M2" => $"Thực hiện kỹ năng, phép tính và giải quyết vấn đề đơn giản về {content.ToLower()}.",
                _ => $"Vận dụng kiến thức về {content.ToLower()} để giải quyết vấn đề thực tiễn."
            };
        }

        #endregion
    }
}
