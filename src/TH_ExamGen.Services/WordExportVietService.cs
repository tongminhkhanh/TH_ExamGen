using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using TH_ExamGen.Core.Config;
using TH_ExamGen.Core.Curriculum;
using TH_ExamGen.Core.VietModels;
using Xceed.Document.NET;
using Xceed.Words.NET;

namespace TH_ExamGen.Services
{
    public class WordExportVietService
    {
        private static readonly System.Xml.Linq.XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        private static readonly Xceed.Drawing.Color BorderBlack = Xceed.Drawing.Color.FromColorName("Black");
        private static readonly Xceed.Drawing.Color BorderGray = Xceed.Drawing.Color.FromColorName("LightGray");
        private static readonly Xceed.Drawing.Color BorderWhite = Xceed.Drawing.Color.FromColorName("White");

        /// <summary>
        /// Xuất RIÊNG File 1: Ma trận đề và Bản đặc tả môn Tiếng Việt (kèm khung phê duyệt)
        /// Trang 1: Bảng Ma trận (Khổ đứng A4 Portrait)
        /// Trang 2: Bản đặc tả chi tiết + Khung phê duyệt 3 cột (Khổ ngang A4 Landscape)
        /// </summary>
        public static void ExportVietMatrixOnlyToDocx(List<VietMatrixItem> matrix, AppSettings settings, int khoiLop, string outputFilePath, string period = "Cuối Học kỳ II")
        {
            CurriculumScopePolicy.ValidateVietMatrix(khoiLop, period, matrix);
            using (var doc = DocX.Create(outputFilePath))
            {
                ApplyA4AndStandardMargins(doc);

                // 1. Header cơ quan & Quốc hiệu
                string periodTitle = period.ToUpper();
                InsertDocumentHeader(doc, settings, $"MA TRẬN VÀ BẢN ĐẶC TẢ ĐỀ KIỂM TRA ĐỊNH KỲ {periodTitle}", khoiLop, "TIẾNG VIỆT");

                // 2. Bảng Ma trận 2 tầng tiêu đề (10 cột) tính toán động (Trang 1: Khổ đứng A4)
                InsertDynamicVietMatrixTable(doc, matrix);

                // Sang trang mới (Khổ ngang A4 cho Bản đặc tả và Khung phê duyệt)
                doc.InsertSectionPageBreak();

                // 3. Bảng đặc tả chi tiết (Trang 2: Khổ ngang A4)
                InsertDynamicVietSpecificationTable(doc, matrix);

                doc.InsertParagraph().SpacingAfter(20);

                // 4. Khung chữ ký phê duyệt 3 cột
                InsertApprovalSignatures(doc);

                doc.Save();
            }

            EnforceMatrixAndSpecSections(outputFilePath);
        }

        /// <summary>
        /// Xuất RIÊNG File 2: Đề kiểm tra Tiếng Việt chính thức và Hướng dẫn chấm
        /// </summary>
        public static void ExportVietExamOnlyToDocx(VietExamPaper paper, AppSettings settings, string outputFilePath, bool skipValidation = false)
        {
            if (!skipValidation)
            {
                CurriculumScopePolicy.ValidateVietQuestions(paper.KhoiLop, paper.Period, paper.DanhSachCauHoi, paper.DanhSachMaTran);
                CurriculumScopePolicy.ValidateVietPassageMatchesMatrix(paper.BaiDoc, paper.DanhSachMaTran, paper.KhoiLop, paper.Period);
            }
            using var doc = DocX.Create(outputFilePath);
            ApplyA4AndStandardMargins(doc);

            // 1. Header Đề thi
            InsertExamHeader(doc, settings, paper);

            // 2. Khung Điểm và Lời nhận xét của giáo viên
            InsertGradeBox(doc);

            // 3. Nội dung đề thi chính thức (Đọc thành tiếng + Đọc thầm + Câu hỏi + Lưới ô ly)
            InsertExamBody(doc, paper);

            // Sang trang mới cho Hướng dẫn chấm
            doc.InsertSectionPageBreak();

            // 4. Hướng dẫn chấm và Biểu điểm chi tiết
            InsertGradingGuide(doc, paper, settings);

            doc.Save();
        }

        /// <summary>
        /// Xuất gộp trọn bộ hồ sơ Tiếng Việt (Ma trận + Đặc tả + Đề + Đáp án)
        /// </summary>
        public static void ExportVietFullPackageToDocx(VietExamPaper paper, AppSettings settings, string outputFilePath, bool skipValidation = false)
        {
            if (!skipValidation)
            {
                CurriculumScopePolicy.ValidateVietQuestions(paper.KhoiLop, paper.Period, paper.DanhSachCauHoi, paper.DanhSachMaTran);
                CurriculumScopePolicy.ValidateVietPassageMatchesMatrix(paper.BaiDoc, paper.DanhSachMaTran, paper.KhoiLop, paper.Period);
            }
            using var doc = DocX.Create(outputFilePath);
            ApplyA4AndStandardMargins(doc);

            // Ma trận & Đặc tả
            string periodTitle = string.IsNullOrWhiteSpace(paper.TieuDe) ? "CUỐI HỌC KỲ II" : paper.TieuDe.ToUpper();
            if (periodTitle.StartsWith("BÀI KIỂM TRA ĐỊNH KỲ "))
            {
                periodTitle = periodTitle.Substring("BÀI KIỂM TRA ĐỊNH KỲ ".Length).Trim();
            }
            InsertDocumentHeader(doc, settings, $"MA TRẬN VÀ BẢN ĐẶC TẢ ĐỀ KIỂM TRA ĐỊNH KỲ {periodTitle}", paper.KhoiLop, "TIẾNG VIỆT");
            InsertDynamicVietMatrixTable(doc, paper.DanhSachMaTran);
            doc.InsertParagraph().SpacingAfter(14);
            InsertDynamicVietSpecificationTable(doc, paper.DanhSachMaTran);
            doc.InsertParagraph().SpacingAfter(20);
            InsertApprovalSignatures(doc);

            doc.InsertSectionPageBreak();

            // Đề thi
            InsertExamHeader(doc, settings, paper);
            InsertGradeBox(doc);
            InsertExamBody(doc, paper);

            doc.InsertSectionPageBreak();

            // Hướng dẫn chấm
            InsertGradingGuide(doc, paper, settings);

            doc.Save();
        }

        #region Helper Styles & Margins

        private static void ApplyA4AndStandardMargins(DocX doc)
        {
            // Thiết lập khổ A4: 210mm x 297mm
            doc.PageWidth = 595.3f;
            doc.PageHeight = 841.9f;

            // Quy chuẩn lề hành chính: Top=2.0cm (56.7pt), Bottom=2.0cm (56.7pt), Left=3.0cm (85.05pt), Right=1.5cm (42.5pt)
            doc.MarginTop = 56.7f;
            doc.MarginBottom = 56.7f;
            doc.MarginLeft = 85.05f;
            doc.MarginRight = 42.5f;
        }

        private static void ApplyA4LandscapeMargins(DocX doc)
        {
            // Thiết lập khổ A4 Ngang: 297mm x 210mm (Quy chuẩn Ma trận & Bản đặc tả chi tiết)
            doc.PageWidth = 841.9f;
            doc.PageHeight = 595.3f;

            // Lề Landscape: Top=1.3cm (36.8pt), Bottom=1.3cm (36.8pt), Left=1.8cm (51.0pt), Right=1.3cm (36.8pt)
            doc.MarginTop = 36.8f;
            doc.MarginBottom = 36.8f;
            doc.MarginLeft = 51.0f;
            doc.MarginRight = 36.8f;
        }

        private static void SetFullBorders(Table table, Border border)
        {
            table.SetBorder(TableBorderType.InsideH, border);
            table.SetBorder(TableBorderType.InsideV, border);
            table.SetBorder(TableBorderType.Top, border);
            table.SetBorder(TableBorderType.Bottom, border);
            table.SetBorder(TableBorderType.Left, border);
            table.SetBorder(TableBorderType.Right, border);
        }

        private static void SetCell(Table table, int row, int col, string text, bool bold, Alignment align = Alignment.center, double fontSize = 14.0, bool italic = false)
        {
            if (row >= table.RowCount || col >= table.ColumnCount) return;
            var cell = table.Rows[row].Cells[col];
            cell.VerticalAlignment = VerticalAlignment.Center;
            var p = cell.Paragraphs[0];
            p.RemoveText(0);
            if (!string.IsNullOrEmpty(text))
            {
                var run = p.Append(text).Font("Times New Roman").FontSize(fontSize);
                if (bold) run.Bold();
                if (italic) run.Italic();
            }
            p.Alignment = align;
        }

        #endregion

        #region Bảng Ma Trận & Bản Đặc Tả Tiếng Việt (File 1)

        private static void InsertDocumentHeader(DocX doc, AppSettings settings, string title, int khoiLop, string subject)
        {
            var headerTable = doc.AddTable(1, 2);
            headerTable.Alignment = Alignment.center;
            headerTable.AutoFit = AutoFit.Window;

            var emptyBorder = new Border(BorderStyle.Tcbs_none, BorderSize.one, 0, BorderWhite);
            SetFullBorders(headerTable, emptyBorder);

            var cellLeft = headerTable.Rows[0].Cells[0];
            cellLeft.Paragraphs[0].Append(settings.TenUBND.ToUpper()).Font("Times New Roman").FontSize(13).Bold().Alignment = Alignment.center;
            var pLeft2 = cellLeft.InsertParagraph();
            pLeft2.Append(settings.TenTruong.ToUpper()).Font("Times New Roman").FontSize(13).Bold().Alignment = Alignment.center;

            var cellRight = headerTable.Rows[0].Cells[1];
            cellRight.Paragraphs[0].Append("CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM").Font("Times New Roman").FontSize(13).Bold().Alignment = Alignment.center;
            var pRight2 = cellRight.InsertParagraph();
            pRight2.Append("Độc lập - Tự do - Hạnh phúc").Font("Times New Roman").FontSize(13).Bold().UnderlineStyle(UnderlineStyle.singleLine).Alignment = Alignment.center;

            doc.InsertTable(headerTable);
            doc.InsertParagraph().SpacingAfter(10);

            var pTitle = doc.InsertParagraph();
            pTitle.Append(title.ToUpper()).Font("Times New Roman").FontSize(14).Bold().Alignment = Alignment.center;
            var pSubTitle = doc.InsertParagraph();
            pSubTitle.Append($"MÔN {subject.ToUpper()} – KHỐI {khoiLop} - NĂM HỌC {settings.NamHoc}").Font("Times New Roman").FontSize(14).Bold().Alignment = Alignment.center;
            pSubTitle.SpacingAfter(14);
        }

