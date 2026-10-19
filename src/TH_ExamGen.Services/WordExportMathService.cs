using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using TH_ExamGen.Core.Config;
using TH_ExamGen.Core.Curriculum;
using TH_ExamGen.Core.MathModels;
using Xceed.Document.NET;
using Xceed.Words.NET;

namespace TH_ExamGen.Services
{
    public class WordExportMathService
    {
        private static readonly XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        private static readonly Xceed.Drawing.Color BorderBlack = Xceed.Drawing.Color.FromColorName("Black");
        private static readonly Xceed.Drawing.Color BorderGray = Xceed.Drawing.Color.FromColorName("LightGray");
        private static readonly Xceed.Drawing.Color BorderWhite = Xceed.Drawing.Color.FromColorName("White");

        /// <summary>
        /// Xuất RIÊNG File 1: Ma trận đề và Bản đặc tả chi tiết (kèm khung phê duyệt)
        /// </summary>
        public static void ExportMathMatrixOnlyToDocx(List<MathMatrixItem> matrix, AppSettings settings, int khoiLop, string outputFilePath, string period = "Cuối Học kỳ II")
        {
            CurriculumScopePolicy.ValidateMathMatrix(khoiLop, period, matrix);
            using var doc = DocX.Create(outputFilePath);
            ApplyA4AndStandardMargins(doc);

            // 1. Header cơ quan & Quốc hiệu
            string periodTitle = period.ToUpper();
            InsertDocumentHeader(doc, settings, $"MA TRẬN VÀ BẢN ĐẶC TẢ ĐỀ KIỂM TRA ĐỊNH KỲ {periodTitle}", khoiLop, "TOÁN");

            // 2. Bảng Ma trận 2 tầng tiêu đề (12 cột) tính toán động
            InsertDynamicMatrixTable(doc, matrix);

            doc.InsertParagraph().SpacingAfter(14);

            // 3. Bảng đặc tả chi tiết (5 cột)
            InsertDynamicSpecificationTable(doc, matrix);

            doc.InsertParagraph().SpacingAfter(20);

            // 4. Khung chữ ký phê duyệt 3 cột
            InsertApprovalSignatures(doc);

            EnforceSingleSpacing(doc);
            doc.Save();
        }

        /// <summary>
        /// Xuất RIÊNG File 2: Đề kiểm tra chính thức và Hướng dẫn chấm (biến động động theo ma trận)
        /// </summary>
        public static void ExportMathExamOnlyToDocx(MathExamPaper paper, AppSettings settings, string outputFilePath, bool skipValidation = false)
        {
            if (!skipValidation)
            {
                CurriculumScopePolicy.ValidateMathQuestions(paper.KhoiLop, paper.Period, paper.DanhSachCauHoi, paper.DanhSachMaTran);
                CurriculumScopePolicy.ValidateMathQuestionScope(paper.KhoiLop, paper.Period, paper.DanhSachCauHoi);
            }
            using var doc = DocX.Create(outputFilePath);
            ApplyA4AndStandardMargins(doc);

            // 1. Header Đề thi
            InsertExamHeader(doc, settings, paper);

            // 2. Khung Điểm và Nhận xét (2 ô viền đen chuẩn mẫu)
            InsertGradeBox(doc);

            // 3. Nội dung đề thi chính thức
            InsertExamBody(doc, paper);

            // Sang trang mới cho Hướng dẫn chấm
            doc.InsertSectionPageBreak();

            // 4. Hướng dẫn chấm và Biểu điểm chi tiết
            InsertGradingGuide(doc, paper, settings);

            EnforceSingleSpacing(doc);
            doc.Save();
        }

        /// <summary>
        /// Xuất gộp trọn bộ hồ sơ (Ma trận + Đặc tả + Đề + Đáp án)
        /// </summary>
        public static void ExportMathFullPackageToDocx(MathExamPaper paper, AppSettings settings, string outputFilePath, bool skipValidation = false)
        {
            if (!skipValidation)
            {
                CurriculumScopePolicy.ValidateMathQuestions(paper.KhoiLop, paper.Period, paper.DanhSachCauHoi, paper.DanhSachMaTran);
                CurriculumScopePolicy.ValidateMathQuestionScope(paper.KhoiLop, paper.Period, paper.DanhSachCauHoi);
            }
            using var doc = DocX.Create(outputFilePath);
            ApplyA4AndStandardMargins(doc);

            // Ma trận & Đặc tả
            string matrixTitle = !string.IsNullOrWhiteSpace(paper.TieuDe)
                ? $"MA TRẬN VÀ BẢN ĐẶC TẢ {paper.TieuDe.ToUpper()}"
                : "MA TRẬN VÀ BẢN ĐẶC TẢ ĐỀ KIỂM TRA ĐỊNH KỲ";
            InsertDocumentHeader(doc, settings, matrixTitle, paper.KhoiLop, "TOÁN");
            InsertDynamicMatrixTable(doc, paper.DanhSachMaTran);
            doc.InsertParagraph().SpacingAfter(14);
            InsertDynamicSpecificationTable(doc, paper.DanhSachMaTran);
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

            EnforceSingleSpacing(doc);
            doc.Save();
        }

        #region Helper Styles & Margins

