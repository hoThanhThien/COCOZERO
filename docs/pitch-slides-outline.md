# Dàn ý Slide Thuyết trình CocoCarbon AI (Tối đa 10 trang)

## Slide 1: Tên sản phẩm & Định vị
- **CocoCarbon AI** — *Từ vườn dừa đến giá trị xanh (From Coconut Farms to Verifiable Value)*
- Nền tảng AI hỗ trợ chuẩn hóa dữ liệu carbon và chuỗi giá trị dừa vùng Bến Tre (tỉnh Vĩnh Long).
- Khẩu hiệu: *Human Centered • AI Powered • Science First*.

## Slide 2: Bài toán thực tế tại Vùng dừa Bến Tre - Vĩnh Long
- Nông hộ thiếu công cụ đơn giản để số hóa vườn dừa, giống, tuổi, ranh giới và ảnh hiện trường.
- Hợp tác xã mất nhiều công tổng hợp thủ công, dễ trùng lặp hồ sơ hoặc thiếu bằng chứng quyền đất.
- Doanh nghiệp/đơn vị phát triển dự án thiếu dữ liệu có cấu trúc để sàng lọc trước khi thuê thẩm định (VVB).

## Slide 3: Giải pháp & Ranh giới sản phẩm trung thực
- Chuỗi giá trị khép kín: Số hóa vườn → Kiểm tra chất lượng dữ liệu AI → Ước tính tham khảo CTU 2015 → Gom dữ liệu HTX → Danh mục Dự án & RFQ.
- Không tự nhận là sàn giao dịch, registry hay VVB; phân biệt rõ Biomass, Carbon Stock, CO2e tham khảo và Tín chỉ đã phát hành.

## Slide 4: Nền tảng Khoa học Carbon (Science First)
- 4 hệ số CTU 2015 [R1] tại Giồng Trôm (24.5180 / 75.2436 / 20.4583 / 69.9189 tCO2/ha); KHÔNG nội suy tuổi, KHÔNG gọi là hấp thụ hằng năm.
- Khảo sát canh tác Bến Tre 2024 (150 hộ, 160–240 cây/ha [R2]) làm bối cảnh mô tả.
- Phân tách rõ IPCC 2006 Table 5.3 (196 tấn sinh khối khô/ha [R3]), Verra VM0042 v2.2 [R5], Gold Standard SOC [R7].

## Slide 5: Lõi AI Carbon Intelligence (Gemini + PyMuPDF + Curated RAG)
- Trích xuất PDF/ảnh kèm trang & đoạn trích bằng chứng; bảo vệ prompt injection (`<untrusted_document_data>`).
- Phát hiện trùng lặp SHA-256 và lệch diện tích/số cây; chấm điểm **Mức độ đầy đủ hồ sơ / Project Readiness** (0–100) yêu cầu Reviewer duyệt.
- Trợ lý RAG chỉ trích nguồn kiểm chứng [R1]–[R12], tự từ chối khi thiếu dữ liệu.

## Slide 6: Kiến trúc Kỹ thuật & Bảo mật
- Web (Next.js/TypeScript) + Android Native (Kotlin/Compose) dùng chung 1 Firebase Auth project.
- C# ASP.NET Core 8 là nguồn sự thật nghiệp vụ (VerifyIdTokenAsync, RBAC, Calculator, PostgreSQL+PostGIS, Private Storage + HMAC Signed URL).
- Python FastAPI AI chạy nội bộ riêng biệt qua `X-Internal-Api-Key`, có fallback an toàn khi AI mất kết nối.

## Slide 7: Kịch bản Demo Trực tiếp
- Đăng nhập → Khai báo vườn → Calculator C# (75.2436 tCO2 tham khảo cho 1 ha Dừa Ta 10 năm) → Upload PDF → AI Auditor trích xuất + Reviewer duyệt → HTX & RFQ.

## Slide 8: Tính khả thi & Mô hình Triển khai
- Web-first qua Docker Compose + Nginx + HTTPS; Android APK dùng chung REST API hiện trường (GPS + Camera).
- Chi phí vận hành gọn nhẹ, phù hợp HTX và doanh nghiệp nông nghiệp ĐBSCL.

## Slide 9: Tác động Xã hội, Môi trường & Kinh tế
- Minh bạch hóa dữ liệu cho nông hộ và HTX vùng dừa Bến Tre - Vĩnh Long.
- Giảm chi phí chuẩn bị hồ sơ dự án ban đầu; sẵn sàng nền tảng dữ liệu mở rộng sang phụ phẩm dừa (xơ, mụn, gáo).

## Slide 10: Lộ trình Phát triển (P0 → P1 → P2)
- **P0 (MVP)**: Farm Registry, Calculator CTU 2015, AI Auditor, HTX Dashboard, Directory/RFQ, Android APK.
- **P1**: Quản lý phụ phẩm dừa kinh tế tuần hoàn, xuất báo cáo PDF, đa ngôn ngữ Anh/Việt.
- **P2**: Hợp tác viện/trường hiệu chuẩn mô hình sinh khối Tier 2/3 địa phương, MRV định kỳ, kết nối registry khi được cấp phép.
