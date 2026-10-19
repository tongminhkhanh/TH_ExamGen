# Thiết kế kiểm soát chương trình khi tạo đề

**Trạng thái:** Các hàng rào chính đã được triển khai; danh mục bài/tuần mặc định vẫn cần giáo viên rà soát trước khi dùng  
**Phạm vi:** Lớp 4–5 · Toán và Tiếng Việt · giữa kỳ/cuối kỳ I và II  
**Ngày:** 18-10-2026

## 1. Mục tiêu

Ngăn AI tạo câu hỏi vượt quá nội dung học sinh đã học theo bộ Kết nối tri thức (KNTT), đồng thời giữ ma trận, đề, đáp án và file Word nhất quán. Phạm vi tuần phải được xác định từ danh mục chương trình đã rà soát, không dựa riêng vào prompt hoặc tên chủ đề người dùng nhập.

Phạm vi này bao gồm cả lỗi ở Bảng II Tiếng Việt: chi tiết được tổng hợp động và kiểm tra trước khi xuất. Người dùng đã duyệt triển khai; tài liệu dưới đây ghi lại quyết định và giới hạn dữ liệu hiện hành.

## 2. Các quyết định đã thống nhất

| Chủ đề | Quyết định |
|---|---|
| Phạm vi | Lớp 4–5, hai môn Toán và Tiếng Việt, đủ bốn kỳ đánh giá. |
| Kiến trúc | Dùng một bộ quy tắc lọc phạm vi chung, nhưng danh mục chương trình riêng cho từng môn. Không tạo màn hình riêng để giáo viên tự xây lịch tuần. |
| Lịch mặc định | Lập lịch KNTT theo bài/tiết; bài có thể trải qua một dải tuần. Ghi nguồn cho từng mục và đánh dấu mục cần xác nhận. Mục chưa xác nhận bị loại khỏi chọn nội dung và sinh đề. |
| Giới hạn theo kỳ | Chỉ dùng bài đã kết thúc trước hoặc trong mốc tuần của kỳ. Với Toán lớp 4 giữa kỳ I, giới hạn đã xác nhận là hết tuần 9; dạng toán tổng–hiệu ở tuần 12 không đủ điều kiện. Mốc của các lớp/kỳ còn lại phải được đối chiếu trong danh mục mặc định trước khi bật tạo đề. |
| Yêu cầu ngoài phạm vi | Báo lý do và tuần học của nội dung, rồi dừng; không tự thay bằng kiến thức gần giống. |
| Thiếu chủ đề Toán | Cho phép lặp bài đã chọn để đủ ma trận, nhưng câu lặp phải nằm trong mục tiêu và cấu trúc được phép của bài. |
| Dạng câu Toán | Khóa cấu trúc bài toán theo danh sách mẫu được phép của từng bài; AI có thể thay ngữ cảnh, số liệu và lời văn trong giới hạn đó, không tự thêm phép toán hoặc dạng lời giải mới. |
| Khung Tiếng Việt | Giữ khung 11 câu/8 điểm: 7 câu đọc hiểu/5 điểm và 4 câu Kiến thức Tiếng Việt/3 điểm. Phân bổ các dòng con linh hoạt theo nội dung đủ điều kiện; nếu không thể lấp khung mà không vượt chương trình thì dừng. |
| Chủ điểm Tiếng Việt | Bài đọc do AI viết phải theo chủ điểm KNTT của lớp và kỳ. Bài giáo viên nhập/chọn từ nguồn ngoài không được lấy từ SGK KNTT, nhưng vẫn phải khớp chủ điểm của kỳ. Không khớp thì chặn xuất đề. |
| Bản nháp cũ | Áp dụng cho toàn bộ phạm vi. Giữ nguyên dữ liệu cũ, đánh dấu mục chưa ánh xạ và chặn tạo/xuất cho tới khi xử lý. Không tự xóa hoặc âm thầm đổi nội dung. |
| Số liệu và DOCX | Bảng I, Bảng II, đáp án và DOCX dùng chung một mô hình câu hỏi đã kiểm tra. Tính tổng từ câu hỏi chi tiết; chặn xuất nếu số liệu giữa các bảng không khớp. |

### Phân loại Tiếng Việt

Từ đồng nghĩa, trái nghĩa, đồng âm và đa nghĩa đều thuộc mạch **Từ vựng**, không chuyển sang Ngữ pháp. Mạch/kỹ năng không có câu hỏi thì để trống hoặc ẩn dòng số liệu; không sinh các chuỗi mẫu như `0-1` hay `0-0,5`.

