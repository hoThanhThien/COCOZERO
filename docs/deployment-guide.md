# Hướng dẫn Triển khai & Vận hành (Deployment Guide)

## 1. Thứ tự triển khai khuyến nghị: Web trước → Android sau
1. Provision VPS Ubuntu (2+ vCPU, 4GB RAM) + domain DNS trỏ về IP VPS.
2. Cài Docker & Docker Compose; sao chép `.env.example` thành `.env` và điền toàn bộ giá trị thật (tuyệt đối không commit `.env`).
3. Đặt Firebase Admin Service Account JSON ở `/run/secrets/firebase-service-account.json` (hoặc volume mount `ro`), đặt `FIREBASE_AUTH_DEV_ALLOW_MOCK=false`.
4. Khởi chạy stack bằng Docker Compose:
   ```bash
   docker compose up -d --build
   ```
   - Nginx lắng nghe port `80/443`, proxy `/` → `web:3000` và `/api/` → `api:8080`.
   - `postgres` (5432) và `ai-service` (8000) chỉ nằm trong network nội bộ `coco_internal`, không public port.
5. Bật HTTPS/TLS với Certbot (Let's Encrypt) hoặc Cloudflare Tunnel/Origin CA.
6. Trong **Firebase Console**:
   - Thêm domain production vào **Authentication → Settings → Authorized domains**.
   - Bật **Google Sign-In** provider.
   - Đăng ký Web OAuth Client & Android app (`com.cococarbon.app`) cùng Debug/Release SHA-1 & SHA-256 fingerprints.
   - Tải `google-services.json` đặt vào `android/app/google-services.json` (đã gitignore).

## 2. Backup PostgreSQL & Vận hành
```bash
docker compose exec postgres pg_dump -U "$POSTGRES_USER" "$POSTGRES_DB" > backup_$(date +%F).sql
```
- Health checks: `GET /health` và `GET /api/v1/health`.
- Logs: `docker compose logs -f api ai-service web nginx`.

## 3. Build Android APK trỏ về Production API
```bash
cd android
./gradlew assembleRelease -PapiBaseUrl=https://your-domain.example/ -PfirebaseWebClientId=YOUR_WEB_CLIENT_ID.apps.googleusercontent.com
```
APK nằm tại `android/app/build/outputs/apk/release/` (hoặc `debug/` cho bản alpha thử nghiệm).
