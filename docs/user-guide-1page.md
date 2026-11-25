# HƯỚNG DẪN SỬ DỤNG NHANH COCOCARBON AI (1 TRANG A4)

**CocoCarbon AI** — *Từ vườn dừa đến giá trị xanh (Trợ lý Tài chính Nông hộ & Chống lừa đảo Hồ sơ Carbon)*
Khu vực khởi điểm: **Vùng dừa Bến Tre — Tỉnh Vĩnh Long**

---

## 1. Truy cập & Tài khoản Dùng thử
- **Web URL (Production)**: *(Điền URL HTTPS công khai của đội khi triển khai; chạy Docker local qua Nginx tại `http://localhost` hoặc Web dev `http://localhost:3000`)*
- **Android APK**: `android/app/build/outputs/apk/debug/app-debug.apk`
- **Đăng nhập Production (Firebase Authentication)**:
  - Hỗ trợ **Google Sign-In** và **Email / Mật khẩu Firebase** ngay tại `/login` (biến `NEXT_PUBLIC_FIREBASE_*` truyền qua Docker build args).
  - **Tài khoản Ban Giám Khảo (Production Firebase Email/Password)**: *(Điền cặp Email/Mật khẩu tài khoản `Farmer` và `Reviewer` đã tạo trên Firebase Console tại thời điểm triển khai)*.
  - *Chế độ Development cục bộ*: Nếu chạy `FIREBASE_AUTH_DEV_ALLOW_MOCK=true`, các nút `[DEMO]` hiển thị sẵn vai trò `Farmer`, `HTX`, `Developer`, `Buyer`, `Reviewer`, `Admin`.

## 2. Quy trình trải nghiệm 5 bước
1. **Kiểm tra Đề nghị Đầu tư Carbon (`/documents`)**: Dán lời mời/điều khoản hợp đồng carbon và mức phí yêu cầu trước → bấm **Kiểm tra Đề nghị Đầu tư** để nhận cảnh báo `DO_NOT_SIGN_OR_PAY`, trích dẫn bằng chứng và đối chiếu 3 chỉ số tách biệt (`Project Readiness`, `Document Completeness`, `Carbon Integrity Risk` — thiếu giấy tờ không bị đánh đồng là gian lận).
2. **Trợ lý Tài chính AI & Ghi Sổ Thu Chi (`/financial-assistant`)**: Nhập câu tự nhiên (VD: *"Chi 3.5 triệu mua phân hữu cơ"*) để AI phân loại và bấm **Xác nhận ghi vào Sổ Thu Chi Vườn**; chạy phân tích để xem tổng hợp Tháng/Quý/Năm, cảnh báo vượt ngân sách, ROI hiệu chỉnh theo số liệu thực tế, biểu đồ dòng tiền, kịch bản nhạy cảm và báo cáo 9 phần.
3. **Số hóa vườn & Calculator CTU 2015 (`/farms`, `/calculator`)**: Khai báo vườn và tính ước tính tham khảo tại C# API (*Dừa Ta 10 năm, 1.0 ha → 75.2436 tCO2 tương đương tham khảo*; từ chối nội suy tuổi khác 4/10).
4. **Upload chứng từ & Reviewer duyệt (`/farms/{id}` → `/documents`)**: Tải PDF/ảnh kiểm tra magic bytes + SHA-256, bóc tách bằng chứng trang và phê duyệt bởi Reviewer.
5. **HTX, Danh mục Dự án & RFQ (`/cooperative`, `/projects`, `/buyer`)**: Gom vùng nguyên liệu theo ủy quyền HTX và gửi yêu cầu trao đổi (RFQ).

## 3. Lưu ý Khoa học & Minh bạch
- Tách bạch rõ `ACTUAL_DATA` (Sổ thu chi), `USER_ASSUMPTION` (giá/sản lượng giả định), `MODEL_ESTIMATE` (Python Engine) và `HYPOTHETICAL_SCENARIO`.
- Con số Calculator CTU 2015 là **ước tính sinh khối tham khảo theo cấp tuổi**, không phải `tCO2/ha/năm`, không phải tín chỉ phát hành hay doanh thu bảo đảm.