Các tổng 11 câu/8 điểm là khung đề được người dùng duyệt, không phải số liệu điền cứng vào từng hàng của Bảng II. Nguồn dữ liệu chuẩn của khung đề chỉ được khai báo một lần; tổng theo mạch, hình thức, mức độ và điểm luôn được cộng từ danh sách câu hỏi.

## 3. Rủi ro kiến trúc hiện tại cần giải quyết

Danh mục chủ đề hiện đã cấp mã ổn định theo môn/lớp/kỳ và dấu phiên bản nội dung; bộ chọn, ma trận, câu hỏi và đường xuất dùng các mã này để kiểm tra allowlist. Các ngân hàng ma trận cũ không có ánh xạ ổn định sẽ bị từ chối xuất cho tới khi tạo lại hoặc xử lý lại.

Các điểm chặn được đặt tại lớp dịch vụ xuất, kể cả các API xuất riêng ma trận/đề và gói đầy đủ, nên đường gọi trực tiếp cũng phải qua kiểm tra phạm vi. Bảng II Tiếng Việt tổng hợp số liệu theo các dòng chi tiết; dòng không có câu để trống. Bộ xuất kiểm tra ma trận trước khi ghi DOCX.

## 4. Mô hình dữ liệu đề xuất

### Danh mục chương trình theo môn

Mỗi mục bài học/chuẩn kiến thức có tối thiểu:

- Mã ổn định; lớp; môn; bộ sách; kỳ; tuần bắt đầu và tuần kết thúc.
- Mạch/chủ điểm; mục tiêu hoặc kỹ năng; dạng câu hỏi/cấu trúc được phép.
- Yêu cầu cần đạt tách theo ba mức độ nhận thức M1/M2/M3 (Thông tư 27) khi mục đã được rà soát mức; mục chưa tách mức giữ một yêu cầu chung.
- Nguồn tham chiếu; ghi chú về căn cứ gán tuần; trạng thái rà soát (`Confirmed` hoặc `NeedsReview`).

Phần giải quyết phạm vi dùng chung, còn mục tiêu, mạch, dạng câu và quy tắc riêng nằm trong danh mục từng môn. Một mục chỉ đủ điều kiện khi đã xác nhận, đúng lớp/môn/kỳ và tuần kết thúc không vượt mốc kỳ.

### Mô hình đề và câu hỏi

Mỗi câu hỏi gắn mã mục chương trình, mục tiêu/kỹ năng, hình thức, mức độ, điểm và mã bài đọc nếu có. Bài đọc có nguồn gốc (`AI`, `teacher-authored`, `external`) cùng thông tin thư mục/đường dẫn sẵn có. Mã bài đọc giúp câu hỏi kiểm tra đúng nội dung văn bản thay vì chỉ đúng nhãn chủ đề.

Tập câu hỏi sau kiểm tra là nguồn duy nhất để dựng các dòng ma trận và phần xuất. Bảng I/Bảng II là các phép nhóm và cộng trên tập câu này, không có phép nhập tổng độc lập.

## 5. Luồng tạo đề và kiểm tra

1. Bộ chọn chỉ hiển thị mục đã xác nhận và đủ điều kiện theo lớp, môn, kỳ, tuần.
2. Giáo viên chọn mục/chủ điểm. Prompt nhận đúng danh sách mã được phép, mục tiêu và dạng câu của chúng; không nhận danh sách kiến thức rộng hơn để AI tự chọn.
3. AI trả về dữ liệu có cấu trúc, trong đó mỗi câu gắn mã mục, kỹ năng, hình thức, mức độ và điểm. Với Tiếng Việt, câu hỏi còn gắn nội dung trong bài đọc.
4. Bộ kiểm tra trong ứng dụng xác minh mã, phạm vi tuần, cấu trúc câu, số lượng/điểm của khung đề và phép cộng theo hàng/cột. Ràng buộc ngữ nghĩa được củng cố bằng kiểm tra AI; kiểm tra prompt không thay thế các kiểm tra xác định được bằng mã.
5. Nếu kết quả sai, hệ thống yêu cầu tạo lại trong số lần giới hạn. Nếu vẫn sai, dừng, hiển thị câu/mục vi phạm và không cho tạo hoặc xuất đề.
6. Trước khi xuất DOCX, hệ thống kiểm tra lại tổng câu, TN/TL, mức độ, điểm và đối chiếu Bảng I với Bảng II. Bất kỳ chênh lệch nào đều chặn xuất.

