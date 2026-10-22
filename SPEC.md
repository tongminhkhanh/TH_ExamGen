# [TH_EXAMGEN] - Đặc Tả Kỹ Thuật & Nghiệp Vụ (Specification)

## 1. Mục tiêu & Phạm vi (Scope & Objectives)
Xây dựng nền tảng tạo đề thi và ma trận kiểm tra định kỳ tự động dành cho giáo viên tiểu học Lớp 4 và Lớp 5 theo chương trình GDPT 2018 (Bộ sách Kết nối tri thức với cuộc sống), tuân thủ định dạng kiểm tra đánh giá theo Thông tư 27/2020/TT-BGDĐT.

---

## 2. Đặc tả Nghiệp vụ Các Môn học

### 2.1. Môn Tiếng Việt (Lớp 4 & Lớp 5)
- **Cấu trúc ma trận chuẩn (11 câu - Tổng 10.0 điểm):**
  - **Phần Đọc hiểu văn bản (6.0 - 7.0 điểm):**
    + 7 câu hỏi bám sát bài đọc hiểu (4 câu Trắc nghiệm M1, 2 câu Tự luận M2, 1 câu Tự luận kết nối vận dụng M3).
    + Ngữ liệu bài đọc: Dung lượng chuẩn (Lớp 4: 250 - 280 chữ; Lớp 5: 300 - 350 chữ). Hỗ trợ AI tự sinh theo chủ điểm SGK Kết nối tri thức hoặc Giáo viên tự dán ngữ liệu ngoài SGK.
  - **Phần Kiến thức Tiếng Việt (3.0 - 4.0 điểm):**
    + 4 câu hỏi bao quát các mạch kiến thức: Từ loại, Câu ghép, Biện pháp tu từ, Phép liên kết câu.
- **Tiêu chuẩn kiểm định ngữ liệu:**
  + Không vi phạm bản quyền ngữ liệu ngoài SGK.
  + Không chứa nội dung nhạy cảm, bạo lực hoặc không phù hợp tâm lý lứa tuổi tiểu học.

### 2.2. Môn Toán (Lớp 4 & Lớp 5)
- **Cấu trúc ma trận chuẩn (10 câu - Tổng 10.0 điểm):**
  - **Hình thức:** 7 câu Trắc nghiệm 4 lựa chọn (7.0 điểm) + 3 câu Tự luận (3.0 điểm).
  - **Phân bổ mức độ:** Mức 1 (40% - 50%), Mức 2 (30%), Mức 3 (20% - 30%).
  - **3 Mạch kiến thức:**
    1. Số và phép tính (Số tự nhiên, phân số, số thập phân, 4 phép tính).
    2. Hình học và đo lường (Góc, hình tam giác, hình thang, hình tròn, diện tích, thể tích).
    3. Một số yếu tố thống kê và xác suất (Biểu đồ cột, biểu đồ hình quạt, xác suất thực nghiệm).
  - **Hình vẽ Toán:** Tự động sinh mã TikZ / SVG hiển thị trực quan và nhúng vào file Word xuất bản.

---

## 3. Đặc tả Bản quyền, Tài khoản & Hạn mức (License & Quota)

| Gói Bản Quyền | Giá Niêm Yết | Thời Hạn | Hạn Mức Đề | Giới Hạn Ngày | Phạm Vi Đợt Kiểm Tra Mở Khóa |
|---|---|---|---|---|---|
| **Dùng Thử (Trial)** | Miễn phí | 3 ngày | 1 đề | 1 đề/ngày | Cả 4 đợt kiểm tra |
| **Gói Kỳ I (`Term1`)** | 69.000đ | 5 tháng | 15 đề | 2 đề/ngày | Giữa Học kỳ I & Cuối Học kỳ I |
| **Gói Kỳ II (`Term2`)** | 69.000đ | 5 tháng | 15 đề | 2 đề/ngày | Giữa Học kỳ II & Cuối Học kỳ II |
| **Gói Cả Năm (`Year`)** | 119.000đ | 12 tháng | 30 đề | 4 đề/ngày | Mở khóa toàn bộ 4 đợt trong năm |
| **Gói Trường (`School`)** | 299.000đ | 12 tháng | 100 đề (5 mã x 20) | 4 đề/ngày/mã | Mở khóa toàn bộ 4 đợt trong năm |

### Tiêu chuẩn Chấp thuận (Acceptance Criteria):
1. **Kiểm tra phiên & thiết bị:** Mỗi thiết bị chỉ được đăng ký tối đa 1 tài khoản dùng thử.
2. **Cooldown & Daily Cap:** Chặn bấm liên tục trong 60 giây; chặn sinh đề vượt quá hạn mức ngày của gói.
3. **Trừ lượt an toàn:** Chỉ trừ quota khi AI đã phản hồi đầy đủ và parse thành công cấu trúc đề thi.
4. **Bảo vệ quyền truy cập:** Mọi tính năng AI và xuất Word ở cả Bước 1 (Ma trận) và Bước 3 (Đề thi) bắt buộc phải đăng nhập và thuộc đợt thi được cấp phép.
