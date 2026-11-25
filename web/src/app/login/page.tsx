"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { ShieldCheck, Sprout, UserCheck } from "lucide-react";
import { apiFetch, setStoredSession } from "@/lib/api";
import { isFirebaseWebConfigured, signInWithEmailPasswordFirebase, signInWithGoogleFirebase } from "@/lib/firebase";
import type { UserProfileDto } from "@/lib/types";

const DEMO_ROLES = [
  { token: "dev-demo:farmer", label: "Nông hộ (Farmer) [DEMO]", desc: "Tạo vườn, upload chứng từ, tính calculator" },
  { token: "dev-demo:coop", label: "Quản lý HTX [DEMO]", desc: "Xem vùng nguyên liệu được ủy quyền" },
  { token: "dev-demo:developer", label: "Project Developer [DEMO]", desc: "Quản lý hồ sơ dự án & phản hồi RFQ" },
  { token: "dev-demo:buyer", label: "Doanh nghiệp Buyer [DEMO]", desc: "Lọc dự án & gửi RFQ tìm hiểu" },
  { token: "dev-demo:reviewer", label: "Reviewer nội bộ [DEMO]", desc: "Duyệt / từ chối đề xuất AI Auditor" },
  { token: "dev-demo:admin", label: "Admin Hệ thống [DEMO]", desc: "Quản lý role, hệ số CTU, audit log" },
];

export default function LoginPage() {
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const firebaseReady = isFirebaseWebConfigured();
  const showDemo = process.env.NEXT_PUBLIC_SHOW_DEMO_MODE !== "false";

  async function handleGoogleLogin() {
    setLoading(true);
    setError(null);
    try {
      const auth = await signInWithGoogleFirebase();
      setStoredSession(auth.idToken, "firebase");
      await apiFetch<UserProfileDto>("/api/v1/me");
      router.push("/dashboard");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Đăng nhập Google thất bại.");
    } finally {
      setLoading(false);
    }
  }

  async function handleEmailLogin(e: React.FormEvent) {
    e.preventDefault();
    if (!email.trim() || !password) return;
    setLoading(true);
    setError(null);
    try {
      const auth = await signInWithEmailPasswordFirebase(email, password);
      setStoredSession(auth.idToken, "firebase");
      await apiFetch<UserProfileDto>("/api/v1/me");
      router.push("/dashboard");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Đăng nhập Email/Mật khẩu Firebase thất bại.");
    } finally {
      setLoading(false);
    }
  }

  async function handleDemoLogin(devToken: string) {
    setLoading(true);
    setError(null);
    try {
      setStoredSession(devToken, "demo");
      await apiFetch<UserProfileDto>("/api/v1/me");
      router.push("/dashboard");
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : "Không thể kết nối máy chủ C# API. Kiểm tra C# API đang chạy và FIREBASE_AUTH_DEV_ALLOW_MOCK=true."
      );
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-[#f6f8f5] p-4">
      <div className="w-full max-w-xl rounded-2xl border border-[#d8e2da] bg-white p-6 shadow-xs sm:p-8">
        <Link href="/" className="inline-flex items-center gap-2 text-sm font-semibold text-[#15803d]">
          <Sprout className="h-5 w-5" /> CocoCarbon AI
        </Link>

        <h1 className="mt-3 text-2xl font-bold text-[#123c29]">Đăng nhập hệ thống</h1>
        <p className="mt-1 text-sm text-[#526158]">
          Sử dụng chung Firebase Authentication với ứng dụng Android. Vai trò và quyền truy cập được kiểm soát hoàn toàn bởi C# API.
        </p>

        {error && (
          <div className="mt-4 rounded-xl border border-red-300 bg-red-50 p-3 text-sm text-red-900">
            {error}
          </div>
        )}

        <div className="mt-6 rounded-xl border border-[#d8e2da] bg-[#f9fbf9] p-4">
          <div className="flex items-center justify-between">
            <h2 className="text-sm font-bold text-[#123c29]">Đăng nhập Google (Firebase Auth)</h2>
            <span className="rounded bg-stone-200 px-2 py-0.5 text-[11px] font-medium">
              {firebaseReady ? "Đã cấu hình" : "Chưa cấu hình NEXT_PUBLIC_FIREBASE_*"}
            </span>
          </div>
          <button
            type="button"
            disabled={loading || !firebaseReady}
            onClick={handleGoogleLogin}
            className="mt-3 flex w-full items-center justify-center gap-2 rounded-xl bg-[#123c29] px-4 py-3 text-sm font-semibold text-white hover:bg-[#15803d] disabled:cursor-not-allowed disabled:opacity-50"
          >
            <ShieldCheck className="h-4 w-4" />
            {loading ? "Đang xác thực..." : "Đăng nhập bằng Google"}
          </button>
          {!firebaseReady && (
            <p className="mt-2 text-xs text-[#526158]">
              Điền biến môi trường Firebase Web (`NEXT_PUBLIC_FIREBASE_API_KEY`, `AUTH_DOMAIN`, `PROJECT_ID`, `APP_ID`) vào build args để bật đăng nhập Firebase thật.
            </p>
          )}
          <form onSubmit={handleEmailLogin} className="mt-4 border-t border-[#e2eae3] pt-3 space-y-2">
            <p className="text-xs font-semibold text-[#123c29]">Hoặc đăng nhập Email / Mật khẩu (Tài khoản dùng thử Firebase Production)</p>
            <div className="grid gap-2 sm:grid-cols-2">
              <input
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="Email tài khoản Firebase"
                disabled={loading || !firebaseReady}
                className="rounded-lg border border-[#d8e2da] bg-white px-3 py-2 text-xs"
              />
              <input
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="Mật khẩu"
                disabled={loading || !firebaseReady}
                className="rounded-lg border border-[#d8e2da] bg-white px-3 py-2 text-xs"
              />
            </div>
            <button
              type="submit"
              disabled={loading || !firebaseReady || !email.trim() || !password}
              className="rounded-lg border border-[#123c29] px-3.5 py-2 text-xs font-semibold text-[#123c29] hover:bg-[#e8f5ec] disabled:opacity-50"
            >
              Đăng nhập Email Firebase
            </button>
          </form>
        </div>

        {showDemo && (
          <div className="mt-6 rounded-xl border border-amber-300 bg-amber-50/60 p-4">
            <div className="flex items-center gap-2 text-sm font-bold text-amber-950">
              <UserCheck className="h-4 w-4" />
              Chế độ Demo phát triển cục bộ (chỉ hoạt động khi Development + FIREBASE_AUTH_DEV_ALLOW_MOCK=true)
            </div>
            <div className="mt-3 grid gap-2 sm:grid-cols-2">
              {DEMO_ROLES.map((item) => (
                <button
                  key={item.token}
                  type="button"
                  disabled={loading}
                  onClick={() => handleDemoLogin(item.token)}
                  className="rounded-xl border border-amber-300 bg-white p-3 text-left transition hover:border-[#15803d] hover:bg-[#e8f5ec]"
                >
                  <p className="text-xs font-bold text-[#123c29]">{item.label}</p>
                  <p className="mt-0.5 text-[11px] text-[#526158]">{item.desc}</p>
                </button>
              ))}
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