## 6. Quy tắc riêng từng môn

### Toán

- Dạng câu được phép là thuộc tính của mục bài học, không suy ra từ prompt tự do.
- Ví dụ hồi quy quan trọng: đề Toán lớp 4 giữa kỳ I hết tuần 9 không được có bài toán tổng–hiệu nếu mục đó thuộc tuần 12.
- Lặp câu/chủ đề chỉ được dùng khi mục tiêu và dạng cấu trúc vẫn nằm trong danh sách đã chọn và đã học.
- Yêu cầu cần đạt theo mức: mục đã tách M1/M2/M3 thì mỗi dòng ma trận phải sao chép nguyên văn yêu cầu cần đạt đúng mức của mục; cùng một mục xuất hiện ở các mức khác nhau không được lặp y hệt yêu cầu cần đạt. Mục chưa tách mức giữ yêu cầu chung cho mọi dòng.
- Điền 3 cột văn bản tách khỏi AI: dòng ma trận do AI gợi ý được ứng dụng tự điền lại nguyên văn `chuDe` (mạch), `noiDungKienThuc` (tên mục) và `yeuCauCanDat` (theo mức) từ danh mục đã xác nhận theo cặp (mã mục, mức độ) ngay sau khi nhận JSON, trước bước kiểm nghiêm ngặt. AI chỉ còn phải chọn đúng mã mục, mức độ, hình thức, điểm và cờ tikz; dòng mang mã/mức không hợp lệ được giữ nguyên để bộ kiểm báo lỗi, còn dòng do giáo viên chỉnh tay vẫn bị kiểm đẳng thức từng ký tự.
- Nếu nội dung được yêu cầu không có trong danh mục kỳ hiện tại, phản hồi nêu tên bài và tuần dự kiến rồi dừng.

### Tiếng Việt

- Khung tổng 11/8 được giữ; số liệu từng dòng phân bổ từ câu hỏi thực tế. Không ép phải có Ngữ pháp, Hoạt động giao tiếp, Sự phát triển ngôn ngữ hoặc Văn bản thông tin khi kỳ không có nội dung tương ứng.
- Các kỹ năng đọc hiểu được chia theo mục tiêu nội dung, hình thức, liên hệ/kết nối khi phù hợp với danh mục kỳ; dòng không có câu để trống/ẩn.
- Bốn dạng từ đồng nghĩa, trái nghĩa, đồng âm, đa nghĩa thuộc Từ vựng.
- Đoạn AI tự viết cần là nội dung mới, theo chủ điểm kỳ, không dùng lại văn bản SGK KNTT. Đoạn giáo viên nhập hoặc lấy từ sách/báo bên ngoài cần lưu nguồn nếu có; vẫn phải khớp chủ điểm KNTT và chỉ được tạo câu hỏi trên kỹ năng đã học. Không có luồng xác nhận ngoại lệ khi bài đọc lệch chủ điểm.

## 7. Bản nháp cũ

Khi tải bản nháp, đối chiếu nội dung hiện tại với danh mục mới. Gợi ý ánh xạ chỉ được hiển thị, không tự áp dụng. Mục ánh xạ mơ hồ hoặc không khớp cần giáo viên sửa/chọn lại. Giữ nguyên nội dung nguồn và đánh dấu trạng thái chưa hợp lệ; khóa tạo đề/xuất DOCX cho đến khi mọi mục liên quan được xử lý.

## 8. Triển khai hiện tại