        private static void ApplyA4AndStandardMargins(DocX doc)
        {
            // Thiết lập khổ A4 và căn lề theo cm quy chuẩn NĐ 30/2020/NĐ-CP:
            // Top = 2.0 cm (56.7 pt), Bottom = 2.0 cm (56.7 pt), Left = 3.0 cm (85.0 pt), Right = 1.5 cm (42.5 pt)
            doc.MarginTop = 56.7f;
            doc.MarginBottom = 56.7f;
            doc.MarginLeft = 85.0f;
            doc.MarginRight = 42.5f;
            doc.PageWidth = 595.3f;   // A4 Width: 21.0 cm
            doc.PageHeight = 841.9f;  // A4 Height: 29.7 cm

            // Đồng bộ trực tiếp vào toàn bộ các section OpenXML (w:sectPr) trong tài liệu
            var body = doc.Xml.Element(w + "body");
            if (body != null)
            {
                var allSectPrs = body.DescendantsAndSelf(w + "sectPr").ToList();
                foreach (var sectPr in allSectPrs)
                {
                    // Khổ A4 (11906 x 16838 twips)
                    var pgSz = sectPr.Element(w + "pgSz");
                    if (pgSz == null)
                    {
                        pgSz = new XElement(w + "pgSz");
                        sectPr.AddFirst(pgSz);
                    }
                    pgSz.SetAttributeValue(w + "w", "11906");
                    pgSz.SetAttributeValue(w + "h", "16838");
                    pgSz.SetAttributeValue(w + "code", "9");

                    // Căn lề T=2cm (1134 dxa), B=2cm (1134 dxa), L=3cm (1701 dxa), R=1.5cm (851 dxa)
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
                    pgMar.SetAttributeValue(w + "header", "709");
                    pgMar.SetAttributeValue(w + "footer", "709");
                    pgMar.SetAttributeValue(w + "gutter", "0");
                }
            }
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

        #endregion

        #region Bảng Ma Trận & Bản Đặc Tả (File 1)

        private static void InsertDocumentHeader(DocX doc, AppSettings settings, string title, int khoiLop, string subject)
        {
            var headerTable = doc.AddTable(1, 2);
            headerTable.Alignment = Alignment.center;
            headerTable.AutoFit = AutoFit.Window;

            var emptyBorder = new Border(BorderStyle.Tcbs_none, BorderSize.one, 0, BorderWhite);
            SetFullBorders(headerTable, emptyBorder);

            var cellLeft = headerTable.Rows[0].Cells[0];
            var pLeft1 = cellLeft.Paragraphs[0];
            pLeft1.Append(settings.TenUBND.ToUpper()).Font("Times New Roman").FontSize(13).Bold().Alignment = Alignment.center;
            SetParagraphSpacing(pLeft1, 2);
            var pLeft2 = cellLeft.InsertParagraph();
            pLeft2.Append(settings.TenTruong.ToUpper()).Font("Times New Roman").FontSize(13).Bold().Alignment = Alignment.center;
            SetParagraphSpacing(pLeft2, 4);

            var cellRight = headerTable.Rows[0].Cells[1];
            var pRight1 = cellRight.Paragraphs[0];
            pRight1.Append("CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM").Font("Times New Roman").FontSize(13).Bold().Alignment = Alignment.center;
            SetParagraphSpacing(pRight1, 2);
            var pRight2 = cellRight.InsertParagraph();
            pRight2.Append("Độc lập - Tự do - Hạnh phúc").Font("Times New Roman").FontSize(13).Bold().UnderlineStyle(UnderlineStyle.singleLine).Alignment = Alignment.center;
            SetParagraphSpacing(pRight2, 4);

            doc.InsertTable(headerTable);
            doc.InsertParagraph().SpacingAfter(10);

            var pTitle = doc.InsertParagraph();
            pTitle.Append(title.ToUpper()).Font("Times New Roman").FontSize(14).Bold().Alignment = Alignment.center;
            SetParagraphSpacing(pTitle, 3);
            var pSubTitle = doc.InsertParagraph();
            pSubTitle.Append($"MÔN {subject.ToUpper()} – KHỐI {khoiLop} - NĂM HỌC {settings.NamHoc}").Font("Times New Roman").FontSize(14).Bold().Alignment = Alignment.center;
            SetParagraphSpacing(pSubTitle, 14);
        }

        /// <summary>
        /// Dựng bảng Ma trận 2 tầng tiêu đề (12 cột) tính toán động 100% dựa trên danh sách câu hỏi
        /// </summary>
        private static void InsertDynamicMatrixTable(DocX doc, List<MathMatrixItem> matrix)
        {
            var pIntro = doc.InsertParagraph();
            pIntro.Append("I. MA TRẬN ĐỀ KIỂM TRA:").Font("Times New Roman").FontSize(14).Bold().SpacingAfter(6);

            if (matrix == null || matrix.Count == 0) return;

            // Nhóm theo chủ đề
            var groupedTopics = matrix.GroupBy(x => x.ChuDe).ToList();
            int rowCount = 2 + (groupedTopics.Count * 3) + 3; // 2 header rows + 3 rows per topic + 3 summary rows

            var table = doc.AddTable(rowCount, 12);
            table.Alignment = Alignment.center;
            table.AutoFit = AutoFit.Window;

            var solidBorder = new Border(BorderStyle.Tcbs_single, BorderSize.one, 0, BorderBlack);
            SetFullBorders(table, solidBorder);

            table.SetTableCellMargin(TableCellMarginType.left, 1.5f);
            table.SetTableCellMargin(TableCellMarginType.right, 1.5f);
            table.SetTableCellMargin(TableCellMarginType.top, 1.5f);
            table.SetTableCellMargin(TableCellMarginType.bottom, 1.5f);

            // Dòng Header 0
            SetCell(table, 0, 0, "TT", true);
            SetCell(table, 0, 1, "Chủ đề", true);
            SetCell(table, 0, 3, "Mức 1", true);
            SetCell(table, 0, 5, "Mức 2", true);
            SetCell(table, 0, 7, "Mức 3", true);
            SetCell(table, 0, 9, "Tổng", true);
            SetCell(table, 0, 11, "Tỉ lệ", true);

            // Dòng Header 1
            SetCell(table, 1, 3, "TNKQ", true);
            SetCell(table, 1, 4, "TL", true);
            SetCell(table, 1, 5, "TNKQ", true);
            SetCell(table, 1, 6, "TL", true);
            SetCell(table, 1, 7, "TNKQ", true);
            SetCell(table, 1, 8, "TL", true);
            SetCell(table, 1, 9, "TNKQ", true);
            SetCell(table, 1, 10, "TL", true);

            double totalExamPoints = matrix.Sum(x => x.Diem);
            if (totalExamPoints <= 0) totalExamPoints = 10.0;

            int currentRow = 2;
            List<int> topicStartRows = new List<int>();
            for (int tIdx = 0; tIdx < groupedTopics.Count; tIdx++)
            {
                var group = groupedTopics[tIdx];
                string topicName = group.Key;
                var items = group.ToList();

                // 3 dòng con cho mỗi chủ đề: Số câu, Câu số, Số điểm
                int rSoCau = currentRow;
                int rCauSo = currentRow + 1;
                int rSoDiem = currentRow + 2;
                topicStartRows.Add(rSoCau);

                SetCell(table, rSoCau, 0, (tIdx + 1).ToString(), true);
                SetCell(table, rSoCau, 1, topicName, true);

                SetCell(table, rSoCau, 2, "Số câu", false);
                SetCell(table, rCauSo, 2, "Câu số", false);
                SetCell(table, rSoDiem, 2, "Số điểm", false);

                // Mức 1 (Col 3: TNKQ, Col 4: TL)
                var m1Tn = items.Where(x => x.MucDo == "M1" && x.HinhThuc == "TN").ToList();
                var m1Tl = items.Where(x => x.MucDo == "M1" && x.HinhThuc == "TL").ToList();
                PopulateSubRowCells(table, rSoCau, rCauSo, rSoDiem, 3, m1Tn);
                PopulateSubRowCells(table, rSoCau, rCauSo, rSoDiem, 4, m1Tl);

                // Mức 2 (Col 5: TNKQ, Col 6: TL)
                var m2Tn = items.Where(x => x.MucDo == "M2" && x.HinhThuc == "TN").ToList();
                var m2Tl = items.Where(x => x.MucDo == "M2" && x.HinhThuc == "TL").ToList();
                PopulateSubRowCells(table, rSoCau, rCauSo, rSoDiem, 5, m2Tn);
                PopulateSubRowCells(table, rSoCau, rCauSo, rSoDiem, 6, m2Tl);

                // Mức 3 (Col 7: TNKQ, Col 8: TL)
                var m3Tn = items.Where(x => x.MucDo == "M3" && x.HinhThuc == "TN").ToList();
                var m3Tl = items.Where(x => x.MucDo == "M3" && x.HinhThuc == "TL").ToList();
                PopulateSubRowCells(table, rSoCau, rCauSo, rSoDiem, 7, m3Tn);
                PopulateSubRowCells(table, rSoCau, rCauSo, rSoDiem, 8, m3Tl);

                // Tổng chủ đề (Col 9: TNKQ, Col 10: TL)
                int totalTopicTn = m1Tn.Count + m2Tn.Count + m3Tn.Count;
                int totalTopicTl = m1Tl.Count + m2Tl.Count + m3Tl.Count;
                double topicPoints = items.Sum(x => x.Diem);
                double topicRatio = (topicPoints / totalExamPoints) * 100.0;

                SetCell(table, rSoCau, 9, totalTopicTn > 0 ? totalTopicTn.ToString() : "", false);
                SetCell(table, rSoCau, 10, totalTopicTl > 0 ? totalTopicTl.ToString() : "", false);
                SetCell(table, rSoCau, 11, $"{topicRatio:0.##}%".Replace(".", ","), true);

                SetCell(table, rSoDiem, 9, totalTopicTn > 0 ? FormatPoint(m1Tn.Sum(x => x.Diem) + m2Tn.Sum(x => x.Diem) + m3Tn.Sum(x => x.Diem)) : "", false);
                SetCell(table, rSoDiem, 10, totalTopicTl > 0 ? FormatPoint(m1Tl.Sum(x => x.Diem) + m2Tl.Sum(x => x.Diem) + m3Tl.Sum(x => x.Diem)) : "", false);

                currentRow += 3;
            }

            // Dòng Tổng số câu
            int rTotalCau = currentRow;
            SetCell(table, rTotalCau, 0, "Tổng số câu", true);
            var allM1Tn = matrix.Where(x => x.MucDo == "M1" && x.HinhThuc == "TN").ToList();
            var allM1Tl = matrix.Where(x => x.MucDo == "M1" && x.HinhThuc == "TL").ToList();
            var allM2Tn = matrix.Where(x => x.MucDo == "M2" && x.HinhThuc == "TN").ToList();
            var allM2Tl = matrix.Where(x => x.MucDo == "M2" && x.HinhThuc == "TL").ToList();
            var allM3Tn = matrix.Where(x => x.MucDo == "M3" && x.HinhThuc == "TN").ToList();
            var allM3Tl = matrix.Where(x => x.MucDo == "M3" && x.HinhThuc == "TL").ToList();

            SetCell(table, rTotalCau, 3, allM1Tn.Count.ToString(), true);
            SetCell(table, rTotalCau, 4, allM1Tl.Count.ToString(), true);
            SetCell(table, rTotalCau, 5, allM2Tn.Count.ToString(), true);
            SetCell(table, rTotalCau, 6, allM2Tl.Count.ToString(), true);
            SetCell(table, rTotalCau, 7, allM3Tn.Count.ToString(), true);
            SetCell(table, rTotalCau, 8, allM3Tl.Count.ToString(), true);
            SetCell(table, rTotalCau, 9, (allM1Tn.Count + allM2Tn.Count + allM3Tn.Count).ToString(), true);
            SetCell(table, rTotalCau, 10, (allM1Tl.Count + allM2Tl.Count + allM3Tl.Count).ToString(), true);
            SetCell(table, rTotalCau, 11, "100%", true);

            // Dòng Số điểm
            int rTotalDiem = currentRow + 1;
            SetCell(table, rTotalDiem, 0, "Số điểm", true);
            double pM1 = matrix.Where(x => x.MucDo == "M1").Sum(x => x.Diem);
            double pM2 = matrix.Where(x => x.MucDo == "M2").Sum(x => x.Diem);
            double pM3 = matrix.Where(x => x.MucDo == "M3").Sum(x => x.Diem);
            SetCell(table, rTotalDiem, 3, FormatPoint(pM1), true);
            SetCell(table, rTotalDiem, 5, FormatPoint(pM2), true);
            SetCell(table, rTotalDiem, 7, FormatPoint(pM3), true);
            SetCell(table, rTotalDiem, 9, FormatPoint(totalExamPoints), true);

            // Dòng Tỉ lệ %
            int rTotalTiLe = currentRow + 2;
            SetCell(table, rTotalTiLe, 0, "Tỉ lệ", true);
            SetCell(table, rTotalTiLe, 3, $"{pM1 / totalExamPoints * 100:0.##}%".Replace(".", ","), true);
            SetCell(table, rTotalTiLe, 5, $"{pM2 / totalExamPoints * 100:0.##}%".Replace(".", ","), true);
            SetCell(table, rTotalTiLe, 7, $"{pM3 / totalExamPoints * 100:0.##}%".Replace(".", ","), true);
            SetCell(table, rTotalTiLe, 9, "100%", true);

            // 1. VERTICAL MERGES FIRST (khi các hàng đều đầy đủ 12 cột)
            // Header vertical merges: TT (col 0), Chủ đề (cols 1 & 2), Tỉ lệ (col 11)
            table.MergeCellsInColumn(0, 0, 1);
            table.MergeCellsInColumn(1, 0, 1);
            table.MergeCellsInColumn(2, 0, 1);
            table.MergeCellsInColumn(11, 0, 1);

            // Topic vertical merges (Cols 0, 1, 11)
            foreach (int r in topicStartRows)
            {
                table.MergeCellsInColumn(0, r, r + 2);
                table.MergeCellsInColumn(1, r, r + 2);
                table.MergeCellsInColumn(11, r, r + 2);
            }

            // Summary vertical merge cho Tỉ lệ 100% (Col 11, Rows rTotalCau to rTotalTiLe)
            table.MergeCellsInColumn(11, rTotalCau, rTotalTiLe);

            // 2. HORIZONTAL MERGES (Theo từng hàng, từ phải sang trái)
            // Row 0
            table.Rows[0].MergeCells(9, 10); // Tổng
            table.Rows[0].MergeCells(7, 8);  // Mức 3
            table.Rows[0].MergeCells(5, 6);  // Mức 2
            table.Rows[0].MergeCells(3, 4);  // Mức 1
            table.Rows[0].MergeCells(1, 2);  // Chủ đề

            // Row 1
            table.Rows[1].MergeCells(1, 2);  // Chủ đề continue

            // Row rTotalCau
            table.Rows[rTotalCau].MergeCells(0, 2); // "Tổng số câu" gộp cols 0..2

            // Row rTotalDiem
            table.Rows[rTotalDiem].MergeCells(9, 10); // Tổng điểm gộp cols 9..10
            table.Rows[rTotalDiem].MergeCells(7, 8);  // Mức 3 gộp cols 7..8
            table.Rows[rTotalDiem].MergeCells(5, 6);  // Mức 2 gộp cols 5..6
            table.Rows[rTotalDiem].MergeCells(3, 4);  // Mức 1 gộp cols 3..4
            table.Rows[rTotalDiem].MergeCells(0, 2);  // "Số điểm" gộp cols 0..2

            // Row rTotalTiLe
            table.Rows[rTotalTiLe].MergeCells(9, 10); // Tổng % gộp cols 9..10
            table.Rows[rTotalTiLe].MergeCells(7, 8);  // Mức 3 % gộp cols 7..8
            table.Rows[rTotalTiLe].MergeCells(5, 6);  // Mức 2 % gộp cols 5..6
            table.Rows[rTotalTiLe].MergeCells(3, 4);  // Mức 1 % gộp cols 3..4
            table.Rows[rTotalTiLe].MergeCells(0, 2);  // "Tỉ lệ" gộp cols 0..2

            doc.InsertTable(table);
        }

        public static string FormatPoint(double point)
        {
            if (point <= 0) return "0";
            return point % 1 == 0 ? $"{point:0}" : $"{point:0.##}".Replace(".", ",");
        }

        private static void PopulateSubRowCells(Table table, int rSoCau, int rCauSo, int rSoDiem, int colIdx, List<MathMatrixItem> items)
        {
            if (items.Count > 0)
            {
                SetCell(table, rSoCau, colIdx, items.Count.ToString(), false);
                SetCell(table, rCauSo, colIdx, string.Join(", ", items.Select(x => x.SoThuTu)), false);
                SetCell(table, rSoDiem, colIdx, FormatPoint(items.Sum(x => x.Diem)), false);
            }
        }

        private static void SetCell(Table table, int row, int col, string text, bool bold, Alignment align = Alignment.center, double fontSize = 14.0)
        {
            if (row >= table.RowCount || col >= table.ColumnCount) return;
            var cell = table.Rows[row].Cells[col];
            cell.VerticalAlignment = VerticalAlignment.Center;
            var p = cell.Paragraphs[0];
            p.RemoveText(0);
            var run = p.Append(text).Font("Times New Roman").FontSize(fontSize);
            if (bold) run.Bold();
            p.Alignment = align;
        }

        private static void SetCellWithMath(DocX doc, Table table, int row, int col, string text, bool bold, Alignment align = Alignment.center, double fontSize = 14.0)
        {
            if (row >= table.RowCount || col >= table.ColumnCount) return;
            var cell = table.Rows[row].Cells[col];
            cell.VerticalAlignment = VerticalAlignment.Center;
            var p = cell.Paragraphs[0];
            p.RemoveText(0);
            p.Alignment = align;
            DocxContentRenderer.AppendTextWithFractions(doc, p, text, "Times New Roman", fontSize, isBold: bold);
            SetParagraphSpacing(p, 1, 1);
        }

        private static void SetParagraphSpacing(Paragraph p, double afterPt = 6.0, double beforePt = 0.0)
        {
            p.SpacingAfter(afterPt);
            p.SpacingBefore(beforePt);
            var pPr = p.Xml.Element(w + "pPr");
            if (pPr == null)
            {
                pPr = new XElement(w + "pPr");
                p.Xml.AddFirst(pPr);
            }
            var spacing = pPr.Element(w + "spacing");
            if (spacing == null)
            {
                spacing = new XElement(w + "spacing");
                pPr.Add(spacing);
            }
            spacing.SetAttributeValue(w + "line", "240");
            spacing.SetAttributeValue(w + "lineRule", "auto");
            spacing.SetAttributeValue(w + "before", ((int)(beforePt * 20)).ToString());
            spacing.SetAttributeValue(w + "after", ((int)(afterPt * 20)).ToString());
        }

        private static void EnforceSingleSpacing(DocX doc)
        {
            foreach (var p in doc.Paragraphs)
            {
                var pPr = p.Xml.Element(w + "pPr");
                if (pPr == null)
                {
                    pPr = new XElement(w + "pPr");
                    p.Xml.AddFirst(pPr);
                }
                var spacing = pPr.Element(w + "spacing");
                if (spacing == null)
                {
                    spacing = new XElement(w + "spacing",
                        new XAttribute(w + "before", "0"),
                        new XAttribute(w + "after", "120"),
                        new XAttribute(w + "line", "240"),
                        new XAttribute(w + "lineRule", "auto"));
                    pPr.Add(spacing);
                }
                else
                {
                    if (spacing.Attribute(w + "line") == null)
                    {
                        spacing.SetAttributeValue(w + "line", "240");
                        spacing.SetAttributeValue(w + "lineRule", "auto");
                    }
                    if (spacing.Attribute(w + "after") == null)
                    {
                        spacing.SetAttributeValue(w + "after", "120"); // 6pt
                    }
                }
            }

            // Đảm bảo toàn bộ tài liệu sau khi tạo xong luôn có đúng khổ A4 và lề T=2, B=2, L=3, R=1.5 cm
            ApplyA4AndStandardMargins(doc);
        }

        /// <summary>
        /// Bảng đặc tả yêu cầu cần đạt (5 cột)
        /// </summary>
        private static void InsertDynamicSpecificationTable(DocX doc, List<MathMatrixItem> matrix)
        {
            var pIntro = doc.InsertParagraph();
            pIntro.Append("II. BẢN ĐẶC TẢ YÊU CẦU CẦN ĐẠT THEO MA TRẬN:").Font("Times New Roman").FontSize(14).Bold().SpacingAfter(6);

            if (matrix == null || matrix.Count == 0) return;

            int[] colWidths = { 550, 2200, 5200, 650, 750 };
            var table = doc.AddTable(matrix.Count + 1, 5);
            table.Alignment = Alignment.center;

            var solidBorder = new Border(BorderStyle.Tcbs_single, BorderSize.one, 0, BorderBlack);
            SetFullBorders(table, solidBorder);

            string[] headers = { "Câu", "Nội dung", "Yêu cầu cần đạt", "Mức", "Hình thức" };
            for (int c = 0; c < headers.Length; c++)
            {
                SetCell(table, 0, c, headers[c], true, Alignment.center, 14.0);
            }

            for (int r = 0; r < matrix.Count; r++)
            {
                var item = matrix[r];
                int rowIdx = r + 1;
                SetCell(table, rowIdx, 0, item.SoThuTu.ToString(), true, Alignment.center, 14.0);
                SetCell(table, rowIdx, 1, item.NoiDungKienThuc, false, Alignment.left, 14.0);
                SetCell(table, rowIdx, 2, item.YeuCauCanDat, false, Alignment.left, 14.0);
                SetCell(table, rowIdx, 3, item.MucDo, true, Alignment.center, 14.0);
                SetCell(table, rowIdx, 4, item.HinhThuc, false, Alignment.center, 14.0);
            }

            // Thiết lập OpenXML tblLayout fixed & gridCol để cố định độ rộng từng cột
            XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
            var tblPr = table.Xml.Element(w + "tblPr");
            if (tblPr != null)
            {
                tblPr.Element(w + "tblLayout")?.Remove();
                tblPr.Add(new XElement(w + "tblLayout", new XAttribute(w + "type", "fixed")));

                tblPr.Element(w + "tblW")?.Remove();
                tblPr.Add(new XElement(w + "tblW", new XAttribute(w + "w", "9350"), new XAttribute(w + "type", "dxa")));
            }

            var existingGrid = table.Xml.Element(w + "tblGrid");
            var tblGrid = new XElement(w + "tblGrid",
                colWidths.Select(cw => new XElement(w + "gridCol", new XAttribute(w + "w", cw.ToString())))
            );
            if (existingGrid != null)
            {
                existingGrid.ReplaceWith(tblGrid);
            }
            else if (tblPr != null)
            {
                tblPr.AddAfterSelf(tblGrid);
            }
            else
            {
                table.Xml.AddFirst(tblGrid);
            }

            foreach (var row in table.Rows)
            {
                for (int c = 0; c < 5; c++)
                {
                    var tcPr = row.Cells[c].Xml.Element(w + "tcPr");
                    if (tcPr == null)
                    {
                        tcPr = new XElement(w + "tcPr");
                        row.Cells[c].Xml.AddFirst(tcPr);
                    }
                    tcPr.Element(w + "tcW")?.Remove();
                    tcPr.Add(new XElement(w + "tcW", new XAttribute(w + "w", colWidths[c].ToString()), new XAttribute(w + "type", "dxa")));
                }
            }

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

            sigTable.Rows[1].Height = 60f; // Khoảng trống ký tên

            doc.InsertTable(sigTable);
        }

        #endregion

        #region Đề Kiểm Tra & Hướng Dẫn Chấm (File 2)

        private static void InsertExamHeader(DocX doc, AppSettings settings, MathExamPaper paper)
        {
            var headerTable = doc.AddTable(1, 2);
            headerTable.Alignment = Alignment.center;
            headerTable.AutoFit = AutoFit.Window;

            var emptyBorder = new Border(BorderStyle.Tcbs_none, BorderSize.one, 0, BorderWhite);
            SetFullBorders(headerTable, emptyBorder);

            var cellLeft = headerTable.Rows[0].Cells[0];
            var pLeft1 = cellLeft.Paragraphs[0];
            pLeft1.Append(settings.TenUBND.ToUpper()).Font("Times New Roman").FontSize(13).Bold().Alignment = Alignment.center;
            SetParagraphSpacing(pLeft1, 2);
            var pLeft2 = cellLeft.InsertParagraph();
            pLeft2.Append(settings.TenTruong.ToUpper()).Font("Times New Roman").FontSize(13).Bold().Alignment = Alignment.center;
            SetParagraphSpacing(pLeft2, 4);

            var cellRight = headerTable.Rows[0].Cells[1];
            var pRight1 = cellRight.Paragraphs[0];
            pRight1.Append("CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM").Font("Times New Roman").FontSize(13).Bold().Alignment = Alignment.center;
            SetParagraphSpacing(pRight1, 2);
            var pRight2 = cellRight.InsertParagraph();
            pRight2.Append("Độc lập - Tự do - Hạnh phúc").Font("Times New Roman").FontSize(13).Bold().UnderlineStyle(UnderlineStyle.singleLine).Alignment = Alignment.center;
            SetParagraphSpacing(pRight2, 4);

            doc.InsertTable(headerTable);
            doc.InsertParagraph().SpacingAfter(6);

            var pTitle = doc.InsertParagraph();
            pTitle.Append(paper.TieuDe.ToUpper()).Font("Times New Roman").FontSize(14).Bold().Alignment = Alignment.center;
            SetParagraphSpacing(pTitle, 2);

            var pNamHoc = doc.InsertParagraph();
            pNamHoc.Append($"NĂM HỌC {settings.NamHoc}").Font("Times New Roman").FontSize(14).Bold().Alignment = Alignment.center;
            SetParagraphSpacing(pNamHoc, 4);

            var pSub = doc.InsertParagraph();
            pSub.Append($"MÔN TOÁN – KHỐI {paper.KhoiLop}").Font("Times New Roman").FontSize(14).Bold().Alignment = Alignment.center;
            SetParagraphSpacing(pSub, 2);

            var pTime = doc.InsertParagraph();
            pTime.Append($"(Thời gian làm bài {paper.ThoiGianPhut} phút – Không kể thời gian giao đề)").Font("Times New Roman").FontSize(14).Italic().Alignment = Alignment.center;
            SetParagraphSpacing(pTime, 8);

            var pName = doc.InsertParagraph();
            pName.Append("Họ và tên:……………………………………………………… Lớp: ………").Font("Times New Roman").FontSize(14).SpacingAfter(8);
            SetParagraphSpacing(pName, 8);
        }

        private static void InsertGradeBox(DocX doc)
        {
            var boxTable = doc.AddTable(1, 2);
            boxTable.Alignment = Alignment.center;
            boxTable.AutoFit = AutoFit.Window;

            var solidBorder = new Border(BorderStyle.Tcbs_single, BorderSize.one, 0, BorderBlack);
            SetFullBorders(boxTable, solidBorder);

            // Ô Điểm
            var cellDiem = boxTable.Rows[0].Cells[0];
            cellDiem.Width = 140;
            var pDiem = cellDiem.Paragraphs[0];
            pDiem.Append("Điểm").Font("Times New Roman").FontSize(14).Bold().Alignment = Alignment.center;

            // Ô Nhận xét của giáo viên
            var cellNx = boxTable.Rows[0].Cells[1];
            var pNx = cellNx.Paragraphs[0];
            pNx.Append("Nhận xét của giáo viên").Font("Times New Roman").FontSize(14).Bold().Alignment = Alignment.center;

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

        public static bool IsQuestionEssay(MathQuestion q)
        {
            if (q == null) return false;
            if (!string.IsNullOrEmpty(q.LoaiCauHoi) && (q.LoaiCauHoi.StartsWith("TL") || q.LoaiCauHoi == "TL_TinhToan")) return true;
            if (q.SoThuTu >= 8) return true;
            // Nếu không có 4 phương án A, B, C, D và không phải dạng Đúng/Sai hoặc Trả lời ngắn,
            // nhưng có các bước giải hoặc nội dung bắt đầu bằng "Đặt tính", "Tính:"
            if (string.IsNullOrEmpty(q.DapAnA) && q.LoaiCauHoi != "TN_DungSai" && q.LoaiCauHoi != "TN_TraLoiNgan" &&
                ((q.NoiDung ?? "").Trim().StartsWith("Đặt tính", StringComparison.OrdinalIgnoreCase) ||
                 (q.NoiDung ?? "").Trim().StartsWith("Tính:", StringComparison.OrdinalIgnoreCase) ||
                 (q.CacBuocGiai != null && q.CacBuocGiai.Count > 0)))
            {
                return true;
            }
            return false;
        }

        private static void InsertExamBody(DocX doc, MathExamPaper paper)
        {
            var tnQuestions = paper.DanhSachCauHoi.Where(q => !IsQuestionEssay(q)).OrderBy(q => q.SoThuTu).ToList();
            var tlQuestions = paper.DanhSachCauHoi.Where(q => IsQuestionEssay(q)).OrderBy(q => q.SoThuTu).ToList();

            double tnTotalPoints = tnQuestions.Sum(q => q.Diem);
            double tlTotalPoints = tlQuestions.Sum(q => q.Diem);

            // PHẦN I. TRẮC NGHIỆM
            var pTN = doc.InsertParagraph();
            pTN.Append($"I. PHẦN TRẮC NGHIỆM ({FormatPoint(tnTotalPoints)} điểm)").Font("Times New Roman").FontSize(14).Bold().SpacingBefore(6).SpacingAfter(3);
            var pSubTN = doc.InsertParagraph();
            pSubTN.Append("Khoanh vào chữ cái đặt trước đáp án đúng nhất và thực hiện các bài tập sau:").Font("Times New Roman").FontSize(14).Italic().SpacingAfter(6);

            foreach (var q in tnQuestions)
            {
                // 1. Kiểm tra nếu câu hỏi có chứa Bảng Markdown (như Thống kê hoặc dạng bảng biểu bất kỳ)
                if (TryRenderMarkdownTableQuestion(doc, q))
                {
                    continue;
                }

                // 2. Dạng câu hỏi Đúng/Sai (Câu 6)
                if (q.LoaiCauHoi == "TN_DungSai")
                {
                    InsertTrueFalseQuestion(doc, q);
                    continue;
                }

                // 3. Dạng câu hỏi Thống kê không có bảng Markdown nhưng có ảnh TikZ hoặc trả lời ngắn
                if (q.LoaiCauHoi == "TN_TraLoiNgan" || q.ChuDe.Contains("Thống kê"))
                {
                    var pQStat = doc.InsertParagraph();
                    pQStat.Append($"Câu {q.SoThuTu}. ").Font("Times New Roman").FontSize(14).Bold();
                    DocxContentRenderer.AppendTextWithFractions(doc, pQStat, q.NoiDung, "Times New Roman", 14.0);
                    pQStat.SpacingAfter(4);

                    if (q.CoHinhTikz && !string.IsNullOrEmpty(q.AnhTikzPath) && File.Exists(q.AnhTikzPath))
                    {
                        try
                        {
                            var img = doc.AddImage(q.AnhTikzPath);
                            var pic = img.CreatePicture();
                            float targetWidth = 260;
                            if (pic.Width > 0 && pic.Height > 0)
                            {
                                float ratio = (float)pic.Height / pic.Width;
                                pic.Width = targetWidth;
                                pic.Height = targetWidth * ratio;
                            }
                            var pImg = doc.InsertParagraph();
                            pImg.AppendPicture(pic).Alignment = Alignment.center;
                            pImg.SpacingAfter(4);
                        }
                        catch { }
                    }

                    // Nếu là câu hỏi có phương án A, B, C, D thì in bảng đáp án
                    if (!string.IsNullOrEmpty(q.DapAnA))
                    {
                        InsertMultipleChoiceOptionsTable(doc, q);
                    }
                    else if (q.LoaiCauHoi == "TN_TraLoiNgan")
                    {
                        var pAns = doc.InsertParagraph();
                        pAns.Append("Trả lời: .........................................................................................................................................").Font("Times New Roman").FontSize(14);
                        pAns.SpacingAfter(4);
                    }

                    doc.InsertParagraph().SpacingAfter(6);
                    continue;
                }

                // 4. Trắc nghiệm tiêu chuẩn: Đề bài + Hình TikZ (nếu có) + Bảng 4 đáp án trên 1 dòng (No Border)
                var pQ = doc.InsertParagraph();
                pQ.Append($"Câu {q.SoThuTu}. ").Font("Times New Roman").FontSize(14).Bold();
                DocxContentRenderer.AppendTextWithFractions(doc, pQ, q.NoiDung, "Times New Roman", 14.0);
                pQ.SpacingAfter(4);

                // TikZ Image (Câu 1, Câu 4, hoặc hình học)
                if (q.CoHinhTikz && !string.IsNullOrEmpty(q.AnhTikzPath) && File.Exists(q.AnhTikzPath))
                {
                    try
                    {
                        var img = doc.AddImage(q.AnhTikzPath);
                        var pic = img.CreatePicture();
                        float targetWidth = 230;
                        if (pic.Width > 0 && pic.Height > 0)
                        {
                            float ratio = (float)pic.Height / pic.Width;
                            pic.Width = targetWidth;
                            pic.Height = targetWidth * ratio;
                        }
                        var pImg = doc.InsertParagraph();
                        pImg.AppendPicture(pic).Alignment = Alignment.center;
                        pImg.SpacingAfter(4);
                    }
                    catch { }
                }

                // Trắc nghiệm: Bảng No Border căn thẳng hàng tuyệt đối
                if (!string.IsNullOrEmpty(q.DapAnA))
                {
                    InsertMultipleChoiceOptionsTable(doc, q);
                }
            }

            // PHẦN II. TỰ LUẬN
            var pTL = doc.InsertParagraph();
            pTL.Append($"II. PHẦN TỰ LUẬN ({FormatPoint(tlTotalPoints)} điểm)").Font("Times New Roman").FontSize(14).Bold().SpacingBefore(10).SpacingAfter(6);

            foreach (var q in tlQuestions)
            {
                // Câu 8 luôn là tính toán (Đặt tính rồi tính) hoặc câu có biểu thức con
                if (q.SoThuTu == 8 || IsCalculationQuestion(q))
                {
                    InsertCalculationQuestion(doc, q);
                }
                // Các câu tự luận giải toán có lời văn -> Tiêu đề "Bài giải" (In đậm, Gạch chân, Căn giữa) + Lưới ô ly 4 ly chuẩn
                else
                {
                    var pQ = doc.InsertParagraph();
                    pQ.Append($"Câu {q.SoThuTu}. ").Font("Times New Roman").FontSize(14).Bold();
                    pQ.Append($"({FormatPoint(q.Diem)} điểm) ").Font("Times New Roman").FontSize(14);
                    DocxContentRenderer.AppendTextWithFractions(doc, pQ, q.NoiDung, "Times New Roman", 14.0);
                    pQ.SpacingAfter(4);

                    // TikZ Sơ đồ đoạn thẳng tóm tắt bài toán nếu có
                    if (q.CoHinhTikz && !string.IsNullOrEmpty(q.AnhTikzPath) && File.Exists(q.AnhTikzPath))
                    {
                        try
                        {
                            var img = doc.AddImage(q.AnhTikzPath);
                            var pic = img.CreatePicture();
                            float targetWidth = 280;
                            if (pic.Width > 0 && pic.Height > 0)
                            {
                                float ratio = (float)pic.Height / pic.Width;
                                pic.Width = targetWidth;
                                pic.Height = targetWidth * ratio;
                            }
                            var pImg = doc.InsertParagraph();
                            pImg.AppendPicture(pic).Alignment = Alignment.center;
                            pImg.SpacingAfter(4);
                        }
                        catch { }
                    }

                    // Tiêu đề Bài giải in đậm, gạch chân, căn giữa và Lưới ô ly (chỉ chèn khi q.SoDongOly > 0)
                    if (q.SoDongOly > 0)
                    {
                        var pGiai = doc.InsertParagraph();
                        pGiai.Append("Bài giải")
                             .Font("Times New Roman")
                             .FontSize(14)
                             .Bold()
                             .UnderlineStyle(UnderlineStyle.singleLine)
                             .Alignment = Alignment.center;
                        pGiai.SpacingAfter(4);

                        AppendOly4LyTable(doc, q.SoDongOly, 18);
                    }
                }

                doc.InsertParagraph().SpacingAfter(6);
            }
        }

        public static bool IsCalculationQuestion(MathQuestion q)
        {
            if (q == null) return false;
            string content = (q.NoiDung ?? string.Empty).Trim();
            // Nếu là bài toán có lời văn (có câu hỏi, ngữ cảnh) -> không phải bài đặt tính
            if (content.Contains("Hỏi ") || content.Contains("hỏi ") ||
                content.Contains("bao nhiêu") || content.Contains("biết rằng") ||
                content.Contains("cửa hàng") || content.Contains("quãng đường") ||
                content.Contains("vận động viên") || content.Contains("thư viện") ||
                content.Contains("bể nước") || content.Contains("kho lương thực") ||
                content.Contains("kho gạo") || content.Contains("xe tải") ||
                content.Contains("vòng quanh"))
            {
                return false;
            }
            if (q.LoaiCauHoi == "TL_LoiVan") return false;

            // Bắt đầu bằng "Đặt tính", "Tính:"
            if (content.StartsWith("Đặt tính", StringComparison.OrdinalIgnoreCase) ||
                content.StartsWith("Tính:", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Có các mục phép tính a) ... b) ...
            if ((content.Contains("a)") || content.Contains("a.")) &&
                (content.Contains("b)") || content.Contains("b.")))
            {
                return true;
            }

            return false;
        }

        private static void InsertCalculationQuestion(DocX doc, MathQuestion q)
        {
            string leadTitle = "Đặt tính rồi tính:";
            string content = (q.NoiDung ?? string.Empty).Trim();

            if (content.StartsWith("Đặt tính rồi tính:", StringComparison.OrdinalIgnoreCase))
            {
                leadTitle = "Đặt tính rồi tính:";
                content = content.Substring("Đặt tính rồi tính:".Length).Trim();
            }
            else if (content.StartsWith("Đặt tính rồi tính", StringComparison.OrdinalIgnoreCase))
            {
                leadTitle = "Đặt tính rồi tính:";
                content = content.Substring("Đặt tính rồi tính".Length).Trim().TrimStart(':', ' ');
            }
            else if (content.StartsWith("Tính:", StringComparison.OrdinalIgnoreCase))
            {
                leadTitle = "Tính:";
                content = content.Substring("Tính:".Length).Trim();
            }
            else if (content.StartsWith("Tính", StringComparison.OrdinalIgnoreCase) && !content.StartsWith("Tính toán", StringComparison.OrdinalIgnoreCase))
            {
                leadTitle = "Tính:";
                content = content.Substring("Tính".Length).Trim().TrimStart(':', ' ');
            }

            var pQ = doc.InsertParagraph();
            pQ.Append($"Câu {q.SoThuTu}. ").Font("Times New Roman").FontSize(14).Bold();
            pQ.Append($"({FormatPoint(q.Diem)} điểm) {leadTitle}").Font("Times New Roman").FontSize(14);
            pQ.SpacingAfter(4);

            var rawLines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                  .Select(s => s.Trim())
                                  .Where(s => !string.IsNullOrEmpty(s))
                                  .ToList();

            if (rawLines.Count <= 1 && (content.Contains("b)") || content.Contains("b.")))
            {
                var parts = System.Text.RegularExpressions.Regex.Split(content, @"(?=[a-d]\)|\b[a-d]\.)");
                rawLines = parts.Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
            }

            List<string> items = new List<string>();
            string[] labels = { "a)", "b)", "c)", "d)" };

            for (int i = 0; i < rawLines.Count && i < 4; i++)
            {
                string cleaned = CleanMathExpression(rawLines[i]);
                if (!string.IsNullOrEmpty(cleaned))
                {
                    if (!System.Text.RegularExpressions.Regex.IsMatch(cleaned, @"^[a-d][\)\.]", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                    {
                        cleaned = $"{labels[i]} {cleaned}";
                    }
                    else
                    {
                        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"^([a-d])[\.]", "$1)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        if (!cleaned.Contains(") "))
                        {
                            cleaned = cleaned.Replace(")", ") ");
                        }
                    }
                    items.Add(cleaned);
                }
            }

            if (items.Count == 0)
            {
                // Fallback nếu không bóc tách được phép tính nào (đầy đủ 4 phép tính: +, -, ×, :)
                items.Add("a) 43 256 + 25 138");
                items.Add("b) 86 524 - 34 219");
                items.Add("c) 14 218 × 4");
                items.Add("d) 48 576 : 3");
            }

            if (items.Count >= 3)
            {
                // Dàn 4 phép tính thành Bảng 2 hàng x 2 cột No Border
                var calcTable = doc.AddTable(2, 2);
                calcTable.Alignment = Alignment.center;
                calcTable.AutoFit = AutoFit.Window;

                var noBorder = new Border(BorderStyle.Tcbs_none, BorderSize.one, 0, BorderWhite);
                SetFullBorders(calcTable, noBorder);

                // Row 0: a) và b)
                var pA = calcTable.Rows[0].Cells[0].Paragraphs[0];
                pA.RemoveText(0);
                DocxContentRenderer.AppendTextWithFractions(doc, pA, items[0], "Times New Roman", 14.0);
                SetParagraphSpacing(pA, 2, 2);

                if (items.Count > 1)
                {
                    var pB = calcTable.Rows[0].Cells[1].Paragraphs[0];
                    pB.RemoveText(0);
                    DocxContentRenderer.AppendTextWithFractions(doc, pB, items[1], "Times New Roman", 14.0);
                    SetParagraphSpacing(pB, 2, 2);
                }

                // Row 1: c) và d)
                var pC = calcTable.Rows[1].Cells[0].Paragraphs[0];
                pC.RemoveText(0);
                DocxContentRenderer.AppendTextWithFractions(doc, pC, items[2], "Times New Roman", 14.0);
                SetParagraphSpacing(pC, 2, 2);

                if (items.Count > 3)
                {
                    var pD = calcTable.Rows[1].Cells[1].Paragraphs[0];
                    pD.RemoveText(0);
                    DocxContentRenderer.AppendTextWithFractions(doc, pD, items[3], "Times New Roman", 14.0);
                    SetParagraphSpacing(pD, 2, 2);
                }

                doc.InsertTable(calcTable);
            }
            else if (items.Count == 2)
            {
                // Dàn 2 phép tính thành Bảng 1 hàng x 2 cột No Border
                var calcTable = doc.AddTable(1, 2);
                calcTable.Alignment = Alignment.center;
                calcTable.AutoFit = AutoFit.Window;

                var noBorder = new Border(BorderStyle.Tcbs_none, BorderSize.one, 0, BorderWhite);
                SetFullBorders(calcTable, noBorder);

                var pA = calcTable.Rows[0].Cells[0].Paragraphs[0];
                pA.RemoveText(0);
                DocxContentRenderer.AppendTextWithFractions(doc, pA, items[0], "Times New Roman", 14.0);
                SetParagraphSpacing(pA, 2, 2);

                var pB = calcTable.Rows[0].Cells[1].Paragraphs[0];
                pB.RemoveText(0);
                DocxContentRenderer.AppendTextWithFractions(doc, pB, items[1], "Times New Roman", 14.0);
                SetParagraphSpacing(pB, 2, 2);

                doc.InsertTable(calcTable);
            }
            else
            {
                var pItem = doc.InsertParagraph();
                DocxContentRenderer.AppendTextWithFractions(doc, pItem, items[0], "Times New Roman", 14.0);
                SetParagraphSpacing(pItem, 2, 2);
            }

            // Chèn khung lưới ô ly 4 ly chuẩn nếu câu có yêu cầu (q.SoDongOly > 0)
            if (q.SoDongOly > 0)
            {
                var pSpacer = doc.InsertParagraph();
                SetParagraphSpacing(pSpacer, 4, 0);
                AppendOly4LyTable(doc, q.SoDongOly, 18);
            }
        }

        private static string CleanMathExpression(string item)
        {
            if (string.IsNullOrWhiteSpace(item)) return "";
            string t = item.Trim();
            t = System.Text.RegularExpressions.Regex.Replace(t, @"\.{3,}", "").Trim();
            t = t.TrimEnd(';', '=', ' ', '.');
            return t.Trim();
        }

        /// <summary>
        /// Tự động phát hiện và bóc tách bảng Markdown (như bảng Thống kê) từ trường NoiDung thành Table Word chuẩn viền đen
        /// </summary>
        private static bool TryRenderMarkdownTableQuestion(DocX doc, MathQuestion q)
        {
            string content = q.NoiDung ?? string.Empty;
            if (!content.Contains("|")) return false;

            var rawLines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
                                  .Select(s => s.Trim())
                                  .ToList();

            var tableLines = rawLines.Where(l => l.Contains("|")).ToList();
            if (tableLines.Count < 2) return false;

            int firstTableIdx = -1;
            int lastTableIdx = -1;
            for (int i = 0; i < rawLines.Count; i++)
            {
                if (rawLines[i].Contains("|"))
                {
                    if (firstTableIdx == -1) firstTableIdx = i;
                    lastTableIdx = i;
                }
            }

            if (firstTableIdx == -1 || lastTableIdx <= firstTableIdx) return false;

            // 1. In phần lời dẫn trước bảng
            var leadLines = rawLines.Take(firstTableIdx).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
            string leadText = leadLines.Count > 0 ? string.Join("\n", leadLines) : "Bảng số liệu sau đây:";

            var pQ = doc.InsertParagraph();
            pQ.Append($"Câu {q.SoThuTu}. ").Font("Times New Roman").FontSize(14).Bold();
            DocxContentRenderer.AppendTextWithFractions(doc, pQ, leadText, "Times New Roman", 14.0);
            pQ.SpacingAfter(6);

            // 2. Chèn ảnh TikZ nếu có
            if (q.CoHinhTikz && !string.IsNullOrEmpty(q.AnhTikzPath) && File.Exists(q.AnhTikzPath))
            {
                try
                {
                    var img = doc.AddImage(q.AnhTikzPath);
                    var pic = img.CreatePicture();
                    float targetWidth = 260;
                    if (pic.Width > 0 && pic.Height > 0)
                    {
                        float ratio = (float)pic.Height / pic.Width;
                        pic.Width = targetWidth;
                        pic.Height = targetWidth * ratio;
                    }
                    var pImg = doc.InsertParagraph();
                    pImg.AppendPicture(pic).Alignment = Alignment.center;
                    pImg.SpacingAfter(6);
                }
                catch { }
            }

            // 3. Phân tích và tạo Bảng Word
            var validTableLines = new List<List<string>>();
            for (int i = firstTableIdx; i <= lastTableIdx; i++)
            {
                string line = rawLines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;
                string stripped = line.Replace("|", "").Replace("-", "").Replace(":", "").Trim();
                if (string.IsNullOrEmpty(stripped)) continue; // Dòng phân cách |---|---|

                var cells = line.Split('|')
                                .Select(c => c.Trim())
                                .ToList();
                if (cells.Count > 0 && string.IsNullOrEmpty(cells[0])) cells.RemoveAt(0);
                if (cells.Count > 0 && string.IsNullOrEmpty(cells[cells.Count - 1])) cells.RemoveAt(cells.Count - 1);

                if (cells.Count > 0)
                {
                    validTableLines.Add(cells);
                }
            }

            if (validTableLines.Count > 0)
            {
                int rowCount = validTableLines.Count;
                int colCount = validTableLines.Max(r => r.Count);

                var table = doc.AddTable(rowCount, colCount);
                table.Alignment = Alignment.center;
                table.AutoFit = AutoFit.Window;

                var solidBorder = new Border(BorderStyle.Tcbs_single, BorderSize.one, 0, BorderBlack);
                SetFullBorders(table, solidBorder);

                for (int r = 0; r < rowCount; r++)
                {
                    var rowData = validTableLines[r];
                    SetTableRowHeight(table.Rows[r], 400);

                    for (int c = 0; c < colCount; c++)
                    {
                        string cellText = c < rowData.Count ? rowData[c] : "";
                        bool isHeader = (r == 0 || c == 0);
                        var cell = table.Rows[r].Cells[c];
                        cell.VerticalAlignment = VerticalAlignment.Center;
                        var p = cell.Paragraphs[0];
                        p.RemoveText(0);
                        p.Alignment = Alignment.center;
                        DocxContentRenderer.AppendTextWithFractions(doc, p, cellText, "Times New Roman", 14.0, isBold: isHeader);
                        SetParagraphSpacing(p, 2, 2);
                    }
                }

                doc.InsertTable(table);
                doc.InsertParagraph().SpacingAfter(6);
            }

            // 4. In các câu hỏi con sau bảng
            var subLines = rawLines.Skip(lastTableIdx + 1).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
            if (subLines.Count > 0)
            {
                foreach (var line in subLines)
                {
                    string subText = line.Trim();
                    if (!subText.Contains("..."))
                    {
                        subText += " .........................................................................................................................................";
                    }
                    var pSub = doc.InsertParagraph();
                    DocxContentRenderer.AppendTextWithFractions(doc, pSub, subText, "Times New Roman", 14.0);
                    pSub.SpacingAfter(4);
                }
            }
            else
            {
                var pSub = doc.InsertParagraph();
                pSub.Append("Trả lời: .........................................................................................................................................").Font("Times New Roman").FontSize(14);
                pSub.SpacingAfter(4);
            }

            return true;
        }

        private static void InsertTrueFalseQuestion(DocX doc, MathQuestion q)
        {
            string content = q.NoiDung ?? string.Empty;
            string prompt = "Đúng ghi Đ, sai ghi S vào ô vuông:";

            List<string> rawItems = new List<string>();
            var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                               .Select(s => s.Trim())
                               .Where(s => !string.IsNullOrEmpty(s))
                               .ToList();

            if (lines.Count > 0)
            {
                if (!lines[0].StartsWith("a)", StringComparison.OrdinalIgnoreCase) && 
                    !lines[0].StartsWith("a.", StringComparison.OrdinalIgnoreCase))
                {
                    prompt = lines[0].TrimEnd(':', ' ', '.') + ":";
                    for (int i = 1; i < lines.Count; i++) rawItems.Add(lines[i]);
                }
                else
                {
                    rawItems.AddRange(lines);
                }
            }

            if (rawItems.Count == 0 && (content.Contains("a)") || content.Contains("a.")))
            {
                var matches = System.Text.RegularExpressions.Regex.Split(content, @"(?=[a-d]\)|\b[a-d]\.)");
                foreach (var m in matches)
                {
                    string t = m.Trim();
                    if (t.StartsWith("a)", StringComparison.OrdinalIgnoreCase) ||
                        t.StartsWith("b)", StringComparison.OrdinalIgnoreCase) ||
                        t.StartsWith("c)", StringComparison.OrdinalIgnoreCase) ||
                        t.StartsWith("d)", StringComparison.OrdinalIgnoreCase) ||
                        t.StartsWith("a.", StringComparison.OrdinalIgnoreCase) ||
                        t.StartsWith("b.", StringComparison.OrdinalIgnoreCase) ||
                        t.StartsWith("c.", StringComparison.OrdinalIgnoreCase) ||
                        t.StartsWith("d.", StringComparison.OrdinalIgnoreCase))
                    {
                        rawItems.Add(t);
                    }
                }
            }

            if (rawItems.Count == 0)
            {
                string a = !string.IsNullOrEmpty(q.DapAnA) ? q.DapAnA : "3 tấn 5 tạ = 3500 kg";
                string b = !string.IsNullOrEmpty(q.DapAnB) ? q.DapAnB : "4 m² 25 dm² = 425 dm²";
                rawItems.Add("a) " + CleanTrueFalseOption(a));
                rawItems.Add("b) " + CleanTrueFalseOption(b));
            }
            else
            {
                for (int i = 0; i < rawItems.Count; i++)
                {
                    rawItems[i] = CleanTrueFalseOption(rawItems[i]);
                }
            }

            var pQ = doc.InsertParagraph();
            pQ.Append($"Câu {q.SoThuTu}. ").Font("Times New Roman").FontSize(14).Bold();
            DocxContentRenderer.AppendTextWithFractions(doc, pQ, prompt, "Times New Roman", 14.0);
            pQ.SpacingAfter(4);

            if (q.CoHinhTikz && !string.IsNullOrEmpty(q.AnhTikzPath) && File.Exists(q.AnhTikzPath))
            {
                try
                {
                    var img = doc.AddImage(q.AnhTikzPath);
                    var pic = img.CreatePicture();
                    float targetWidth = 230;
                    if (pic.Width > 0 && pic.Height > 0)
                    {
                        float ratio = (float)pic.Height / pic.Width;
                        pic.Width = targetWidth;
                        pic.Height = targetWidth * ratio;
                    }
                    var pImg = doc.InsertParagraph();
                    pImg.AppendPicture(pic).Alignment = Alignment.center;
                    pImg.SpacingAfter(4);
                }
                catch { }
            }

            // Dựng bảng ô vuông điền Đ/S:
            // 1. Nếu có đúng 2 mục và ngắn (<= 35 ký tự): Dàn 1 hàng x 4 cột (Mục A | Ô vuông | Mục B | Ô vuông)
            if (rawItems.Count == 2 && rawItems.All(it => it.Length <= 35))
            {
                var table = doc.AddTable(1, 4);
                table.Alignment = Alignment.center;
                table.AutoFit = AutoFit.Window;
                SetFullBorders(table, new Border(BorderStyle.Tcbs_none, BorderSize.one, 0, BorderWhite));

                SetTableRowHeight(table.Rows[0], 567);

                SetTrueFalseTextCell(doc, table.Rows[0].Cells[0], rawItems[0]);
                FormatSquareCheckboxCell(table.Rows[0].Cells[1]);

                SetTrueFalseTextCell(doc, table.Rows[0].Cells[2], rawItems[1]);
                FormatSquareCheckboxCell(table.Rows[0].Cells[3]);

                doc.InsertTable(table);
            }
            // 2. Nếu có 4 mục ngắn (<= 35 ký tự): Dàn 2 hàng x 4 cột
            else if (rawItems.Count == 4 && rawItems.All(it => it.Length <= 35))
            {
                var table = doc.AddTable(2, 4);
                table.Alignment = Alignment.center;
                table.AutoFit = AutoFit.Window;
                SetFullBorders(table, new Border(BorderStyle.Tcbs_none, BorderSize.one, 0, BorderWhite));

                for (int r = 0; r < 2; r++)
                {
                    SetTableRowHeight(table.Rows[r], 567);
                    int idx1 = r * 2;
                    int idx2 = r * 2 + 1;

                    SetTrueFalseTextCell(doc, table.Rows[r].Cells[0], rawItems[idx1]);
                    FormatSquareCheckboxCell(table.Rows[r].Cells[1]);

                    SetTrueFalseTextCell(doc, table.Rows[r].Cells[2], rawItems[idx2]);
                    FormatSquareCheckboxCell(table.Rows[r].Cells[3]);
                }

                doc.InsertTable(table);
            }
            // 3. Các trường hợp còn lại: Mỗi mục 1 hàng gồm [Phát biểu] | [Ô vuông]
            else
            {
                var table = doc.AddTable(rawItems.Count, 2);
                table.Alignment = Alignment.center;
                table.AutoFit = AutoFit.Window;
                SetFullBorders(table, new Border(BorderStyle.Tcbs_none, BorderSize.one, 0, BorderWhite));

                for (int r = 0; r < rawItems.Count; r++)
                {
                    SetTableRowHeight(table.Rows[r], 567);
                    SetTrueFalseTextCell(doc, table.Rows[r].Cells[0], rawItems[r]);
                    FormatSquareCheckboxCell(table.Rows[r].Cells[1]);
                }

                doc.InsertTable(table);
            }

            doc.InsertParagraph().SpacingAfter(6);
        }

        private static void SetTrueFalseTextCell(DocX doc, Cell cell, string itemText)
        {
            cell.VerticalAlignment = VerticalAlignment.Center;
            var p = cell.Paragraphs[0];
            p.RemoveText(0);
            p.Alignment = Alignment.left;
            DocxContentRenderer.AppendTextWithFractions(doc, p, itemText, "Times New Roman", 14.0);
            SetParagraphSpacing(p, 2, 2);
        }

        private static void FormatSquareCheckboxCell(Cell cell)
        {
            var tcPr = cell.Xml.Element(w + "tcPr");
            if (tcPr == null)
            {
                tcPr = new XElement(w + "tcPr");
                cell.Xml.AddFirst(tcPr);
            }
            // Ô vuông 1.0 cm x 1.0 cm = 567 dxa
            tcPr.Element(w + "tcW")?.Remove();
            tcPr.Add(new XElement(w + "tcW", new XAttribute(w + "w", "567"), new XAttribute(w + "type", "dxa")));

            // Kẻ viền đen 4 cạnh
            tcPr.Element(w + "tcBorders")?.Remove();
            tcPr.Add(new XElement(w + "tcBorders",
                new XElement(w + "top", new XAttribute(w + "val", "single"), new XAttribute(w + "sz", "6"), new XAttribute(w + "space", "0"), new XAttribute(w + "color", "000000")),
                new XElement(w + "left", new XAttribute(w + "val", "single"), new XAttribute(w + "sz", "6"), new XAttribute(w + "space", "0"), new XAttribute(w + "color", "000000")),
                new XElement(w + "bottom", new XAttribute(w + "val", "single"), new XAttribute(w + "sz", "6"), new XAttribute(w + "space", "0"), new XAttribute(w + "color", "000000")),
                new XElement(w + "right", new XAttribute(w + "val", "single"), new XAttribute(w + "sz", "6"), new XAttribute(w + "space", "0"), new XAttribute(w + "color", "000000"))
            ));

            cell.VerticalAlignment = VerticalAlignment.Center;
            var p = cell.Paragraphs[0];
            p.RemoveText(0);
            p.Alignment = Alignment.center;
            SetParagraphSpacing(p, 0, 0);
        }

        private static void SetTableRowHeight(Row row, int heightDxa = 567)
        {
            var trPr = row.Xml.Element(w + "trPr");
            if (trPr == null)
            {
                trPr = new XElement(w + "trPr");
                row.Xml.AddFirst(trPr);
            }
            trPr.Element(w + "trHeight")?.Remove();
            trPr.Add(new XElement(w + "trHeight", new XAttribute(w + "val", heightDxa.ToString()), new XAttribute(w + "hRule", "atLeast")));
        }

        private static string CleanTrueFalseOption(string text)
        {
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\[\s*\]", "").Trim();
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\.{3,}", "").Trim();
            return text.TrimEnd(';', ' ', '\r', '\n');
        }

        private static void InsertMultipleChoiceOptionsTable(DocX doc, MathQuestion q)
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
            // 3. Đáp án dài (> 55 ký tự) -> Mỗi đáp án 1 dòng riêng
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
                    p.SpacingAfter(4);
                }
            }
        }

