using System;
using System.Collections.Generic;
using TH_ExamGen.Core.Curriculum;

namespace TH_ExamGen.Core.VietModels
{
    public static class VietCurriculumBank
    {
        public static readonly string[] ExamPeriods = new[]
        {
            "Giữa Học kỳ I",
            "Cuối Học kỳ I",
            "Giữa Học kỳ II",
            "Cuối Học kỳ II"
        };

        public static readonly string[] MachKienThucList = new[]
        {
            "Đọc hiểu văn bản",
            "Kiến thức Tiếng Việt"
        };

        public static List<VietMatrixItem> GetMatrixByGradeAndPeriod(int grade, string period)
        {
            List<VietMatrixItem> matrix;
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

            return CurriculumMatrixMapper.AttachVietIds(grade, period, matrix);
        }

        public static string GetDefaultTheme(int grade, string period)
        {
            if (grade == 5)
            {
                return period switch
                {
                    "Giữa Học kỳ I" => "Khung trời tuổi thơ, khát vọng và ước mơ",
                    "Cuối Học kỳ I" => "Hương sắc mọi miền, tiếp bước cha ông, tình yêu đất nước",
                    "Giữa Học kỳ II" => "Vẻ đẹp cuộc sống, chung tay vì cộng đồng",
                    _ => "Thế giới muôn màu, hòa bình và vươn tới tương lai"
                };
            }

            return period switch
            {
                "Giữa Học kỳ I" => "Mỗi người một vẻ, trải nghiệm và khám phá thế giới xung quanh",
                "Cuối Học kỳ I" => "Chắp cánh ước mơ, thế giới tuổi thơ và lòng nhân ái",
                "Giữa Học kỳ II" => "Sống để yêu thương, uống nước nhớ nguồn và lòng biết ơn",
                _ => "Quê hương trong tôi, vì một thế giới bình yên và bảo vệ môi trường"
            };
        }

        #region Ma trận chuẩn Khối 4 (SGK Kết nối tri thức)

        public static List<VietMatrixItem> GetGrade4MidTerm1Matrix()
        {
            return new List<VietMatrixItem>
            {
                new VietMatrixItem
                {
                    SoThuTu = 1,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Chi tiết trực tiếp trong bài đọc",
                    YeuCauCanDat = "Nhận biết các chi tiết, sự việc, hoàn cảnh được nêu trực tiếp trong văn bản.",
                    MucDo = "M1",
                    HinhThuc = "TN",
                    Diem = 0.5
                },
                new VietMatrixItem
                {
                    SoThuTu = 2,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Hành động, đặc điểm nhân vật",
                    YeuCauCanDat = "Nhận biết hành động, lời nói hoặc đặc điểm của nhân vật chính trong bài đọc.",
                    MucDo = "M1",
                    HinhThuc = "TN",
                    Diem = 0.5
                },
                new VietMatrixItem
                {
                    SoThuTu = 3,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Đọc hiểu hình thức: Nghĩa của từ ngữ, hình ảnh so sánh, gợi cảm",
                    YeuCauCanDat = "Nhận biết và hiểu ý nghĩa của biện pháp so sánh hoặc từ ngữ, hình ảnh tiêu biểu trong văn cảnh bài đọc.",
                    MucDo = "M1",
                    HinhThuc = "TN",
                    Diem = 0.5
                },
                new VietMatrixItem
                {
                    SoThuTu = 4,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Ý nghĩa thông tin đoạn văn",
                    YeuCauCanDat = "Hiểu nội dung hoặc thông tin cơ bản mà một đoạn văn truyền tải.",
                    MucDo = "M1",
                    HinhThuc = "TN",
                    Diem = 0.5
                },
                new VietMatrixItem
                {
                    SoThuTu = 5,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Giải thích lý do, nguyên nhân sự việc",
                    YeuCauCanDat = "Giải thích được nguyên nhân, lý do vì sao nhân vật có hành động hoặc cảm xúc như trong bài.",
                    MucDo = "M2",
                    HinhThuc = "TL",
                    Diem = 1.0
                },
                new VietMatrixItem
                {
                    SoThuTu = 6,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Đọc hiểu hình thức: Cảm nhận về chi tiết nghệ thuật",
                    YeuCauCanDat = "Nêu suy nghĩ, cảm nhận của em về một chi tiết nghệ thuật, hình ảnh hoặc câu văn giàu cảm xúc trong bài.",
                    MucDo = "M2",
                    HinhThuc = "TL",
                    Diem = 1.0
                },
                new VietMatrixItem
                {
                    SoThuTu = 7,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Liên hệ, so sánh, kết nối: Rút ra bài học, thông điệp và liên hệ bản thân",
                    YeuCauCanDat = "Rút ra bài học, thông điệp ý nghĩa của văn bản và liên hệ với bản thân trong thực tế.",
                    MucDo = "M3",
                    HinhThuc = "TL",
                    Diem = 1.0
                },
                new VietMatrixItem
                {
                    SoThuTu = 8,
                    MachKienThuc = "Kiến thức Tiếng Việt",
                    NoiDungKienThuc = "Từ vựng: Danh từ chung và Danh từ riêng",
                    YeuCauCanDat = "Nhận biết danh từ chung, danh từ riêng trong câu văn được trích từ bài đọc.",
                    MucDo = "M1",
                    HinhThuc = "TN",
                    Diem = 0.5
                },
                new VietMatrixItem
                {
                    SoThuTu = 9,
                    MachKienThuc = "Kiến thức Tiếng Việt",
                    NoiDungKienThuc = "Từ vựng: Quy tắc viết hoa danh từ riêng",
                    YeuCauCanDat = "Nhận biết và viết đúng quy tắc viết hoa danh từ riêng (tên người, tên địa lí Việt Nam hoặc nước ngoài).",
                    MucDo = "M1",
                    HinhThuc = "TN",
                    Diem = 0.5
                },
                new VietMatrixItem
                {
                    SoThuTu = 10,
                    MachKienThuc = "Kiến thức Tiếng Việt",
                    NoiDungKienThuc = "Ngữ pháp / Dấu câu: Tác dụng của dấu gạch ngang",
                    YeuCauCanDat = "Xác định đúng tác dụng của dấu gạch ngang (đánh dấu lời đối thoại, bộ phận chú thích hoặc các ý liệt kê).",
                    MucDo = "M2",
                    HinhThuc = "TL",
                    Diem = 1.0
                },
                new VietMatrixItem
                {
                    SoThuTu = 11,
                    MachKienThuc = "Kiến thức Tiếng Việt",
                    NoiDungKienThuc = "Hoạt động giao tiếp: Vận dụng đặt câu có danh từ riêng và dấu gạch ngang",
                    YeuCauCanDat = "Vận dụng đặt 1-2 câu có sử dụng danh từ riêng và dấu gạch ngang phù hợp với chủ đề của bài học.",
                    MucDo = "M3",
                    HinhThuc = "TL",
                    Diem = 1.0
                }
            };
        }

