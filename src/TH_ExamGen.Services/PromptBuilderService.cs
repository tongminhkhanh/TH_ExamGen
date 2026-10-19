using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using TH_ExamGen.Core;
using TH_ExamGen.Core.Curriculum;
using TH_ExamGen.Core.MathModels;
using TH_ExamGen.Core.VietModels;

namespace TH_ExamGen.Services
{
    public class PromptBuilderService
    {
        public static string BuildMathPrompt(int khoiLop, int thoiGianPhut, List<MathMatrixItem> matrix, string period = "Cuối Học kỳ II")
        {
            var sb = new StringBuilder();
            sb.AppendLine("Bạn là chuyên gia thẩm định và ra đề kiểm tra môn Toán cấp Tiểu học của Bộ Giáo dục và Đào tạo Việt Nam.");
            sb.AppendLine($"Hãy tạo một đề kiểm tra định kỳ môn Toán Khối {khoiLop} - ĐỢT: {period.ToUpper()}, thời gian làm bài {thoiGianPhut} phút, bám sát Chương trình GDPT 2018 (Bộ sách Kết nối tri thức với cuộc sống).");
            sb.AppendLine();
            sb.AppendLine(GetMathCurriculumBoundary(khoiLop, period));
            sb.AppendLine();
            AppendMathLevelGuidance(sb);
            sb.AppendLine();
            sb.AppendLine("Dưới đây là BẢN MA TRẬN ĐẶC TẢ CHI TIẾT 10 CÂU CẦN TUÂN THỦ NGHIÊM NGẶT:");
            
            foreach (var item in matrix)
            {
                sb.AppendLine($"- Câu {item.SoThuTu}: Mã mục chương trình '{item.MaMucChuongTrinh}' | Chủ đề '{item.ChuDe}' | Nội dung: '{item.NoiDungKienThuc}' | Yêu cầu cần đạt: '{item.YeuCauCanDat}' | Mức độ: {item.MucDo} | Hình thức: {item.HinhThuc} | Điểm: {item.Diem}đ | Có TikZ: {(item.CoHinhTikz ? "CÓ" : "KHÔNG")}");
            }

            sb.AppendLine();
            sb.AppendLine("QUY TẮC BẮT BUỘC KHI RA ĐỀ VÀ HƯỚNG DẪN CHẤM:");
            sb.AppendLine("1. ĐỒNG BỘ 100% TỪ MA TRẬN (BƯỚC 1) SANG BỘ CÂU HỎI (BƯỚC 2):");
            sb.AppendLine("   - Câu thứ [i] trong đề thi BẮT BUỘC phải kiểm tra đúng 100% 'Nội dung kiến thức', 'Yêu cầu cần đạt', 'Mức độ' (M1/M2/M3) và 'Hình thức' (TN/TL) của dòng thứ [i] trong ma trận.");
            sb.AppendLine("   - Giữ nguyên mã mục chương trình, chủ đề và phân phối điểm số của dòng ma trận; chỉ dùng kiến thức, thuật toán và cấu trúc bài nằm trong mục được cấp. Không tự mở rộng sang bài/tuần khác. Hệ thống từ chối câu không khớp mã mục hoặc ngoài phạm vi.");
            sb.AppendLine("2. SỐ LƯỢNG VÀ CƠ CẤU ĐỀ THI (CHUẨN THÔNG TƯ 27):");
            sb.AppendLine("   - Đúng 10 câu hỏi với tổng điểm chính xác 10.0 điểm: 7 câu Trắc nghiệm (Câu 1-7, mỗi câu 1.0đ = 7.0đ) và 3 câu Tự luận (Câu 8-10, mỗi câu 1.0đ = 3.0đ).");
            sb.AppendLine("   - Tỉ lệ mức độ nhận thức 40% - 40% - 20%: Câu 1-4 (M1 TN, 4.0đ); Câu 5-7 (M2 TN, 3.0đ) + Câu 8 (M2 TL, 1.0đ); Câu 9-10 (M3 TL, 2.0đ). Tuyệt đối không có câu trắc nghiệm ở Mức 3.");
            if (khoiLop == 4)
            {
                sb.AppendLine("   - CẢNH BÁO QUAN TRỌNG NHẤT KHỐI 4: TUYỆT ĐỐI KHÔNG SỬ DỤNG SỐ THẬP PHÂN ở bất kỳ câu nào trong toàn bộ đề thi (Số thập phân chỉ học ở Lớp 5). Toàn bộ câu hỏi Khối 4 chỉ được dùng Số tự nhiên và Phân số. Các câu 'Đặt tính rồi tính' của Khối 4 cũng chỉ đặt tính và tính với số tự nhiên (ví dụ 345 628 + 218 435); TUYỆT ĐỐI không dùng phép tính số thập phân như 105,7 + 34,92.");
            }
            sb.AppendLine("3. KIỂM TRA TÍNH ĐỒNG NHẤT GIỮA ĐỀ BÀI VÀ HƯỚNG DẪN CHẤM (CACBUOCGIAI):");
            sb.AppendLine("   - Câu dạng Trắc nghiệm 4 lựa chọn (loaiCauHoi = TN_4LuaChon): BẮT BUỘC có đủ 4 phương án dapAnA, dapAnB, dapAnC, dapAnD; dapAnDung là một trong các chữ cái A, B, C, D.");
            sb.AppendLine("   - Câu dạng Trắc nghiệm Đúng/Sai (loaiCauHoi = TN_DungSai): Bảng đáp án/hướng dẫn chấm phải ghi rõ kết quả Đ/S cho từng ý a, b, c, d (không ghi A, B, C, D).");
            sb.AppendLine("   - Câu dạng Tự luận có nhiều ý nhỏ: Đề bài có bao nhiêu ý thì Hướng dẫn chấm/cacBuocGiai phải giải chi tiết và chia điểm khớp bấy nhiêu ý.");
            sb.AppendLine("   - ĐỘ CHÍNH XÁC TOÁN HỌC 100%: Mọi phép tính số học và đổi đơn vị đo lường trong Đề bài và Hướng dẫn chấm (cacBuocGiai) BẮT BUỘC phải được tính toán và kiểm tra lại kết quả toán học chính xác 100% trước khi xuất ra.");
            sb.AppendLine("4. Phân số trong câu hỏi và đáp án BẮT BUỘC viết theo định dạng LaTeX: \\frac{tử}{mẫu} (ví dụ \\frac{3}{5}, \\frac{7}{12}). Các phép tính viết: +, -, \\times, \\div.");
            sb.AppendLine("5. QUY TẮC THIẾT KẾ ĐỒ HỌA SƯ PHẠM VÀ HÌNH VẼ TIKZ (coHinhTikz: true):");
            sb.AppendLine("   - BẮT BUỘC: Các nhãn chữ trong TikZ viết TIẾNG VIỆT CÓ DẤU đầy đủ (ví dụ: 'Gạo nếp:', 'Gạo tẻ:', 'Tổng: 150 kg', 'Lớp 4A', 'Học sinh'). Hệ thống sẽ tự biên dịch chuẩn.");
            sb.AppendLine("   - Chỉ tạo hình khi dòng ma trận được bật coHinhTikz và hình đó cần thiết cho đúng mục tiêu của dòng; không gắn một dạng hình với số thứ tự câu cố định.");
            sb.AppendLine("   - Với hình chia phần, hình học, bảng số liệu, biểu đồ hoặc sơ đồ, thể hiện đúng dữ kiện của câu; không thêm dạng toán, phép tính hoặc khái niệm chỉ vì có ví dụ minh họa bên dưới.");
            sb.AppendLine("   - Bảng số liệu có thể dùng Markdown trong trường noiDung để Word dựng bảng; chỉ dùng nếu yêu cầu của dòng ma trận cần đọc bảng.");
            sb.AppendLine("   - Với hình học TikZ, ghi nhãn tiếng Việt có dấu và giữ đúng tên, thứ tự đỉnh đã nêu trong câu.");
            sb.AppendLine("   - Đặt mã TikZ đầy đủ vào trường 'maTikz' (bắt đầu bằng \\begin{tikzpicture} và kết thúc bằng \\end{tikzpicture}).");
            sb.AppendLine();
            sb.AppendLine(GetMathFigureLibrary(khoiLop));
            sb.AppendLine("6. Dạng toán, phép toán, yêu cầu và cấu trúc mỗi câu phải đúng dòng ma trận và mục chương trình đã cấp. Các ví dụ hình vẽ trong thư viện chỉ hướng dẫn cách vẽ, không cấp phép dùng kiến thức hay dạng câu ngoài ma trận.");
            sb.AppendLine($"7. Giữ nguyên mức độ, hình thức, điểm và yêu cầu cần đạt của ma trận. Với mỗi câu tự luận, soDongOly và các bước giải phải phù hợp nội dung của chính dòng đó; tổng điểm các bước bằng đúng {matrix.Sum(item => item.Diem):0.#} điểm.");
            sb.AppendLine("8. Không tự thêm phép toán, chủ đề hoặc dạng toán vào câu có lời văn. Chỉ dùng cấu trúc thuộc mã chương trình của dòng đó; tuyệt đối không tự ý thêm các dạng toán ngoài phạm vi tuần của đợt kiểm tra đang chọn.");
            sb.AppendLine("   - QUY TẮC BIỂU ĐIỂM TIỂU HỌC: Mọi ý nhỏ và bước giải chỉ được chia điểm lẻ tối thiểu là 0.25đ hoặc 0.5đ (tuyệt đối KHÔNG chia 0.1đ, 0.2đ hay 0.3đ). Với câu 1.0 điểm: nếu 2 bước thì mỗi bước 0.5đ; nếu 3 bước thì (0.5đ - 0.25đ - 0.25đ); nếu 4 bước thì mỗi bước 0.25đ sao cho tổng các bước bằng đúng 1.0 điểm.");
            sb.AppendLine("   - QUY TẮC BẮT BUỘC RIÊNG CHO CÂU 8 (TỰ LUẬN - ĐẶT TÍNH RỒI TÍNH, 1.0 ĐIỂM):");
            sb.AppendLine("     + Câu 8 LUÔN LUÔN là Mức M2, dạng 'Đặt tính rồi tính' gồm đúng 4 ý nhỏ: a), b), c), d). Biểu điểm cacBuocGiai gồm đúng 4 bước giải chi tiết tương ứng (mỗi phép tính đúng được 0.25 điểm, tổng đúng 1.0 điểm).");
            sb.AppendLine("     + Tiêu đề câu hỏi viết: 'Đặt tính rồi tính:' và trình bày 4 phép tính a), b), c), d).");
            sb.AppendLine("     + Hướng dẫn chấm (cacBuocGiai) phải trình bày rõ kết quả từng ý a, b, c, d với điểm 0.25 cho mỗi ý.");
            if (khoiLop == 4 && ExamPeriodHelper.IsMidTerm1(period))
            {
                sb.AppendLine("     + ĐẶC BIỆT KHỐI 4 GIỮA KỲ I (bám sát chương trình tuần 1-9): Lấy từ giữa kỳ 1 xuống chương trình +, -, x, : của lớp 3:");
                sb.AppendLine("       * Phép cộng (+): Đặt tính và tính với các số tự nhiên trong phạm vi 100 000 hoặc lớp triệu (ví dụ: 45 238 + 29 147 hoặc 326 514 + 148 290).");
                sb.AppendLine("       * Phép trừ (-): Đặt tính và tính với các số tự nhiên trong phạm vi 100 000 hoặc lớp triệu (ví dụ: 83 516 - 36 248 hoặc 592 731 - 248 105).");
                sb.AppendLine("       * Phép nhân (x): Ôn tập kiến thức Lớp 3 lên — nhân số có nhiều chữ số với số có MỘT CHỮ SỐ (ví dụ: 14 218 x 4 hoặc 23 105 x 3). TUYỆT ĐỐI KHÔNG nhân với số có hai chữ số vì chưa học!");
                sb.AppendLine("       * Phép chia (:): Ôn tập kiến thức Lớp 3 lên — chia số có nhiều chữ số cho số có MỘT CHỮ SỐ (chia hết hoặc chia có dư, ví dụ: 48 576 : 3 hoặc 35 240 : 5). TUYỆT ĐỐI KHÔNG chia cho số có hai chữ số vì chưa học!");
            }
            else if (khoiLop == 4)
            {
                sb.AppendLine("     + Khối 4: 4 phép tính gồm +, -, x, : với số tự nhiên đúng phạm vi chương trình đến đợt kiểm tra hiện tại (TUYỆT ĐỐI không dùng số thập phân).");
            }
            else if (khoiLop == 5 && ExamPeriodHelper.IsMidTerm1(period))
            {
                sb.AppendLine("     + ĐẶC BIỆT KHỐI 5 GIỮA KỲ I (bám sát chương trình tuần 1-9): 4 phép tính Đặt tính rồi tính BẮT BUỘC chỉ gồm CỘNG và TRỪ số thập phân (gồm 2 phép cộng, 2 phép trừ: ví dụ a) cộng hai số thập phân, b) cộng số thập phân với số tự nhiên, c) trừ hai số thập phân, d) số tự nhiên trừ số thập phân). TUYỆT ĐỐI KHÔNG cho nhân, chia số thập phân vì tuần 1-9 chưa học!");
            }
            else if (khoiLop == 5 && ExamPeriodHelper.IsFinalTerm1(period))
            {
                sb.AppendLine("     + Khối 5 Cuối kỳ I: 4 phép tính Đặt tính rồi tính gồm đúng 4 phép tính cộng, trừ, nhân, chia số thập phân: a) cộng (+), b) trừ (-), c) nhân (x), d) chia (:).");
            }
            else if (khoiLop == 5 && ExamPeriodHelper.IsMidTerm2(period))
            {
                sb.AppendLine("     + Khối 5 Giữa kỳ II: 4 phép tính Đặt tính rồi tính gồm phép tính với số thập phân và số đo thời gian (a: nhân/chia số thập phân, b: cộng thời gian, c: trừ thời gian, d: nhân/chia thời gian).");
            }
            else if (khoiLop == 5)
            {
                sb.AppendLine("     + Khối 5 Cuối kỳ II: 4 phép tính Đặt tính rồi tính gồm 4 phép tính cộng, trừ, nhân, chia: a) cộng (+), b) trừ (-), c) nhân (x), d) chia (:) với số thập phân hoặc phân số.");
            }
            else
            {
                sb.AppendLine("     + Bốn phép tính gồm a) cộng, b) trừ, c) nhân, d) chia đúng theo phạm vi kiến thức đã học đến kỳ kiểm tra.");
            }
            sb.AppendLine("   - QUY CÁCH TRÌNH BÀY SƯ PHẠM TIỂU HỌC CHO CÁC BƯỚC GIẢI TỰ LUẬN (CACBUOCGIAI):");
            sb.AppendLine("     Căn cứ vào 'loaiCauHoi' và 'noiDungKienThuc' của từng dòng ma trận để áp dụng đúng quy chuẩn trình bày:");
            sb.AppendLine("     + ĐỐI VỚI BÀI TOÁN CÓ LỜI VĂN / TOÁN THỰC TẾ (loaiCauHoi = TL_LoiVan hoặc nội dung chứa giải toán có lời văn, chuyển động, thực tế, diện tích, thể tích, tỉ số phần trăm...):");
            sb.AppendLine("       * Nếu bài toán cần đổi đơn vị: Dòng đầu tiên của bước giải đầu tiên BẮT BUỘC ghi riêng: 'Đổi: ...\\n' (Ví dụ: 'Đổi: 5 dm = 0,5 m; 75 cm = 0,75 m\\n'). TUYỆT ĐỐI CẤM ghép văn xuôi giải thích vào dòng đổi.");
            sb.AppendLine("       * MỖI BƯỚC GIẢI TRONG 'cacBuocGiai' BẮT BUỘC TÁCH THÀNH 2 DÒNG RIÊNG BIỆT BẰNG KÝ TỰ '\\n':");
            sb.AppendLine("         Dòng 1: Câu lời giải (kết thúc bằng dấu hai chấm ':'). Ví dụ: 'Số lít dầu cửa hàng có là:'");
            sb.AppendLine("         Dòng 2: Phép tính (kèm đơn vị danh số trong ngoặc đơn, kết thúc bằng kết quả). Ví dụ: '12,5 \\times 4 = 50 (l)'");
            sb.AppendLine("         TUYỆT ĐỐI CẤM viết dính liền câu lời giải và phép tính trên cùng một dòng (như 'Số lít dầu... là: 12,5 x 4 = 50 (l)').");
            sb.AppendLine("       * DÒNG ĐÁP SỐ: Ở bước giải cuối cùng, BẮT BUỘC xuống dòng riêng biệt ghi: '\\nĐáp số: ...' (Ví dụ: '\\nĐáp số: 50 l dầu.'). TUYỆT ĐỐI CẤM dính vào đuôi của phép tính cuối cùng.");
            sb.AppendLine("       * TUYỆT ĐỐI KHÔNG dùng ký tự LaTeX rác '\\,' hoặc '\\ ' trong các con số nghìn (viết '18 000', CẤM viết '18\\,000').");
            sb.AppendLine("     + ĐỐI VỚI BÀI TOÁN TÍNH THUẬN TIỆN / BIẾN ĐỔI BIỂU THỨC (loaiCauHoi = TL_TinhToan hoặc nội dung chứa tính thuận tiện, biểu thức):");
            sb.AppendLine("       * Trình bày các bước biến đổi biểu thức toán học rõ ràng từng dòng, KHÔNG bịa câu lời giải văn xuôi. Mỗi bước trong cacBuocGiai ghi rõ biểu thức sau khi nhóm/rút gọn và điểm số tương ứng.");
            sb.AppendLine("9. Trường loaiCauHoi CHỈ được nhận đúng một trong các giá trị sau (TUYỆT ĐỐI không tự chế giá trị khác như 'TuLuan', 'Tuluan', 'TL'): TN_4LuaChon (trắc nghiệm 4 lựa chọn), TN_DungSai (trắc nghiệm đúng/sai), TN_TraLoiNgan (trắc nghiệm trả lời ngắn), TL_TinhToan (tự luận tính toán/giải toán), TL_LoiVan (tự luận lời văn/giải thích/trình bày). Câu tự luận bắt buộc dùng TL_TinhToan hoặc TL_LoiVan.");
            sb.AppendLine("10. Trả về định dạng JSON thuần túy, tuyệt đối không bọc trong markdown codeblock, không thêm lời chào, cấu trúc JSON như sau:");
            sb.AppendLine(@"{
  ""monHoc"": ""Toán"",
  ""khoiLop"": 4,
  ""thoiGianPhut"": 40,
  ""tieuDe"": ""BÀI KIỂM TRA ĐỊNH KỲ CUỐI HỌC KỲ II"",
  ""danhSachCauHoi"": [
    {
      ""soThuTu"": 1,
      ""maMucChuongTrinh"": ""KNTT-..."",
      ""chuDe"": ""Số và phép tính"",
      ""yeuCauCanDat"": ""..."",
      ""mucDo"": ""M1"",
      ""loaiCauHoi"": ""TN_4LuaChon"",
      ""diem"": 1.0,
      ""noiDung"": ""..."",
      ""dapAnA"": ""..."",
      ""dapAnB"": ""..."",
      ""dapAnC"": ""..."",
      ""dapAnD"": ""..."",
      ""dapAnDung"": ""A"",
      ""coHinhTikz"": false,
      ""maTikz"": """",
      ""soDongOly"": 0,
      ""cacBuocGiai"": [
        { ""buoc"": 1, ""noiDung"": ""..."", ""diem"": 0.25 }
      ]
    },
    {
      ""soThuTu"": 8,
      ""maMucChuongTrinh"": ""KNTT-..."",
      ""chuDe"": ""Số và phép tính"",
      ""yeuCauCanDat"": ""..."",
      ""mucDo"": ""M2"",
      ""loaiCauHoi"": ""TL_TinhToan"",
      ""diem"": 1.0,
      ""noiDung"": ""Đặt tính rồi tính:\na) ...    b) ...\nc) ...    d) ..."",
      ""coHinhTikz"": false,
      ""maTikz"": """",
      ""soDongOly"": 4,
      ""cacBuocGiai"": [
        { ""buoc"": 1, ""noiDung"": ""a) Đặt tính và tính đúng: ..."", ""diem"": 0.25 },
        { ""buoc"": 2, ""noiDung"": ""b) Đặt tính và tính đúng: ..."", ""diem"": 0.25 },
        { ""buoc"": 3, ""noiDung"": ""c) Đặt tính và tính đúng: ..."", ""diem"": 0.25 },
        { ""buoc"": 4, ""noiDung"": ""d) Đặt tính và tính đúng: ..."", ""diem"": 0.25 }
      ]
    },
    {
      ""soThuTu"": 9,
      ""maMucChuongTrinh"": ""KNTT-..."",
      ""chuDe"": ""Số và phép tính"",
      ""yeuCauCanDat"": ""..."",
      ""mucDo"": ""M3"",
      ""loaiCauHoi"": ""TL_LoiVan"",
      ""diem"": 1.0,
      ""noiDung"": ""Một mảnh đất hình chữ nhật có chiều dài ..."",
      ""coHinhTikz"": false,
      ""maTikz"": """",
      ""soDongOly"": 6,
      ""cacBuocGiai"": [
        { ""buoc"": 1, ""noiDung"": ""Đổi: 5 dm = 0,5 m\nChiều rộng mảnh đất là:\n12,5 - 0,5 = 12 (m)"", ""diem"": 0.5 },
        { ""buoc"": 2, ""noiDung"": ""Diện tích mảnh đất là:\n12,5 \\times 12 = 150 (m^2)\nĐáp số: 150 m^2."", ""diem"": 0.5 }
      ]
    }
  ]
}");

            return sb.ToString();
        }

        public static string BuildVietPrompt(int khoiLop, int thoiGianPhut, List<VietMatrixItem> matrix, ReadingPassage? passage = null, string chuDe = "Bảo vệ môi trường, tình yêu thiên nhiên", string period = "Cuối Học kỳ II")
        {
            var sb = new StringBuilder();
            sb.AppendLine("Bạn là chuyên gia thẩm định và ra đề kiểm tra môn Tiếng Việt cấp Tiểu học của Bộ Giáo dục và Đào tạo Việt Nam.");
            sb.AppendLine($"Hãy xây dựng một đề kiểm tra định kỳ môn Tiếng Việt Khối {khoiLop} - ĐỢT: {period.ToUpper()}, bám sát Thông tư 27/2020/BGDĐT và Chương trình GDPT 2018 (Bộ sách Kết nối tri thức với cuộc sống).");
            sb.AppendLine();
            sb.AppendLine(GetVietCurriculumBoundary(khoiLop, period));
            sb.AppendLine();

            if (passage != null && !string.IsNullOrWhiteSpace(passage.NoiDung))
            {
                sb.AppendLine("NGỮ LIỆU ĐÃ CÓ SẴN (Hãy sử dụng nguyên văn bài đọc này; không chép hoặc bổ sung văn bản từ SGK Kết nối tri thức):");
                sb.AppendLine($"Tiêu đề: {passage.TieuDe}");
                sb.AppendLine($"Tác giả: {passage.TacGia}");
                sb.AppendLine($"Thể loại: {passage.TheLoai}");
                sb.AppendLine($"Chủ điểm kỳ đã xác nhận: {chuDe}");
                sb.AppendLine($"Nguồn gốc: {passage.NguonGoc}; thông tin nguồn: {passage.NguonChiTiet}");
                sb.AppendLine($"Nội dung bài đọc:\n{passage.NoiDung}");
            }
            else
            {
                int minWords = khoiLop == 5 ? 350 : 280;
                int maxWords = khoiLop == 5 ? 450 : 350;
                sb.AppendLine($"YÊU CẦU TẠO NGỮ LIỆU BÀI ĐỌC HIỂU (BẮT BUỘC ĐỦ ĐỘ DÀI VÀ CẤU TRÚC THEO CHUẨN GDPT 2018):");
                sb.AppendLine($"- Chủ điểm bắt buộc phải là một chủ điểm đã xác nhận của Tiếng Việt lớp {khoiLop} trong kỳ {period}: '{chuDe}'. Sáng tác văn bản mới, không lấy hoặc phỏng tác văn bản từ SGK Kết nối tri thức.");
                sb.AppendLine($"- ĐỘ DÀI BẮT BUỘC: Khoảng {minWords} - {maxWords} tiếng (TUYỆT ĐỐI KHÔNG viết ngắn dưới {minWords} tiếng).");
                sb.AppendLine($"- CẤU TRÚC VĂN BẢN BẮT BUỘC GỒM 3 - 4 ĐOẠN VĂN HOÀN CHỈNH:");
                sb.AppendLine("  + Đoạn 1 (Mở đầu): Giới thiệu cụ thể bối cảnh thời gian, không gian, cảnh vật và các nhân vật.");
                sb.AppendLine("  + Đoạn 2 & 3 (Thân bài - Diễn biến): Kể chi tiết sự việc, cao trào hoặc tình huống thử thách. BẮT BUỘC có ít nhất 1-2 câu đối thoại trực tiếp giữa các nhân vật (hoặc lời nói, suy nghĩ nội tâm); miêu tả sinh động hành động, cảm xúc; lồng ghép các hình ảnh so sánh hoặc nhân hóa tự nhiên, giàu sức gợi cảm.");
                sb.AppendLine("  + Đoạn 4 (Kết bài): Kết thúc câu chuyện, dư ba cảm xúc, bài học cuộc sống hoặc thông điệp nhân văn sâu sắc.");
                sb.AppendLine($"- LƯU Ý QUAN TRỌNG: TUYỆT ĐỐI CẤM viết tóm tắt ngắn ngủi hoặc cụt ý. Bài đọc PHẢI có đầy đủ chi tiết, tình tiết và vẻ đẹp nghệ thuật từ ngữ để học sinh lớp {khoiLop} có ngữ liệu trả lời 7 câu hỏi đọc hiểu (đặc biệt là câu đọc hiểu hình thức, giải thích nguyên nhân và liên hệ rút ra bài học).");
            }

            sb.AppendLine();
            sb.AppendLine($"Dưới đây là BẢN MA TRẬN ĐẶC TẢ CHI TIẾT {matrix.Count} CÂU HỎI (Phần Kiểm tra đọc hiểu & Luyện từ và câu - 8 điểm):");
            foreach (var item in matrix)
            {
                sb.AppendLine($"- Câu {item.SoThuTu}: Mã mục chương trình '{item.MaMucChuongTrinh}' | Mạch '{item.MachKienThuc}' | Nội dung: '{item.NoiDungKienThuc}' | Yêu cầu: '{item.YeuCauCanDat}' | Mức: {item.MucDo} | Hình thức: {item.HinhThuc} | Điểm: {item.Diem}đ");
            }

            sb.AppendLine();
            sb.AppendLine("QUY TẮC BẮT BUỘC VỀ BỘ CÂU HỎI VÀ SỐ DÒNG Ô LY (soDongOly):");
            sb.AppendLine("1. Giữ đúng khung 11 câu/8 điểm; phân bố 7 câu Đọc hiểu/5 điểm và 4 câu Kiến thức Tiếng Việt/3 điểm như ma trận đã cấp.");
            sb.AppendLine("2. Với từng câu, giữ nguyên mã mục chương trình, mạch, yêu cầu cần đạt, mức độ, hình thức và điểm của dòng ma trận tương ứng. Nội dung đọc hiểu phải dựa trên bài đọc này; kỹ năng Tiếng Việt chỉ lấy từ mục đã xác nhận.");
            sb.AppendLine("3. Không bắt buộc phải tạo câu cho Ngữ pháp, Hoạt động giao tiếp, Sự phát triển ngôn ngữ hoặc Văn bản thông tin nếu ma trận không có mục đã xác nhận tương ứng. Từ đồng nghĩa, trái nghĩa, đồng âm và đa nghĩa luôn thuộc Từ vựng.");
            sb.AppendLine("4. Nếu mục đã chọn không đủ để lập khung mà không vượt nội dung của mục đó, dừng và báo lỗi; không tự thêm dạng câu ngoài ma trận.");
            sb.AppendLine("5. CUNG CẤP HƯỚNG DẪN CHẤM (huongDanCham) VÀ BIỂU ĐIỂM CHO TỪNG CÂU:");
            AppendVietGradingGuideRules(sb, "   ");
            sb.AppendLine("6. Trường loaiCauHoi của mỗi câu CHỈ được nhận đúng một trong hai giá trị sau (TUYỆT ĐỐI không tự chế giá trị khác như 'TuLuan', 'Tuluan'): \"TN_4LuaChon\" (câu trắc nghiệm 4 lựa chọn) hoặc \"TL\" (câu tự luận).");
            sb.AppendLine("7. Trả về JSON thuần túy, không có markdown codeblock, không lời dẫn, theo mẫu cấu trúc sau:");
            sb.AppendLine(@"{
  ""monHoc"": ""Tiếng Việt"",
  ""khoiLop"": " + khoiLop + @",
  ""thoiGianPhut"": " + thoiGianPhut + @",
  ""tieuDe"": ""BÀI KIỂM TRA ĐỊNH KỲ " + period.ToUpper() + @""",
  ""baiDoc"": {
    ""tieuDe"": ""..."",
    ""tacGia"": ""..."",
    ""theLoai"": ""Văn bản văn học"",
    ""chuDe"": ""..."",
    ""maMucChuongTrinh"": ""KNTT-..."",
    ""nguonGoc"": ""AI-sáng-tác; không lấy từ SGK Kết nối tri thức"",
    ""nguonChiTiet"": ""AI"",
    ""noiDung"": ""...""
  },
  ""danhSachCauHoi"": [
    {
      ""soThuTu"": 1,
      ""maMucChuongTrinh"": ""KNTT-..."",
      ""machKienThuc"": ""Đọc hiểu văn bản"",
      ""yeuCauCanDat"": ""..."",
      ""mucDo"": ""M1"",
      ""loaiCauHoi"": ""TN_4LuaChon"",
      ""diem"": 0.5,
      ""noiDung"": ""..."",
      ""dapAnA"": ""..."",
      ""dapAnB"": ""..."",
      ""dapAnC"": ""..."",
      ""dapAnD"": ""..."",
      ""dapAnDung"": ""A"",
      ""huongDanCham"": ""Đáp án đúng: A. Lý do: ..."",
      ""soDongOly"": 0
    },
    {
      ""soThuTu"": 9,
      ""maMucChuongTrinh"": ""KNTT-..."",
      ""machKienThuc"": ""Đọc hiểu văn bản"",
      ""yeuCauCanDat"": ""..."",
      ""mucDo"": ""M2"",
      ""loaiCauHoi"": ""TL"",
      ""diem"": 1.0,
      ""noiDung"": ""..."",
      ""huongDanCham"": ""Đáp án gợi ý: ...\nBiểu điểm:\n- ...: 0.5 điểm.\n- ...: 0.5 điểm."",
      ""soDongOly"": 3
    }
  ]
}");

