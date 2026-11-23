# Register Dữ liệu Khoa học & Tài liệu Tham chiếu (Scientific Data Source Register)

Tất cả hệ số, ngưỡng tham khảo và hướng dẫn phương pháp luận trong **CocoCarbon AI** đều phải truy vết tới nguồn gốc cụ thể dưới đây. Nền tảng **không** trộn lẫn hệ số khác đơn vị, **không** nội suy tuyến tính giữa các cấp tuổi, và **không** trình bày ước tính tham khảo như tín chỉ carbon đã phát hành hay lượng hấp thụ hằng năm.

---

## 1. Bảng nguồn tham chiếu chính thức

| Mã nguồn | Tài liệu / Tác giả / Năm | Giá trị / Nội dung chính được sử dụng | Giới hạn bắt buộc trong CocoCarbon AI |
| :--- | :--- | :--- | :--- |
| **[R1]** | Nguyễn Thị Thanh Trúc & Lê Anh Tuấn (2015). *"Ước lượng khả năng hấp thụ CO2 của cây dừa qua sinh khối tại huyện Giồng Trôm, tỉnh Bến Tre"*. Tạp chí Khoa học Đại học Cần Thơ, Số chuyên đề Môi trường và Biến đổi khí hậu, tr. 193–199. | 4 hệ số tham khảo (tCO2/ha):<br>• Dừa cao (Dừa Ta), 4 năm: **24.5180** tCO2/ha<br>• Dừa cao (Dừa Ta), 10 năm: **75.2436** tCO2/ha<br>• Dừa thấp (Xiêm xanh), 4 năm: **20.4583** tCO2/ha<br>• Dừa thấp (Xiêm xanh), 10 năm: **69.9189** tCO2/ha | 4 ô tiêu chuẩn 10m×10m, 3 cây/ô (12 cây mẫu) tại Giồng Trôm; rễ ước tính qua R/S = 1/5 (không đào trực tiếp); carbon fraction 0.47–0.50 (IPCC 2003) đã được tác giả tính sẵn vào giá trị tCO2/ha (không nhân lại). Không theo dõi tăng trưởng 1 năm → cấm diễn giải là tCO2/ha/năm và cấm lấy `(age10 - age4)/6`. |
| **[R1a]** | Trang bài báo & metadata — Tạp chí Khoa học Đại học Cần Thơ | Xác nhận tác giả, số tạp chí, năm xuất bản 2015 và trích dẫn chuẩn. | Chỉ dùng cho trích dẫn nguồn học thuật. |
| **[R2]** | Cục Đổi mới sáng tạo, Chuyển đổi xanh và Khuyến công (02/07/2024). *"Thực trạng canh tác dừa tại tỉnh Bến Tre"*. | Khảo sát 150 hộ tại 5 huyện (Châu Thành, Giồng Trôm, Ba Tri, Thạnh Phú, Bình Đại); mật độ trồng quan sát **160–240 cây/ha**. | Chỉ là dữ liệu bối cảnh nông học mô tả; không phải tiêu chuẩn chứng nhận, không dùng để hiệu chỉnh ngược hệ số [R1] và không tự động đánh dấu vườn hợp lệ/không hợp lệ. |
| **[R3]** | IPCC (2006). *2006 IPCC Guidelines for National Greenhouse Gas Inventories*, Vol. 4, Ch. 5 (Cropland), Table 5.3. | Mục *Coconut - SE Asia*: **196 tonnes/ha** (above-ground dry biomass). | Đây là **sinh khối khô trên mặt đất (tấn chất khô/ha)**, không phải tCO2/ha, không phải mức hấp thụ hằng năm, không phải tín chỉ. Tuyệt đối không ghép/trộn với hệ số [R1]. |
| **[R4]** | IPCC (2019). *2019 Refinement to the 2006 IPCC Guidelines*, Vol. 4, Ch. 5 (Cropland). | Xem cây dừa là perennial woody cropland; hướng dẫn phương pháp gain-loss và stock-difference; Tier 1–3; Table 5.3 cập nhật không có hàng hệ số riêng cho dừa. | Định hướng nâng cấp Tier 2 (hệ số địa phương Bến Tre/Vĩnh Long) và Tier 3 (mô hình hiệu chuẩn + quan sát độc lập). Dùng song song với IPCC 2006 [R4a]. |
| **[R4a]** | IPCC Task Force on National GHG Inventories — *2019 Refinement Vol. 4 landing page*. | Xác nhận bản 2019 bổ sung cùng bản 2006, không thay thế hoàn toàn. | Tài liệu định hướng khung kiểm kê quốc gia. |
| **[R5]** | Verra (2025). *VM0042 Methodology for Improved Agricultural Land Management, v2.2* (có hiệu lực 21/10/2025). | Khung phương pháp cải thiện quản lý đất nông nghiệp, giảm phát thải & loại bỏ SOC trong phạm vi áp dụng cụ thể. | Không tự động áp dụng cho sinh khối thân dừa; phải đáp ứng đầy đủ scope, baseline, additionality, leakage, monitoring, verification. |
| **[R6]** | Verra (11/06/2026). *Corrections and Clarifications to IALM Methodology (VM0042)*. | Bổ sung hiệu đính/làm rõ khi áp dụng VM0042 v2.2. | Bắt buộc kiểm tra nếu dự án chọn VM0042. |
| **[R7]** | Gold Standard (08/09/2025). *Soil Organic Carbon Model Requirements and Guidelines, v1.0*. | Yêu cầu về chọn mô hình SOC, dữ liệu, hiệu chuẩn, validation, bất định và xác minh theo thời gian. | Chỉ áp dụng cho nhánh carbon hữu cơ đất (SOC), không phải phương trình sinh khối toàn cây dừa. |
| **[R8]** | Mugasha & Zahabu (2026). *"Allometric models for estimating above- and belowground biomass of Cocos nucifera L. at Kisarawe and Mkuranga Districts in Tanzania"*, Agroforestry Systems. | Minh chứng có thể xây dựng phương trình allometric đặc thù loài dừa theo DBH và chiều cao. | Hệ số Tanzania **không được** chuyển thẳng sang vùng dừa Bến Tre / Vĩnh Long nếu chưa lấy mẫu địa phương và validation độc lập. |
| **[R9]** | Firebase Documentation — *Authenticate with Google on Android*. | Credential Manager + Google ID option + Firebase Auth trên Kotlin/Android. | Quy chuẩn kỹ thuật xác thực Android. |
| **[R10]** | Firebase Documentation — *Verify ID Tokens*. | Client gửi Firebase ID Token qua HTTPS Authorization Bearer; backend xác minh token. | Quy chuẩn bảo mật xác thực API. |
| **[R11]** | Firebase Admin SDK .NET Reference — `VerifyIdTokenAsync`. | API C# xác minh Firebase ID token và trích xuất UID đáng tin cậy. | Nguồn sự thật xác thực tại C# ASP.NET Core. |
| **[R12]** | Chính phủ Việt Nam / Cổng TTĐT (21/01/2026). *Thí điểm sàn giao dịch carbon trong nước (bối cảnh Nghị định 29/2026/NĐ-CP)*. | Bối cảnh pháp lý thị trường carbon nội địa Việt Nam. | CocoCarbon AI không tự nhận là sàn giao dịch hay registry chính thức; giao dịch thực tế phải tuân thủ pháp luật hiện hành. |

