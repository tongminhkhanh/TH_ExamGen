# Bộ MiKTeX offline cho TH_ExamGen (vẽ hình TikZ không cần Internet)

TH_ExamGen hiện vẽ hình theo 2 cách:

1. **Cục bộ (offline):** dùng `pdflatex` + `pdftoppm` của MiKTeX — nhanh, 300 DPI, không cần mạng.
2. **Kroki (online):** nếu không tìm thấy MiKTeX, app gọi dịch vụ `kroki.io` — **cần Internet**.

Máy trường **không có Internet** thì phải dùng cách 1. Thư mục này chứa công cụ để tạo
sẵn một bộ MiKTeX portable đầy đủ, chỉ việc copy sang là dùng.

---

## Tóm tắt 3 bước

```
(1) Máy CÓ Internet + đủ chỗ   ->  chạy  Build-TikzOffline.ps1
(2) Copy thư mục "tools\TikzOffline" sang USB -> chép sang máy trường
(3) Máy trường                  ->  chạy  Use-TikzOffline.ps1 -AppDir <thư mục publish>
(4) Máy trường                  ->  chạy  Test-TikzOffline.ps1 -Show   (kiểm tra nhanh)
```

Xong. Mở app lên là tự vẽ hình offline.

---

## Chi tiết

### Bước 1 — Dựng bộ (máy có Internet)

Mở PowerShell tại thư mục `tools\TikzOffline` và chạy:

```powershell
# Dựng vào chính thư mục này (.\bundle\miktex)
.\Build-TikzOffline.ps1

# Hoặc trỏ ra ổ/USB còn nhiều chỗ:
.\Build-TikzOffline.ps1 -TargetDir E:\TH_ExamGen_TikZ\miktex
```

> Nếu PowerShell chặn script, chạy trước:
> ```powershell
> Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
> ```

Script sẽ tự động:

| Bước | Việc làm |
|------|----------|
| 1 | Tải trình cài MiKTeX (`miktexsetup`) |
| 2 | Cài **MiKTeX Portable** (không ghi registry, không sửa PATH) |
| 3 | **Warm-up**: biên dịch thử 1 tài liệu TikZ đúng như app dùng, để tải sẵn mọi gói + font (`pgf/tikz`, `standalone`, `amsmath`, `amsfonts`, `graphicx`, font T1, thư viện `calc/shapes.geometric/arrows/patterns/positioning/decorations.pathreplacing`…) |
| 4 | Bảo đảm có `pdftoppm.exe` (chuyển PDF → PNG) |
| 5 | **Tắt tự-tải-gói** (`AutoInstall=0`) để khi offline không bị treo chờ mạng |
| 6 | Kiểm tra lại: biên dịch lần cuối khi đã tắt tự-tải → **phải thành công** mới báo xong |

Kết thúc, script in ra **thư mục bin** cần dùng (đây chính là giá trị `MiktexBinPath`) và
ghi lại vào `bundle\binpath.txt`.

**Dung lượng / thời gian:** bộ `basic` chiếm khoảng **600 MB – 1 GB** sau khi cài
(cần thêm ~1.5 GB trống trong lúc dựng). Thời gian tải tùy mạng, thường 5–20 phút.

Tùy chọn:

- `-PackageSet essential` → nhỏ hơn (warm-up sẽ tải bù; bộ cài vẫn đủ chạy).
- `-TargetDir <đường dẫn>` → đặt bộ ở nơi khác (khuyến nghị khi ổ C: chật).
- `-Mirror <URL>` → chỉ định kho gói MiKTeX.

---

### Bước 2 — Chép sang máy trường

Copy **cả thư mục `TikzOffline`** (đã có `bundle\miktex`) sang USB, rồi chép sang máy trường
(ví dụ `D:\TH_ExamGen_TikZ`). Bộ cài này **portable** — chạy được ở bất kỳ ổ/thư mục nào.

---

### Bước 3 — Trỏ app vào bộ offline (máy trường)

Tại thư mục `TikzOffline` trên máy trường:

```powershell
.\Use-TikzOffline.ps1 -AppDir "C:\đường\dẫn\tới\publish"
```

