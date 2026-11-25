"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { KeyRound, ShieldCheck, Sprout, UserCheck } from "lucide-react";
import { apiFetch, setStoredSession } from "@/lib/api";
import {
  isFirebaseWebConfigured,
  setFirebaseRuntimeConfig,
  signInWithEmailPasswordFirebase,
  signInWithGoogleFirebase,
  type FirebasePublicConfig,
} from "@/lib/firebase";
import type { UserProfileDto } from "@/lib/types";

const DEMO_ROLES = [
  { token: "dev-demo:farmer", label: "Nông hộ (Farmer) [DEMO]", desc: "Tạo vườn, sổ thu chi, tính calculator & hỏi AI" },
  { token: "dev-demo:coop", label: "Quản lý HTX [DEMO]", desc: "Xem vùng nguyên liệu được ủy quyền" },
  { token: "dev-demo:developer", label: "Project Developer [DEMO]", desc: "Quản lý hồ sơ dự án & phản hồi RFQ" },
  { token: "dev-demo:buyer", label: "Doanh nghiệp Buyer [DEMO]", desc: "Lọc dự án & gửi RFQ tìm hiểu" },
  { token: "dev-demo:reviewer", label: "Reviewer nội bộ [DEMO]", desc: "Duyệt / từ chối đề xuất AI Auditor" },
  { token: "dev-demo:admin", label: "Admin Hệ thống [DEMO]", desc: "Quản lý role, hệ số CTU, audit log" },
];

interface LoginClientProps {
  runtimeFirebaseConfig: FirebasePublicConfig;
  showDemoMode: boolean;
}