        public static List<VietMatrixItem> GetGrade4FinalTerm1Matrix()
        {
            return new List<VietMatrixItem>
            {
                new VietMatrixItem
                {
                    SoThuTu = 1,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Chi tiết trực tiếp trong bài đọc",
                    YeuCauCanDat = "Nhận biết thông tin, số liệu hoặc sự kiện nêu trực tiếp trong văn bản.",
                    MucDo = "M1",
                    HinhThuc = "TN",
                    Diem = 0.5
                },
                new VietMatrixItem
                {
                    SoThuTu = 2,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Đặc điểm, hành động của nhân vật",
                    YeuCauCanDat = "Nhận biết đặc điểm ngoại hình, tính cách hoặc hành động của đối tượng được miêu tả.",
                    MucDo = "M1",
                    HinhThuc = "TN",
                    Diem = 0.5
                },
                new VietMatrixItem
                {
                    SoThuTu = 3,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Đọc hiểu hình thức: Hiểu từ ngữ, hình ảnh so sánh, gợi cảm",
                    YeuCauCanDat = "Nhận biết và hiểu ý nghĩa của biện pháp so sánh hoặc từ ngữ, hình ảnh gợi cảm trong văn bản.",
                    MucDo = "M1",
                    HinhThuc = "TN",
                    Diem = 0.5
                },
                new VietMatrixItem
                {
                    SoThuTu = 4,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Nội dung, ý nghĩa cơ bản của đoạn trích",
                    YeuCauCanDat = "Xác định nội dung chính hoặc thông điệp cơ bản của một đoạn trích.",
                    MucDo = "M1",
                    HinhThuc = "TN",
                    Diem = 0.5
                },
                new VietMatrixItem
                {
                    SoThuTu = 5,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Giải thích nguyên nhân, động cơ",
                    YeuCauCanDat = "Giải thích nguyên nhân diễn biến câu chuyện hoặc động cơ hành động của nhân vật.",
                    MucDo = "M2",
                    HinhThuc = "TL",
                    Diem = 1.0
                },
                new VietMatrixItem
                {
                    SoThuTu = 6,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Đánh giá, nhận xét tình cảm của nhân vật",
                    YeuCauCanDat = "Nêu nhận xét về phẩm chất tốt đẹp, lòng nhân ái hoặc ước mơ cao đẹp của nhân vật.",
                    MucDo = "M2",
                    HinhThuc = "TL",
                    Diem = 1.0
                },
                new VietMatrixItem
                {
                    SoThuTu = 7,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Liên hệ, so sánh, kết nối: Thông điệp cuộc sống và bài học bản thân",
                    YeuCauCanDat = "Rút ra bài học nhân văn từ câu chuyện và nêu việc làm cụ thể liên hệ bản thân để thực hiện bài học đó.",
                    MucDo = "M3",
                    HinhThuc = "TL",
                    Diem = 1.0
                },
                new VietMatrixItem
                {
                    SoThuTu = 8,
                    MachKienThuc = "Kiến thức Tiếng Việt",
                    NoiDungKienThuc = "Từ vựng: Động từ và Tính từ",
                    YeuCauCanDat = "Nhận biết tính từ, động từ chỉ màu sắc, hình dáng, hoạt động, trạng thái trong câu.",
                    MucDo = "M1",
                    HinhThuc = "TN",
                    Diem = 0.5
                },
                new VietMatrixItem
                {
                    SoThuTu = 9,
                    MachKienThuc = "Kiến thức Tiếng Việt",
                    NoiDungKienThuc = "Ngữ pháp / Dấu câu: Tác dụng của dấu ngoặc kép",
                    YeuCauCanDat = "Nhận biết tác dụng của dấu ngoặc kép (đánh dấu lời nói trực tiếp hoặc từ ngữ mang ý nghĩa đặc biệt).",
                    MucDo = "M1",
                    HinhThuc = "TN",
                    Diem = 0.5
                },
                new VietMatrixItem
                {
                    SoThuTu = 10,
                    MachKienThuc = "Kiến thức Tiếng Việt",
                    NoiDungKienThuc = "Từ vựng / Ngữ pháp: Phân loại Danh từ - Động từ - Tính từ",
                    YeuCauCanDat = "Phân loại các từ cho trước vào 3 nhóm từ loại (Danh từ, Động từ, Tính từ).",
                    MucDo = "M2",
                    HinhThuc = "TL",
                    Diem = 1.0
                },
                new VietMatrixItem
                {
                    SoThuTu = 11,
                    MachKienThuc = "Kiến thức Tiếng Việt",
                    NoiDungKienThuc = "Hoạt động giao tiếp: Vận dụng viết câu có sử dụng biện pháp nhân hóa",
                    YeuCauCanDat = "Vận dụng viết 1-2 câu văn có sử dụng biện pháp nhân hóa và dấu ngoặc kép miêu tả một sự vật thân thuộc.",
                    MucDo = "M3",
                    HinhThuc = "TL",
                    Diem = 1.0
                }
            };
        }