        /// <summary>
        /// Dựng bảng Ma trận 2 tầng tiêu đề (10 cột) đúng 100% mẫu quy chuẩn (media_1792025842122.png)
        /// Gồm 2 mạch kiến thức lớn: 1. Đọc hiểu văn bản (3 dòng con) & 2. Kiến thức tiếng Việt (3 dòng con),
        /// cùng khối tổng kết (Số câu, Số điểm và tỉ lệ tính theo dữ liệu thực tế).
        /// </summary>
        private static void InsertDynamicVietMatrixTable(DocX doc, List<VietMatrixItem> matrix)
        {
            var pIntro = doc.InsertParagraph();
            pIntro.Append("I. MA TRẬN ĐỀ KIỂM TRA ĐỊNH KỲ (PHẦN ĐỌC HIỂU & LTVC):").Font("Times New Roman").FontSize(14).Bold().SpacingAfter(6);

            if (matrix == null || matrix.Count == 0) return;

            // Bảng chuẩn 11 hàng x 10 cột
            var table = doc.AddTable(11, 10);
            table.Alignment = Alignment.center;
            table.AutoFit = AutoFit.Window;

            var solidBorder = new Border(BorderStyle.Tcbs_single, BorderSize.one, 0, BorderBlack);
            SetFullBorders(table, solidBorder);

            table.SetTableCellMargin(TableCellMarginType.left, 2.0f);
            table.SetTableCellMargin(TableCellMarginType.right, 2.0f);
            table.SetTableCellMargin(TableCellMarginType.top, 2.0f);
            table.SetTableCellMargin(TableCellMarginType.bottom, 2.0f);

            // Row 0 Header
            SetCell(table, 0, 0, "Mạch kiến thức,\nkĩ năng", true, Alignment.center, 14.0);
            SetCell(table, 0, 1, "Số câu\nvà\ncâu số", true, Alignment.center, 14.0);
            SetCell(table, 0, 2, "Mức 1", true, Alignment.center, 14.0);
            SetCell(table, 0, 4, "Mức 2", true, Alignment.center, 14.0);
            SetCell(table, 0, 6, "Mức 3", true, Alignment.center, 14.0);
            SetCell(table, 0, 8, "Tổng", true, Alignment.center, 14.0);

            // Row 1 Header
            SetCell(table, 1, 2, "TN\nKQ", true, Alignment.center, 14.0);
            SetCell(table, 1, 3, "TL", true, Alignment.center, 14.0);
            SetCell(table, 1, 4, "TN\nKQ", true, Alignment.center, 14.0);
            SetCell(table, 1, 5, "TL", true, Alignment.center, 14.0);
            SetCell(table, 1, 6, "TN\nKQ", true, Alignment.center, 14.0);
            SetCell(table, 1, 7, "TL", true, Alignment.center, 14.0);
            SetCell(table, 1, 8, "TN\nKQ", true, Alignment.center, 14.0);
            SetCell(table, 1, 9, "TL", true, Alignment.center, 14.0);

            // Bảng I và Bảng II phải dùng cùng một cách phân vùng câu hỏi.
            // Ưu tiên mạch kiến thức trong Step 1; helper chỉ dùng số câu khi mạch bị bỏ trống.
            var (b1, b2) = SplitMatrixSections(matrix);

            // ==================== KHỐI 1: ĐỌC HIỂU VĂN BẢN (Rows 2, 3, 4) ====================
            string b1Desc = "Đọc hiểu văn bản:\n- Hiểu nội dung văn bản. Chọn được ý trả lời đúng.\n- Nhận xét được hình ảnh, nhân vật hoặc chi tiết trong bài đọc; biết liên hệ những điều đọc được với bản thân vào thực tế.";
            SetCell(table, 2, 0, b1Desc, false, Alignment.both, 14.0);

            SetCell(table, 2, 1, "Số câu", false, Alignment.center, 14.0, italic: true);
            SetCell(table, 3, 1, "Câu số", false, Alignment.center, 14.0, italic: true);
            SetCell(table, 4, 1, "Số điểm", false, Alignment.center, 14.0, italic: true);

            PopulateBlockSubRows(table, 2, 3, 4, b1);

            // ==================== KHỐI 2: KIẾN THỨC TIẾNG VIỆT (Rows 5, 6, 7) ====================
            string b2Bullet1 = "- Xác định các từ loại, thành phần câu, nghĩa của từ hoặc biện pháp tu từ đã học.";
            string b2Bullet2 = "- Đặt câu đúng ngữ pháp, phân tích cấu tạo câu, sử dụng từ ngữ sinh động.";
            if (b2.Count > 0)
            {
                var m1List = b2.Where(x => x.MucDo == "M1").Select(x => x.NoiDungKienThuc).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
                var m23List = b2.Where(x => x.MucDo != "M1").Select(x => x.NoiDungKienThuc).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
                if (m1List.Count > 0) b2Bullet1 = "- Xác định, nhận biết: " + string.Join("; ", m1List) + ".";
                if (m23List.Count > 0) b2Bullet2 = "- Vận dụng: " + string.Join("; ", m23List) + ".";
            }
            string b2Desc = "Kiến thức tiếng Việt:\n" + b2Bullet1 + "\n" + b2Bullet2;
            SetCell(table, 5, 0, b2Desc, false, Alignment.both, 14.0);

            SetCell(table, 5, 1, "Số câu", false, Alignment.center, 14.0, italic: true);
            SetCell(table, 6, 1, "Câu số", false, Alignment.center, 14.0, italic: true);
            SetCell(table, 7, 1, "Số điểm", false, Alignment.center, 14.0, italic: true);

            PopulateBlockSubRows(table, 5, 6, 7, b2);

            // ==================== KHỐI TỔNG KẾT (Rows 8, 9, 10) ====================
            SetCell(table, 8, 0, "Tổng", true, Alignment.center, 14.0);

            // Row 8: Số câu
            SetCell(table, 8, 1, "Số câu", true, Alignment.center, 14.0);
            int allM1Tn = matrix.Count(x => x.MucDo == "M1" && x.HinhThuc == "TN");
            int allM1Tl = matrix.Count(x => x.MucDo == "M1" && x.HinhThuc == "TL");
            int allM2Tn = matrix.Count(x => x.MucDo == "M2" && x.HinhThuc == "TN");
            int allM2Tl = matrix.Count(x => x.MucDo == "M2" && x.HinhThuc == "TL");
            int allM3Tn = matrix.Count(x => x.MucDo == "M3" && x.HinhThuc == "TN");
            int allM3Tl = matrix.Count(x => x.MucDo == "M3" && x.HinhThuc == "TL");
            int totalTn = matrix.Count(x => x.HinhThuc == "TN");
            int totalTl = matrix.Count(x => x.HinhThuc == "TL");

            SetCell(table, 8, 2, allM1Tn.ToString(), false, Alignment.center, 14.0);
            SetCell(table, 8, 3, allM1Tl.ToString(), false, Alignment.center, 14.0);
            SetCell(table, 8, 4, allM2Tn.ToString(), false, Alignment.center, 14.0);
            SetCell(table, 8, 5, allM2Tl.ToString(), false, Alignment.center, 14.0);
            SetCell(table, 8, 6, allM3Tn.ToString(), false, Alignment.center, 14.0);
            SetCell(table, 8, 7, allM3Tl.ToString(), false, Alignment.center, 14.0);
            SetCell(table, 8, 8, totalTn.ToString(), false, Alignment.center, 14.0);
            SetCell(table, 8, 9, totalTl.ToString(), false, Alignment.center, 14.0);

            // Row 9: Số điểm
            SetCell(table, 9, 1, "Số điểm", true, Alignment.center, 14.0);
            double pM1 = matrix.Where(x => x.MucDo == "M1").Sum(x => x.Diem);
            double pM2 = matrix.Where(x => x.MucDo == "M2").Sum(x => x.Diem);
            double pM3 = matrix.Where(x => x.MucDo == "M3").Sum(x => x.Diem);
            double totalExamPoints = matrix.Sum(x => x.Diem);

            SetCell(table, 9, 2, FormatMatrixPoint(pM1), false, Alignment.center, 14.0);
            SetCell(table, 9, 4, FormatMatrixPoint(pM2), false, Alignment.center, 14.0);
            SetCell(table, 9, 6, FormatMatrixPoint(pM3), false, Alignment.center, 14.0);
            SetCell(table, 9, 8, FormatMatrixPoint(totalExamPoints), true, Alignment.center, 14.0);

            // Row 10: Tỉ lệ
            SetCell(table, 10, 1, "Tỉ lệ", true, Alignment.center, 14.0);
            SetCell(table, 10, 2, totalExamPoints > 0 ? FormatPercent(pM1 / totalExamPoints * 100) : "", false, Alignment.center, 14.0, italic: true);
            SetCell(table, 10, 4, totalExamPoints > 0 ? FormatPercent(pM2 / totalExamPoints * 100) : "", false, Alignment.center, 14.0, italic: true);
            SetCell(table, 10, 6, totalExamPoints > 0 ? FormatPercent(pM3 / totalExamPoints * 100) : "", false, Alignment.center, 14.0, italic: true);
            SetCell(table, 10, 8, "", false, Alignment.center, 14.0);

            // ==================== MERGE CELLS ====================
            // 1. Dọc (Vertical Merges First)
            table.MergeCellsInColumn(0, 0, 1);   // Header Col 0
            table.MergeCellsInColumn(1, 0, 1);   // Header Col 1
            table.MergeCellsInColumn(0, 2, 4);   // Khối 1: Đọc hiểu
            table.MergeCellsInColumn(0, 5, 7);   // Khối 2: Tiếng Việt
            table.MergeCellsInColumn(0, 8, 10);  // Khối 3: Tổng

            // 2. Ngang (Horizontal Merges - Từ Phải qua Trái)
            // Row 0 Header
            table.Rows[0].MergeCells(8, 9); // Tổng
            table.Rows[0].MergeCells(6, 7); // Mức 3
            table.Rows[0].MergeCells(4, 5); // Mức 2
            table.Rows[0].MergeCells(2, 3); // Mức 1

            // Row 9 Điểm
            table.Rows[9].MergeCells(8, 9); // Điểm Tổng
            table.Rows[9].MergeCells(6, 7); // Điểm M3
            table.Rows[9].MergeCells(4, 5); // Điểm M2
            table.Rows[9].MergeCells(2, 3); // Điểm M1

            // Row 10 Tỉ lệ
            table.Rows[10].MergeCells(8, 9); // Trống
            table.Rows[10].MergeCells(6, 7); // Tỉ lệ M3
            table.Rows[10].MergeCells(4, 5); // Tỉ lệ M2
            table.Rows[10].MergeCells(2, 3); // Tỉ lệ M1

            doc.InsertTable(table);
        }