---

## 2. Phân biệt khái niệm bắt buộc trong toàn bộ sản phẩm

1. **Biomass** (Sinh khối tươi/khô) ≠ **Carbon stock** (Trữ lượng C tại 1 thời điểm) ≠ **CO2e stock** (`C × 44/12` chỉ sau khi có trữ lượng C hợp lệ).
2. **CO2e stock tham khảo** ≠ **Annual stock change / removal** (thay đổi trữ lượng giữa 2 thời điểm).
3. **Net project removals/reductions** (sau baseline, additionality, leakage, uncertainty, safeguards) ≠ **Issued credit** (tín chỉ đã phát hành bởi registry) ≠ **Retired credit** (tín chỉ đã hủy, không thể bán lại).

---

## 3. Địa giới hành chính và địa danh

- **Địa danh lịch sử / vùng sản xuất**: Vùng dừa **Bến Tre** (ví dụ các khu vực nghiên cứu/khảo sát cũ: Giồng Trôm, Châu Thành, Ba Tri, Thạnh Phú, Bình Đại).
- **Địa giới hành chính hiện hành**: Tỉnh **Vĩnh Long** (lưu trường `Province = "Vĩnh Long"`, `Commune = ...`, đồng thời giữ trường `HistoricalRegion = "Vùng dừa Bến Tre"` để bảo toàn ngữ cảnh khoa học).