1. Danh mục theo lớp/môn/kỳ có nguồn tham chiếu, tuần bắt đầu–kết thúc và căn cứ tuần. Mục được gán trạng thái rà soát; mặc định mục chưa xác nhận không được đưa vào sinh đề. Các tuần ngoài mốc Toán lớp 4 giữa kỳ I vẫn là ước lượng cần đối chiếu phân phối bài/tiết của trường.
2. Bộ lọc phạm vi được dùng ở bộ chọn, ma trận, prompt, kiểm tra đầu ra AI, bản nháp và dịch vụ DOCX. Nếu nội dung không có trong allowlist đã xác nhận, hệ thống báo lỗi và dừng.
3. AI thử tạo lại tối đa ba lần khi dữ liệu không đạt kiểm tra; nếu vẫn lỗi thì dừng và không xuất. Ma trận Tiếng Việt giới hạn một chủ điểm đọc cho một bài đọc; bài đọc phải khớp chủ điểm, và câu hỏi phải ở kỹ năng đã học. Bài do giáo viên nhập có trường nguồn riêng. Ma trận Toán do AI gợi ý được điền lại 3 cột văn bản từ cặp (mã mục, mức độ) đã xác nhận trước khi kiểm; thông báo lỗi nêu đúng giá trị cần ghi để lần tạo lại sửa trúng.
4. Bản nháp cũ được tính là cần rà soát khi thiếu mã/phiên bản danh mục hoặc chứa nội dung ngoài phạm vi; các thao tác sinh/xuất bị chặn cho tới khi xử lý. Nội dung bản nháp cũ không tự đổi.
5. Các đường xuất DOCX kiểm tra ma trận và bài đọc ở tầng dịch vụ. Bảng II Tiếng Việt cộng động từ chi tiết; không có số câu thì ô số liệu trống. Bảng I/II và đề được kiểm tra trên cùng ma trận đã gắn vào đề trước lúc xuất.

### Giới hạn dữ liệu hiện tại

- Tên một số bài/chủ điểm được khởi tạo từ danh mục và nguồn tham chiếu KNTT; tuần bắt đầu–kết thúc chưa được chứng minh đồng nhất cho mọi trường. Trạng thái mặc định là chưa xác nhận. Giáo viên cần đối chiếu và xác nhận theo phân phối chương trình đang áp dụng trước khi dùng.
- Chỉ mốc Toán lớp 4 giữa kỳ I đến tuần 9 và việc chuyển dạng tổng–hiệu sang tuần 12 được chốt theo yêu cầu người dùng. Các kỳ/lớp còn lại không được coi là lịch tuần chính thức.
- Mô hình mã danh mục/phiên bản là nguồn dùng lúc chạy, nhưng danh mục môn Toán và Tiếng Việt vẫn được lưu ở catalog riêng. Chưa có màn hình quản trị danh mục để sửa toàn bộ phân phối bài/tiết; việc hiệu chỉnh tuần hiện cần cập nhật dữ liệu catalog rồi xác nhận lại.
- Bộ chặn bảo đảm nội dung được gắn vào mục tiêu đã xác nhận và kiểm tra tổng/khung, nhưng việc đánh giá ngữ nghĩa còn giới hạn ở các mẫu/ràng buộc có thể kiểm tra tự động. Giáo viên vẫn cần đọc duyệt đề.

## 9. Điều kiện kiểm soát khi chạy

- Nội dung `NeedsReview`, sai kỳ hoặc vượt tuần không xuất hiện như mục được chọn để sinh đề.
- Ví dụ Toán lớp 4 giữa kỳ I: tuần 9 được dùng làm ranh giới; dạng tổng–hiệu tuần 12 bị từ chối.
- Yêu cầu ngoài phạm vi nêu rõ bài/tuần và dừng; không tự nới kiến thức.
- Mọi câu Toán có cấu trúc được phép; lặp nội dung không làm nảy sinh mục tiêu mới.
- Ma trận Toán phải khớp yêu cầu cần đạt đúng mức của mục đã xác nhận: mục đã tách M1/M2/M3 thì dòng ma trận sao chép nguyên văn yêu cầu của đúng mức; cùng một mục xuất hiện ở nhiều mức mà lặp y hệt yêu cầu cần đạt bị chặn và đưa vào diện cần lập lại ma trận. Dòng do AI gợi ý được tự điền lại 3 cột văn bản từ cặp (mã mục, mức độ) đã xác nhận trước khi kiểm; nếu vẫn sai, thông báo lỗi nêu giá trị đúng cần ghi theo từng cột.
- Tiếng Việt giữ khung 11 câu/8 điểm nhưng mọi chi tiết theo mạch, hình thức, mức độ và điểm cộng từ câu hỏi thực tế; mạch không có câu không bị lấp số mẫu.
- Trái nghĩa, đồng âm, đa nghĩa và đồng nghĩa được xếp vào Từ vựng.
- Bài đọc không khớp chủ điểm bị chặn xuất; bài đọc ngoài SGK KNTT có nguồn được lưu và câu hỏi chỉ dùng kỹ năng trong phạm vi kỳ.
- Bản nháp cũ không bị sửa/xóa âm thầm; mục chưa ánh xạ khóa tạo/xuất.
- Bảng I, Bảng II, đáp án và DOCX cho cùng một tập câu hỏi cho tổng khớp chính xác; không hardcode tổng hàng chi tiết.