        private static void PopulateBlockSubRows(Table table, int rSoCau, int rCauSo, int rSoDiem, List<VietMatrixItem> items)
        {
            // Mức 1 (Col 2: TN, Col 3: TL)
            var m1Tn = items.Where(x => x.MucDo == "M1" && x.HinhThuc == "TN").ToList();
            var m1Tl = items.Where(x => x.MucDo == "M1" && x.HinhThuc == "TL").ToList();
            PopulateCellTriplet(table, rSoCau, rCauSo, rSoDiem, 2, m1Tn);
            PopulateCellTriplet(table, rSoCau, rCauSo, rSoDiem, 3, m1Tl);

            // Mức 2 (Col 4: TN, Col 5: TL)
            var m2Tn = items.Where(x => x.MucDo == "M2" && x.HinhThuc == "TN").ToList();
            var m2Tl = items.Where(x => x.MucDo == "M2" && x.HinhThuc == "TL").ToList();
            PopulateCellTriplet(table, rSoCau, rCauSo, rSoDiem, 4, m2Tn);
            PopulateCellTriplet(table, rSoCau, rCauSo, rSoDiem, 5, m2Tl);

            // Mức 3 (Col 6: TN, Col 7: TL)
            var m3Tn = items.Where(x => x.MucDo == "M3" && x.HinhThuc == "TN").ToList();
            var m3Tl = items.Where(x => x.MucDo == "M3" && x.HinhThuc == "TL").ToList();
            PopulateCellTriplet(table, rSoCau, rCauSo, rSoDiem, 6, m3Tn);
            PopulateCellTriplet(table, rSoCau, rCauSo, rSoDiem, 7, m3Tl);

            // Tổng TN (Col 8) & Tổng TL (Col 9)
            int cTn = m1Tn.Count + m2Tn.Count + m3Tn.Count;
            int cTl = m1Tl.Count + m2Tl.Count + m3Tl.Count;
            if (cTn > 0) SetCell(table, rSoCau, 8, cTn.ToString(), false, Alignment.center, 14.0);
            if (cTl > 0) SetCell(table, rSoCau, 9, cTl.ToString(), false, Alignment.center, 14.0);

            double pTn = m1Tn.Sum(x => x.Diem) + m2Tn.Sum(x => x.Diem) + m3Tn.Sum(x => x.Diem);
            double pTl = m1Tl.Sum(x => x.Diem) + m2Tl.Sum(x => x.Diem) + m3Tl.Sum(x => x.Diem);
            if (pTn > 0) SetCell(table, rSoDiem, 8, FormatMatrixPoint(pTn), false, Alignment.center, 14.0);
            if (pTl > 0) SetCell(table, rSoDiem, 9, FormatMatrixPoint(pTl), false, Alignment.center, 14.0);
        }

        private static void PopulateCellTriplet(Table table, int rSoCau, int rCauSo, int rSoDiem, int col, List<VietMatrixItem> items)
        {
            if (items.Count > 0)
            {
                SetCell(table, rSoCau, col, items.Count.ToString(), false, Alignment.center, 14.0);
                SetCell(table, rCauSo, col, string.Join(",", items.Select(x => x.SoThuTu)), false, Alignment.center, 14.0);
                SetCell(table, rSoDiem, col, FormatMatrixPoint(items.Sum(x => x.Diem)), false, Alignment.center, 14.0);
            }
        }

        /// <summary>
        /// Chia câu hỏi thành hai khối theo cùng quy tắc mà Bảng I sử dụng.
        /// Mạch kiến thức là phân loại chính của dữ liệu Step 1; số thứ tự chỉ
        /// được dùng cho các dòng chưa có mạch kiến thức.
        /// </summary>
        private static (List<VietMatrixItem> Reading, List<VietMatrixItem> Vietnamese) SplitMatrixSections(IEnumerable<VietMatrixItem>? source)
        {
            var items = (source ?? Enumerable.Empty<VietMatrixItem>())
                .Where(x => x != null)
                .ToList();

            var reading = items
                .Where(x => IsReadingSection(x.MachKienThuc)
                         || (string.IsNullOrWhiteSpace(x.MachKienThuc)
                             && x.SoThuTu > 0 && x.SoThuTu <= 7))
                .ToList();
            var vietnamese = items.Where(x => !reading.Contains(x)).ToList();
            return (reading, vietnamese);
        }

        private static bool IsReadingSection(string? section)
        {
            return !string.IsNullOrWhiteSpace(section)
                && section.Contains("đọc hiểu", StringComparison.OrdinalIgnoreCase);
        }

        private static string FormatPoint(double point)
        {
            if (point <= 0) return "0";
            return point % 1 == 0 ? $"{point:0}" : $"{point:0.##}".Replace(".", ",");
        }

        /// <summary>
        /// Định dạng điểm trong Bảng I/II. Số nguyên hiển thị số nguyên (1, 2, 3... không có ,0);
        /// số thập phân hiển thị dấu phẩy (0,5); giá trị không có dữ liệu trả về chuỗi rỗng.
        /// </summary>
        private static string FormatMatrixPoint(double point)
        {
            var normalized = Math.Round(point, 2);
            if (normalized <= 0) return string.Empty;

            string formatted = Math.Abs(normalized % 1) < 0.0001
                ? $"{normalized:0}"
                : $"{normalized:0.##}";
            return formatted.Replace(".", ",");
        }

        private static string FormatPercent(double percent)
        {
            return $"{percent:0.##}%".Replace(".", ",");
        }