Script sẽ kiểm tra đủ `pdflatex.exe` + `pdftoppm.exe`, rồi ghi đường dẫn vào
`MiktexBinPath` trong `appsettings.json` của app. **Mở lại app** là xong.

> Muốn tự chọn trong app: mở **Cài đặt** → ô **đường dẫn MiKTeX** → **Chọn thư mục** →
> trỏ tới thư mục `...\bundle\miktex\texmfs\install\miktex\bin\x64`.

Quay lại dùng Kroki (cần Internet): `.\Use-TikzOffline.ps1 -AppDir <publish> -Revert`

> Script cũng tự chạy `initexmf --update-fndb` để MiKTeX nhận đúng vị trí mới sau khi
> copy sang ổ/thư mục khác (thao tác cục bộ, không cần mạng).

---

### Bước 4 — Kiểm tra offline (máy trường)

```powershell
.\Test-TikzOffline.ps1 -Show
```

Script biên dịch 1 hình mẫu (đúng preamble của app, có cả dấu tiếng Việt), xuất ảnh
`test-output\tikz-test.png` và tự mở lên. Nếu thấy hình chữ nhật / hình tròn / tam giác
và dấu tiếng Việt (hình chữ nhật, hình tròn, tam giác, â é ò ũ đ ư ơ) thì bộ cài đạt yêu cầu.

---

## Kiểm tra nhanh trên máy trường

1. Mở app → tạo 1 câu có hình (ví dụ hình chữ nhật / hình tròn) → hình hiện ra **trong lúc rút mạng**.
2. Nếu bật được tiếng Việt có dấu trong hình (`hình tròn`, `đường cao`…) là font T1 đã đủ.

> Cách xác nhận app đang dùng offline hay Kroki: nếu rút mạng mà vẫn vẽ được ⇒ đang dùng MiKTeX.
> Nếu hình không hiện và app báo lỗi tải ⇒ đang dùng Kroki.

---

## Khắc phục sự cố

| Hiện tượng | Cách xử lý |
|------------|------------|
| Build báo **thiếu gói** ở bước 6 | Máy dựng tạm mất mạng lúc warm-up. Chạy lại `Build-TikzOffline.ps1`. |
| **Không thấy `pdftoppm.exe`** | Trong `bin`, mở `miktex-console.exe` → tab **Packages** → tìm `miktex-poppler-bin-x64` → **Install** → chạy lại script. |
| App vẫn dùng Kroki dù đã cấu hình | Sai `MiktexBinPath`. Phải trỏ đúng thư mục **chứa `pdflatex.exe`** (thường là `...\bin\x64`). |
| Hình thiếu tiếng Việt có dấu | Chưa warm-up đủ font T1. Chạy lại script (đừng dùng `-SkipWarmup`). |
| Build chạy chậm / mạng yếu | Dùng `-Mirror` trỏ tới kho MiKTeX gần, hoặc chạy lại (script tải tiếp phần còn thiếu). |
| Copy sang ổ/thư mục khác bị lỗi font/đường dẫn | Chạy lại `Use-TikzOffline.ps1` (tự chạy `initexmf --update-fndb`), hoặc chạy tay `<bin>\initexmf.exe --update-fndb`. |
| **Không đủ dung lượng ổ C:** | Dựng sang ổ/USB khác: `.\Build-TikzOffline.ps1 -TargetDir E:\TH_ExamGen_TikZ\miktex` (cần ~1.5 GB trống khi dựng; bộ `basic` sau đó ~0.6–1 GB). |

---

## Ghi chú kỹ thuật

- Bộ cài dùng `miktexsetup --portable` với `--use-registry=no --modify-path=no` ⇒ **hoàn toàn
  không đụng** vào hệ thống máy (không registry, không PATH). Xoá thư mục = gỡ sạch.
- App tìm `pdflatex` và `pdftoppm` trong **cùng một thư mục** `MiktexBinPath`; nếu không có
  ở đó thì tìm theo `PATH`. Vì vậy bộ cài đặt cả hai file trong `…\bin\x64`.
- Sau warm-up, `AutoInstall=0` buộc `pdflatex` **lỗi ngay** nếu thiếu gói, thay vì treo chờ mạng.