## 10. Nguồn và việc cần xác nhận dữ liệu

Các trang học liệu của Nhà Xuất bản Giáo dục Việt Nam là điểm bắt đầu để rà soát sách KNTT: [Vở bài tập Toán 4 – Tập một](https://hoclieubotro.nxbgd.vn/sach-bai-tap/18), [Vở bài tập Tiếng Việt 4 – Tập một](https://hoclieubotro.nxbgd.vn/sach-bai-tap/28/28004), [Vở bài tập Tiếng Việt 5 – Tập một](https://hoclieubotro.nxbgd.vn/sach-bai-tap/30/30003). Chương trình giáo dục phổ thông tổng thể của Bộ GDĐT là nguồn tham khảo về khung chương trình: [văn bản hợp nhất chương trình tổng thể](https://moet.gov.vn/content/vanban/Lists/VBPQ/Attachments/1483/vbhn-chuong-trinh-tong-the.pdf).

Các trang sách là đầu mối tham khảo nội dung và bộ sách, không chứng minh tuần dạy giống nhau ở mọi trường. Catalog hiện ghi rõ trạng thái tuần cần đối chiếu; chỉ mục đã được người dùng rà soát/xác nhận mới mở cho AI sử dụng. Mốc tuần Toán lớp 4 giữa kỳ I đến tuần 9 là quyết định đã chốt; các mốc còn lại cần được kiểm tra theo phân phối bài/tiết tại trường.

## Nhật ký quyết định

1. Phạm vi mở rộng sang lớp 4–5, Toán + Tiếng Việt, đủ bốn kỳ.
2. Kiến trúc dùng bộ lọc chung và danh mục riêng theo môn.
3. Lịch KNTT mặc định theo bài/tiết, ghi nguồn và đánh dấu mục cần xác nhận; mục chưa xác nhận bị loại khỏi sinh đề.
4. Nội dung yêu cầu ngoài kỳ phải dừng và nêu tuần; bài Toán có thể lặp trong cùng mục tiêu/cấu trúc.
5. Câu Toán theo cấu trúc được phép; tạo lại có giới hạn rồi dừng nếu còn vi phạm.
6. Tiếng Việt giữ 11 câu/8 điểm; mạch phân bổ động và dừng nếu không thể đáp ứng trong chương trình.
7. Bài đọc AI theo chủ điểm kỳ; bài đọc từ nguồn ngoài SGK KNTT cũng phải khớp chủ điểm; lệch chủ điểm chặn xuất.
8. Bản nháp cũ bị đánh dấu/chặn cho cả hai môn, lớp 4–5 và đủ bốn kỳ.
9. Bảng I, Bảng II, đáp án và DOCX dùng một tập câu hỏi; tổng tự cộng và sai lệch chặn xuất.
10. Người dùng duyệt thứ tự 5 bước và sau đó yêu cầu thực hiện; mã nguồn đã được cập nhật theo các hàng rào chính. Dữ liệu lịch tuần ngoài mốc Toán lớp 4 giữa kỳ I còn cần rà soát trước khi xác nhận sử dụng.
11. Yêu cầu cần đạt tách theo mức M1/M2/M3 (Thông tư 27) bắt đầu từ danh mục Toán 4 giữa kỳ I: mapper ghi yêu cầu theo mức của từng dòng, validator chặn yêu cầu sai mức và lặp y hệt giữa các mức khác nhau của cùng một mục, cả ba prompt (gợi ý ma trận, sinh đề, thẩm định) đều ràng buộc theo mức. Mục chưa tách mức giữ yêu cầu chung.
12. Khắc phục lỗi "Dòng 1 đã đổi mạch hoặc nội dung..." khi gợi ý ma trận Toán 4 giữa kỳ I: ứng dụng tự điền lại 3 cột văn bản từ cặp (mã mục, mức độ) đã xác nhận sau khi nhận JSON AI (`CurriculumMatrixMapper.FillCatalogTexts`); prompt ma trận thêm khối "CÁCH ĐIỀN 3 CỘT VĂN BẢN" và gọi tên field trong các quy tắc sao chép; thông báo lỗi validator nêu giá trị đúng cần ghi theo từng cột. Dòng do giáo viên chỉnh tay vẫn bị kiểm nghiêm ngặt như cũ.
