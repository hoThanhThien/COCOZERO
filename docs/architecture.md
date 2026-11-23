# Kiến trúc hệ thống CocoCarbon AI

## 1. Sơ đồ tổng thể và ranh giới dịch vụ

```
  Web Client (Next.js / TypeScript)          Android Native (Kotlin / Compose)
                 |                                        |
                 +----------- HTTPS / Bearer Token -------+
                                     |
                         Nginx Reverse Proxy (TLS)
                                     |
                     C# ASP.NET Core 8 Web API (Core Truth)
             Auth (Firebase VerifyIdTokenAsync) | RBAC | Ownership
             Farms | Calculator (4 CTU factors) | Projects | RFQ | Audit
                 |                                        |
        PostgreSQL + PostGIS                       Python FastAPI (Internal Only)
      Users / Farms / Projects                     Gemini Interactions API / PyMuPDF
      Calculations / AuditLogs                     Extraction / Anomaly / Curated RAG
                 |
      Private Storage (Local / S3 + HMAC Signed URLs)
```

### Quy tắc ranh giới
1. **C# ASP.NET Core 8 Web API** là nguồn sự thật nghiệp vụ duy nhất: xác minh Firebase ID Token, RBAC + kiểm tra quyền sở hữu đối tượng, tính toán `Coconut Carbon Calculator` bằng kiểu `decimal`, quản lý `ResearchFactors` theo phiên bản, lưu lịch sử tính toán bất biến, quản lý tệp riêng tư và điều phối AI.
2. **Python FastAPI AI Service** chỉ chạy ở mạng nội bộ (`ai-service:8000`), không expose ra Internet qua Nginx, xác thực với C# qua `X-Internal-Api-Key`, không ghi trực tiếp vào dữ liệu nghiệp vụ cốt lõi.
3. **Fallback an toàn**: Nếu Python AI Service hoặc Gemini API ngừng hoạt động hoặc quá thời gian chờ (`AI_SERVICE_TIMEOUT_SECONDS`), đăng ký vườn dừa, quản lý hồ sơ, tính toán carbon C# và tạo RFQ vẫn hoạt động bình thường.
4. **Dữ liệu không đáng tin cậy**: Mọi tệp PDF/ảnh tải lên được coi là untrusted data (không bao giờ là chỉ thị điều khiển prompt), được kiểm tra định dạng, kích thước, magic-bytes signature, băm SHA-256 và lưu ở storage riêng tư với URL ký tạm thời (signed URL).

## 2. Luồng xác thực Firebase chung (Web & Android)

1. Người dùng đăng nhập Google qua Firebase JS SDK (Web) hoặc Credential Manager + Firebase Auth (Android) trên **cùng một Firebase project**.
2. Client lấy `Firebase ID Token` và gửi kèm header `Authorization: Bearer <token>` tới `/api/v1/*`.
3. Backend C# gọi `FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token)` để lấy `uid` và `email` đáng tin cậy từ token đã ký, ánh xạ tới bảng `Users` + `UserRoles` trong PostgreSQL.
4. Backend **không bao giờ tin** `uid`, `email` hoặc `role` do client gửi trong request body. Việc nâng quyền `Admin` / `Reviewer` bị khóa trừ khi được `Admin` phê duyệt hoặc khớp `ADMIN_BOOTSTRAP_EMAILS` trong cấu hình máy chủ.