            return sb.ToString();
        }

        /// <summary>
        /// Quy tắc BẮT BUỘC cho hướng dẫn chấm (huongDanCham) môn Tiếng Việt: 2 khối nhãn
        /// "Đáp án gợi ý:" + "Biểu điểm:" cho tự luận, "Đáp án đúng: X. Lý do: ..." cho trắc nghiệm.
        /// Dùng chung cho prompt sinh đề và prompt "viết lại hướng dẫn chấm" để không lệch chuẩn.
        /// </summary>
        private static void AppendVietGradingGuideRules(StringBuilder sb, string indent = "")
        {
            sb.AppendLine(indent + "a) VỚI CÂU TỰ LUẬN: huongDanCham phải gồm ĐÚNG 2 KHỐI theo thứ tự sau:");
            sb.AppendLine(indent + "   - Khối 1 mở đầu bằng \"Đáp án gợi ý: \" rồi nêu NỘI DUNG TRẢ LỜI CỤ THỂ mà học sinh cần đạt, bám sát ngữ liệu bài đọc (nêu rõ các ý/từ ngữ/hình ảnh cần có - giáo viên đọc là chấm được ngay).");
            sb.AppendLine(indent + "     + Câu hỏi về biện pháp tu từ: BẮT BUỘC ghi rõ tên biện pháp + từ ngữ/hình ảnh làm căn cứ + tác dụng của biện pháp đó.");
            sb.AppendLine(indent + "     + Câu hỏi đặt câu/viết đoạn văn: BẮT BUỘC có 1-2 câu văn mẫu cụ thể.");
            sb.AppendLine(indent + "     + Câu hỏi nêu bài học/liên hệ bản thân: BẮT BUỘC gợi ý 2-3 nội dung chấp nhận được.");
            sb.AppendLine(indent + "   - Khối 2 mở đầu bằng \"Biểu điểm: \" rồi liệt kê barem chi tiết, MỖI Ý MỘT DÒNG bắt đầu bằng \"- \", ghi rõ số điểm từng ý (0.25 / 0.5 / 0.75 / 1.0). Tổng điểm các ý phải ĐÚNG BẰNG số điểm của câu.");
            sb.AppendLine(indent + "   - Ví dụ 1 giá trị huongDanCham hợp lệ (khi ghi vào JSON, ký tự xuống dòng mã hóa thành \\n):");
            sb.AppendLine(indent + "     \"Đáp án gợi ý: Hai tính từ chỉ đặc điểm của tiếng suối là “trong trẻo” và “mát lạnh”.\\nBiểu điểm:\\n- Tìm đúng cả 2 tính từ: 1.0 điểm.\\n- Tìm đúng 1 tính từ: 0.5 điểm.\\n- Không tìm được tính từ nào: 0 điểm.\"");
            sb.AppendLine(indent + "   - TUYỆT ĐỐI KHÔNG viết chung chung kiểu \"Nêu đúng và đầy đủ nguyên nhân: 1.0 điểm\" hay \"Chỉ ra đúng biện pháp tu từ: 0.5 điểm\" mà không cho biết nội dung trả lời đúng là gì.");
            sb.AppendLine(indent + "b) VỚI CÂU TRẮC NGHIỆM: huongDanCham ghi \"Đáp án đúng: <A/B/C/D>. Lý do: <một câu ngắn giải thích vì sao đáp án đúng>\". BẮT BUỘC có phần \"Lý do\" ở các câu trắc nghiệm về từ vựng/nghĩa của từ (từ đồng nghĩa, trái nghĩa, đa nghĩa, đồng âm); khuyến khích thêm ở các câu còn lại khi cần.");
            sb.AppendLine(indent + "c) QUY TẮC BIỂU ĐIỂM TIỂU HỌC: Mọi ý nhỏ chỉ được chia điểm lẻ tối thiểu là 0.25đ hoặc 0.5đ (tuyệt đối KHÔNG chia lẻ 0.1đ hay 0.2đ theo chuẩn Thông tư 27).");
            sb.AppendLine(indent + "   - Với câu tự luận 1.0 điểm có 2 ý nhỏ (hoặc 2 yêu cầu): Mỗi ý đúng đạt 0.5 điểm (2 × 0.5 = 1.0đ).");
            sb.AppendLine(indent + "   - Với câu tự luận 1.0 điểm có 4 từ/chi tiết/chỗ trống: Mỗi ý đúng đạt 0.25 điểm (4 × 0.25 = 1.0đ).");
            sb.AppendLine(indent + "   - Với câu tự luận diễn đạt/rút ra bài học/liên hệ bản thân hoặc viết đoạn văn (1.0 điểm): Chia barem rõ ràng theo 0.25đ và 0.5đ (Ví dụ: Nêu được bài học ý nghĩa: 0.5đ; Nêu việc làm cụ thể liên hệ bản thân: 0.5đ; hoặc Đạt trọn vẹn: 1.0đ; Đạt ý chính nhưng chưa trọn vẹn: 0.5đ hoặc 0.75đ; Nêu được 1 ý nhỏ: 0.25đ).");
        }

        /// <summary>
        /// Prompt cho nút "Viết lại Hướng dẫn chấm (AI)": giữ nguyên đề hiện có của giáo viên,
        /// chỉ viết lại trường huongDanCham chi tiết (có Đáp án gợi ý + Biểu điểm) cho mọi câu.
        /// </summary>
        public static string BuildVietGuideRewritePrompt(VietExamPaper paper)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Bạn là chuyên gia thẩm định và chấm thi môn Tiếng Việt cấp Tiểu học của Bộ Giáo dục và Đào tạo Việt Nam (Chương trình GDPT 2018).");
            sb.AppendLine("Nhiệm vụ: VIẾT LẠI HƯỚNG DẪN CHẤM CHI TIẾT (huongDanCham) cho TẤT CẢ các câu hỏi của đề kiểm tra dưới đây.");
            sb.AppendLine("Mục tiêu: giáo viên đọc hướng dẫn chấm là chấm được ngay, không phải tự đoán nội dung đúng; TUYỆT ĐỐI không viết chung chung.");
            sb.AppendLine("Giữ nguyên số thứ tự câu và số điểm từng câu; chỉ làm mới trường huongDanCham.");
            sb.AppendLine();

            if (paper.BaiDoc != null && !string.IsNullOrWhiteSpace(paper.BaiDoc.NoiDung))
            {
                sb.AppendLine("NGỮ LIỆU BÀI ĐỌC:");
                sb.AppendLine($"- Tiêu đề: {paper.BaiDoc.TieuDe}");
                if (!string.IsNullOrWhiteSpace(paper.BaiDoc.TacGia))
                {
                    sb.AppendLine($"- Tác giả: {paper.BaiDoc.TacGia}");
                }
                sb.AppendLine($"- Nội dung: {paper.BaiDoc.NoiDung}");
                sb.AppendLine();
            }

            sb.AppendLine($"DANH SÁCH {paper.DanhSachCauHoi.Count} CÂU HỎI HIỆN CÓ CỦA ĐỀ:");
            foreach (var q in paper.DanhSachCauHoi.OrderBy(x => x.SoThuTu))
            {
                if (q.LoaiCauHoi == "TN_4LuaChon")
                {
                    sb.AppendLine($"- Câu {q.SoThuTu} (TRẮC NGHIỆM {q.Diem} điểm): {q.NoiDung}");
                    sb.AppendLine($"    A. {q.DapAnA}");
                    sb.AppendLine($"    B. {q.DapAnB}");
                    sb.AppendLine($"    C. {q.DapAnC}");
                    sb.AppendLine($"    D. {q.DapAnD}");
                    sb.AppendLine($"    => Đáp án đúng: {q.DapAnDung}");
                }
                else
                {
                    sb.AppendLine($"- Câu {q.SoThuTu} (TỰ LUẬN {q.Diem} điểm - mức {q.MucDo}): {q.NoiDung}");
                }

                if (!string.IsNullOrWhiteSpace(q.HuongDanCham))
                {
                    sb.AppendLine($"    (Bản hướng dẫn chấm cũ chưa chi tiết, cần viết lại hoàn toàn: {q.HuongDanCham})");
                }
            }

            sb.AppendLine();
            AppendVietGradingGuideRules(sb);
            sb.AppendLine();
            sb.AppendLine("YÊU CẦU ĐẦU RA: Trả về JSON thuần túy, không markdown fence, đúng cấu trúc:");
            sb.AppendLine("{ \"danhSach\": [ { \"soThuTu\": 1, \"huongDanCham\": \"...\" }, { \"soThuTu\": 2, \"huongDanCham\": \"...\" } ] }");
            sb.AppendLine($"Trong đó danhSach phải có ĐỦ {paper.DanhSachCauHoi.Count} phần tử (mỗi câu một phần tử), soThuTu khớp đúng số thứ tự câu hỏi hiện có, huongDanCham là hướng dẫn chấm MỚI viết theo đúng quy tắc trên.");
            return sb.ToString();
        }

        /// <summary>
        /// Khối định nghĩa 3 mức độ nhận thức và cách viết yêu cầu cần đạt theo mức (Thông tư 27),
        /// dùng chung cho cả ba prompt Toán (gợi ý ma trận, sinh đề, thẩm định) để AI hiểu và giữ đúng mức.
        /// </summary>
        private static void AppendMathLevelGuidance(StringBuilder sb)
        {
            sb.AppendLine("=== QUY TẮC VIẾT CỘT 'NỘI DUNG KIẾN THỨC' VÀ 'YÊU CẦU CẦN ĐẠT' (THÔNG TƯ 27 & GDPT 2018) ===");
            sb.AppendLine("1. QUY TẮC ĐẶT TÊN CỘT 'NỘI DUNG KIẾN THỨC':");
            sb.AppendLine("   - BẮT BUỘC ghi tên 'Đơn vị kiến thức cụ thể' theo Chương trình GDPT 2018 (Ví dụ: 'Số có nhiều chữ số đến lớp triệu', 'Bốn phép tính với số có nhiều chữ số', 'Đặt tính rồi tính (Cộng, trừ, nhân, chia)', 'Đơn vị đo diện tích', 'Chu vi và diện tích hình phẳng', 'Số thập phân và các phép tính'...).");
            sb.AppendLine("   - TUYỆT ĐỐI KHÔNG dùng tên chương/chủ đề chung chung trong SGK như: 'Ôn tập đầu năm', 'Ôn tập và bổ sung', 'Ôn tập học kỳ', 'Chủ đề 1...'.");
            sb.AppendLine("   - Không để 2 câu hỏi ở 2 dạng khác nhau (ví dụ 1 câu đọc/viết số TN và 1 câu đặt tính TL) có cùng một tên 'Nội dung kiến thức' mơ hồ. Mỗi dòng phải chỉ rõ kiến thức mà câu hỏi đó kiểm tra.");
            sb.AppendLine();
            sb.AppendLine("2. QUY TẮC VIẾT CỘT 'YÊU CẦU CẦN ĐẠT' (Khớp 100% với cột Nội dung kiến thức):");
            sb.AppendLine("   - Nội dung của cột 'Yêu cầu cần đạt' trên mỗi dòng BẮT BUỘC phải chứa các từ khóa chính của cột 'Nội dung kiến thức' ở ngay dòng đó (Hỏi nội dung gì thì Yêu cầu cần đạt phải mô tả đúng nội dung đó, tuyệt đối không râu ông nọ cắm cằm bà kia).");
            sb.AppendLine("   - Câu văn trong cột 'Yêu cầu cần đạt' BẮT BUỘC phải bắt đầu bằng ĐỘNG TỪ hành động tương ứng với Mức độ nhận thức:");
            sb.AppendLine("     + M1 (Nhận biết): Mở đầu bằng các từ 'Nhận biết được...', 'Đọc, viết được...', 'Nêu được...', 'Xác định được...', 'Chỉ ra được...'.");
            sb.AppendLine("     + M2 (Kết nối/Thông hiểu): Mở đầu bằng các từ 'Thực hiện được...', 'Đặt tính và tính được...', 'Tính được...', 'So sánh, sắp xếp được...', 'Chuyển đổi được...', 'Giải thích được...'.");
            sb.AppendLine("     + M3 (Vận dụng): Mở đầu bằng các từ 'Vận dụng...', 'Giải quyết được vấn đề thực tiễn...', 'Giải và trình bày được bài toán thực tế...'.");
            sb.AppendLine("   - Mục đã có yêu cầu cần đạt riêng cho M1/M2/M3 trong danh mục: SAO CHÉP NGUYÊN VĂN yêu cầu cần đạt đúng mức cho dòng tương ứng.");
            sb.AppendLine("   - Cùng một mục xuất hiện ở các mức khác nhau: yêu cầu cần đạt của từng mức BẮT BUỘC khác nhau, không lặp y hệt.");
            sb.AppendLine("   - Mục chỉ có một yêu cầu cần đạt chung (không tách mức): sao chép nguyên văn cho mọi dòng của mục đó.");
            sb.AppendLine("   - TUYỆT ĐỐI không tự chế yêu cầu cần đạt khác với danh mục đã xác nhận.");
        }

        /// <summary>
        /// Ghi một mục Toán kèm yêu cầu cần đạt riêng theo M1/M2/M3 (nếu mục đã tách mức),
        /// để AI sao chép nguyên văn đúng yêu cầu của từng mức khi lập dòng ma trận.
        /// </summary>
        private static void AppendMathTopicWithLevels(StringBuilder sb, MathTopicItem topic)
        {
            sb.AppendLine("- " + CurriculumScopePolicy.FormatTopic(topic));
            if (!topic.CoYeuCauTheoMuc) return;
            sb.AppendLine("    • Yêu cầu cần đạt M1: " + topic.YeuCauCanDatM1);
            sb.AppendLine("    • Yêu cầu cần đạt M2: " + topic.YeuCauCanDatM2);
            sb.AppendLine("    • Yêu cầu cần đạt M3: " + topic.YeuCauCanDatM3);
        }

        public static string GetMathTopicCatalogText(int khoiLop, string period)
        {
            var topics = CurriculumScopePolicy.GetConfirmedMathTopics(khoiLop, period);
            var sb = new StringBuilder();
            sb.AppendLine($"=== DANH MỤC CHỦ ĐỀ/BÀI HỌC SGK KẾT NỐI TRI THỨC - MÔN TOÁN LỚP {khoiLop} - ĐỢT: {period.ToUpper()} ===");
            sb.AppendLine("Chỉ mục đã xác nhận và kết thúc trong mốc tuần của kỳ mới được dùng.");
            if (topics.Count == 0) sb.AppendLine("KHÔNG CÓ MỤC NÀO ĐÃ XÁC NHẬN. Không được sinh ma trận hoặc đề.");
            foreach (var t in topics)
            {
                AppendMathTopicWithLevels(sb, t);
            }
            return sb.ToString();
        }

        public static string BuildMatrixSuggestionPrompt(int khoiLop, string period, string userRequirement, bool autoSelectTopics = false, List<string>? selectedTopics = null)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Bạn là chuyên gia khảo thí Tiểu học của Bộ Giáo dục và Đào tạo Việt Nam.");
            sb.AppendLine($"Hãy xây dựng một BẢN MA TRẬN ĐẶC TẢ ĐỀ KIỂM TRA MÔN TOÁN LỚP {khoiLop} - ĐỢT: {period.ToUpper()}.");
            sb.AppendLine("Bộ sách giáo khoa áp dụng: KẾT NỐI TRI THỨC VỚI CUỘC SỐNG (Chương trình GDPT 2018).");
            sb.AppendLine();
            sb.AppendLine(GetMathCurriculumBoundary(khoiLop, period));
            sb.AppendLine();
            AppendMathLevelGuidance(sb);
            sb.AppendLine();
            if (selectedTopics != null && selectedTopics.Count > 0)
            {
                var selectedCatalog = CurriculumScopePolicy.GetConfirmedMathTopics(khoiLop, period).ToDictionary(x => x.MaMuc, StringComparer.Ordinal);
                sb.AppendLine("CHẾ ĐỘ ĐẶC BIỆT: GIÁO VIÊN ĐÃ TÍCH CHỌN CÁC CHỦ ĐỀ/BÀI HỌC TRỌNG TÂM SGK DƯỚI ĐÂY:");
                sb.AppendLine("CHỈ được dùng đúng các mã mục chương trình đã chọn; AI BẮT BUỘC giữ mã đó trên từng dòng ma trận.");
                foreach (var topicId in selectedTopics)
                {
                    if (!selectedCatalog.TryGetValue(topicId, out var topic))
                        throw new InvalidOperationException($"Mục chương trình {topicId} chưa được xác nhận hoặc không thuộc {period}.");
                    AppendMathTopicWithLevels(sb, topic);
                }
                sb.AppendLine();
                sb.AppendLine("QUY TẮC RÀNG BUỘC KHI XÂY DỰNG MA TRẬN THEO CHỦ ĐỀ ĐÃ CHỌN:");
                sb.AppendLine("1. Mọi dòng phải dùng một mã trong danh sách đã chọn; không chỉ ưu tiên mà phải loại trừ toàn bộ nội dung ngoài danh sách.");
                sb.AppendLine("2. Phân bổ theo mạch kiến thức trong danh sách đã chọn; có thể lặp một mục nếu cần nhưng không đổi mục tiêu.");
                sb.AppendLine("3. Mỗi dòng sao chép chính xác từ mục mang mã đó: \"chuDe\" = tên mạch; \"noiDungKienThuc\" = tên mục (cụm trước dấu hai chấm, KHÔNG lấy phần mô tả sau dấu hai chấm); \"yeuCauCanDat\" = đúng theo mức M1/M2/M3 của dòng.");
                sb.AppendLine("4. Mã trên từng dòng phải nằm trong chính danh sách giáo viên đã chọn, không dùng mã khác dù mã đó đã xác nhận.");
            }
            else if (autoSelectTopics)
            {
                sb.AppendLine("CHẾ ĐỘ ĐẶC BIỆT: GIÁO VIÊN KHÔNG NHẬP ĐỊNH HƯỚNG - AI TỰ CHỌN CHỦ ĐỀ THEO SGK KẾT NỐI TRI THỨC.");
                sb.AppendLine();
                sb.AppendLine(GetMathTopicCatalogText(khoiLop, period));
                sb.AppendLine();
                sb.AppendLine("QUY TẮC TỰ CHỌN CHỦ ĐỀ (BẮT BUỘC):");
                sb.AppendLine("1. CHỈ được chọn mã mục đã xác nhận trong DANH MỤC trên. Bắt buộc điền đúng mã vào maMucChuongTrinh trên từng dòng.");
                sb.AppendLine("2. Trải đều cả đợt kiểm tra: ưu tiên bao phủ đủ 3 mạch 'Số và phép tính', 'Hình học và đo lường', 'Thống kê và xác suất'; không dồn quá nhiều câu vào một nhóm chủ đề.");
                sb.AppendLine("3. Đa dạng dạng bài giữa các câu, hạn chế trùng dạng (ví dụ không chọn 2 câu cùng dạng 'so sánh số thập phân').");
                sb.AppendLine("4. Mỗi dòng sao chép chính xác từ mục mang mã đó: \"chuDe\" = tên mạch; \"noiDungKienThuc\" = tên mục (cụm trước dấu hai chấm, KHÔNG lấy phần mô tả sau dấu hai chấm); \"yeuCauCanDat\" = đúng theo mức M1/M2/M3 của dòng; không mở rộng sang bài hoặc tuần chưa xác nhận.");
            }
            else if (!string.IsNullOrWhiteSpace(userRequirement))
            {
                sb.AppendLine("CHẾ ĐỘ ĐẶC BIỆT: GIÁO VIÊN NHẬP ĐỊNH HƯỚNG RIÊNG CHO ĐỀ KIỂM TRA.");
                sb.AppendLine($"ĐỊNH HƯỚNG CỦA GIÁO VIÊN:\n\"{userRequirement.Trim()}\"");
                sb.AppendLine();
                sb.AppendLine(GetMathTopicCatalogText(khoiLop, period));
                sb.AppendLine();
                sb.AppendLine("QUY TẮC RÀNG BUỘC KHI LẬP MA TRẬN THEO ĐỊNH HƯỚNG CỦA GIÁO VIÊN:");
                sb.AppendLine("1. Bám sát định hướng của giáo viên để phân bổ 10 câu hỏi theo đúng mức độ và hình thức.");
                sb.AppendLine("2. Mỗi dòng ma trận BẮT BUỘC chọn đúng 1 mã mục chương trình phù hợp nhất từ DANH MỤC TRÊN và điền vào maMucChuongTrinh.");
                sb.AppendLine("3. Mỗi dòng sao chép chính xác từ mục mang mã đó: \"chuDe\" = tên mạch; \"noiDungKienThuc\" = tên mục (cụm trước dấu hai chấm, KHÔNG lấy phần mô tả sau dấu hai chấm); \"yeuCauCanDat\" = đúng theo mức M1/M2/M3 của dòng.");
            }
            sb.AppendLine();
            sb.AppendLine("QUY TẮC BẮT BUỘC VỀ MA TRẬN:");
            sb.AppendLine("1. Đúng 10 câu hỏi với tổng điểm chính xác 10.0 điểm.");
            sb.AppendLine("2. Phân bổ hình thức: 7 câu Trắc nghiệm (TN) - 7.0 điểm (mỗi câu 1.0đ); 3 câu Tự luận (TL) - 3.0 điểm (mỗi câu 1.0đ). Câu 1-7 là TN, Câu 8-10 là TL.");
            sb.AppendLine("3. Phân bổ mức độ nhận thức chuẩn Thông tư 27 (Tỉ lệ 40% - 40% - 20%):");
            sb.AppendLine("   - Mức 1 (Nhận biết): 40% = 4.0 điểm (4 câu Trắc nghiệm: Câu 1, 2, 3, 4).");
            sb.AppendLine("   - Mức 2 (Kết nối/Thông hiểu): 40% = 4.0 điểm (3 câu Trắc nghiệm: Câu 5, 6, 7 + 1 câu Tự luận: Câu 8).");
            sb.AppendLine("   - Mức 3 (Vận dụng): 20% = 2.0 điểm (2 câu Tự luận: Câu 9, 10).");
            sb.AppendLine("   - TUYỆT ĐỐI KHÔNG xếp câu hỏi Trắc nghiệm (TN) vào Mức 3 để tránh làm lệch tỉ lệ chuẩn 40% - 40% - 20%.");
            sb.AppendLine("4. CẤU TRÚC BẮT BUỘC CHO 3 CÂU TỰ LUẬN (Câu 8, Câu 9, Câu 10):");
            sb.AppendLine("   - CÂU 8 LUÔN LUÔN LÀ: Mức M2, Hình thức TL, Điểm 1.0đ. Dạng toán 'Đặt tính rồi tính' gồm đúng 4 ý nhỏ a, b, c, d (mỗi ý 0.25đ).");
            if (khoiLop == 4 && ExamPeriodHelper.IsMidTerm1(period))
            {
                sb.AppendLine("     + Khối 4 Giữa kỳ I: Lấy từ giữa kỳ 1 xuống chương trình +, -, x, : của lớp 3 (nhân và chia với số có MỘT chữ số). Cấm nhân/chia số có 2 chữ số. Nội dung: 'Đặt tính rồi tính (Cộng, trừ, nhân, chia)'. YCCĐ: 'Đặt tính và tính được đúng 4 phép tính cộng, trừ, nhân, chia (4 ý a, b, c, d; mỗi ý 0.25đ)'.");
            }
            else if (khoiLop == 5 && ExamPeriodHelper.IsMidTerm1(period))
            {
                sb.AppendLine("     + Khối 5 Giữa kỳ I: BẮT BUỘC chỉ gồm phép cộng và phép trừ số thập phân (2 phép cộng, 2 phép trừ; cấm nhân/chia số thập phân). Nội dung: 'Đặt tính rồi tính (Cộng, trừ số thập phân)'. YCCĐ: 'Đặt tính và tính được đúng 4 phép tính cộng, trừ số thập phân (4 ý a, b, c, d; mỗi ý 0.25đ)'.");
            }
            else if (khoiLop == 5 && ExamPeriodHelper.IsFinalTerm1(period))
            {
                sb.AppendLine("     + Khối 5 Cuối kỳ I: 4 phép tính cộng, trừ, nhân, chia số thập phân. Nội dung: 'Đặt tính rồi tính (Cộng, trừ, nhân, chia số thập phân)'. YCCĐ: 'Đặt tính và tính được đúng 4 phép tính cộng, trừ, nhân, chia số thập phân (4 ý a, b, c, d; mỗi ý 0.25đ)'.");
            }
            else if (khoiLop == 5 && ExamPeriodHelper.IsMidTerm2(period))
            {
                sb.AppendLine("     + Khối 5 Giữa kỳ II: Phép tính với số thập phân và thời gian. Nội dung: 'Đặt tính rồi tính (Số thập phân và thời gian)'. YCCĐ: 'Đặt tính và tính được đúng 4 phép tính với số thập phân và số đo thời gian (4 ý a, b, c, d; mỗi ý 0.25đ)'.");
            }
            else if (khoiLop == 5)
            {
                sb.AppendLine("     + Khối 5 Cuối kỳ II: Phối hợp 4 phép tính. Nội dung: 'Đặt tính rồi tính (Bốn phép tính)'. YCCĐ: 'Đặt tính và tính được đúng 4 phép tính cộng, trừ, nhân, chia (4 ý a, b, c, d; mỗi ý 0.25đ)'.");
            }
            else
            {
                sb.AppendLine("     + Nội dung: 'Đặt tính rồi tính (Cộng, trừ, nhân, chia)'. Yêu cầu cần đạt: 'Đặt tính và tính được đúng 4 phép tính cộng, trừ, nhân, chia (4 ý a, b, c, d; mỗi ý 0.25đ)'.");
            }
            sb.AppendLine("   - CÂU 9: Mức M3, Hình thức TL, Điểm 1.0đ. Thường là bài toán có lời văn 2-3 bước tính (Hình học và đo lường hoặc Số và phép tính).");
            sb.AppendLine("   - CÂU 10: Mức M3, Hình thức TL, Điểm 1.0đ. Bài toán thực tế hoặc vận dụng nâng cao (tính thuận tiện hoặc giải toán 3 bước tính).");
            sb.AppendLine("5. KHÓA GIỚI HẠN CHƯƠNG TRÌNH THEO ĐỢT THI (CHỐNG LỆCH TUẦN HỌC):");
            sb.AppendLine("   - Giữa Học kỳ I: Chỉ lấy đơn vị kiến thức từ Tuần 1 đến hết Tuần 9 (Cấm kiến thức từ Tuần 10 trở đi; Khối 4 cấm bài toán tìm hai số khi biết tổng và hiệu).");
            sb.AppendLine("   - Cuối Học kỳ I: Chỉ lấy đơn vị kiến thức từ Tuần 1 đến hết Tuần 18 (Trọng tâm Tuần 10 đến Tuần 17; Cấm kiến thức Học kỳ II).");
            sb.AppendLine("   - Giữa Học kỳ II: Chỉ lấy đơn vị kiến thức từ Tuần 19 đến hết Tuần 27 (Cấm kiến thức từ Tuần 28 trở đi).");
            sb.AppendLine("   - Cuối Học kỳ II: Lấy kiến thức trong phạm vi từ Tuần 1 đến hết Tuần 35 (Trọng tâm Học kỳ II từ Tuần 19 đến Tuần 34).");
            sb.AppendLine("   - Kiểm soát chéo: Khối 4 tuyệt đối KHÔNG có kiến thức Khối 5 (số thập phân, hình tròn, tỉ số phần trăm, vận tốc/chuyển động đều).");
            sb.AppendLine("   - Nếu trong khoảng tuần của đợt thi chưa học đến mạch nào (ví dụ Giữa kỳ 1 chưa học Thống kê và xác suất), để 0 câu ở mạch đó, chỉ phân bổ vào các mạch đã học.");
            sb.AppendLine("6. Cột \"chuDe\" chỉ nhận đúng 1 trong 3 mạch: 'Số và phép tính', 'Hình học và đo lường', 'Thống kê và xác suất'.");
            sb.AppendLine("7. Mỗi dòng phải sao chép chính xác từ mục mang mã đó: \"chuDe\" = tên mạch; \"noiDungKienThuc\" = tên mục (cụm trước dấu hai chấm, KHÔNG lấy phần mô tả sau dấu hai chấm); \"yeuCauCanDat\" = đúng theo mức M1/M2/M3 của dòng (cùng mục ở các mức khác nhau bắt buộc khác yêu cầu cần đạt); nếu cần lặp để đủ khung, chỉ được lặp cùng mục tiêu và không thêm cấu trúc khác.");
            sb.AppendLine("8. Chỉ bật coHinhTikz khi yêu cầu của dòng cần hình; mọi ví dụ hình vẽ bên dưới chỉ là quy cách trình bày, không phải yêu cầu kiến thức hay vị trí câu cố định.");
            sb.AppendLine("CÁCH ĐIỀN 3 CỘT VĂN BẢN TRÊN MỖI DÒNG (bắt buộc sao chép nguyên văn từ mục mang mã của dòng; hệ thống đối chiếu từng ký tự với danh mục đã xác nhận):");
            sb.AppendLine("   - \"chuDe\": tên mạch của mục — chính là phần trong dấu ngoặc \"(...)\" của mục trong danh mục.");
            sb.AppendLine("   - \"noiDungKienThuc\": tên mục — cụm nằm giữa dấu ngoặc đóng \")\" và dấu hai chấm \":\" của mục trong danh mục; KHÔNG đưa phần mô tả chi tiết (phần sau dấu hai chấm) vào cột này.");
            sb.AppendLine("   - \"yeuCauCanDat\": yêu cầu cần đạt đúng mức của dòng — lấy nguyên văn từ bullet yêu cầu (bắt đầu bằng \"•\") ngay dưới mục trong danh mục, đúng với cột \"mucDo\" (M1/M2/M3) của dòng; không lấy bullet của mức khác.");
            sb.AppendLine("9. Trả về JSON thuần túy (không bọc trong markdown codeblock, không thêm lời dẫn), định dạng danh sách mảng JSON:");
            sb.AppendLine(@"[
  {
    ""soThuTu"": 1,
    ""maMucChuongTrinh"": ""KNTT-..."",
    ""chuDe"": ""Số và phép tính"",
    ""noiDungKienThuc"": ""..."",
    ""yeuCauCanDat"": ""..."",
    ""mucDo"": ""M1"",
    ""hinhThuc"": ""TN"",
    ""diem"": 1.0,
    ""coHinhTikz"": false
  }
]");
            return sb.ToString();
        }

        public static string BuildMatrixAuditPrompt(int khoiLop, string period, List<MathMatrixItem> matrix)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Bạn là Tổ trưởng chuyên môn và Chuyên gia thẩm định đề kiểm tra Tiểu học.");
            sb.AppendLine($"Hãy thẩm định Bản Ma Trận & Đặc Tả đề kiểm tra môn Toán Lớp {khoiLop} - Đợt: {period}.");
            sb.AppendLine("Bộ sách: KẾT NỐI TRI THỨC VỚI CUỘC SỐNG (Chương trình GDPT 2018).");
            sb.AppendLine();
            sb.AppendLine(GetMathCurriculumBoundary(khoiLop, period));
            sb.AppendLine();
            AppendMathLevelGuidance(sb);
            sb.AppendLine();
            sb.AppendLine("DƯỚI ĐÂY LÀ DANH SÁCH CÁC CÂU TRONG MA TRẬN HIỆN TẠI:");
            foreach (var item in matrix)
            {
                sb.AppendLine($"- Câu {item.SoThuTu}: [{item.ChuDe}] - {item.NoiDungKienThuc} | YCCĐ: {item.YeuCauCanDat} | Mức: {item.MucDo} | Dạng: {item.HinhThuc} | Điểm: {item.Diem}đ | TikZ: {(item.CoHinhTikz ? "Có" : "Không")}");
            }
            sb.AppendLine();
            sb.AppendLine("NỘI DUNG YÊU CẦU THẨM ĐỊNH:");
            sb.AppendLine("1. Đánh giá tính cân đối điểm số (tổng điểm 10.0đ, tỉ lệ 70% TN / 30% TL, tỉ lệ M1/M2/M3 chuẩn Thông tư 27 là 40% M1 - 40% M2 - 20% M3, không xếp TN vào M3).");
            sb.AppendLine("2. Đánh giá tính phù hợp của kiến thức đối với học sinh lớp " + khoiLop + " ở đợt " + period + " sách Kết nối tri thức (có bị vượt chương trình hay kiến thức chưa học không?).");
            sb.AppendLine("3. Đánh giá độ phủ kiến thức và phát hiện các câu hỏi bị trùng lặp dạng bài.");
            sb.AppendLine("4. Đưa ra các kiến nghị cải tiến cụ thể, ngắn gọn, súc tích giúp giáo viên hoàn thiện đề thi.");
            sb.AppendLine("5. Kiểm tra yêu cầu cần đạt của từng dòng: có khớp đúng yêu cầu của mức M1/M2/M3 đối với mục đã xác nhận không; cùng một mục ở các mức khác nhau có bị lặp y hệt yêu cầu cần đạt không.");
            sb.AppendLine("Trình bày kết quả thẩm định bằng tiếng Việt rõ ràng, mạch lạc, trực tiếp, không vòng vo.");
            return sb.ToString();
        }

        public static string GetVietTopicCatalogText(int khoiLop, string period)
        {
            var topics = CurriculumScopePolicy.GetConfirmedVietTopics(khoiLop, period);
            var sb = new StringBuilder();
            sb.AppendLine($"=== DANH MỤC CHỦ ĐIỂM ĐỌC HIỂU & ĐƠN VỊ KIẾN THỨC TIẾNG VIỆT SGK KẾT NỐI TRI THỨC - LỚP {khoiLop} - ĐỢT: {period.ToUpper()} ===");
            sb.AppendLine("Chỉ mục đã xác nhận và kết thúc trong mốc tuần của kỳ mới được dùng.");
            if (topics.Count == 0) sb.AppendLine("KHÔNG CÓ MỤC NÀO ĐÃ XÁC NHẬN. Không được sinh ma trận hoặc đề.");
            foreach (var t in topics)
            {
                sb.AppendLine("- " + CurriculumScopePolicy.FormatTopic(t));
            }
            return sb.ToString();
        }

        public static string BuildVietMatrixSuggestionPrompt(int khoiLop, string period, string userRequirement, bool autoSelectTopics = false, List<string>? selectedTopics = null)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Bạn là chuyên gia khảo thí Tiểu học môn Tiếng Việt của Bộ Giáo dục và Đào tạo Việt Nam.");
            sb.AppendLine($"Hãy xây dựng một BẢN MA TRẬN ĐẶC TẢ ĐỀ KIỂM TRA MÔN TIẾNG VIỆT LỚP {khoiLop} - ĐỢT: {period.ToUpper()}.");
            sb.AppendLine("Bộ sách giáo khoa áp dụng: KẾT NỐI TRI THỨC VỚI CUỘC SỐNG (Chương trình GDPT 2018).");
            sb.AppendLine();
            sb.AppendLine(GetVietCurriculumBoundary(khoiLop, period));
            sb.AppendLine();
            if (selectedTopics != null && selectedTopics.Count > 0)
            {
                var selectedCatalog = CurriculumScopePolicy.GetConfirmedVietTopics(khoiLop, period).ToDictionary(x => x.MaMuc, StringComparer.Ordinal);
                sb.AppendLine("CHẾ ĐỘ ĐẶC BIỆT: GIÁO VIÊN ĐÃ TÍCH CHỌN CÁC CHỦ ĐIỂM & ĐƠN VỊ KIẾN THỨC SGK DƯỚI ĐÂY:");
                sb.AppendLine("CHỈ được dùng đúng các mã mục đã chọn; từng câu và dòng ma trận phải gắn với một mã trong danh sách.");
                foreach (var topicId in selectedTopics)
                {
                    if (!selectedCatalog.TryGetValue(topicId, out var topic))
                        throw new InvalidOperationException($"Mục chương trình {topicId} chưa được xác nhận hoặc không thuộc {period}.");
                    sb.AppendLine("- " + CurriculumScopePolicy.FormatTopic(topic));
                }
                sb.AppendLine();
                sb.AppendLine("QUY TẮC RÀNG BUỘC KHI XÂY DỰNG MA TRẬN THEO CHỦ ĐIỂM/KIẾN THỨC ĐÃ CHỌN:");
                sb.AppendLine("1. Câu hỏi và dòng ma trận phải dùng đúng mã mục đã chọn; nội dung phải nằm trong kỹ năng/chủ điểm của mã đó, không được chỉ ưu tiên.");
                sb.AppendLine("2. Nếu các mục đã chọn không đủ để lập đủ khung đề mà không lặp ngoài mục tiêu thì báo lỗi, không tự thêm nội dung khác.");
                sb.AppendLine("3. Chỉ phân bổ trong đúng kỹ năng và tuần của các mã mục đã chọn.");
                sb.AppendLine("4. Mỗi dòng phải giữ mã mục, nội dung và yêu cầu cần đạt của mục đã xác nhận.");
            }
            else if (autoSelectTopics)
            {
                sb.AppendLine("CHẾ ĐỘ ĐẶC BIỆT: GIÁO VIÊN KHÔNG NHẬP ĐỊNH HƯỚNG - AI TỰ CHỌN CHỦ ĐỀ THEO SGK KẾT NỐI TRI THỨC.");
                sb.AppendLine();
                sb.AppendLine(GetVietTopicCatalogText(khoiLop, period));
                sb.AppendLine();
                sb.AppendLine("QUY TẮC TỰ CHỌN CHỦ ĐỀ (BẮT BUỘC):");
                sb.AppendLine("1. CHỈ được chọn mã mục đã xác nhận trong DANH MỤC trên. Bắt buộc điền đúng maMucChuongTrinh cho từng câu.");
                sb.AppendLine("2. Phân bổ 7 câu Đọc hiểu/5 điểm và 4 câu Kiến thức Tiếng Việt/3 điểm; chỉ dùng mục đã xác nhận.");
                sb.AppendLine("3. Nếu thiếu mục hợp lệ để lập khung thì báo không đủ điều kiện; không thêm kỹ năng ngoài kỳ.");
                sb.AppendLine("4. Mỗi mục Kiến thức Tiếng Việt sao chép chính xác mạch, chủ đề và yêu cầu cần đạt của mã. Bảy câu Đọc hiểu cùng dùng một mã chủ điểm cho một bài đọc.");
            }
            else if (!string.IsNullOrWhiteSpace(userRequirement))
            {
                sb.AppendLine("CHẾ ĐỘ ĐẶC BIỆT: GIÁO VIÊN NHẬP ĐỊNH HƯỚNG RIÊNG CHO ĐỀ KIỂM TRA.");
                sb.AppendLine($"ĐỊNH HƯỚNG CỦA GIÁO VIÊN:\n\"{userRequirement.Trim()}\"");
                sb.AppendLine();
                sb.AppendLine(GetVietTopicCatalogText(khoiLop, period));
                sb.AppendLine();
                sb.AppendLine("QUY TẮC RÀNG BUỘC KHI LẬP MA TRẬN THEO ĐỊNH HƯỚNG CỦA GIÁO VIÊN:");
                sb.AppendLine("1. Bám sát định hướng của giáo viên để phân bổ 11 câu hỏi (7 câu Đọc hiểu/5 điểm và 4 câu Kiến thức Tiếng Việt/3 điểm).");
                sb.AppendLine("2. Mỗi dòng ma trận BẮT BUỘC chọn đúng 1 mã mục chương trình phù hợp nhất từ DANH MỤC TRÊN và điền vào maMucChuongTrinh.");
                sb.AppendLine("3. Mỗi mục Kiến thức Tiếng Việt sao chép chính xác mạch, chủ đề và yêu cầu cần đạt của mã. Bảy câu Đọc hiểu cùng dùng một mã chủ điểm cho một bài đọc.");
            }
            sb.AppendLine();
            sb.AppendLine("QUY TẮC BẮT BUỘC VỀ MA TRẬN TIẾNG VIỆT (BÁM SÁT MA TRẬN VÀ BẢNG ĐẶC TẢ CHUẨN KHỐI 4, 5):");
            sb.AppendLine("1. Giữ đúng chuẩn khung 11 câu/8 điểm: 7 câu Đọc hiểu/5 điểm và 4 câu Kiến thức Tiếng Việt/3 điểm; tổng hình thức BẮT BUỘC TN: 6, TL: 5; tổng mức độ BẮT BUỘC M1: 6, M2: 3, M3: 2.");
            sb.AppendLine("2. CẤU TRÚC CHI TIẾT 11 CÂU BẮT BUỘC PHẢI PHÂN BỔ NHƯ SAU:");
            sb.AppendLine("   - Câu 1: Đọc hiểu văn bản | Đọc hiểu nội dung | YCCĐ nhận biết chi tiết, hình ảnh | Mức: M1 | Hình thức: TN | Điểm: 0.5");
            sb.AppendLine("   - Câu 2: Đọc hiểu văn bản | Đọc hiểu nội dung | YCCĐ nhận biết chi tiết, sự việc | Mức: M1 | Hình thức: TN | Điểm: 0.5");
            sb.AppendLine("   - Câu 3: Đọc hiểu văn bản | Đọc hiểu nội dung | YCCĐ làm rõ nhân vật, sự việc | Mức: M1 | Hình thức: TN | Điểm: 0.5");
            sb.AppendLine("   - Câu 4: Đọc hiểu văn bản | Đọc hiểu nội dung | YCCĐ nêu ý chính, nội dung đoạn/bài | Mức: M1 | Hình thức: TN | Điểm: 0.5");
            sb.AppendLine("   - Câu 5: Đọc hiểu văn bản | Đọc hiểu hình thức | YCCĐ hiểu từ ngữ, hình ảnh, nghệ thuật, nhân vật | Mức: M2 | Hình thức: TL | Điểm: 1.0");
            sb.AppendLine("   - Câu 6: Đọc hiểu văn bản | Đọc hiểu hình thức | YCCĐ giải thích lý do, ý nghĩa hành động, sự việc | Mức: M2 | Hình thức: TL | Điểm: 1.0");
            sb.AppendLine("   - Câu 7: Đọc hiểu văn bản | Liên hệ, so sánh, kết nối | YCCĐ rút ra bài học, liên hệ bản thân | Mức: M3 | Hình thức: TL | Điểm: 1.0");
            sb.AppendLine("   - Câu 8: Kiến thức Tiếng Việt | (Nội dung từ vựng/ngữ pháp đã xác nhận) | YCCĐ mức 1 | Mức: M1 | Hình thức: TN | Điểm: 0.5");
            sb.AppendLine("   - Câu 9: Kiến thức Tiếng Việt | (Nội dung từ vựng/ngữ pháp đã xác nhận) | YCCĐ mức 1 | Mức: M1 | Hình thức: TN | Điểm: 0.5");
            sb.AppendLine("   - Câu 10: Kiến thức Tiếng Việt | (Nội dung ngữ pháp/dấu câu đã xác nhận) | YCCĐ mức 2 | Mức: M2 | Hình thức: TL | Điểm: 1.0");
            sb.AppendLine("   - Câu 11: Kiến thức Tiếng Việt | (Nội dung vận dụng đã xác nhận) | YCCĐ mức 3 | Mức: M3 | Hình thức: TL | Điểm: 1.0");
            sb.AppendLine("3. Bảy câu Đọc hiểu (Câu 1 đến Câu 7) BẮT BUỘC cùng gắn với MỘT mã mục chủ điểm đọc hiểu (KNTT-...) từ danh mục trên.");
            sb.AppendLine("4. Bốn câu Kiến thức Tiếng Việt (Câu 8 đến 11) chọn các mã mục kiến thức phù hợp trong danh mục kỳ này; không bắt buộc tạo dòng cho mạch không có nội dung đã xác nhận.");
            sb.AppendLine("5. Trả về JSON thuần túy (không bọc trong markdown codeblock, không thêm lời dẫn), định dạng danh sách mảng JSON:");
            sb.AppendLine(@"[
  {
    ""soThuTu"": 1,
    ""maMucChuongTrinh"": ""KNTT-..."",
    ""machKienThuc"": ""Đọc hiểu văn bản"",
    ""noiDungKienThuc"": ""Đọc hiểu nội dung"",
    ""yeuCauCanDat"": ""Nhận biết được một số chi tiết tiêu biểu trong bài đọc."",
    ""mucDo"": ""M1"",
    ""hinhThuc"": ""TN"",
    ""diem"": 0.5
  }
]");
            return sb.ToString();
        }

        public static string BuildVietMatrixAuditPrompt(int khoiLop, string period, List<VietMatrixItem> matrix)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Bạn là Tổ trưởng chuyên môn và Chuyên gia thẩm định đề kiểm tra Tiếng Việt cấp Tiểu học.");
            sb.AppendLine($"Hãy thẩm định Bản Ma Trận & Bản Đặc Tả đề kiểm tra môn Tiếng Việt Lớp {khoiLop} - Đợt: {period}.");
            sb.AppendLine("Bộ sách giáo khoa: KẾT NỐI TRI THỨC VỚI CUỘC SỐNG (Chương trình GDPT 2018).");
            sb.AppendLine();
            sb.AppendLine(GetVietCurriculumBoundary(khoiLop, period));
            sb.AppendLine();
            sb.AppendLine("DƯỚI ĐÂY LÀ DANH SÁCH 11 CÂU TRONG MA TRẬN HIỆN TẠI (PHẦN ĐỌC HIỂU & LTVC - 8 ĐIỂM):");
            foreach (var item in matrix)
            {
                sb.AppendLine($"- Câu {item.SoThuTu}: [{item.MachKienThuc}] - {item.NoiDungKienThuc} | YCCĐ: {item.YeuCauCanDat} | Mức: {item.MucDo} | Dạng: {item.HinhThuc} | Điểm: {item.Diem}đ");
            }
            sb.AppendLine();
            sb.AppendLine("NỘI DUNG YÊU CẦU THẨM ĐỊNH:");
            sb.AppendLine("1. Đánh giá cơ cấu điểm: Tổng điểm phần Đọc hiểu & LTVC có đúng 8.0 điểm không? (Đọc hiểu 5.0đ, LTVC 3.0đ, cộng 2.0đ Đọc thành tiếng = 10.0đ). Tỉ lệ TN/TL có hợp lý không?");
            sb.AppendLine("2. Đánh giá tính chuẩn xác của Yêu cầu cần đạt so với chuẩn kiến thức kĩ năng lớp " + khoiLop + " ở đợt " + period + " theo SGK Kết nối tri thức (có bị vượt chương trình hoặc lạc đề không?).");
            sb.AppendLine("3. Đánh giá tỉ lệ phân bổ 3 mức độ (M1, M2, M3) theo Thông tư 27.");
            sb.AppendLine("4. Đưa ra các khuyến nghị cải tiến cụ thể, thiết thực để giáo viên hoàn thiện bộ đề.");
            sb.AppendLine("Trình bày kết quả thẩm định bằng tiếng Việt rõ ràng, mạch lạc, trực tiếp, có tiêu đề phân đoạn rõ ràng.");
            return sb.ToString();
        }

        public static string GetMathCurriculumBoundary(int khoiLop, string period)
        {
            var topics = CurriculumScopePolicy.GetConfirmedMathTopics(khoiLop, period);
            var sb = new StringBuilder();
            sb.AppendLine($"=== PHẠM VI TOÁN LỚP {khoiLop} — {period.ToUpper()} — KNTT ===");
            sb.AppendLine($"Mốc kỳ: hết tuần {CurriculumScopePolicy.GetWeekLimit(period)}. Chỉ các mã mục đã xác nhận dưới đây được phép sử dụng.");
            if (topics.Count == 0)
            {
                sb.AppendLine("DANH MỤC RỖNG: chưa có bài/tuần được giáo viên xác nhận. Bắt buộc dừng, không tạo ma trận hoặc đề.");
                return sb.ToString();
            }

            foreach (var topic in topics)
                AppendMathTopicWithLevels(sb, topic);

            if (khoiLop == 4)
                sb.AppendLine("Quy tắc khối 4: không sử dụng số thập phân. Mọi dạng toán khác phải nằm đúng yêu cầu của mã mục tương ứng.");
            return sb.ToString();
        }

        public static string GetVietCurriculumBoundary(int khoiLop, string period)
        {
            var topics = CurriculumScopePolicy.GetConfirmedVietTopics(khoiLop, period);
            var sb = new StringBuilder();
            sb.AppendLine($"=== PHẠM VI TIẾNG VIỆT LỚP {khoiLop} — {period.ToUpper()} — KNTT ===");
            sb.AppendLine($"Mốc kỳ: hết tuần {CurriculumScopePolicy.GetWeekLimit(period)}. Chỉ các mã mục đã xác nhận dưới đây được phép sử dụng.");
            if (topics.Count == 0)
            {
                sb.AppendLine("DANH MỤC RỖNG: chưa có chủ điểm/kỹ năng được giáo viên xác nhận. Bắt buộc dừng, không tạo ma trận hoặc đề.");
                return sb.ToString();
            }

            foreach (var topic in topics)
                sb.AppendLine("- " + CurriculumScopePolicy.FormatTopic(topic));
            sb.AppendLine("Từ đồng nghĩa, trái nghĩa, đồng âm và đa nghĩa thuộc mạch Từ vựng.");
            return sb.ToString();
        }

        /// <summary>
        /// Thư viện hình vẽ chuẩn SGK Tiểu học (quy ước chung + mẫu vàng TikZ + checklist tự kiểm),
        /// được nhồi vào cả prompt sinh đề và prompt vẽ hình để AI bám đúng chuẩn trình bày.
        /// khoiLop: 4 hoặc 5 (dùng để chọn danh sách dạng hình trọng tâm; 0 = liệt kê đầy đủ).
        /// </summary>
        public static string GetMathFigureLibrary(int khoiLop)
        {
            var sb = new StringBuilder();
            void Sample(string title, string code)
            {
                sb.AppendLine(title);
                sb.AppendLine(code);
                sb.AppendLine();
            }

            sb.AppendLine("=== THƯ VIỆN HÌNH VẼ CHUẨN SÁCH GIÁO KHOA TIỂU HỌC (BẮT BUỘC TUÂN THỦ KHI VẼ TIKZ) ===");
            sb.AppendLine();
            sb.AppendLine("A. QUY ƯỚC CHUNG CHO MỌI HÌNH VẼ:");
            sb.AppendLine("1. Điểm/đỉnh đặt tên chữ in hoa đúng như đề bài (A, B, C, D, H, O, M, N...) và đặt phía ngoài hình, không đè lên nét vẽ.");
            sb.AppendLine("2. Góc vuông: vẽ ô vuông nhỏ (cạnh khoảng 0,3 - 0,4 đơn vị) tại đỉnh góc vuông, KHÔNG ghi số 90 trên hình.");
            sb.AppendLine("3. Đường cao, đường chéo phụ và cạnh khuất của hình khối 3D: BẮT BUỘC vẽ nét đứt [dashed]; đường cao dùng [dashed, blue!80!black] kèm ô vuông góc tại chân đường cao H.");
            sb.AppendLine("4. Tam giác tù: chân đường cao nằm NGOÀI đáy. BẮT BUỘC kéo dài đáy bằng nét đứt đến chân đường cao rồi mới dựng đường cao và ô vuông góc.");
            sb.AppendLine(khoiLop == 4
                ? "5. Nhãn số đo: CHỈ ghi số liệu mà đề bài đã cho, và Khối 4 chỉ dùng SỐ TỰ NHIÊN trong nhãn (ví dụ 12 cm; 120 kg). TUYỆT ĐỐI KHÔNG tự bịa số: nếu đề không nêu số thì ghi nhãn chữ (đáy lớn, đáy bé, chiều cao, chiều dài, chiều rộng, bán kính) hoặc chữ cái (a, b, h, m, n)."
                : "5. Nhãn số đo: CHỈ ghi số liệu mà đề bài đã cho (ví dụ 12 cm; 3,5 m). TUYỆT ĐỐI KHÔNG tự bịa số: nếu đề không nêu số thì ghi nhãn chữ (đáy lớn, đáy bé, chiều cao, chiều dài, chiều rộng, bán kính) hoặc chữ cái (a, b, h, m, n).");
            sb.AppendLine(khoiLop == 4
                ? "6. Khối 4 TUYỆT ĐỐI không dùng số thập phân trong nhãn hình (số thập phân chỉ học ở Lớp 5); nếu cần phân số thì viết dạng LaTeX \\frac{tử}{mẫu}."
                : "6. Số thập phân viết dấu phẩy kiểu Việt Nam trong nhãn hình (12,5 cm).");
            sb.AppendLine("7. Hình gọn trong khoảng 7cm x 5cm, đặt cân đối giữa khung (scale 0.75 - 1.0), không tràn lề khi chèn vào Word.");
            sb.AppendLine("8. Nét chính dùng [thick]; chữ nhãn dùng font=\\footnotesize. Hình phẳng (chữ nhật, vuông, thang, bình hành, thoi) đặt cạnh dài nằm ngang.");
            sb.AppendLine("9. Biểu đồ: trục có mũi tên [->] và vạch chia kèm số; giá trị ghi ngay trên đỉnh cột / trong miếng quạt / trên đoạn thẳng.");
            sb.AppendLine();
            sb.AppendLine("B. CÁC DẠNG HÌNH TRỌNG TÂM THEO KHỐI:");
            if (khoiLop == 4)
            {
                sb.AppendLine("- Khối 4: hình bình hành + đường cao; hình thoi + hai đường chéo (nét đứt); hai đường thẳng vuông góc (ô vuông góc tại O); hai đường thẳng song song; góc nhọn / vuông / tù / bẹt; biểu đồ cột; sơ đồ đoạn thẳng; hình tròn phân số; hình chữ nhật / hình vuông (ôn tập).");
            }
            else if (khoiLop == 5)
            {
                sb.AppendLine("- Khối 5: tam giác + đường cao; hình thang (đáy lớn - đáy bé - chiều cao); hình tròn (bán kính OA, đường kính AB); hình hộp chữ nhật - hình lập phương (3D, cạnh khuất nét đứt); biểu đồ cột; biểu đồ hình quạt tròn; sơ đồ đoạn thẳng (tổng - tỉ, hiệu - tỉ).");
            }
            else
            {
                sb.AppendLine("- Khối 4: hình bình hành + đường cao; hình thoi + hai đường chéo; hai đường thẳng vuông góc / song song; góc; biểu đồ cột; sơ đồ đoạn thẳng.");
                sb.AppendLine("- Khối 5: tam giác + đường cao; hình thang; hình tròn; hình hộp chữ nhật - hình lập phương 3D; biểu đồ cột; biểu đồ hình quạt tròn; sơ đồ đoạn thẳng.");
            }
            sb.AppendLine();
            sb.AppendLine("C. MẪU VÀNG TIKZ (tham khảo cấu trúc; PHẢI thay bằng số liệu thật của đề, không chép y nguyên):");
            sb.AppendLine();

            Sample("[Mẫu 1] Tam giác + đường cao (nhãn chữ khi đề không cho số):", @"\begin{tikzpicture}[scale=0.8]
  \coordinate (B) at (0,0);
  \coordinate (C) at (5,0);
  \coordinate (A) at (1.8,3.0);
  \coordinate (H) at (1.8,0);
  \draw[thick] (A) -- (B) -- (C) -- cycle;
  \draw[thick, dashed, blue!80!black] (A) -- (H) node[midway, right, font=\footnotesize] {chiều cao};
  \draw (1.8,0.35) -- (2.15,0.35) -- (2.15,0);
  \node[below left, font=\footnotesize] at (H) {H};
  \node[above, font=\footnotesize] at (A) {A};
  \node[below left, font=\footnotesize] at (B) {B};
  \node[below right, font=\footnotesize] at (C) {C};
  \draw[<->, >=stealth, gray] (0,-0.45) -- (5,-0.45) node[midway, below, font=\footnotesize, text=black] {đáy};
\end{tikzpicture}");

            Sample("[Mẫu 2] Hình thang + đường cao (đáy lớn đặt dưới):", @"\begin{tikzpicture}[scale=0.8]
  \coordinate (A) at (1.5,3.0);
  \coordinate (B) at (4.5,3.0);
  \coordinate (C) at (6.0,0);
  \coordinate (D) at (0,0);
  \coordinate (H) at (1.5,0);
  \draw[thick] (A) -- (B) -- (C) -- (D) -- cycle;
  \draw[thick, dashed, blue!80!black] (A) -- (H) node[midway, right, font=\footnotesize] {chiều cao};
  \draw (1.5,0.35) -- (1.85,0.35) -- (1.85,0);
  \node[above left, font=\footnotesize] at (A) {A};
  \node[above right, font=\footnotesize] at (B) {B};
  \node[below right, font=\footnotesize] at (C) {C};
  \node[below left, font=\footnotesize] at (D) {D};
  \node[below, font=\footnotesize] at (H) {H};
  \node[above, font=\footnotesize] at (3.0,3.0) {đáy bé};
  \node[below, font=\footnotesize] at (3.0,0) {đáy lớn};
\end{tikzpicture}");

            Sample("[Mẫu 3] Hình tròn tâm O, bán kính OA (đường kính AB thì vẽ dây qua tâm, nhãn A và B ở hai đầu):", @"\begin{tikzpicture}[scale=0.9]
  \draw[thick] (0,0) circle (2);
  \draw[thick] (0,0) -- (2,0);
  \fill (0,0) circle (1.2pt);
  \node[below left, font=\footnotesize] at (0,0) {O};
  \node[right, font=\footnotesize] at (2,0) {A};
  \node[above, font=\footnotesize] at (1.0,0.1) {bán kính};
\end{tikzpicture}");

            Sample("[Mẫu 4] Biểu đồ cột (trục có mũi tên, vạch chia kèm số, giá trị trên đỉnh cột):", @"\begin{tikzpicture}[xscale=1.2, yscale=0.15, font=\small]
  \draw[->, thick] (0,0) -- (5.3,0) node[below=3pt] {Lớp};
  \draw[->, thick] (0,0) -- (0,34) node[left=3pt] {Số cây};
  \foreach \y in {0,5,10,15,20,25,30} {
    \draw[thick] (0,\y) -- (-0.12,\y) node[left, font=\footnotesize] {\y};
  }
  \foreach \pos/\cay/\lop in {1/30/Lớp 4A, 2/25/Lớp 4B, 3/20/Lớp 4C, 4/40/Lớp 4D} {
    \filldraw[cyan!35, draw=blue, thick] (\pos-0.32,0) rectangle (\pos+0.32,\cay);
    \node[above, font=\footnotesize] at (\pos,\cay) {\cay};
    \node[below, font=\footnotesize] at (\pos,0) {\lop};
  }
\end{tikzpicture}");

            Sample("[Mẫu 5] Biểu đồ hình quạt tròn (ví dụ 25% - 30% - 45%, có chú giải màu):", @"\begin{tikzpicture}[scale=0.85]
  \fill[red!35]   (0,0) -- (0:2.2)   arc (0:90:2.2)   -- cycle;
  \fill[blue!35]  (0,0) -- (90:2.2)  arc (90:198:2.2) -- cycle;
  \fill[green!35] (0,0) -- (198:2.2) arc (198:360:2.2) -- cycle;
  \foreach \a in {0,90,198} { \draw[thick] (0,0) -- (\a:2.2); }
  \draw[thick] (0,0) circle (2.2);
  \fill (0,0) circle (1.2pt);
  \node[font=\footnotesize] at (45:1.35) {25\%};
  \node[font=\footnotesize] at (144:1.35) {30\%};
  \node[font=\footnotesize] at (279:1.35) {45\%};
  \fill[red!35]   (2.6,1.0) rectangle (2.95,1.35);
  \node[anchor=west, font=\footnotesize] at (3.05,1.175) {Truyện thiếu nhi};
  \fill[blue!35]  (2.6,0.3) rectangle (2.95,0.65);
  \node[anchor=west, font=\footnotesize] at (3.05,0.475) {Sách giáo khoa};
  \fill[green!35] (2.6,-0.4) rectangle (2.95,-0.05);
  \node[anchor=west, font=\footnotesize] at (3.05,-0.225) {Sách tham khảo};
\end{tikzpicture}");

            Sample("[Mẫu 6] Sơ đồ đoạn thẳng (tổng - tỉ hoặc hiệu - tỉ; ngoặc nhọn ghi tổng/hiệu):", @"\begin{tikzpicture}[scale=0.75]
  \node[anchor=east, font=\footnotesize] at (0,1.5) {Số bé:};
  \draw[thick] (0.2,1.5) -- (2.2,1.5);
  \foreach \x in {0,1,2} { \draw[thick] ({0.2+\x},1.35) -- ({0.2+\x},1.65); }
  \node[anchor=east, font=\footnotesize] at (0,0.4) {Số lớn:};
  \draw[thick] (0.2,0.4) -- (3.2,0.4);
  \foreach \x in {0,1,2,3} { \draw[thick] ({0.2+\x},0.25) -- ({0.2+\x},0.55); }
  \draw[thick, decoration={brace, mirror, amplitude=6pt}, decorate] (0.2,-0.1) -- (3.2,-0.1) node[midway, below=9pt, font=\footnotesize] {Tổng: 50};
\end{tikzpicture}");

            Sample("[Mẫu 7] Hình bình hành ABCD + đường cao h (khối 4):", @"\begin{tikzpicture}[scale=0.8]
  \coordinate (D) at (0,0);
  \coordinate (C) at (5,0);
  \coordinate (A) at (1.6,2.6);
  \coordinate (B) at (6.6,2.6);
  \coordinate (H) at (1.6,0);
  \draw[thick] (A) -- (B) -- (C) -- (D) -- cycle;
  \draw[thick, dashed, blue!80!black] (A) -- (H) node[midway, right, font=\footnotesize] {chiều cao};
  \draw (1.6,0.35) -- (1.95,0.35) -- (1.95,0);
  \node[above left, font=\footnotesize] at (A) {A};
  \node[above right, font=\footnotesize] at (B) {B};
  \node[below right, font=\footnotesize] at (C) {C};
  \node[below left, font=\footnotesize] at (D) {D};
  \node[below, font=\footnotesize] at (H) {H};
\end{tikzpicture}");

            Sample("[Mẫu 8] Hình thoi ABCD + hai đường chéo m, n (khối 4):", @"\begin{tikzpicture}[scale=0.8]
  \coordinate (A) at (0,2.0);
  \coordinate (B) at (3.0,3.5);
  \coordinate (C) at (6.0,2.0);
  \coordinate (D) at (3.0,0.5);
  \draw[thick] (A) -- (B) -- (C) -- (D) -- cycle;
  \draw[thick, dashed, blue!80!black] (A) -- (C);
  \draw[thick, dashed, blue!80!black] (B) -- (D);
  \draw (3.35,2.0) -- (3.35,2.35) -- (3.0,2.35);
  \node[above, font=\footnotesize] at (1.5,2.0) {m};
  \node[right, font=\footnotesize] at (3.0,2.9) {n};
  \node[above left, font=\footnotesize] at (A) {A};
  \node[above, font=\footnotesize] at (B) {B};
  \node[above right, font=\footnotesize] at (C) {C};
  \node[below, font=\footnotesize] at (D) {D};
\end{tikzpicture}");

            Sample("[Mẫu 9] Hai đường thẳng AB và CD vuông góc tại O (khối 4; song song thì vẽ hai đường không cắt nhau, ghi tên a, b):", @"\begin{tikzpicture}[scale=0.85]
  \draw[thick] (-0.6,0) -- (5.2,0);
  \node[left, font=\footnotesize] at (-0.6,0) {A};
  \node[right, font=\footnotesize] at (5.2,0) {B};
  \draw[thick] (2.2,-0.9) -- (2.2,3.3);
  \node[below, font=\footnotesize] at (2.2,-0.9) {D};
  \node[above, font=\footnotesize] at (2.2,3.3) {C};
  \draw (2.55,0) -- (2.55,0.35) -- (2.2,0.35);
  \fill (2.2,0) circle (1.2pt);
  \node[below left, font=\footnotesize] at (2.2,0) {O};
\end{tikzpicture}");

            Sample("[Mẫu 10] Hình hộp chữ nhật 3D (khối 5; cạnh khuất nét đứt. Hình lập phương vẽ tương tự nhưng các cạnh bằng nhau, cả 3 nhãn cùng ghi a hoặc cùng số đo cạnh):", @"\begin{tikzpicture}[scale=0.75]
  \coordinate (A) at (0,3);
  \coordinate (B) at (4.5,3);
  \coordinate (C) at (4.5,0);
  \coordinate (D) at (0,0);
  \coordinate (A1) at (2.0,4.2);
  \coordinate (B1) at (6.5,4.2);
  \coordinate (C1) at (6.5,1.2);
  \coordinate (D1) at (2.0,1.2);
  \draw[thick, dashed] (D) -- (D1) -- (A1);
  \draw[thick, dashed] (D1) -- (C1);
  \draw[thick] (A) -- (B) -- (C) -- (D) -- cycle;
  \draw[thick] (A) -- (A1) -- (B1) -- (B);
  \draw[thick] (B1) -- (C1) -- (C);
  \node[below, font=\footnotesize] at (2.25,0) {chiều dài};
  \node[below right, font=\footnotesize] at (5.5,0.6) {chiều rộng};
  \node[left, font=\footnotesize] at (0,1.5) {chiều cao};
\end{tikzpicture}");

            sb.AppendLine("D. TỰ KIỂM TRA TRƯỚC KHI TRẢ KẾT QUẢ:");
            sb.AppendLine("- Hình vẽ đúng loại đề yêu cầu, đúng tên điểm/đỉnh và đúng thứ tự chữ cái như đề bài.");
            sb.AppendLine("- Mọi số đo trên hình khớp 100% với đề bài; KHÔNG thêm số, KHÔNG bịa số khi đề không cho.");
            sb.AppendLine("- Đường cao / đường chéo / cạnh khuất là nét đứt; góc vuông có ô vuông nhỏ; tam giác tù có kéo dài đáy bằng nét đứt.");
            sb.AppendLine("- Mã bắt đầu bằng \\begin{tikzpicture} và kết thúc bằng \\end{tikzpicture}; mỗi lệnh một dòng, không chứa markdown.");

            return sb.ToString();
        }
    }
}