        private static void SetOptionCell(DocX doc, Cell cell, string label, string text)
        {
            var p = cell.Paragraphs[0];
            p.RemoveText(0);
            p.Append($"{label}. ").Font("Times New Roman").FontSize(14.0).Bold();
            DocxContentRenderer.AppendTextWithFractions(doc, p, text, "Times New Roman", 14.0);
        }

        public static void AppendOly4LyTable(DocX doc, int soDongLon, int soCot = 18)
        {
            if (soDongLon <= 0) return;
            int totalRows = soDongLon * 4;

            var table = doc.AddTable(totalRows, soCot);
            table.Alignment = Alignment.center;

            XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

            var tblPr = table.Xml.Element(w + "tblPr");
            if (tblPr != null)
            {
                tblPr.Element(w + "tblBorders")?.Remove();
                var tblBorders = new XElement(w + "tblBorders",
                    new XElement(w + "top", new XAttribute(w + "val", "single"), new XAttribute(w + "sz", "4"), new XAttribute(w + "space", "0"), new XAttribute(w + "color", "auto")),
                    new XElement(w + "left", new XAttribute(w + "val", "single"), new XAttribute(w + "sz", "4"), new XAttribute(w + "space", "0"), new XAttribute(w + "color", "auto")),
                    new XElement(w + "bottom", new XAttribute(w + "val", "single"), new XAttribute(w + "sz", "4"), new XAttribute(w + "space", "0"), new XAttribute(w + "color", "auto")),
                    new XElement(w + "right", new XAttribute(w + "val", "single"), new XAttribute(w + "sz", "4"), new XAttribute(w + "space", "0"), new XAttribute(w + "color", "auto")),
                    new XElement(w + "insideH", new XAttribute(w + "val", "dotted"), new XAttribute(w + "sz", "3"), new XAttribute(w + "space", "0"), new XAttribute(w + "color", "auto")),
                    new XElement(w + "insideV", new XAttribute(w + "val", "single"), new XAttribute(w + "sz", "4"), new XAttribute(w + "space", "0"), new XAttribute(w + "color", "auto"))
                );
                tblPr.Add(tblBorders);

                tblPr.Element(w + "tblCellMar")?.Remove();
                tblPr.Add(new XElement(w + "tblCellMar",
                    new XElement(w + "left", new XAttribute(w + "w", "0"), new XAttribute(w + "type", "dxa")),
                    new XElement(w + "right", new XAttribute(w + "w", "0"), new XAttribute(w + "type", "dxa"))
                ));
            }

            var trElements = table.Xml.Elements(w + "tr").ToList();
            for (int r = 0; r < trElements.Count; r++)
            {
                var tr = trElements[r];
                int sub = r % 4;

                var trPr = tr.Element(w + "trPr");
                if (trPr == null)
                {
                    trPr = new XElement(w + "trPr");
                    tr.AddFirst(trPr);
                }
                trPr.Element(w + "trHeight")?.Remove();
                trPr.Add(new XElement(w + "trHeight", new XAttribute(w + "val", "150"), new XAttribute(w + "hRule", "exact")));

                bool isTopSingle = (sub == 0);
                bool isBottomSingle = (sub == 3) || (r == trElements.Count - 1);

                foreach (var tc in tr.Elements(w + "tc"))
                {
                    var tcPr = tc.Element(w + "tcPr");
                    if (tcPr == null)
                    {
                        tcPr = new XElement(w + "tcPr");
                        tc.AddFirst(tcPr);
                    }
                    tcPr.Element(w + "tcW")?.Remove();
                    tcPr.Add(new XElement(w + "tcW", new XAttribute(w + "w", "514"), new XAttribute(w + "type", "dxa")));

                    tcPr.Element(w + "tcBorders")?.Remove();
                    var tcBorders = new XElement(w + "tcBorders",
                        new XElement(w + "top", 
                            new XAttribute(w + "val", isTopSingle ? "single" : "dotted"), 
                            new XAttribute(w + "sz", isTopSingle ? "4" : "3"), 
                            new XAttribute(w + "space", "0"), 
                            new XAttribute(w + "color", "auto")),
                        new XElement(w + "left", 
                            new XAttribute(w + "val", "single"), 
                            new XAttribute(w + "sz", "4"), 
                            new XAttribute(w + "space", "0"), 
                            new XAttribute(w + "color", "auto")),
                        new XElement(w + "bottom", 
                            new XAttribute(w + "val", isBottomSingle ? "single" : "dotted"), 
                            new XAttribute(w + "sz", isBottomSingle ? "4" : "3"), 
                            new XAttribute(w + "space", "0"), 
                            new XAttribute(w + "color", "auto")),
                        new XElement(w + "right", 
                            new XAttribute(w + "val", "single"), 
                            new XAttribute(w + "sz", "4"), 
                            new XAttribute(w + "space", "0"), 
                            new XAttribute(w + "color", "auto"))
                    );
                    tcPr.Add(tcBorders);

                    foreach (var p in tc.Elements(w + "p"))
                    {
                        var pPr = p.Element(w + "pPr");
                        if (pPr == null)
                        {
                            pPr = new XElement(w + "pPr");
                            p.AddFirst(pPr);
                        }
                        pPr.Element(w + "spacing")?.Remove();
                        pPr.Add(new XElement(w + "spacing", new XAttribute(w + "before", "0"), new XAttribute(w + "after", "0"), new XAttribute(w + "line", "60"), new XAttribute(w + "lineRule", "exact")));
                        pPr.Element(w + "rPr")?.Remove();
                        pPr.Add(new XElement(w + "rPr", new XElement(w + "sz", new XAttribute(w + "val", "8")), new XElement(w + "szCs", new XAttribute(w + "val", "8"))));
                    }
                }
            }

            doc.InsertTable(table);
        }