        private static void InsertDynamicVietSpecificationTable(DocX doc, List<VietMatrixItem> matrix)
        {
            var pIntro = doc.InsertParagraph();
            pIntro.Append("II. BẢNG MA TRẬN ĐẶC TẢ CHI TIẾT MÔN TIẾNG VIỆT:").Font("Times New Roman").FontSize(14).Bold().SpacingBefore(8).SpacingAfter(6);

            if (matrix == null || matrix.Count == 0) return;

            // Bảng Ma trận đặc tả 13 cột chuẩn Bộ Giáo dục & Đào tạo (Thông tư 27 & GDPT 2018)
            var table = doc.AddTable(14, 13);
            table.Alignment = Alignment.center;
            table.AutoFit = AutoFit.Window;

            var solidBorder = new Border(BorderStyle.Tcbs_single, BorderSize.one, 0, BorderBlack);
            table.SetBorder(TableBorderType.InsideH, solidBorder);
            table.SetBorder(TableBorderType.InsideV, solidBorder);
            table.SetBorder(TableBorderType.Top, solidBorder);
            table.SetBorder(TableBorderType.Bottom, solidBorder);
            table.SetBorder(TableBorderType.Left, solidBorder);
            table.SetBorder(TableBorderType.Right, solidBorder);

            table.SetTableCellMargin(TableCellMarginType.left, 2f);
            table.SetTableCellMargin(TableCellMarginType.right, 2f);
            table.SetTableCellMargin(TableCellMarginType.top, 2f);
            table.SetTableCellMargin(TableCellMarginType.bottom, 2f);

            // ==================== HEADER (Rows 0 & 1) ====================
            SetCell(table, 0, 0, "Nội dung đánh giá", true, Alignment.center, 11.5);
            SetCell(table, 0, 3, "Yêu cầu cần đạt", true, Alignment.center, 11.5);
            SetCell(table, 0, 4, "Số câu", true, Alignment.center, 11.5);
            SetCell(table, 0, 6, "Hình thức", true, Alignment.center, 11.5);
            SetCell(table, 0, 8, "Mức", true, Alignment.center, 11.5);
            SetCell(table, 0, 11, "Điểm", true, Alignment.center, 11.5);

            // ==================== PHÂN NHÓM DỮ LIỆU ĐỘNG ====================
            // Dùng đúng bộ phân vùng của Bảng I để hai bảng không thể lệch tổng.
            var (b1, b2) = SplitMatrixSections(matrix);

            bool HasKeyword(VietMatrixItem item, params string[] keywords)
            {
                string text = $"{item.NoiDungKienThuc} {item.YeuCauCanDat}".ToLowerInvariant();
                return keywords.Any(kw => text.Contains(kw.ToLowerInvariant()));
            }

            // 1. Phân bổ mạch Đọc hiểu văn bản vào 3 dòng của Văn bản văn học.
            // M3 luôn là "Liên hệ, so sánh, kết nối"; việc này tránh từ khóa "so sánh"
            // trong mô tả làm câu M3 rơi nhầm vào dòng hình thức.
            var b1Lh = b1.Where(x => string.Equals(x.MucDo?.Trim(), "M3", StringComparison.OrdinalIgnoreCase)
                                  || HasKeyword(x, "liên hệ, so sánh, kết nối", "liên hệ:", "liên hệ bản thân", "rút ra bài học", "bài học từ", "cảm nhận cá nhân")).ToList();
            var b1Remaining = b1.Where(x => !b1Lh.Contains(x)).ToList();

            // Các mô tả có chỉ dấu rõ ràng về hình thức được gom vào dòng tương ứng.
            // Không tự bù hoặc chuyển câu chỉ để làm đầy dòng.
            var b1Ht = b1Remaining.Where(x => HasKeyword(x, "đọc hiểu hình thức", "hình thức:", "hình thức -", "hình ảnh nghệ thuật", "hình ảnh biểu tượng", "biện pháp tu từ", "nghệ thuật miêu tả", "chi tiết nghệ thuật", "từ ngữ gợi cảm", "gợi cảm", "so sánh", "nhân hóa", "điệp từ", "điệp ngữ", "ý nghĩa đoạn văn", "ý nghĩa của đoạn", "ý nghĩa thông tin")).ToList();
            var b1Nd = b1Remaining.Where(x => !b1Ht.Contains(x)).ToList();

            // 2. Phân loại mạch Kiến thức Tiếng Việt theo từ khóa nội dung & yêu cầu cần đạt
            var naList = new List<VietMatrixItem>();
            var tvList = new List<VietMatrixItem>();
            var npList = new List<VietMatrixItem>();
            var hdgtList = new List<VietMatrixItem>();
            var btList = new List<VietMatrixItem>();
            var unclassifiedList = new List<VietMatrixItem>();

            foreach (var item in b2)
            {
                string nd = (item.NoiDungKienThuc ?? "").Trim();

                // 1. Ưu tiên tiền tố phân loại tường minh trong tên nội dung kiến thức
                if (nd.StartsWith("Hoạt động giao tiếp", StringComparison.OrdinalIgnoreCase) ||
                    nd.StartsWith("Giao tiếp", StringComparison.OrdinalIgnoreCase))
                {
                    hdgtList.Add(item);
                }
                else if (nd.StartsWith("Ngữ âm", StringComparison.OrdinalIgnoreCase) ||
                         nd.StartsWith("Chính tả", StringComparison.OrdinalIgnoreCase))
                {
                    naList.Add(item);
                }
                else if (nd.StartsWith("Ngữ pháp", StringComparison.OrdinalIgnoreCase) ||
                         nd.StartsWith("Dấu câu", StringComparison.OrdinalIgnoreCase))
                {
                    npList.Add(item);
                }
                else if (nd.StartsWith("Từ vựng", StringComparison.OrdinalIgnoreCase))
                {
                    tvList.Add(item);
                }
                else if (nd.StartsWith("Sự phát triển của ngôn ngữ", StringComparison.OrdinalIgnoreCase) ||
                         nd.StartsWith("Biến thể", StringComparison.OrdinalIgnoreCase) ||
                         nd.StartsWith("Từ mượn", StringComparison.OrdinalIgnoreCase))
                {
                    btList.Add(item);
                }
                // 2. Nhận diện theo từ khóa đặc thù Ngữ pháp / Dấu câu / Thành phần câu (xét trước để không bị 'đặt câu' cướp)
                else if (HasKeyword(item, "ngữ pháp", "dấu câu", "dấu gạch ngang", "dấu hai chấm", "dấu ngoặc kép", "dấu ngoặc đơn", "chủ ngữ", "vị ngữ", "trạng ngữ", "thành phần câu", "cấu tạo câu", "câu đơn", "câu ghép", "câu kể", "câu hỏi", "câu cảm", "câu khiến", "kết từ", "quan hệ từ", "đại từ", "từ hô ứng"))
                {
                    npList.Add(item);
                }
                // 3. Từ vựng & Tu từ
                else if (HasKeyword(item, "từ vựng", "từ đồng nghĩa", "từ trái nghĩa", "từ đồng âm", "từ đa nghĩa", "từ nhiều nghĩa", "nghĩa của từ", "thành ngữ", "tục ngữ", "từ đơn", "từ phức", "từ ghép", "từ láy", "từ loại", "danh từ", "động từ", "tính từ", "biện pháp tu từ", "tu từ", "nhân hóa", "so sánh", "điệp từ", "điệp ngữ"))
                {
                    tvList.Add(item);
                }
                // 4. Ngữ âm & Chữ viết
                else if (HasKeyword(item, "ngữ âm", "chính tả", "viết hoa", "vần", "âm đầu", "thanh điệu"))
                {
                    naList.Add(item);
                }
                // 5. Sự phát triển ngôn ngữ & biến thể
                else if (HasKeyword(item, "từ mượn", "từ địa phương", "biến thể", "từ mới", "phát triển của ngôn ngữ"))
                {
                    btList.Add(item);
                }
                // 6. Hoạt động giao tiếp (liên kết câu / nghi thức giao tiếp / viết đoạn văn / thực hành đặt câu, viết câu theo ngữ cảnh, chủ đề)
                else if (HasKeyword(item, "hoạt động giao tiếp", "giao tiếp", "liên kết câu", "phép lặp", "phép thay thế", "từ ngữ nối", "đoạn văn", "viết đoạn", "lịch sự", "nghi thức", "đặt câu", "viết câu", "dùng từ đặt câu"))
                {
                    hdgtList.Add(item);
                }
                else
                {
                    // Do not silently force an unsupported strand into Vocabulary (or Grammar).
                    // A catalog item must be classified explicitly before its detail row can be exported.
                    unclassifiedList.Add(item);
                }
            }

            if (unclassifiedList.Count > 0)
                throw new InvalidOperationException($"Không xác định được mạch Bảng II cho câu {string.Join(", ", unclassifiedList.Select(item => item.SoThuTu))}; đã dừng xuất để tránh phân loại sai nội dung.");

            void SetSpecRowCounts(int r, int cSoCau, List<VietMatrixItem> items)
            {
                if (items == null || items.Count == 0) return;

                SetCell(table, r, cSoCau, items.Count.ToString(), false, Alignment.center, 11.0);
                int tn = items.Count(x => x.HinhThuc == "TN");
                int tl = items.Count(x => x.HinhThuc == "TL");
                int m1 = items.Count(x => x.MucDo == "M1");
                int m2 = items.Count(x => x.MucDo == "M2");
                int m3 = items.Count(x => x.MucDo == "M3");
                double d = items.Sum(x => x.Diem);

                if (tn > 0) SetCell(table, r, 6, tn.ToString(), false, Alignment.center, 11.0);
                if (tl > 0) SetCell(table, r, 7, tl.ToString(), false, Alignment.center, 11.0);
                if (m1 > 0) SetCell(table, r, 8, m1.ToString(), false, Alignment.center, 11.0);
                if (m2 > 0) SetCell(table, r, 9, m2.ToString(), false, Alignment.center, 11.0);
                if (m3 > 0) SetCell(table, r, 10, m3.ToString(), false, Alignment.center, 11.0);
                if (d > 0) SetCell(table, r, 11, FormatMatrixPoint(d), false, Alignment.center, 11.0);
            }

            // ==================== HEADER ROW 1 ====================
            SetCell(table, 1, 0, "Đọc hiểu", true, Alignment.center, 11.0);
            SetCell(table, 1, 1, "Văn bản văn học\n(Truyện, miêu tả, thơ)", true, Alignment.center, 10.5);
            SetCell(table, 1, 4, b1.Count > 0 ? b1.Count.ToString() : "", true, Alignment.center, 11.5);
            SetCell(table, 1, 6, "TN", true, Alignment.center, 11.0);
            SetCell(table, 1, 7, "TL", true, Alignment.center, 11.0);
            SetCell(table, 1, 8, "1", true, Alignment.center, 11.0);
            SetCell(table, 1, 9, "2", true, Alignment.center, 11.0);
            SetCell(table, 1, 10, "3", true, Alignment.center, 11.0);

            // ==================== KHỐI 1: VĂN BẢN VĂN HỌC (Rows 2, 3, 4) ====================
            SetCell(table, 2, 2, "Đọc hiểu nội dung", true, Alignment.left, 10.5);
            string reqNd = b1Nd.Count > 0 ? string.Join("; ", b1Nd.Select(x => x.YeuCauCanDat).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct()) : "";
            SetCell(table, 2, 3, reqNd, false, Alignment.left, 10.5);
            SetSpecRowCounts(2, 5, b1Nd);
            double b1Diem = b1.Sum(x => x.Diem);
            SetCell(table, 2, 12, b1.Count > 0 ? FormatMatrixPoint(b1Diem) : "", true, Alignment.center, 11.5);

            SetCell(table, 3, 2, "Đọc hiểu hình thức", true, Alignment.left, 10.5);
            string reqHt = b1Ht.Count > 0 ? string.Join("; ", b1Ht.Select(x => x.YeuCauCanDat).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct()) : "";
            SetCell(table, 3, 3, reqHt, false, Alignment.left, 10.5);
            SetSpecRowCounts(3, 5, b1Ht);

            SetCell(table, 4, 2, "Liên hệ, so sánh, kết nối", true, Alignment.left, 10.5);
            string reqLh = b1Lh.Count > 0 ? string.Join("; ", b1Lh.Select(x => x.YeuCauCanDat).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct()) : "";
            SetCell(table, 4, 3, reqLh, false, Alignment.left, 10.5);
            SetSpecRowCounts(4, 5, b1Lh);

            // ==================== KHỐI 2: VĂN BẢN THÔNG TIN (Rows 5, 6, 7) ====================
            SetCell(table, 5, 1, "Văn bản thông tin", true, Alignment.center, 10.5);
            SetCell(table, 5, 2, "Đọc hiểu nội dung", false, Alignment.left, 10.5);
            SetCell(table, 5, 3, "", false, Alignment.left, 10.5);

            SetCell(table, 6, 2, "Đọc hiểu hình thức", false, Alignment.left, 10.5);
            SetCell(table, 6, 3, "", false, Alignment.left, 10.5);

            SetCell(table, 7, 2, "Liên hệ, so sánh, kết nối", false, Alignment.left, 10.5);
            SetCell(table, 7, 3, "", false, Alignment.left, 10.5);

            // ==================== KHỐI 3: KIẾN THỨC TIẾNG VIỆT (Rows 8, 9, 10, 11, 12) ====================
            SetCell(table, 8, 0, "Kiến thức Tiếng Việt", true, Alignment.center, 11.0);
            SetCell(table, 8, 1, "Ngữ âm", true, Alignment.center, 10.5);
            string reqNa = naList.Count > 0 ? string.Join("; ", naList.Select(x => x.YeuCauCanDat).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct()) : "";
            SetCell(table, 8, 3, reqNa, false, Alignment.left, 10.5);
            SetCell(table, 8, 4, b2.Count > 0 ? b2.Count.ToString() : "", true, Alignment.center, 11.5);
            SetSpecRowCounts(8, 5, naList);
            double b2Diem = b2.Sum(x => x.Diem);
            SetCell(table, 8, 12, b2.Count > 0 ? FormatMatrixPoint(b2Diem) : "", true, Alignment.center, 11.5);

            SetCell(table, 9, 1, "Từ vựng", true, Alignment.center, 10.5);
            string reqTv = tvList.Count > 0 ? string.Join("; ", tvList.Select(x => x.YeuCauCanDat).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct()) : "";
            SetCell(table, 9, 3, reqTv, false, Alignment.left, 10.5);
            SetSpecRowCounts(9, 5, tvList);

            SetCell(table, 10, 1, "Ngữ pháp", true, Alignment.center, 10.5);
            string reqNp = npList.Count > 0 ? string.Join("; ", npList.Select(x => x.YeuCauCanDat).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct()) : "";
            SetCell(table, 10, 3, reqNp, false, Alignment.left, 10.5);
            SetSpecRowCounts(10, 5, npList);

            SetCell(table, 11, 1, "Hoạt động giao tiếp", true, Alignment.center, 10.5);
            string reqHdgt = hdgtList.Count > 0 ? string.Join("; ", hdgtList.Select(x => x.YeuCauCanDat).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct()) : "";
            SetCell(table, 11, 3, reqHdgt, false, Alignment.left, 10.5);
            SetSpecRowCounts(11, 5, hdgtList);

            SetCell(table, 12, 1, "Sự phát triển của ngôn ngữ và các biến thể của ngôn ngữ", true, Alignment.center, 10.0);
            string reqBt = btList.Count > 0 ? string.Join("; ", btList.Select(x => x.YeuCauCanDat).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct()) : "";
            SetCell(table, 12, 3, reqBt, false, Alignment.left, 10.5);
            SetSpecRowCounts(12, 5, btList);

            // ==================== HÀNG 13: TỔNG ====================
            SetCell(table, 13, 0, "Tổng", true, Alignment.center, 11.5);
            var detailRows = new[] { b1Nd, b1Ht, b1Lh, naList, tvList, npList, hdgtList, btList };
            var detailItems = detailRows.SelectMany(items => items).ToList();
            if (detailItems.Count != matrix.Count)
            {
                throw new InvalidOperationException("Bảng đặc tả chưa phân bổ đủ câu hỏi từ Bước 1.");
            }
            int totalQuestions = detailRows.Sum(items => items.Count);
            int totalTn = detailRows.Sum(items => items.Count(x => x.HinhThuc == "TN"));
            int totalTl = detailRows.Sum(items => items.Count(x => x.HinhThuc == "TL"));
            int totalM1 = detailRows.Sum(items => items.Count(x => x.MucDo == "M1"));
            int totalM2 = detailRows.Sum(items => items.Count(x => x.MucDo == "M2"));
            int totalM3 = detailRows.Sum(items => items.Count(x => x.MucDo == "M3"));
            double totalExamPoints = detailRows.Sum(items => items.Sum(x => x.Diem));

            SetCell(table, 13, 4, totalQuestions > 0 ? totalQuestions.ToString() : "", true, Alignment.center, 11.5);
            SetCell(table, 13, 6, totalTn > 0 ? totalTn.ToString() : "", true, Alignment.center, 11.5);
            SetCell(table, 13, 7, totalTl > 0 ? totalTl.ToString() : "", true, Alignment.center, 11.5);
            SetCell(table, 13, 8, totalM1 > 0 ? totalM1.ToString() : "", true, Alignment.center, 11.5);
            SetCell(table, 13, 9, totalM2 > 0 ? totalM2.ToString() : "", true, Alignment.center, 11.5);
            SetCell(table, 13, 10, totalM3 > 0 ? totalM3.ToString() : "", true, Alignment.center, 11.5);
            SetCell(table, 13, 11, totalExamPoints > 0 ? FormatMatrixPoint(totalExamPoints) : "", true, Alignment.center, 11.5);

            // ==================== MERGE CELLS ====================
            // 1. Dọc (Vertical Merges First)
            table.MergeCellsInColumn(3, 0, 1);   // Col 3: Yêu cầu cần đạt (các hàng tiêu đề)
            table.MergeCellsInColumn(11, 0, 1);  // Col 11: Điểm (các hàng tiêu đề)
            table.MergeCellsInColumn(12, 0, 1);  // Col 12: Điểm (các hàng tiêu đề)

            table.MergeCellsInColumn(0, 1, 7);   // Col 0: Đọc hiểu (Row 1-7)
            table.MergeCellsInColumn(1, 1, 4);   // Col 1: Văn bản văn học (Row 1-4)
            table.MergeCellsInColumn(1, 5, 7);   // Col 1: Văn bản thông tin (Row 5-7)
            table.MergeCellsInColumn(4, 1, 7);   // Col 4: Số câu khối Đọc hiểu
            table.MergeCellsInColumn(12, 2, 7);  // Col 12: Điểm khối Đọc hiểu

            table.MergeCellsInColumn(0, 8, 12);  // Col 0: Kiến thức Tiếng Việt (Row 8-12)
            table.MergeCellsInColumn(4, 8, 12);  // Col 4: Số câu khối Tiếng Việt
            table.MergeCellsInColumn(12, 8, 12); // Col 12: Điểm khối Tiếng Việt

            // 2. Ngang (Horizontal Merges - Từ Phải qua Trái)
            // Row 0
            table.Rows[0].MergeCells(11, 12); // Điểm
            table.Rows[0].MergeCells(8, 10);  // Mức
            table.Rows[0].MergeCells(6, 7);   // Hình thức
            table.Rows[0].MergeCells(4, 5);   // Số câu
            table.Rows[0].MergeCells(0, 2);   // Nội dung đánh giá

            // Row 1
            table.Rows[1].MergeCells(11, 12); // Điểm

            // Row 8 to 12: Merge Col 1 and 2
            for (int r = 8; r <= 12; r++)
            {
                table.Rows[r].MergeCells(1, 2);
            }

            // Row 13: Dòng Tổng
            table.Rows[13].MergeCells(11, 12); // Điểm
            table.Rows[13].MergeCells(4, 5);   // Số câu
            table.Rows[13].MergeCells(0, 2);   // Tổng

            doc.InsertTable(table);
        }