        public static List<VietMatrixItem> GetGrade4MidTerm2Matrix()
        {
            return new List<VietMatrixItem>
            {
                new VietMatrixItem
                {
                    SoThuTu = 1,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Chi tiết trực tiếp trong bài đọc",
                    YeuCauCanDat = "Nhận biết chi tiết, hoàn cảnh, sự việc được kể trực tiếp trong văn bản.",
                    MucDo = "M1",
                    HinhThuc = "TN",
                    Diem = 0.5
                },
                new VietMatrixItem
                {
                    SoThuTu = 2,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Hành động, phẩm chất nhân vật",
                    YeuCauCanDat = "Nhận biết hành động, phẩm chất tốt đẹp của nhân vật thể hiện tình yêu thương, sẻ chia.",
                    MucDo = "M1",
                    HinhThuc = "TN",
                    Diem = 0.5
                },
                new VietMatrixItem
                {
                    SoThuTu = 3,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Đọc hiểu hình thức: Ý nghĩa từ ngữ gợi cảm, hình ảnh so sánh",
                    YeuCauCanDat = "Nhận biết và hiểu ý nghĩa của biện pháp so sánh hoặc từ ngữ, hình ảnh giàu cảm xúc gợi lên lòng biết ơn hoặc tình thương yêu.",
                    MucDo = "M1",
                    HinhThuc = "TN",
                    Diem = 0.5
                },
                new VietMatrixItem
                {
                    SoThuTu = 4,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Nội dung cơ bản của đoạn văn",
                    YeuCauCanDat = "Xác định ý chính hoặc thông tin nổi bật của đoạn văn trong bài.",
                    MucDo = "M1",
                    HinhThuc = "TN",
                    Diem = 0.5
                },
                new VietMatrixItem
                {
                    SoThuTu = 5,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Giải thích nguyên nhân, cảm xúc của nhân vật",
                    YeuCauCanDat = "Giải thích nguyên nhân vì sao nhân vật có cảm xúc biết ơn, xúc động hoặc hành động sẻ chia.",
                    MucDo = "M2",
                    HinhThuc = "TL",
                    Diem = 1.0
                },
                new VietMatrixItem
                {
                    SoThuTu = 6,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Đọc hiểu hình thức: Cảm nhận về chi tiết nghệ thuật, nghĩa cử đẹp",
                    YeuCauCanDat = "Nêu suy nghĩ, cảm phục của em trước một chi tiết nghệ thuật hoặc nghĩa cử cao đẹp của nhân vật trong bài đọc.",
                    MucDo = "M2",
                    HinhThuc = "TL",
                    Diem = 1.0
                },
                new VietMatrixItem
                {
                    SoThuTu = 7,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Liên hệ, so sánh, kết nối: Bài học về lòng biết ơn / Tình yêu thương",
                    YeuCauCanDat = "Rút ra bài học sâu sắc về lòng nhân ái, truyền thống uống nước nhớ nguồn và liên hệ bản thân.",
                    MucDo = "M3",
                    HinhThuc = "TL",
                    Diem = 1.0
                },
                new VietMatrixItem
                {
                    SoThuTu = 8,
                    MachKienThuc = "Kiến thức Tiếng Việt",
                    NoiDungKienThuc = "Từ vựng: Quy tắc viết hoa tên cơ quan, tổ chức",
                    YeuCauCanDat = "Nhận biết và viết đúng quy tắc viết hoa tên cơ quan, tổ chức trong ngữ cảnh cụ thể.",
                    MucDo = "M1",
                    HinhThuc = "TN",
                    Diem = 0.5
                },
                new VietMatrixItem
                {
                    SoThuTu = 9,
                    MachKienThuc = "Kiến thức Tiếng Việt",
                    NoiDungKienThuc = "Ngữ pháp: Thành phần Chủ ngữ - Vị ngữ trong câu",
                    YeuCauCanDat = "Nhận biết bộ phận chủ ngữ và vị ngữ trong câu kể được trích từ bài đọc.",
                    MucDo = "M1",
                    HinhThuc = "TN",
                    Diem = 0.5
                },
                new VietMatrixItem
                {
                    SoThuTu = 10,
                    MachKienThuc = "Kiến thức Tiếng Việt",
                    NoiDungKienThuc = "Ngữ pháp: Phân tích cấu tạo câu kể (Chủ ngữ - Vị ngữ)",
                    YeuCauCanDat = "Xác định rõ bộ phận Chủ ngữ và Vị ngữ trong câu văn cho trước.",
                    MucDo = "M2",
                    HinhThuc = "TL",
                    Diem = 1.0
                },
                new VietMatrixItem
                {
                    SoThuTu = 11,
                    MachKienThuc = "Kiến thức Tiếng Việt",
                    NoiDungKienThuc = "Hoạt động giao tiếp: Vận dụng đặt câu kể theo chủ đề",
                    YeuCauCanDat = "Vận dụng đặt 1-2 câu kể có đầy đủ hai thành phần chính Chủ ngữ và Vị ngữ nói về lòng biết ơn hoặc tình yêu thương.",
                    MucDo = "M3",
                    HinhThuc = "TL",
                    Diem = 1.0
                }
            };
        }

