# HƯỚNG DẪN SỬ DỤNG NHANH COCOCARBON AI (1 TRANG A4)

**CocoCarbon AI** — *Từ vườn dừa đến giá trị xanh (From Coconut Farms to Verifiable Value)*
Khu vực khởi điểm: **Vùng dừa Bến Tre — Tỉnh Vĩnh Long**

---

## 1. Truy cập & Đăng nhập
- **Web URL**: *(Điền URL production sau khi triển khai VPS; môi trường local mặc định `http://localhost:3000`)*
- **Android APK**: `android/app/build/outputs/apk/release/app-release.apk` (hoặc `debug/app-debug.apk` cho bản kiểm thử nội bộ)
- **Đăng nhập**: Dùng **Google Sign-In** (Firebase Authentication) hoặc chọn các tài khoản **[DEMO]** trên màn hình `/login` khi `FIREBASE_AUTH_DEV_ALLOW_MOCK=true` (`Farmer`, `HTX`, `Project Developer`, `Buyer`, `Reviewer`, `Admin`).

## 2. Quy trình trải nghiệm 5 bước
1. **Số hóa vườn dừa (`/farms`)**: Khai báo tên vườn, tỉnh **Vĩnh Long**, xã (vùng dừa Bến Tre), diện tích (ha), số cây, giống (*Dừa Ta / Xiêm xanh*), tuổi cây, tọa độ GPS kèm ô đồng thuận.
2. **Ước tính tham khảo (`/calculator`)**: Chọn vườn vừa tạo và bấm **Tính ước tính tham khảo**. Ví dụ chuẩn: *Dừa Ta, 10 năm, 1.0 ha → 75.2436 tCO2 tương đương tham khảo* (tính trực tiếp ở C# backend theo CTU 2015 [R1], kèm đối chiếu bối cảnh mật độ 160–240 cây/ha [R2]).
3. **Upload chứng từ & AI Carbon Auditor (`/farms/{id}` → `/documents`)**: Tải lên tệp PDF hoặc ảnh vườn. Hệ thống kiểm tra MIME/magic bytes/SHA-256, gọi Python FastAPI + Gemini phân tích trích xuất trường dữ liệu kèm trang chứng cứ, phát hiện trùng lặp/sai lệch và tạo bảng **Mức độ đầy đủ hồ sơ / Project Readiness**.
4. **Hợp tác xã & Rà soát nội bộ (`/cooperative`, `/documents`)**: Reviewer chấp nhận/chỉnh sửa/từ chối gợi ý AI; Quản lý HTX xem tổng diện tích vùng nguyên liệu được ủy quyền.
5. **Danh mục Dự án & RFQ (`/projects`, `/buyer`)**: Doanh nghiệp tìm hiểu dự án ở giai đoạn *Concept / DataCollection / Feasibility* và gửi yêu cầu báo giá/trao đổi (**RFQ**).

## 3. Lưu ý dữ liệu & Ranh giới khoa học
- Các bản ghi có sẵn trong hệ thống đều gắn nhãn **[DEMO]**.
- Con số từ Calculator là **ước tính tham khảo theo sinh khối và cấp tuổi CTU 2015**, **không phải** lượng hấp thụ hằng năm (`tCO2/ha/năm`), **không phải** tín chỉ carbon đã phát hành và **không đảm bảo** doanh thu.
- CocoCarbon AI không thay thế cơ quan quản lý nhà nước, registry, tổ chức thẩm định (VVB) hay sàn giao dịch carbon được cấp phép.