        public static void AppendOlyTable(DocX doc, int rows, int cols)
        {
            AppendOly4LyTable(doc, Math.Max(1, rows / 4), cols);
        }

        private static List<MathStepSolution> AutoExtractCalculationSteps(string answerText, double totalDiem)
        {
            var list = new List<MathStepSolution>();
            if (string.IsNullOrWhiteSpace(answerText)) return list;

            var rawLines = answerText.Split(new[] { "\r\n", "\r", "\n", ";" }, StringSplitOptions.RemoveEmptyEntries)
                                     .Select(s => s.Trim())
                                     .Where(s => !string.IsNullOrEmpty(s))
                                     .ToList();

            var matched = rawLines.Where(l => System.Text.RegularExpressions.Regex.IsMatch(l, @"^[a-d]\)", System.Text.RegularExpressions.RegexOptions.IgnoreCase)).ToList();
            if (matched.Count >= 2)
            {
                double eachPoint = Math.Round(totalDiem / matched.Count, 2);
                for (int i = 0; i < matched.Count; i++)
                {
                    list.Add(new MathStepSolution
                    {
                        Buoc = i + 1,
                        NoiDung = matched[i],
                        Diem = eachPoint
                    });
                }
            }
            return list;
        }

        private enum MathStepLineKind
        {
            Normal,
            DoiDonVi,
            LoiGiai,
            PhepTinh,
            DapSo
        }