        private static void InsertApprovalSignatures(DocX doc)
        {
            var sigTable = doc.AddTable(2, 3);
            sigTable.Alignment = Alignment.center;
            sigTable.AutoFit = AutoFit.Window;

            var emptyBorder = new Border(BorderStyle.Tcbs_none, BorderSize.one, 0, BorderWhite);
            SetFullBorders(sigTable, emptyBorder);

            sigTable.Rows[0].Cells[0].Paragraphs[0].Append("NGƯỜI LẬP MA TRẬN\n(Ký, ghi rõ họ tên)").Font("Times New Roman").FontSize(14).Bold().Alignment = Alignment.center;
            sigTable.Rows[0].Cells[1].Paragraphs[0].Append("TỔ TRƯỞNG CHUYÊN MÔN\n(Ký, ghi rõ họ tên)").Font("Times New Roman").FontSize(14).Bold().Alignment = Alignment.center;
            sigTable.Rows[0].Cells[2].Paragraphs[0].Append("BAN GIÁM HIỆU DUYỆT\n(Ký tên, đóng dấu)").Font("Times New Roman").FontSize(14).Bold().Alignment = Alignment.center;

            sigTable.Rows[1].Height = 60f;

            doc.InsertTable(sigTable);
        }

        #endregion

        #region Đề Kiểm Tra & Hướng Dẫn Chấm Tiếng Việt (File 2)

        private static void InsertExamHeader(DocX doc, AppSettings settings, VietExamPaper paper)
        {
            var headerTable = doc.AddTable(1, 2);
            headerTable.Alignment = Alignment.center;
            headerTable.AutoFit = AutoFit.Window;

            var emptyBorder = new Border(BorderStyle.Tcbs_none, BorderSize.one, 0, BorderWhite);
            SetFullBorders(headerTable, emptyBorder);

            var cellLeft = headerTable.Rows[0].Cells[0];
            cellLeft.Paragraphs[0].Append(settings.TenUBND.ToUpper()).Font("Times New Roman").FontSize(13).Bold().Alignment = Alignment.center;
            var pLeft2 = cellLeft.InsertParagraph();
            pLeft2.Append(settings.TenTruong.ToUpper()).Font("Times New Roman").FontSize(13).Bold().Alignment = Alignment.center;

            var cellRight = headerTable.Rows[0].Cells[1];
            cellRight.Paragraphs[0].Append("CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM").Font("Times New Roman").FontSize(13).Bold().Alignment = Alignment.center;
            var pRight2 = cellRight.InsertParagraph();
            pRight2.Append("Độc lập – Tự do – Hạnh phúc").Font("Times New Roman").FontSize(13).Bold().UnderlineStyle(UnderlineStyle.singleLine).Alignment = Alignment.center;

            doc.InsertTable(headerTable);
            doc.InsertParagraph().SpacingAfter(6);

            var pTitle = doc.InsertParagraph();
            string periodUpper = "CUỐI HỌC KỲ II";
            if (!string.IsNullOrWhiteSpace(paper.TieuDe))
            {
                periodUpper = paper.TieuDe.ToUpper();
                if (periodUpper.StartsWith("BÀI KIỂM TRA ĐỊNH KỲ "))
                {
                    periodUpper = periodUpper.Substring("BÀI KIỂM TRA ĐỊNH KỲ ".Length).Trim();
                }
            }
            pTitle.Append($"BÀI KIỂM TRA ĐỊNH KỲ {periodUpper} - KHỐI {paper.KhoiLop}\nNĂM HỌC {settings.NamHoc}").Font("Times New Roman").FontSize(14).Bold().Alignment = Alignment.center;

            var pSub = doc.InsertParagraph();
            pSub.Append($"MÔN TIẾNG VIỆT\n(Thời gian làm bài {paper.ThoiGianPhut} phút – Không kể thời gian đọc thành tiếng)").Font("Times New Roman").FontSize(14).Italic().Alignment = Alignment.center;
            pSub.SpacingAfter(8);

            var pName = doc.InsertParagraph();
            pName.Append("Họ và tên:……………………………………………………… Lớp: ………").Font("Times New Roman").FontSize(14).SpacingAfter(8);
        }