        public static List<VietMatrixItem> GetGrade4FinalTerm2Matrix()
        {
            return new List<VietMatrixItem>
            {
                new VietMatrixItem
                {
                    SoThuTu = 1,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Chi tiết trực tiếp trong bài đọc",
                    YeuCauCanDat = "Nhận biết các chi tiết, sự việc, hoàn cảnh được nêu trực tiếp trong văn bản.",
                    MucDo = "M1",
                    HinhThuc = "TN",
                    Diem = 0.5
                },
                new VietMatrixItem
                {
                    SoThuTu = 2,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Hành động, đặc điểm nhân vật",
                    YeuCauCanDat = "Nhận biết hành động, đặc điểm của nhân vật hoặc đối tượng chính trong bài.",
                    MucDo = "M1",
                    HinhThuc = "TN",
                    Diem = 0.5
                },
                new VietMatrixItem
                {
                    SoThuTu = 3,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Đọc hiểu hình thức: Từ ngữ, hình ảnh so sánh, nhân hóa",
                    YeuCauCanDat = "Nhận biết và hiểu ý nghĩa của biện pháp so sánh, nhân hóa hoặc từ ngữ, hình ảnh gợi cảm trong văn bản.",
                    MucDo = "M1",
                    HinhThuc = "TN",
                    Diem = 0.5
                },
                new VietMatrixItem
                {
                    SoThuTu = 4,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Ý nghĩa chi tiết trong bài",
                    YeuCauCanDat = "Hiểu nội dung hoặc thông tin cơ bản mà đoạn văn truyền tải.",
                    MucDo = "M1",
                    HinhThuc = "TN",
                    Diem = 0.5
                },
                new VietMatrixItem
                {
                    SoThuTu = 5,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Giải thích chi tiết, lý do",
                    YeuCauCanDat = "Giải thích được nguyên nhân, lý do vì sao nhân vật có hành động hoặc cảm xúc như trong bài.",
                    MucDo = "M2",
                    HinhThuc = "TL",
                    Diem = 1.0
                },
                new VietMatrixItem
                {
                    SoThuTu = 6,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Đọc hiểu hình thức: Suy luận, cảm nhận chi tiết nghệ thuật",
                    YeuCauCanDat = "Nêu suy nghĩ, cảm nhận về một chi tiết nghệ thuật, hình ảnh đặc sắc hoặc biện pháp tu từ trong văn bản.",
                    MucDo = "M2",
                    HinhThuc = "TL",
                    Diem = 1.0
                },
                new VietMatrixItem
                {
                    SoThuTu = 7,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Liên hệ, so sánh, kết nối: Đánh giá, nhận xét, bài học liên hệ bản thân",
                    YeuCauCanDat = "Rút ra bài học, thông điệp ý nghĩa của văn bản và liên hệ với bản thân trong thực tế.",
                    MucDo = "M3",
                    HinhThuc = "TL",
                    Diem = 1.0
                },
                new VietMatrixItem
                {
                    SoThuTu = 8,
                    MachKienThuc = "Kiến thức Tiếng Việt",
                    NoiDungKienThuc = "Từ vựng / Dấu câu: Tác dụng của dấu hai chấm, dấu ngoặc đơn hoặc từ loại",
                    YeuCauCanDat = "Nhận biết tác dụng của dấu hai chấm, dấu ngoặc đơn hoặc phân biệt từ loại trong câu.",
                    MucDo = "M1",
                    HinhThuc = "TN",
                    Diem = 0.5
                },
                new VietMatrixItem
                {
                    SoThuTu = 9,
                    MachKienThuc = "Kiến thức Tiếng Việt",
                    NoiDungKienThuc = "Ngữ pháp: Trạng ngữ chỉ nguyên nhân, mục đích, phương tiện",
                    YeuCauCanDat = "Nhận biết trạng ngữ chỉ nguyên nhân (Vì sao?), mục đích (Để làm gì?) hoặc phương tiện (Bằng gì?).",
                    MucDo = "M1",
                    HinhThuc = "TN",
                    Diem = 0.5
                },
                new VietMatrixItem
                {
                    SoThuTu = 10,
                    MachKienThuc = "Kiến thức Tiếng Việt",
                    NoiDungKienThuc = "Ngữ pháp: Xác định thành phần câu (Trạng ngữ, Chủ ngữ, Vị ngữ)",
                    YeuCauCanDat = "Phân tích cấu tạo câu: xác định rõ Trạng ngữ, Chủ ngữ, Vị ngữ trong câu văn cho trước.",
                    MucDo = "M2",
                    HinhThuc = "TL",
                    Diem = 1.0
                },
                new VietMatrixItem
                {
                    SoThuTu = 11,
                    MachKienThuc = "Kiến thức Tiếng Việt",
                    NoiDungKienThuc = "Hoạt động giao tiếp: Vận dụng đặt câu có trạng ngữ theo chủ đề",
                    YeuCauCanDat = "Vận dụng đặt 1-2 câu có trạng ngữ chỉ nguyên nhân hoặc mục đích nói về bảo vệ môi trường / yêu quê hương.",
                    MucDo = "M3",
                    HinhThuc = "TL",
                    Diem = 1.0
                }
            };
        }