        private class MathFormattedLine
        {
            public string Text { get; set; } = string.Empty;
            public MathStepLineKind Kind { get; set; } = MathStepLineKind.Normal;
        }

        private static List<MathFormattedLine> NormalizeStepLines(string rawContent, bool isCalcQ)
        {
            var result = new List<MathFormattedLine>();
            if (string.IsNullOrWhiteSpace(rawContent)) return result;

            // Xóa bỏ các ký tự LaTeX formatting rác như \, hoặc \  trong số tiền, số tự nhiên (ví dụ 18\,000 -> 18 000)
            string cleaned = rawContent.Replace(@"\,", " ").Replace(@"\ ", " ");

            var rawLines = cleaned.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var rawLine in rawLines)
            {
                string line = rawLine.Trim();
                if (string.IsNullOrEmpty(line)) continue;

                if (isCalcQ)
                {
                    result.Add(new MathFormattedLine { Text = line, Kind = MathStepLineKind.Normal });
                    continue;
                }

                // 1. Tách phần "Đáp số:" nếu bị dính liền ở cuối dòng (ví dụ: "... = 900 000 (đồng). Đáp số: 900 000 đồng")
                string? pendingDapSo = null;
                int dapSoIdx = -1;
                string[] dapSoMarkers = new[] { "Đáp số:", "Đáp số :", "Đ/s:", "Đ/s :" };
                foreach (var marker in dapSoMarkers)
                {
                    int idx = line.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                    if (idx > 0)
                    {
                        dapSoIdx = idx;
                        break;
                    }
                }

                if (dapSoIdx > 0)
                {
                    pendingDapSo = line.Substring(dapSoIdx).Trim();
                    line = line.Substring(0, dapSoIdx).Trim();
                    if (line.EndsWith("."))
                    {
                        line = line.Substring(0, line.Length - 1).Trim();
                    }
                }

                // 2. Kiểm tra nếu dòng bắt đầu bằng "Đổi:" hoặc "Đổi đơn vị:"
                if (line.StartsWith("Đổi:", StringComparison.OrdinalIgnoreCase) || 
                    line.StartsWith("Đổi ", StringComparison.OrdinalIgnoreCase) ||
                    System.Text.RegularExpressions.Regex.IsMatch(line, @"^[a-d]\)\s*Đổi:", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                {
                    result.Add(new MathFormattedLine { Text = line, Kind = MathStepLineKind.DoiDonVi });
                }
                // 3. Kiểm tra nếu dòng là "Đáp số:" đứng đầu dòng
                else if (line.StartsWith("Đáp số:", StringComparison.OrdinalIgnoreCase) || 
                         line.StartsWith("Đáp số :", StringComparison.OrdinalIgnoreCase) ||
                         line.StartsWith("Đ/s:", StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(new MathFormattedLine { Text = line, Kind = MathStepLineKind.DapSo });
                }
                // 4. Kiểm tra trường hợp dính liền Lời giải và Phép tính trên cùng 1 dòng
                // Ví dụ: "Số lít dầu cửa hàng có là: 12,5 \times 4 = 50 (l)."
                else
                {
                    int colonIdx = line.IndexOf(':');
                    if (colonIdx > 0 && colonIdx < line.Length - 1)
                    {
                        string beforeColon = line.Substring(0, colonIdx + 1).Trim();
                        string afterColon = line.Substring(colonIdx + 1).Trim();

                        // Nếu vế sau chứa biểu thức phép tính (=) và vế trước là câu lời giải (>3 ký tự)
                        if (afterColon.Contains("=") && beforeColon.Length > 3)
                        {
                            result.Add(new MathFormattedLine { Text = beforeColon, Kind = MathStepLineKind.LoiGiai });
                            result.Add(new MathFormattedLine { Text = afterColon, Kind = MathStepLineKind.PhepTinh });
                        }
                        else
                        {
                            result.Add(new MathFormattedLine { Text = line, Kind = MathStepLineKind.Normal });
                        }
                    }
                    else if (line.Contains("=") && (line.Contains("(") || line.Contains("+") || line.Contains("-") || line.Contains(@"\times") || line.Contains(@"\div") || line.Contains("*") || line.Contains("/")))
                    {
                        result.Add(new MathFormattedLine { Text = line, Kind = MathStepLineKind.PhepTinh });
                    }
                    else
                    {
                        result.Add(new MathFormattedLine { Text = line, Kind = MathStepLineKind.Normal });
                    }
                }

                if (!string.IsNullOrEmpty(pendingDapSo))
                {
                    result.Add(new MathFormattedLine { Text = pendingDapSo, Kind = MathStepLineKind.DapSo });
                }
            }

            return result;
        }

        private static void InsertGradingGuide(DocX doc, MathExamPaper paper, AppSettings settings)
        {
            var pHeader = doc.InsertParagraph();
            pHeader.Append("HƯỚNG DẪN CHẤM VÀ BIỂU ĐIỂM").Font("Times New Roman").FontSize(14).Bold().Alignment = Alignment.center;
            SetParagraphSpacing(pHeader, 3);

            string examTitle = !string.IsNullOrWhiteSpace(paper.TieuDe) ? paper.TieuDe.ToUpper() : "ĐỀ KIỂM TRA ĐỊNH KỲ";
            var pSub1 = doc.InsertParagraph();
            pSub1.Append($"{examTitle} - NĂM HỌC {settings.NamHoc}").Font("Times New Roman").FontSize(14).Bold().Alignment = Alignment.center;
            SetParagraphSpacing(pSub1, 2);

            var pSub2 = doc.InsertParagraph();
            pSub2.Append($"MÔN: TOÁN – KHỐI {paper.KhoiLop}").Font("Times New Roman").FontSize(14).Bold().Alignment = Alignment.center;
            SetParagraphSpacing(pSub2, 6);

            var tnQuestions = paper.DanhSachCauHoi.Where(q => !IsQuestionEssay(q)).OrderBy(q => q.SoThuTu).ToList();
            var tlQuestions = paper.DanhSachCauHoi.Where(q => IsQuestionEssay(q)).OrderBy(q => q.SoThuTu).ToList();

            double tnTotalPoints = tnQuestions.Sum(q => q.Diem);
            double tlTotalPoints = tlQuestions.Sum(q => q.Diem);

            // 1. Phần Trắc nghiệm: Bảng 3 hàng chuẩn mẫu
            var pTN = doc.InsertParagraph();
            pTN.Append($"I. PHẦN TRẮC NGHIỆM ({FormatPoint(tnTotalPoints)} điểm)").Font("Times New Roman").FontSize(14).Bold();
            SetParagraphSpacing(pTN, 6);

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
                    string ansText = !string.IsNullOrEmpty(q.DapAnDung) ? q.DapAnDung : (q.LoaiCauHoi == "TN_DungSai" ? "a) Đ, b) S" : "A");
                    double ansFontSize = ansText.Length > 6 ? 11.0 : 14.0;
                    SetCellWithMath(doc, table, 1, col, ansText, true, Alignment.center, ansFontSize);
                    SetCell(table, 2, col, $"{FormatPoint(q.Diem)}đ", false, Alignment.center, 14.0);
                }

                doc.InsertTable(table);

                // Ghi chú barem điểm chi tiết cho các câu trắc nghiệm đặc thù (Đúng/Sai, Bảng số liệu thống kê)
                var dsQuestion = tnQuestions.FirstOrDefault(q => q.LoaiCauHoi == "TN_DungSai");
                if (dsQuestion != null)
                {
                    string ans = dsQuestion.DapAnDung ?? "";
                    int itemCount = ans.Split(';').Length;
                    if (itemCount < 2 && ans.Contains(",")) itemCount = ans.Split(',').Length;
                    if (itemCount < 2) itemCount = 2; // Mặc định 2 ý (a, b)
                    double diemMoiY = Math.Round(dsQuestion.Diem / itemCount, 2);
                    string strDiem = (diemMoiY == 0.25 || itemCount >= 4) ? "0,25" : FormatPoint(diemMoiY);

                    var pNoteDS = doc.InsertParagraph();
                    pNoteDS.Append($"* Hướng dẫn chấm Câu {dsQuestion.SoThuTu} (Đúng/Sai): Mỗi ý điền đúng đạt {strDiem} điểm. Điền sai hoặc không điền vào ô vuông không cho điểm.").Font("Times New Roman").FontSize(12.5).Italic();
                    SetParagraphSpacing(pNoteDS, 2);
                }

                var statQuestion = tnQuestions.FirstOrDefault(q => q.SoThuTu == 7);
                if (statQuestion != null)
                {
                    string ans = statQuestion.DapAnDung ?? "";
                    bool hasTwoParts = ans.Contains("a)") && ans.Contains("b)");
                    if (hasTwoParts)
                    {
                        double diemMoiY = Math.Round(statQuestion.Diem / 2.0, 2);
                        var pNoteStat = doc.InsertParagraph();
                        pNoteStat.Append($"* Hướng dẫn chấm Câu {statQuestion.SoThuTu}: Trả lời đúng mỗi ý (a, b) đạt {FormatPoint(diemMoiY)} điểm.").Font("Times New Roman").FontSize(12.5).Italic();
                        SetParagraphSpacing(pNoteStat, 2);
                    }
                }

                var pSpacerTN = doc.InsertParagraph();
                SetParagraphSpacing(pSpacerTN, 6);
            }

            // 2. Phần Tự luận: Bảng 2 cột [Bài giải] | [Biểu điểm]
            var pTL = doc.InsertParagraph();
            pTL.Append($"II. PHẦN TỰ LUẬN ({FormatPoint(tlTotalPoints)} điểm)").Font("Times New Roman").FontSize(14).Bold();
            SetParagraphSpacing(pTL, 6);

            foreach (var q in tlQuestions)
            {
                var pQTitle = doc.InsertParagraph();
                pQTitle.Append($"Câu {q.SoThuTu}. ({FormatPoint(q.Diem)} điểm)").Font("Times New Roman").FontSize(14).Bold();
                SetParagraphSpacing(pQTitle, 4);

                bool isCalcQ = IsCalculationQuestion(q);
                var steps = (q.CacBuocGiai != null && q.CacBuocGiai.Count > 0)
                    ? q.CacBuocGiai
                    : (isCalcQ ? AutoExtractCalculationSteps(q.DapAnDung ?? "", q.Diem) : null);

                if (steps != null && steps.Count > 0)
                {
                    var stepTable = doc.AddTable(steps.Count + 1, 2);
                    stepTable.Alignment = Alignment.center;
                    stepTable.AutoFit = AutoFit.Window;

                    var solidBorder = new Border(BorderStyle.Tcbs_single, BorderSize.one, 0, BorderBlack);
                    SetFullBorders(stepTable, solidBorder);

                    SetCell(stepTable, 0, 0, "Bài giải", true, Alignment.center, 14.0);
                    SetCell(stepTable, 0, 1, "Biểu điểm", true, Alignment.center, 14.0);
                    stepTable.Rows[0].Cells[1].Width = 100;

                    double stepPointDefault = Math.Round(q.Diem / steps.Count, 2);

                    for (int s = 0; s < steps.Count; s++)
                    {
                        var step = steps[s];
                        int r = s + 1;
                        var cellSol = stepTable.Rows[r].Cells[0];
                        cellSol.VerticalAlignment = VerticalAlignment.Center;

                        var formattedLines = NormalizeStepLines(step.NoiDung ?? "", isCalcQ);
                        if (formattedLines.Count == 0) formattedLines.Add(new MathFormattedLine { Text = "", Kind = MathStepLineKind.Normal });

                        for (int li = 0; li < formattedLines.Count; li++)
                        {
                            var fLine = formattedLines[li];
                            var pSol = li == 0 ? cellSol.Paragraphs[0] : cellSol.InsertParagraph();
                            pSol.RemoveText(0);

                            switch (fLine.Kind)
                            {
                                case MathStepLineKind.DoiDonVi:
                                    pSol.Alignment = Alignment.left;
                                    SetParagraphSpacing(pSol, 2);
                                    break;

                                case MathStepLineKind.LoiGiai:
                                    pSol.Alignment = Alignment.left;
                                    SetParagraphSpacing(pSol, 1);
                                    break;

                                case MathStepLineKind.PhepTinh:
                                    pSol.Alignment = Alignment.left;
                                    pSol.IndentationBefore = 24f; // Thụt lề đầu dòng phép tính nổi bật dưới câu lời giải
                                    SetParagraphSpacing(pSol, 2);
                                    break;

                                case MathStepLineKind.DapSo:
                                    pSol.Alignment = Alignment.right; // Đáp số căn lề phải
                                    SetParagraphSpacing(pSol, 2);
                                    break;

                                default:
                                    pSol.Alignment = Alignment.left;
                                    SetParagraphSpacing(pSol, li == formattedLines.Count - 1 ? 2 : 1);
                                    break;
                            }

                            DocxContentRenderer.AppendTextWithFractions(doc, pSol, fLine.Text, "Times New Roman", 14.0);
                        }

                        // Linh hoạt tính điểm cho từng phép tính/bước giải theo tổng điểm câu
                        double stepPt = step.Diem;
                        if (isCalcQ || Math.Abs(steps.Sum(x => x.Diem) - q.Diem) > 0.05 || stepPt <= 0)
                        {
                            stepPt = stepPointDefault;
                        }

                        SetCell(stepTable, r, 1, $"({FormatPoint(stepPt)} điểm)", true, Alignment.center, 14.0);
                    }

                    doc.InsertTable(stepTable);

                    if (isCalcQ)
                    {
                        var pNoteCalc = doc.InsertParagraph();
                        pNoteCalc.Append($"* Lưu ý chấm Câu {q.SoThuTu}: Học sinh thực hiện đúng mỗi phép tính (đặt tính đúng và tính đúng kết quả) đạt điểm tối đa của phép tính đó. Đặt tính sai cột/hàng nhưng kết quả đúng không cho điểm; đặt tính đúng nhưng kết quả sai không cho điểm.").Font("Times New Roman").FontSize(12.5).Italic();
                        SetParagraphSpacing(pNoteCalc, 3);
                    }

                    var pSpacerStep = doc.InsertParagraph();
                    SetParagraphSpacing(pSpacerStep, 6);
                }
                else
                {
                    var ansLines = (q.DapAnDung ?? "").Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    for (int li = 0; li < ansLines.Length; li++)
                    {
                        var pAns = doc.InsertParagraph();
                        DocxContentRenderer.AppendTextWithFractions(doc, pAns, ansLines[li], "Times New Roman", 14.0);
                        SetParagraphSpacing(pAns, li == ansLines.Length - 1 ? 6 : 3);
                    }
                }
            }

            // Lưu ý sư phạm
            var pNote = doc.InsertParagraph();
            pNote.Append("*(Học sinh giải cách khác, đúng vẫn cho điểm tối đa).\n*Trình bày bẩn, chữ - số viết chưa đẹp, chưa rõ ràng trừ 1,0 điểm toàn bài./.").Font("Times New Roman").FontSize(14).Italic();
            SetParagraphSpacing(pNote, 6);
        }

        #endregion
    }
}