        private static void InsertGradeBox(DocX doc)
        {
            var boxTable = doc.AddTable(1, 2);
            boxTable.Alignment = Alignment.center;
            boxTable.AutoFit = AutoFit.Window;

            var solidBorder = new Border(BorderStyle.Tcbs_single, BorderSize.one, 0, BorderBlack);
            SetFullBorders(boxTable, solidBorder);

            var cellDiem = boxTable.Rows[0].Cells[0];
            cellDiem.Width = 140;
            cellDiem.Paragraphs[0].Append("Điểm").Font("Times New Roman").FontSize(14).Bold().Alignment = Alignment.center;

            var cellNx = boxTable.Rows[0].Cells[1];
            cellNx.Paragraphs[0].Append("Lời nhận xét của giáo viên").Font("Times New Roman").FontSize(14).Bold().Alignment = Alignment.center;

            for (int i = 0; i < 3; i++)
            {
                var dotP = cellNx.InsertParagraph();
                dotP.Append(".........................................................................................................................................")
                    .Font("Times New Roman").FontSize(10).SpacingAfter(2);
            }

            boxTable.Rows[0].Height = 60f;

            doc.InsertTable(boxTable);
            doc.InsertParagraph().SpacingAfter(10);
        }

        private static void InsertExamBody(DocX doc, VietExamPaper paper)
        {
            // A. KIỂM TRA KĨ NĂNG ĐỌC VÀ KIẾN THỨC TIẾNG VIỆT (10 điểm)
            var pMain = doc.InsertParagraph();
            pMain.Append("A. KIỂM TRA KĨ NĂNG ĐỌC VÀ KIẾN THỨC TIẾNG VIỆT (10 điểm)").Font("Times New Roman").FontSize(14).Bold().SpacingBefore(4).SpacingAfter(6);

            // I. Đọc thành tiếng (2 điểm)
            var pI = doc.InsertParagraph();
            pI.Append("I. Kiểm tra đọc thành tiếng (2 điểm)").Font("Times New Roman").FontSize(14).Bold();
            pI.Append(" - Khoảng 105 - 110 tiếng/phút/em.").Font("Times New Roman").FontSize(14).Italic().SpacingAfter(6);

            // II. Đọc hiểu kết hợp kiến thức tiếng Việt (8 điểm)
            var pII = doc.InsertParagraph();
            pII.Append("II. Kiểm tra đọc hiểu kết hợp kiểm tra kiến thức tiếng Việt (8 điểm)").Font("Times New Roman").FontSize(14).Bold().SpacingAfter(6);

            // BÀI ĐỌC THẦM ĐÓNG KHUNG CHỮ NHẬT
            if (paper.BaiDoc != null && !string.IsNullOrEmpty(paper.BaiDoc.NoiDung))
            {
                var boxReading = doc.AddTable(1, 1);
                boxReading.Alignment = Alignment.center;
                boxReading.AutoFit = AutoFit.Window;

                var solidBorder = new Border(BorderStyle.Tcbs_single, BorderSize.one, 0, BorderBlack);
                SetFullBorders(boxReading, solidBorder);

                var cell = boxReading.Rows[0].Cells[0];
                var pIntro = cell.Paragraphs[0];
                pIntro.Append("Đọc thầm bài văn sau:").Font("Times New Roman").FontSize(14).Italic().SpacingAfter(6);

                var pTitle = cell.InsertParagraph();
                pTitle.Append(paper.BaiDoc.TieuDe.ToUpper()).Font("Times New Roman").FontSize(14).Bold().Alignment = Alignment.center;
                pTitle.SpacingAfter(6);

                var pContent = cell.InsertParagraph();
                DocxContentRenderer.AppendTextWithFractions(doc, pContent, paper.BaiDoc.NoiDung, "Times New Roman", 14.0);
                pContent.IndentationFirstLine = 20f;
                pContent.SpacingAfter(6);

                if (!string.IsNullOrEmpty(paper.BaiDoc.TacGia))
                {
                    var pAuthor = cell.InsertParagraph();
                    pAuthor.Append($"Theo {paper.BaiDoc.TacGia}").Font("Times New Roman").FontSize(14).Italic().Alignment = Alignment.right;
                    pAuthor.SpacingAfter(6);
                }

                doc.InsertTable(boxReading);
                doc.InsertParagraph().SpacingAfter(6);
            }

            var pGuide = doc.InsertParagraph();
            pGuide.Append("Khoanh vào chữ cái đặt trước đáp án đúng nhất và thực hiện các bài tập sau:").Font("Times New Roman").FontSize(14).Italic().SpacingAfter(6);

            // 11 CÂU HỎI TIẾNG VIỆT
            foreach (var q in paper.DanhSachCauHoi.OrderBy(x => x.SoThuTu))
            {
                var pQ = doc.InsertParagraph();
                pQ.Append($"Câu {q.SoThuTu} ({q.MucDo}). ").Font("Times New Roman").FontSize(14).Bold();
                DocxContentRenderer.AppendTextWithFractions(doc, pQ, q.NoiDung, "Times New Roman", 14.0);
                pQ.SpacingAfter(6);

                if (q.LoaiCauHoi == "TN_4LuaChon" && !string.IsNullOrEmpty(q.DapAnA))
                {
                    InsertMultipleChoiceOptionsTable(doc, q);
                }
                else
                {
                    // Tự luận: kẻ bảng ô ly chuẩn mẫu (20 cột x (số dòng lớn * 4) hàng con với 3 nét chấm)
                    int soDong = q.SoDongOly > 0 ? q.SoDongOly : (q.SoThuTu == 6 || q.SoThuTu == 11 ? 3 : 2);
                    AppendOlyTable(doc, soDong, 20);
                    doc.InsertParagraph().SpacingAfter(6);
                }
            }
        }

        private static void InsertMultipleChoiceOptionsTable(DocX doc, VietQuestion q)
        {
            string optA = q.DapAnA ?? "";
            string optB = q.DapAnB ?? "";
            string optC = q.DapAnC ?? "";
            string optD = q.DapAnD ?? "";

            int maxLen = Math.Max(optA.Length, Math.Max(optB.Length, Math.Max(optC.Length, optD.Length)));

            // 1. Đáp án ngắn (<= 25 ký tự) -> Dàn 4 phương án trên 1 dòng duy nhất bằng Table 1 hàng x 4 cột No Border
            if (maxLen <= 25 && !string.IsNullOrEmpty(optC) && !string.IsNullOrEmpty(optD))
            {
                var table = doc.AddTable(1, 4);
                table.Alignment = Alignment.center;
                table.AutoFit = AutoFit.Window;
                SetFullBorders(table, new Border(BorderStyle.Tcbs_none, BorderSize.one, 0, BorderWhite));

                SetOptionCell(doc, table.Rows[0].Cells[0], "A", optA);
                SetOptionCell(doc, table.Rows[0].Cells[1], "B", optB);
                SetOptionCell(doc, table.Rows[0].Cells[2], "C", optC);
                SetOptionCell(doc, table.Rows[0].Cells[3], "D", optD);

                doc.InsertTable(table);
            }
            // 2. Đáp án vừa (26 - 55 ký tự) -> Table 2 hàng x 2 cột No Border
            else if (maxLen <= 55 && !string.IsNullOrEmpty(optC))
            {
                var table = doc.AddTable(2, 2);
                table.Alignment = Alignment.center;
                table.AutoFit = AutoFit.Window;
                SetFullBorders(table, new Border(BorderStyle.Tcbs_none, BorderSize.one, 0, BorderWhite));

                SetOptionCell(doc, table.Rows[0].Cells[0], "A", optA);
                SetOptionCell(doc, table.Rows[0].Cells[1], "B", optB);
                SetOptionCell(doc, table.Rows[1].Cells[0], "C", optC);
                SetOptionCell(doc, table.Rows[1].Cells[1], "D", optD);

                doc.InsertTable(table);
            }
            // 3. Đáp án dài (> 55 ký tự) -> Mỗi đáp án 1 dòng riêng với SpacingAfter = 6pt
            else
            {
                string[] labels = { "A", "B", "C", "D" };
                string[] texts = { optA, optB, optC, optD };
                for (int i = 0; i < 4; i++)
                {
                    if (string.IsNullOrEmpty(texts[i])) continue;
                    var p = doc.InsertParagraph();
                    p.Append($"{labels[i]}. ").Font("Times New Roman").FontSize(14.0).Bold();
                    DocxContentRenderer.AppendTextWithFractions(doc, p, texts[i], "Times New Roman", 14.0);
                    p.SpacingAfter(6);
                }
            }
        }

        private static void SetOptionCell(DocX doc, Cell cell, string label, string text)
        {
            cell.VerticalAlignment = VerticalAlignment.Center;
            var p = cell.Paragraphs[0];
            p.RemoveText(0);
            p.Append($"{label}. ").Font("Times New Roman").FontSize(14.0).Bold();
            DocxContentRenderer.AppendTextWithFractions(doc, p, text, "Times New Roman", 14.0);
            p.SpacingAfter(6);
        }