        #endregion

        #region Ma trận chuẩn Khối 5 (SGK Kết nối tri thức)

        public static List<VietMatrixItem> GetGrade5MidTerm1Matrix()
        {
            return new List<VietMatrixItem>
            {
                new VietMatrixItem
                {
                    SoThuTu = 1,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Chi tiết trực tiếp trong bài đọc",
                    YeuCauCanDat = "Nhận biết các chi tiết, sự việc, nhân vật được nêu trực tiếp trong văn bản.",
                    MucDo = "M1",
                    HinhThuc = "TN",
                    Diem = 0.5
                },
                new VietMatrixItem
                {
                    SoThuTu = 2,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Diễn biến sự việc, ước mơ tuổi thơ",
                    YeuCauCanDat = "Nhận biết trình tự diễn biến sự việc hoặc ước mơ, khát vọng của nhân vật trong bài.",
                    MucDo = "M1",
                    HinhThuc = "TN",
                    Diem = 0.5
                },
                new VietMatrixItem
                {
                    SoThuTu = 3,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Đọc hiểu hình thức: Ý nghĩa từ ngữ gợi cảm, hình ảnh so sánh",
                    YeuCauCanDat = "Nhận biết và hiểu ý nghĩa của biện pháp so sánh hoặc từ ngữ, hình ảnh gợi cảm giàu chất thơ trong văn cảnh.",
                    MucDo = "M1",
                    HinhThuc = "TN",
                    Diem = 0.5
                },
                new VietMatrixItem
                {
                    SoThuTu = 4,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Chủ đề, cảm xúc của tác giả",
                    YeuCauCanDat = "Hiểu tình cảm, thái độ của tác giả đối với tuổi thơ, thiên nhiên hoặc ước mơ được thể hiện qua bài đọc.",
                    MucDo = "M1",
                    HinhThuc = "TN",
                    Diem = 0.5
                },
                new VietMatrixItem
                {
                    SoThuTu = 5,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Giải thích nguyên nhân, động cơ nhân vật",
                    YeuCauCanDat = "Giải thích nguyên nhân vì sao nhân vật kiên trì theo đuổi ước mơ hoặc có hành động ý nghĩa.",
                    MucDo = "M2",
                    HinhThuc = "TL",
                    Diem = 1.0
                },
                new VietMatrixItem
                {
                    SoThuTu = 6,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Đọc hiểu hình thức: Đánh giá, nhận xét chi tiết nghệ thuật",
                    YeuCauCanDat = "Nêu suy nghĩ, đánh giá về một chi tiết nghệ thuật, hình ảnh đẹp nhất hoặc biện pháp tu từ trong bài đọc.",
                    MucDo = "M2",
                    HinhThuc = "TL",
                    Diem = 1.0
                },
                new VietMatrixItem
                {
                    SoThuTu = 7,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Liên hệ, so sánh, kết nối: Bài học về khát vọng, ý chí và liên hệ bản thân",
                    YeuCauCanDat = "Rút ra bài học cuộc sống sâu sắc về lòng kiên trì, nuôi dưỡng ước mơ và liên hệ với chính bản thân em.",
                    MucDo = "M3",
                    HinhThuc = "TL",
                    Diem = 1.0
                },
                new VietMatrixItem
                {
                    SoThuTu = 8,
                    MachKienThuc = "Kiến thức Tiếng Việt",
                    NoiDungKienThuc = "Từ vựng: Từ đồng nghĩa",
                    YeuCauCanDat = "Nhận biết từ đồng nghĩa hoàn toàn hoặc từ đồng nghĩa không hoàn toàn trong câu văn.",
                    MucDo = "M1",
                    HinhThuc = "TN",
                    Diem = 0.5
                },
                new VietMatrixItem
                {
                    SoThuTu = 9,
                    MachKienThuc = "Kiến thức Tiếng Việt",
                    NoiDungKienThuc = "Ngữ pháp / Dấu câu: Tác dụng của dấu câu (dấu gạch ngang, dấu ngoặc kép) hoặc thành phần câu",
                    YeuCauCanDat = "Xác định đúng tác dụng của dấu gạch ngang, dấu ngoặc kép hoặc nhận biết thành phần câu trong ngữ cảnh.",
                    MucDo = "M1",
                    HinhThuc = "TN",
                    Diem = 0.5
                },
                new VietMatrixItem
                {
                    SoThuTu = 10,
                    MachKienThuc = "Kiến thức Tiếng Việt",
                    NoiDungKienThuc = "Từ vựng: Lựa chọn từ đồng nghĩa thích hợp / từ đa nghĩa",
                    YeuCauCanDat = "Tìm từ đồng nghĩa thay thế từ in đậm để câu văn thêm sinh động, chính xác hơn hoặc phân biệt nghĩa của từ.",
                    MucDo = "M2",
                    HinhThuc = "TL",
                    Diem = 1.0
                },
                new VietMatrixItem
                {
                    SoThuTu = 11,
                    MachKienThuc = "Kiến thức Tiếng Việt",
                    NoiDungKienThuc = "Hoạt động giao tiếp: Vận dụng từ trái nghĩa/đồng nghĩa đặt câu",
                    YeuCauCanDat = "Vận dụng đặt 1-2 câu có sử dụng cặp từ trái nghĩa hoặc từ đồng nghĩa nói về chủ đề ước mơ tuổi thơ.",
                    MucDo = "M3",
                    HinhThuc = "TL",
                    Diem = 1.0
                }
            };
        }