export function LoginClient({ runtimeFirebaseConfig, showDemoMode }: LoginClientProps) {
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setFirebaseRuntimeConfig(runtimeFirebaseConfig);
  }, [runtimeFirebaseConfig]);

  const firebaseReady = isFirebaseWebConfigured(runtimeFirebaseConfig);

  async function handleGoogleLogin() {
    setLoading(true);
    setError(null);
    try {
      const auth = await signInWithGoogleFirebase(runtimeFirebaseConfig);
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
      const auth = await signInWithEmailPasswordFirebase(email, password, runtimeFirebaseConfig);
      setStoredSession(auth.idToken, "firebase");
      await apiFetch<UserProfileDto>("/api/v1/me");
      router.push("/dashboard");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Email hoặc mật khẩu chưa đúng.");
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
          : "Không thể kết nối máy chủ API. Vui lòng kiểm tra dịch vụ backend đang hoạt động."
      );
    } finally {
      setLoading(false);
    }
  }

  const firebaseCard = (
    <div className="rounded-xl border border-[#d8e2da] bg-[#f9fbf9] p-4 space-y-3">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h2 className="flex items-center gap-1.5 text-sm font-bold text-[#123c29]">
          <KeyRound className="h-4 w-4 text-[#15803d]" />
          Đăng nhập Firebase Authentication
        </h2>
        <span
          className={`rounded-full px-2.5 py-0.5 text-[11px] font-semibold ${
            firebaseReady ? "bg-emerald-100 text-emerald-900" : "bg-stone-200 text-stone-700"
          }`}
        >
          {firebaseReady ? "Đã kết nối" : "Chế độ cục bộ"}
        </span>
      </div>

      {firebaseReady ? (
        <>
          <button
            type="button"
            disabled={loading}
            onClick={handleGoogleLogin}
            className="flex w-full items-center justify-center gap-2 rounded-xl bg-[#123c29] px-4 py-3 text-sm font-semibold text-white transition hover:bg-[#15803d] disabled:opacity-50"
          >
            <ShieldCheck className="h-4 w-4" />
            {loading ? "Đang xác thực..." : "Đăng nhập bằng Google"}
          </button>

          <form onSubmit={handleEmailLogin} className="border-t border-[#e2eae3] pt-3 space-y-2.5">
            <p className="text-xs font-semibold text-[#123c29]">
              Hoặc đăng nhập bằng Email &amp; Mật khẩu
            </p>
            <div className="grid gap-2 sm:grid-cols-2">
              <input
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="Email tài khoản"
                disabled={loading}
                className="rounded-lg border border-[#d8e2da] bg-white px-3 py-2 text-xs"
              />
              <input
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="Mật khẩu"
                disabled={loading}
                className="rounded-lg border border-[#d8e2da] bg-white px-3 py-2 text-xs"
              />
            </div>
            <button
              type="submit"
              disabled={loading || !email.trim() || !password}
              className="rounded-lg border border-[#123c29] px-3.5 py-2 text-xs font-semibold text-[#123c29] transition hover:bg-[#e8f5ec] disabled:opacity-50"
            >
              Đăng nhập bằng Email
            </button>
          </form>
        </>
      ) : (
        <p className="text-xs text-[#526158] leading-relaxed">
          Máy chủ đang chạy ở chế độ thử nghiệm cục bộ và chưa gắn khóa dự án Firebase Web.
          {showDemoMode
            ? " Vui lòng chọn trực tiếp một vai trò dùng thử ở khung phía trên để trải nghiệm đầy đủ tính năng."
            : " Quản trị viên cần cấu hình thông số Firebase Web để kích hoạt đăng nhập."}
        </p>
      )}
    </div>
  );

  const demoCard = showDemoMode ? (
    <div className="rounded-xl border border-amber-300 bg-amber-50/70 p-4">
      <div className="flex items-center gap-2 text-sm font-bold text-amber-950">
        <UserCheck className="h-4 w-4 text-amber-800" />
        Chọn vai trò trải nghiệm nhanh (Môi trường Demo)
      </div>
      <p className="mt-1 text-xs text-amber-900/85">
        Mỗi vai trò được phân quyền RBAC độc lập ở backend để kiểm thử toàn bộ quy trình từ nông hộ đến rà soát viên.
      </p>
      <div className="mt-3 grid gap-2 sm:grid-cols-2">
        {DEMO_ROLES.map((item) => (
          <button
            key={item.token}
            type="button"
            disabled={loading}
            onClick={() => handleDemoLogin(item.token)}
            className="rounded-xl border border-amber-300 bg-white p-3 text-left transition hover:border-[#15803d] hover:bg-[#e8f5ec] disabled:opacity-50"
          >
            <p className="text-xs font-bold text-[#123c29]">{item.label}</p>
            <p className="mt-0.5 text-[11px] text-[#526158]">{item.desc}</p>
          </button>
        ))}
      </div>
    </div>
  ) : null;

  return (
    <div className="flex min-h-screen items-center justify-center bg-[#f6f8f5] p-4">
      <div className="w-full max-w-xl rounded-2xl border border-[#d8e2da] bg-white p-6 shadow-xs sm:p-8">
        <Link href="/" className="inline-flex items-center gap-2 text-sm font-semibold text-[#15803d]">
          <Sprout className="h-5 w-5" /> CocoCarbon AI
        </Link>

        <h1 className="mt-3 text-2xl font-bold text-[#123c29]">Đăng nhập hệ thống</h1>
        <p className="mt-1 text-sm text-[#526158]">
          Sử dụng chung hệ thống xác thực với ứng dụng Android. Quyền truy cập được kiểm soát chặt chẽ tại máy chủ API.
        </p>

        {error && (
          <div className="mt-4 rounded-xl border border-red-300 bg-red-50 p-3 text-sm text-red-900">
            {error}
          </div>
        )}

        <div className="mt-6 space-y-5">
          {firebaseReady ? (
            <>
              {firebaseCard}
              {demoCard}
            </>
          ) : (
            <>
              {demoCard}
              {firebaseCard}
            </>
          )}
        </div>
      </div>
    </div>
  );
}