        /// <summary>
        /// Kẻ lưới 4 ô ly / 5 dòng kẻ chuẩn tiểu học (Đúng 100% mẫu vở ô ly học sinh)
        /// Mỗi dòng ô ly lớn gồm 4 hàng con cao chuẩn 6.0pt (~2.1mm) khóa cứng exact, viền dọc và ngang chính nét liền, 3 viền ngang phụ bên trong nét chấm (dotted).
        /// </summary>
        public static void AppendOlyTable(DocX doc, int soDongOly, int cols = 20)
        {
            if (soDongOly <= 0) soDongOly = 2;
            int totalRows = soDongOly * 4;

            var table = doc.AddTable(totalRows, cols);
            table.Alignment = Alignment.center;
            table.AutoFit = AutoFit.Window;

            table.SetTableCellMargin(TableCellMarginType.left, 0f);
            table.SetTableCellMargin(TableCellMarginType.right, 0f);
            table.SetTableCellMargin(TableCellMarginType.top, 0f);
            table.SetTableCellMargin(TableCellMarginType.bottom, 0f);

            var emptyBorder = new Border(BorderStyle.Tcbs_none, BorderSize.one, 0, BorderWhite);
            SetFullBorders(table, emptyBorder);

            for (int r = 0; r < totalRows; r++)
            {
                var row = table.Rows[r];
                row.Height = 6.0f;

                // Khóa cứng chiều cao chính xác trong OpenXML tránh bị font chữ đẩy phình hàng
                var trPr = row.Xml.Element(w + "trPr");
                if (trPr == null)
                {
                    trPr = new System.Xml.Linq.XElement(w + "trPr");
                    row.Xml.AddFirst(trPr);
                }
                trPr.Element(w + "trHeight")?.Remove();
                trPr.Add(new System.Xml.Linq.XElement(w + "trHeight",
                    new System.Xml.Linq.XAttribute(w + "val", "120"), // 120 dxa = 6.0 pt
                    new System.Xml.Linq.XAttribute(w + "hRule", "exact")));

                bool isTopOuter = (r == 0);
                bool isTopMain = (r % 4 == 0);
                bool isBottomOuter = (r == totalRows - 1);
                bool isBottomMain = ((r + 1) % 4 == 0);

                string topVal = isTopOuter ? "single" : (isTopMain ? "single" : "dotted");
                string topSz = isTopOuter ? "8" : (isTopMain ? "6" : "4");
                string topColor = (isTopOuter || isTopMain) ? "334155" : "94A3B8";

                string botVal = isBottomOuter ? "single" : (isBottomMain ? "single" : "dotted");
                string botSz = isBottomOuter ? "8" : (isBottomMain ? "6" : "4");
                string botColor = (isBottomOuter || isBottomMain) ? "334155" : "94A3B8";

                for (int c = 0; c < cols; c++)
                {
                    var cell = row.Cells[c];
                    if (cell.Paragraphs.Count > 0)
                    {
                        var p = cell.Paragraphs[0];
                        p.RemoveText(0);
                        p.FontSize(1);
                        p.SpacingBefore(0);
                        p.SpacingAfter(0);
                    }

                    bool isLeftOuter = (c == 0);
                    bool isRightOuter = (c == cols - 1);

                    string leftVal = "single";
                    string leftSz = isLeftOuter ? "8" : "4";
                    string leftColor = isLeftOuter ? "334155" : "94A3B8";

                    string rightVal = "single";
                    string rightSz = isRightOuter ? "8" : "4";
                    string rightColor = isRightOuter ? "334155" : "94A3B8";

                    SetCellCustomBorders(cell, topVal, topSz, topColor, botVal, botSz, botColor, leftVal, leftSz, leftColor, rightVal, rightSz, rightColor);
                }
            }

            doc.InsertTable(table);
        }

        private static void SetCellCustomBorders(
            Cell cell, 
            string topVal, string topSz, string topColor, 
            string botVal, string botSz, string botColor, 
            string leftVal, string leftSz, string leftColor, 
            string rightVal, string rightSz, string rightColor)
        {
            var tcPr = cell.Xml.Element(w + "tcPr");
            if (tcPr == null)
            {
                tcPr = new System.Xml.Linq.XElement(w + "tcPr");
                cell.Xml.AddFirst(tcPr);
            }
            tcPr.Element(w + "tcBorders")?.Remove();
            var tcBorders = new System.Xml.Linq.XElement(w + "tcBorders",
                new System.Xml.Linq.XElement(w + "top", new System.Xml.Linq.XAttribute(w + "val", topVal), new System.Xml.Linq.XAttribute(w + "sz", topSz), new System.Xml.Linq.XAttribute(w + "space", "0"), new System.Xml.Linq.XAttribute(w + "color", topColor)),
                new System.Xml.Linq.XElement(w + "left", new System.Xml.Linq.XAttribute(w + "val", leftVal), new System.Xml.Linq.XAttribute(w + "sz", leftSz), new System.Xml.Linq.XAttribute(w + "space", "0"), new System.Xml.Linq.XAttribute(w + "color", leftColor)),
                new System.Xml.Linq.XElement(w + "bottom", new System.Xml.Linq.XAttribute(w + "val", botVal), new System.Xml.Linq.XAttribute(w + "sz", botSz), new System.Xml.Linq.XAttribute(w + "space", "0"), new System.Xml.Linq.XAttribute(w + "color", botColor)),
                new System.Xml.Linq.XElement(w + "right", new System.Xml.Linq.XAttribute(w + "val", rightVal), new System.Xml.Linq.XAttribute(w + "sz", rightSz), new System.Xml.Linq.XAttribute(w + "space", "0"), new System.Xml.Linq.XAttribute(w + "color", rightColor))
            );
            tcPr.Add(tcBorders);
        }

        private static void InsertGradingGuide(DocX doc, VietExamPaper paper, AppSettings settings)
        {
            var pHeader = doc.InsertParagraph();
            pHeader.Append("HƯỚNG DẪN CHẤM VÀ BIỂU ĐIỂM").Font("Times New Roman").FontSize(14).Bold().Alignment = Alignment.center;

            var pSub = doc.InsertParagraph();
            string periodUpper = "CUỐI HỌC KỲ II";
            if (!string.IsNullOrWhiteSpace(paper.TieuDe))
            {
                periodUpper = paper.TieuDe.ToUpper();
                if (periodUpper.StartsWith("BÀI KIỂM TRA ĐỊNH KỲ "))
                {
                    periodUpper = periodUpper.Substring("BÀI KIỂM TRA ĐỊNH KỲ ".Length).Trim();
                }
            }
            pSub.Append($"BÀI KIỂM TRA ĐỊNH KỲ {periodUpper}, NĂM HỌC {settings.NamHoc}\nMÔN: TIẾNG VIỆT – KHỐI {paper.KhoiLop}").Font("Times New Roman").FontSize(14).Bold().Alignment = Alignment.center;
            pSub.SpacingAfter(10);

            // A. Đọc thành tiếng
            var pA = doc.InsertParagraph();
            pA.Append("I. KIỂM TRA ĐỌC THÀNH TIẾNG (2,0 điểm)").Font("Times New Roman").FontSize(14).Bold().SpacingAfter(4);
            var pADesc = doc.InsertParagraph();
            DocxContentRenderer.AppendTextWithFractions(doc, pADesc, paper.HuongDanDocThanhTieng, "Times New Roman", 14.0, isItalic: true);
            pADesc.SpacingAfter(8);

            // B. Đọc hiểu & LTVC
            var pB = doc.InsertParagraph();
            pB.Append("II. ĐỌC HIỂU VÀ KIẾN THỨC TIẾNG VIỆT (8,0 điểm)").Font("Times New Roman").FontSize(14).Bold().SpacingAfter(6);

            var tnQuestions = paper.DanhSachCauHoi.Where(q => q.LoaiCauHoi == "TN_4LuaChon").OrderBy(q => q.SoThuTu).ToList();
            var tlQuestions = paper.DanhSachCauHoi.Where(q => q.LoaiCauHoi != "TN_4LuaChon").OrderBy(q => q.SoThuTu).ToList();

            if (tnQuestions.Count > 0)
            {
                var table = doc.AddTable(3, tnQuestions.Count + 1);
                table.Alignment = Alignment.center;
                table.AutoFit = AutoFit.Window;

                var solidBorder = new Border(BorderStyle.Tcbs_single, BorderSize.one, 0, BorderBlack);
                SetFullBorders(table, solidBorder);

                SetCell(table, 0, 0, "Câu", true, Alignment.center, 14.0);
                SetCell(table, 1, 0, "Đáp án", true, Alignment.center, 14.0);
                SetCell(table, 2, 0, "Điểm", true, Alignment.center, 14.0);

                for (int i = 0; i < tnQuestions.Count; i++)
                {
                    int col = i + 1;
                    var q = tnQuestions[i];
                    SetCell(table, 0, col, q.SoThuTu.ToString(), true, Alignment.center, 14.0);
                    SetCell(table, 1, col, !string.IsNullOrEmpty(q.DapAnDung) ? q.DapAnDung : "A", true, Alignment.center, 14.0);
                    SetCell(table, 2, col, $"{FormatPoint(q.Diem)}đ", false, Alignment.center, 14.0);
                }

                doc.InsertTable(table);
                doc.InsertParagraph().SpacingAfter(10);

                // Lưu ý giải thích đáp án trắc nghiệm (chỉ in khi huongDanCham có nội dung giải thích mới)
                var tnNotes = new List<(int SoThuTu, string Note)>();
                foreach (var q in tnQuestions)
                {
                    if (TryExtractMultipleChoiceNote(q.HuongDanCham, out string note))
                    {
                        tnNotes.Add((q.SoThuTu, note));
                    }
                }

                if (tnNotes.Count > 0)
                {
                    var pNoteHeader = doc.InsertParagraph();
                    pNoteHeader.Append("Lưu ý giải thích đáp án trắc nghiệm:").Font("Times New Roman").FontSize(14).Bold().SpacingAfter(2);

                    foreach (var (soThuTu, note) in tnNotes)
                    {
                        var pNote = doc.InsertParagraph();
                        pNote.IndentationBefore = 14;
                        pNote.Append($"Câu {soThuTu}: ").Font("Times New Roman").FontSize(14).Bold();
                        DocxContentRenderer.AppendTextWithFractions(doc, pNote, note, "Times New Roman", 14.0);
                        pNote.SpacingAfter(2);
                    }

                    doc.InsertParagraph().SpacingAfter(6);
                }
            }

            if (tlQuestions.Count > 0)
            {
                var pTL = doc.InsertParagraph();
                pTL.Append("Hướng dẫn chấm phần Tự luận:").Font("Times New Roman").FontSize(14).Bold().SpacingAfter(4);

                foreach (var q in tlQuestions)
                {
                    var pQ = doc.InsertParagraph();
                    pQ.Append($"Câu {q.SoThuTu} ({FormatPoint(q.Diem)} điểm): ").Font("Times New Roman").FontSize(14).Bold();
                    DocxContentRenderer.AppendTextWithFractions(doc, pQ, q.HuongDanCham, "Times New Roman", 14.0);
                    pQ.SpacingAfter(6);
                }
            }
        }