        public static List<VietMatrixItem> GetGrade5FinalTerm1Matrix()
        {
            return new List<VietMatrixItem>
            {
                new VietMatrixItem
                {
                    SoThuTu = 1,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Chi tiết trực tiếp trong bài đọc",
                    YeuCauCanDat = "Nhận biết các chi tiết, sự kiện lịch sử hoặc hình ảnh danh lam thắng cảnh được nêu trong bài.",
                    MucDo = "M1",
                    HinhThuc = "TN",
                    Diem = 0.5
                },
                new VietMatrixItem
                {
                    SoThuTu = 2,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Phẩm chất người anh hùng / Vẻ đẹp đất nước",
                    YeuCauCanDat = "Nhận biết nét đẹp tâm hồn, sự dũng cảm của thế hệ đi trước hoặc vẻ đẹp đặc trưng của quê hương.",
                    MucDo = "M1",
                    HinhThuc = "TN",
                    Diem = 0.5
                },
                new VietMatrixItem
                {
                    SoThuTu = 3,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Đọc hiểu hình thức: Ý nghĩa từ ngữ nghệ thuật, hình ảnh so sánh",
                    YeuCauCanDat = "Nhận biết và hiểu ý nghĩa của biện pháp so sánh hoặc từ ngữ nghệ thuật, hình ảnh mang tính biểu tượng trong văn bản.",
                    MucDo = "M1",
                    HinhThuc = "TN",
                    Diem = 0.5
                },
                new VietMatrixItem
                {
                    SoThuTu = 4,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Ý chính, thông điệp đoạn trích",
                    YeuCauCanDat = "Xác định ý chính hoặc thông điệp ca ngợi tình yêu quê hương đất nước qua đoạn văn.",
                    MucDo = "M1",
                    HinhThuc = "TN",
                    Diem = 0.5
                },
                new VietMatrixItem
                {
                    SoThuTu = 5,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Giải thích ý nghĩa lịch sử, hình tượng",
                    YeuCauCanDat = "Giải thích vì sao sự hy sinh, cống hiến của thế hệ cha ông lại để lại dấu ấn sâu sắc cho non sông.",
                    MucDo = "M2",
                    HinhThuc = "TL",
                    Diem = 1.0
                },
                new VietMatrixItem
                {
                    SoThuTu = 6,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Đọc hiểu hình thức: Cảm nhận về chi tiết nghệ thuật, tình cảm yêu nước",
                    YeuCauCanDat = "Nêu suy nghĩ, xúc động của em trước một chi tiết nghệ thuật giàu cảm xúc hoặc tình yêu quê hương đất nước của tác giả.",
                    MucDo = "M2",
                    HinhThuc = "TL",
                    Diem = 1.0
                },
                new VietMatrixItem
                {
                    SoThuTu = 7,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Liên hệ, so sánh, kết nối: Lòng tự hào dân tộc và trách nhiệm học sinh",
                    YeuCauCanDat = "Rút ra bài học về lòng biết ơn đối với thế hệ cha ông và trách nhiệm học tập, rèn luyện liên hệ bản thân em.",
                    MucDo = "M3",
                    HinhThuc = "TL",
                    Diem = 1.0
                },
                new VietMatrixItem
                {
                    SoThuTu = 8,
                    MachKienThuc = "Kiến thức Tiếng Việt",
                    NoiDungKienThuc = "Từ vựng: Từ nhiều nghĩa (nghĩa gốc và nghĩa chuyển)",
                    YeuCauCanDat = "Nhận biết từ được dùng theo nghĩa gốc hay nghĩa chuyển trong câu trích từ bài đọc.",
                    MucDo = "M1",
                    HinhThuc = "TN",
                    Diem = 0.5
                },
                new VietMatrixItem
                {
                    SoThuTu = 9,
                    MachKienThuc = "Kiến thức Tiếng Việt",
                    NoiDungKienThuc = "Ngữ pháp / Từ loại: Đại từ (đại từ xưng hô, đại từ thay thế)",
                    YeuCauCanDat = "Nhận biết đại từ xưng hô hoặc đại từ dùng để thay thế nhằm tránh lặp từ trong đoạn văn.",
                    MucDo = "M1",
                    HinhThuc = "TN",
                    Diem = 0.5
                },
                new VietMatrixItem
                {
                    SoThuTu = 10,
                    MachKienThuc = "Kiến thức Tiếng Việt",
                    NoiDungKienThuc = "Ngữ pháp: Kết từ (Quan hệ từ)",
                    YeuCauCanDat = "Xác định kết từ hoặc điền cặp quan hệ từ thích hợp để liên kết các vế câu hoặc từ ngữ.",
                    MucDo = "M2",
                    HinhThuc = "TL",
                    Diem = 1.0
                },
                new VietMatrixItem
                {
                    SoThuTu = 11,
                    MachKienThuc = "Kiến thức Tiếng Việt",
                    NoiDungKienThuc = "Hoạt động giao tiếp: Vận dụng đặt câu với từ nhiều nghĩa hoặc kết từ",
                    YeuCauCanDat = "Vận dụng đặt 2 câu phân biệt nghĩa gốc và nghĩa chuyển của một từ hoặc viết 2 câu dùng kết từ và đại từ thay thế.",
                    MucDo = "M3",
                    HinhThuc = "TL",
                    Diem = 1.0
                }
            };
        }

