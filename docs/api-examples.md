# CocoCarbon AI — API Reference & Request/Response Samples

OpenAPI / Swagger UI (khi chạy C# API): `/swagger`

## 1. Xác thực Firebase ID Token (`GET /api/v1/me`)
```bash
curl -H "Authorization: Bearer <FIREBASE_ID_TOKEN>" https://your-domain/api/v1/me
```
Response `200 OK`:
```json
{
  "id": "uuid",
  "firebaseUid": "firebase-uid",
  "email": "farmer@example.com",
  "displayName": "Nguyễn Văn An",
  "roles": [{ "roleCode": "Farmer", "approvalStatus": "Approved" }]
}
```

## 2. Coconut Carbon Calculator (`POST /api/v1/carbon-calculator/estimate`)
Ví dụ chuẩn Dừa Ta 10 năm tuổi, 1.0 ha:
```bash
curl -X POST https://your-domain/api/v1/carbon-calculator/estimate \
  -H "Authorization: Bearer <FIREBASE_ID_TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{"areaHa":1.0,"treeCount":200,"variety":"TallDuaTa","treeAgeYears":10}'
```
Response `200 OK`:
```json
{
  "estimatedCO2EquivalentT": 75.2436,
  "observedTreeDensityPerHa": 200.00,
  "formulaVersion": "CTU-2015-AREA-V1",
  "sourceCitation": "R1: Nguyễn Thị Thanh Trúc & Lê Anh Tuấn (2015) - ..."
}
```
Nếu tuổi khác `4` hoặc `10` (vd: `7`), API trả `422 Unprocessable Entity` với `errorCode: "UNSUPPORTED_AGE_CLASS"` — tuyệt đối không nội suy.

## 3. Upload tài liệu riêng tư & AI Carbon Auditor
- `POST /api/v1/farms/{id}/files` (multipart `file` PDF/JPEG/PNG; kiểm tra MIME + magic bytes + giới hạn dung lượng + SHA-256).
- `GET /api/v1/files/{fileId}/signed-url` -> trả liên kết HMAC tạm thời `/api/v1/files/{fileId}/download?uid=...&expires=...&sig=...`.
- `POST /api/v1/ai/document-analysis` -> C# gọi Python FastAPI nội bộ (`X-Internal-Api-Key`), lưu `AiAnalysis` (`ReviewStatus: PendingReview`), trả `extractedFields`, `findings`, `duplicates`, `evidence`, `readinessRubric`.
- `POST /api/v1/ai/analyses/{id}/review` -> Chỉ `Reviewer` / `Admin` phê duyệt (`Accepted` / `Edited` / `Rejected`).

## 4. Hợp tác xã, Dự án, RFQ & Admin
- `GET /api/v1/cooperatives/{id}/dashboard`
- `GET/POST /api/v1/projects`, `GET/PATCH /api/v1/projects/{id}`, `POST /api/v1/projects/{id}/farms`
- `GET/POST /api/v1/rfqs`, `POST /api/v1/rfqs/{id}/respond`
- `GET /api/v1/admin/users`, `POST /api/v1/admin/users/{id}/roles`, `GET/POST /api/v1/admin/research-factors`, `GET /api/v1/admin/review-queue`, `GET /api/v1/admin/audit-logs`