        /// <summary>
        /// Trích xuất phần "Lý do: ..." từ huongDanCham của câu trắc nghiệm theo định dạng mới
        /// ("Đáp án đúng: X. Lý do: ..."). Trả về false với hướng dẫn chấm cũ không kèm giải thích
        /// (ví dụ "Học sinh chọn đúng đáp án A đạt 0,5 điểm.").
        /// </summary>
        private static bool TryExtractMultipleChoiceNote(string? huongDanCham, out string note)
        {
            note = string.Empty;
            if (string.IsNullOrWhiteSpace(huongDanCham))
            {
                return false;
            }

            var match = Regex.Match(huongDanCham, @"Lý\s*do\s*:\s*(?<note>.+)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (!match.Success)
            {
                return false;
            }

            // Gộp xuống dòng (nếu có) thành khoảng trắng để in gọn trên một dòng.
            note = Regex.Replace(match.Groups["note"].Value, @"\s+", " ").Trim();
            return note.Length > 0;
        }

        #endregion

        #region OpenXML Section Orientation Enforcer

        /// <summary>
        /// Đảm bảo chuẩn xác Section 0 (Trang 1 - Bảng I) là khổ đứng (Portrait)
        /// và Section 1 (Trang 2 - Bảng II + Chữ ký) là khổ ngang (Landscape) theo đúng mẫu MA TRAN VA DAC TA.docx
        /// </summary>
        private static void EnforceMatrixAndSpecSections(string docxFilePath)
        {
            try
            {
                using var zip = ZipFile.Open(docxFilePath, ZipArchiveMode.Update);
                var entry = zip.GetEntry("word/document.xml");
                if (entry == null) return;

                XDocument xdoc;
                using (var stream = entry.Open())
                {
                    xdoc = XDocument.Load(stream);
                }

                var body = xdoc.Root?.Element(w + "body");
                if (body == null) return;

                var sectPrs = body.DescendantsAndSelf(w + "sectPr").ToList();
                if (sectPrs.Count >= 2)
                {
                    // Section 0: Khổ đứng A4 (Portrait) 11906 x 16838 twips
                    SetSectionOrientationXml(sectPrs[0], 11906, 16838, isLandscape: false);

                    // Section 1: Khổ ngang A4 (Landscape) 16838 x 11906 twips
                    SetSectionOrientationXml(sectPrs[1], 16838, 11906, isLandscape: true);
                }

                // Định dạng chuẩn xác 100% độ rộng các cột và ô theo mẫu MA TRAN VA DAC TA.docx
                var tables = body.Descendants(w + "tbl").ToList();
                foreach (var tbl in tables)
                {
                    string tblText = tbl.Value;
                    if (tblText.Contains("Mạch kiến thức") || tblText.Contains("Số câu và câu số"))
                    {
                        ApplyTable1ExactWidths(tbl);
                    }
                    else if (tblText.Contains("Nội dung đánh giá") || tblText.Contains("Yêu cầu cần đạt"))
                    {
                        ApplyTable2ExactWidths(tbl);
                    }
                }

                using (var stream = entry.Open())
                {
                    stream.SetLength(0);
                    xdoc.Save(stream);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EnforceMatrixAndSpecSections error: {ex.Message}");
            }
        }

        private static void ApplyTable1ExactWidths(XElement tbl)
        {
            // tblPr: w=5000 pct, tblCellMar=40 dxa
            SetTablePropertiesXml(tbl, "5000");

            // tblGrid: 10 cột chuẩn theo MA TRAN VA DAC TA.docx
            string[] gridCols = { "3054", "1156", "850", "659", "671", "659", "557", "618", "596", "532" };
            SetTableGridXml(tbl, gridCols);

            // tcW cho từng ô của 11 hàng
            string[] headerCols = { "3584", "1276", "1559", "1418", "1226", "1167" };
            string[] standardCols = { "3584", "1276", "850", "709", "709", "709", "567", "659", "616", "551" };

            var rows = tbl.Elements(w + "tr").ToList();
            for (int r = 0; r < rows.Count; r++)
            {
                var cells = rows[r].Elements(w + "tc").ToList();
                string[] widths = (cells.Count == 6) ? headerCols : standardCols;
                SetRowCellWidthsXml(rows[r], widths);
            }
        }

        private static void ApplyTable2ExactWidths(XElement tbl)
        {
            // tblPr: w=5100 pct, tblCellMar=40 dxa
            SetTablePropertiesXml(tbl, "5100");

            // tblGrid: 13 cột chuẩn theo MA TRAN VA DAC TA.docx
            string[] gridCols = { "706", "1276", "1276", "4538", "709", "850", "567", "851", "850", "709", "709", "709", "818" };
            SetTableGridXml(tbl, gridCols);

            string[] r0Widths = { "3258", "4538", "1559", "1418", "2268", "1527" };
            string[] r1Widths = { "706", "1276", "1276", "4538", "709", "850", "567", "851", "850", "709", "709", "1527" };
            string[] standard13 = { "706", "1276", "1276", "4538", "709", "850", "567", "851", "850", "709", "709", "709", "818" };
            string[] tvMerged12 = { "706", "2552", "4538", "709", "850", "567", "851", "850", "709", "709", "709", "818" };
            string[] rLastWidths = { "3258", "4538", "1559", "567", "851", "850", "709", "709", "1527" };

            var rows = tbl.Elements(w + "tr").ToList();
            for (int r = 0; r < rows.Count; r++)
            {
                var cells = rows[r].Elements(w + "tc").ToList();
                string[] widths;
                if (r == 0) widths = r0Widths;
                else if (r == 1) widths = r1Widths;
                else if (r == rows.Count - 1) widths = rLastWidths;
                else if (cells.Count == 12) widths = tvMerged12;
                else widths = standard13;

                SetRowCellWidthsXml(rows[r], widths);
            }
        }

        private static void SetTablePropertiesXml(XElement tbl, string widthPct)
        {
            var tblPr = tbl.Element(w + "tblPr");
            if (tblPr == null)
            {
                tblPr = new XElement(w + "tblPr");
                tbl.AddFirst(tblPr);
            }

            var tblW = tblPr.Element(w + "tblW");
            if (tblW == null)
            {
                tblW = new XElement(w + "tblW");
                tblPr.Add(tblW);
            }
            tblW.SetAttributeValue(w + "w", widthPct);
            tblW.SetAttributeValue(w + "type", "pct");

            var cellMar = tblPr.Element(w + "tblCellMar");
            if (cellMar == null)
            {
                cellMar = new XElement(w + "tblCellMar");
                tblPr.Add(cellMar);
            }
            cellMar.RemoveAll();
            cellMar.Add(new XElement(w + "top", new XAttribute(w + "w", "40"), new XAttribute(w + "type", "dxa")));
            cellMar.Add(new XElement(w + "left", new XAttribute(w + "w", "40"), new XAttribute(w + "type", "dxa")));
            cellMar.Add(new XElement(w + "bottom", new XAttribute(w + "w", "40"), new XAttribute(w + "type", "dxa")));
            cellMar.Add(new XElement(w + "right", new XAttribute(w + "w", "40"), new XAttribute(w + "type", "dxa")));
        }

        private static void SetTableGridXml(XElement tbl, string[] colWidths)
        {
            var tblGrid = tbl.Element(w + "tblGrid");
            if (tblGrid == null)
            {
                tblGrid = new XElement(w + "tblGrid");
                var tblPr = tbl.Element(w + "tblPr");
                if (tblPr != null) tblPr.AddAfterSelf(tblGrid);
                else tbl.AddFirst(tblGrid);
            }
            tblGrid.RemoveAll();
            foreach (var cw in colWidths)
            {
                tblGrid.Add(new XElement(w + "gridCol", new XAttribute(w + "w", cw)));
            }
        }

        private static void SetRowCellWidthsXml(XElement tr, string[] widths)
        {
            var cells = tr.Elements(w + "tc").ToList();
            for (int c = 0; c < cells.Count && c < widths.Length; c++)
            {
                var tc = cells[c];
                var tcPr = tc.Element(w + "tcPr");
                if (tcPr == null)
                {
                    tcPr = new XElement(w + "tcPr");
                    tc.AddFirst(tcPr);
                }
                var tcW = tcPr.Element(w + "tcW");
                if (tcW == null)
                {
                    tcW = new XElement(w + "tcW");
                    tcPr.Add(tcW);
                }
                tcW.SetAttributeValue(w + "w", widths[c]);
                tcW.SetAttributeValue(w + "type", "dxa");
            }
        }

        private static void SetSectionOrientationXml(XElement sectPr, int widthTwips, int heightTwips, bool isLandscape)
        {
            var pgSz = sectPr.Element(w + "pgSz");
            if (pgSz == null)
            {
                pgSz = new XElement(w + "pgSz");
                sectPr.AddFirst(pgSz);
            }
            pgSz.SetAttributeValue(w + "w", widthTwips.ToString());
            pgSz.SetAttributeValue(w + "h", heightTwips.ToString());
            pgSz.SetAttributeValue(w + "code", "9");
            if (isLandscape)
            {
                pgSz.SetAttributeValue(w + "orient", "landscape");
            }
            else
            {
                pgSz.Attribute(w + "orient")?.Remove();
            }

            // Margins chuẩn hành chính mẫu: Top=2cm (1134 dxa), Bottom=2cm (1134 dxa), Left=3cm (1701 dxa), Right=1.5cm (851 dxa)
            var pgMar = sectPr.Element(w + "pgMar");
            if (pgMar == null)
            {
                pgMar = new XElement(w + "pgMar");
                sectPr.Add(pgMar);
            }
            pgMar.SetAttributeValue(w + "top", "1134");
            pgMar.SetAttributeValue(w + "bottom", "1134");
            pgMar.SetAttributeValue(w + "left", "1701");
            pgMar.SetAttributeValue(w + "right", "851");
            pgMar.SetAttributeValue(w + "header", "720");
            pgMar.SetAttributeValue(w + "footer", "720");
            pgMar.SetAttributeValue(w + "gutter", "0");
        }

        #endregion
    }
}