        public static List<VietMatrixItem> GetGrade5MidTerm2Matrix()
        {
            return new List<VietMatrixItem>
            {
                new VietMatrixItem
                {
                    SoThuTu = 1,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Chi tiết trực tiếp trong bài đọc",
                    YeuCauCanDat = "Nhận biết các chi tiết, sự việc cụ thể về nếp sống đẹp, văn hóa hoặc tinh thần cộng đồng.",
                    MucDo = "M1",
                    HinhThuc = "TN",
                    Diem = 0.5
                },
                new VietMatrixItem
                {
                    SoThuTu = 2,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Việc làm thể hiện tinh thần trách nhiệm",
                    YeuCauCanDat = "Nhận biết hành động dũng cảm, ý thức giữ gìn an ninh trật tự hoặc tinh thần vì cộng đồng.",
                    MucDo = "M1",
                    HinhThuc = "TN",
                    Diem = 0.5
                },
                new VietMatrixItem
                {
                    SoThuTu = 3,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Đọc hiểu hình thức: Từ ngữ gợi cảm, nghệ thuật miêu tả, so sánh",
                    YeuCauCanDat = "Nhận biết và hiểu ý nghĩa của biện pháp so sánh, điệp từ hoặc từ ngữ gợi cảm, hình ảnh giàu giá trị thẩm mỹ trong văn cảnh.",
                    MucDo = "M1",
                    HinhThuc = "TN",
                    Diem = 0.5
                },
                new VietMatrixItem
                {
                    SoThuTu = 4,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Nội dung, thông điệp xã hội của văn bản",
                    YeuCauCanDat = "Xác định tư tưởng chính hoặc thông điệp nhân văn về tình người và trách nhiệm xã hội.",
                    MucDo = "M1",
                    HinhThuc = "TN",
                    Diem = 0.5
                },
                new VietMatrixItem
                {
                    SoThuTu = 5,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Phân tích động cơ, nguyên nhân hành động đẹp",
                    YeuCauCanDat = "Giải thích nguyên nhân vì sao nhân vật sẵn sàng cống hiến hoặc giúp đỡ người khác.",
                    MucDo = "M2",
                    HinhThuc = "TL",
                    Diem = 1.0
                },
                new VietMatrixItem
                {
                    SoThuTu = 6,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Đọc hiểu hình thức: Cảm nhận về chi tiết nghệ thuật, nghĩa cử cao đẹp",
                    YeuCauCanDat = "Nêu suy nghĩ, cảm phục của em trước một chi tiết nghệ thuật giàu sức biểu cảm hoặc một tấm gương vì cộng đồng trong bài đọc.",
                    MucDo = "M2",
                    HinhThuc = "TL",
                    Diem = 1.0
                },
                new VietMatrixItem
                {
                    SoThuTu = 7,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Liên hệ, so sánh, kết nối: Bài học về lối sống đẹp và trách nhiệm cộng đồng",
                    YeuCauCanDat = "Rút ra bài học về lối sống nhân ái, có trách nhiệm với cộng đồng và liên hệ bản thân em.",
                    MucDo = "M3",
                    HinhThuc = "TL",
                    Diem = 1.0
                },
                new VietMatrixItem
                {
                    SoThuTu = 8,
                    MachKienThuc = "Kiến thức Tiếng Việt",
                    NoiDungKienThuc = "Từ vựng / Tu từ: Biện pháp tu từ điệp từ, điệp ngữ",
                    YeuCauCanDat = "Nhận biết biện pháp điệp từ, điệp ngữ hoặc từ ngữ biểu cảm trong câu trích từ bài đọc.",
                    MucDo = "M1",
                    HinhThuc = "TN",
                    Diem = 0.5
                },
                new VietMatrixItem
                {
                    SoThuTu = 9,
                    MachKienThuc = "Kiến thức Tiếng Việt",
                    NoiDungKienThuc = "Ngữ pháp: Cách nối các vế câu ghép bằng kết từ hoặc dấu câu",
                    YeuCauCanDat = "Nhận biết cặp kết từ biểu thị quan hệ nguyên nhân - kết quả, tương phản hoặc tăng tiến trong câu ghép.",
                    MucDo = "M1",
                    HinhThuc = "TN",
                    Diem = 0.5
                },
                new VietMatrixItem
                {
                    SoThuTu = 10,
                    MachKienThuc = "Kiến thức Tiếng Việt",
                    NoiDungKienThuc = "Ngữ pháp: Phân tích cấu tạo ngữ pháp câu ghép",
                    YeuCauCanDat = "Phân tích cấu tạo câu ghép: gạch chéo phân tách các vế, xác định rõ Chủ ngữ - Vị ngữ của từng vế.",
                    MucDo = "M2",
                    HinhThuc = "TL",
                    Diem = 1.0
                },
                new VietMatrixItem
                {
                    SoThuTu = 11,
                    MachKienThuc = "Kiến thức Tiếng Việt",
                    NoiDungKienThuc = "Hoạt động giao tiếp: Vận dụng đặt câu ghép có cặp kết từ",
                    YeuCauCanDat = "Vận dụng đặt 1-2 câu ghép có sử dụng cặp kết từ (nguyên nhân - kết quả hoặc tương phản) phù hợp với chủ đề bài học.",
                    MucDo = "M3",
                    HinhThuc = "TL",
                    Diem = 1.0
                }
            };
        }

        public static List<VietMatrixItem> GetGrade5FinalTerm2Matrix()
        {
            return new List<VietMatrixItem>
            {
                new VietMatrixItem
                {
                    SoThuTu = 1,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Chi tiết trực tiếp trong bài đọc",
                    YeuCauCanDat = "Nhận biết thông tin, số liệu, sự việc được nêu trực tiếp trong văn bản.",
                    MucDo = "M1",
                    HinhThuc = "TN",
                    Diem = 0.5
                },
                new VietMatrixItem
                {
                    SoThuTu = 2,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Tâm trạng, tình cảm của nhân vật",
                    YeuCauCanDat = "Nhận biết diễn biến tâm lý, tình cảm gắn bó với mái trường, bè bạn hoặc khát vọng tương lai.",
                    MucDo = "M1",
                    HinhThuc = "TN",
                    Diem = 0.5
                },
                new VietMatrixItem
                {
                    SoThuTu = 3,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Đọc hiểu hình thức: Nghệ thuật tu từ, hình ảnh biểu tượng",
                    YeuCauCanDat = "Nhận biết và hiểu ý nghĩa sâu sắc của biện pháp tu từ, từ ngữ hoặc hình ảnh biểu tượng trong bài.",
                    MucDo = "M1",
                    HinhThuc = "TN",
                    Diem = 0.5
                },
                new VietMatrixItem
                {
                    SoThuTu = 4,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Tư tưởng, chủ đề khái quát của văn bản",
                    YeuCauCanDat = "Hiểu tư tưởng, chủ đề khái quát mà tác giả muốn gửi gắm tới các bạn học sinh.",
                    MucDo = "M1",
                    HinhThuc = "TN",
                    Diem = 0.5
                },
                new VietMatrixItem
                {
                    SoThuTu = 5,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Giải thích ý nghĩa triết lý, nhân sinh",
                    YeuCauCanDat = "Giải thích nguyên nhân hoặc ý nghĩa sâu sắc của một nhận định, quan điểm trong bài đọc.",
                    MucDo = "M2",
                    HinhThuc = "TL",
                    Diem = 1.0
                },
                new VietMatrixItem
                {
                    SoThuTu = 6,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Đọc hiểu hình thức: Cảm nhận vẻ đẹp nghệ thuật, quan điểm cuộc sống",
                    YeuCauCanDat = "Nêu suy nghĩ, bày tỏ quan điểm của em trước một chi tiết nghệ thuật hoặc thông điệp giàu ý nghĩa trong bài đọc.",
                    MucDo = "M2",
                    HinhThuc = "TL",
                    Diem = 1.0
                },
                new VietMatrixItem
                {
                    SoThuTu = 7,
                    MachKienThuc = "Đọc hiểu văn bản",
                    NoiDungKienThuc = "Liên hệ, so sánh, kết nối: Thông điệp tương lai và hành trang chuyển cấp",
                    YeuCauCanDat = "Rút ra thông điệp cuộc sống sâu sắc và liên hệ với hành trang học tập, rèn luyện của bản thân em khi bước vào lớp 6.",
                    MucDo = "M3",
                    HinhThuc = "TL",
                    Diem = 1.0
                },
                new VietMatrixItem
                {
                    SoThuTu = 8,
                    MachKienThuc = "Kiến thức Tiếng Việt",
                    NoiDungKienThuc = "Ngữ pháp: Cặp từ hô ứng nối các vế câu ghép",
                    YeuCauCanDat = "Nhận biết câu ghép nối bằng cặp từ hô ứng (vừa... đã..., càng... càng..., chưa... đã...).",
                    MucDo = "M1",
                    HinhThuc = "TN",
                    Diem = 0.5
                },
                new VietMatrixItem
                {
                    SoThuTu = 9,
                    MachKienThuc = "Kiến thức Tiếng Việt",
                    NoiDungKienThuc = "Hoạt động giao tiếp / Ngữ pháp: Các biện pháp liên kết câu",
                    YeuCauCanDat = "Nhận diện phép liên kết câu (phép lặp từ ngữ, phép thay thế từ ngữ, phép dùng từ ngữ nối).",
                    MucDo = "M1",
                    HinhThuc = "TN",
                    Diem = 0.5
                },
                new VietMatrixItem
                {
                    SoThuTu = 10,
                    MachKienThuc = "Kiến thức Tiếng Việt",
                    NoiDungKienThuc = "Ngữ pháp / Giao tiếp: Phân tích phép liên kết câu trong đoạn trích",
                    YeuCauCanDat = "Chỉ rõ từ ngữ thực hiện phép liên kết giữa hai câu và nêu tác dụng tạo sự gắn kết chặt chẽ về nội dung.",
                    MucDo = "M2",
                    HinhThuc = "TL",
                    Diem = 1.0
                },
                new VietMatrixItem
                {
                    SoThuTu = 11,
                    MachKienThuc = "Kiến thức Tiếng Việt",
                    NoiDungKienThuc = "Hoạt động giao tiếp: Vận dụng viết đoạn văn có sử dụng phép liên kết câu",
                    YeuCauCanDat = "Viết đoạn văn ngắn 3-4 câu nói về mái trường/thầy cô hoặc tương lai, có sử dụng ít nhất một phép liên kết câu và chỉ rõ.",
                    MucDo = "M3",
                    HinhThuc = "TL",
                    Diem = 1.0
                }
            };
        }

        #endregion
    }
}
