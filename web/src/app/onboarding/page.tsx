"use client";

import { useEffect, useState } from "react";
import { AppShell } from "@/components/AppShell";
import { apiFetch } from "@/lib/api";
import type { UserProfileDto } from "@/lib/types";

const CONSENT_TYPES = [
  { type: "GpsLocation", label: "Thu thập tọa độ GPS / ranh giới vườn dừa" },
  { type: "FieldPhoto", label: "Lưu trữ và hiển thị ảnh hiện trường vườn dừa trong phạm vi được ủy quyền" },
  { type: "AiThirdPartyProcessing", label: "Phân tích chứng từ qua AI nội bộ / mô hình Gemini (không lưu dữ liệu vào mục đích huấn luyện)" },
  { type: "CooperativeDataSharing", label: "Chia sẻ tổng hợp diện tích với Hợp tác xã / Dự án khi được xác nhận" },
  { type: "TermsPrivacy", label: "Điều khoản sử dụng & Chính sách dữ liệu cá nhân CocoCarbon AI (v1.0-2026-11)" },
];

export default function OnboardingPage() {
  const [me, setMe] = useState<UserProfileDto | null>(null);
  const [displayName, setDisplayName] = useState("");
  const [phone, setPhone] = useState("");
  const [requestedRole, setRequestedRole] = useState("CooperativeManager");
  const [justification, setJustification] = useState("");
  const [statusMsg, setStatusMsg] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function loadMe() {
    try {
      const data = await apiFetch<UserProfileDto>("/api/v1/me");
      setMe(data);
      setDisplayName(data.displayName);
      setPhone(data.phone || "");
    } catch (e) {
      setError(e instanceof Error ? e.message : "Vui lòng đăng nhập trước.");
    }
  }

  useEffect(() => {
    loadMe();
  }, []);

  async function saveProfile(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setStatusMsg(null);
    try {
      await apiFetch("/api/v1/me/profile", {
        method: "PUT",
        body: JSON.stringify({ displayName, phone }),
      });
      setStatusMsg("Đã cập nhật hồ sơ cá nhân.");
      loadMe();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Lỗi cập nhật hồ sơ.");
    }
  }

  async function recordConsent(consentType: string, textShown: string, granted: boolean) {
    setError(null);
    setStatusMsg(null);
    try {
      await apiFetch("/api/v1/me/consents", {
        method: "POST",
        body: JSON.stringify({
          consentType,
          granted,
          policyVersion: "v1.0-2026-11",
          textShown,
        }),
      });
      setStatusMsg(`Đã ghi nhận đồng thuận (${consentType} = ${granted ? "Đồng ý" : "Từ chối"}).`);
      loadMe();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Lỗi ghi nhận consent.");
    }
  }

  async function submitRoleRequest(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setStatusMsg(null);
    try {
      const res = await apiFetch<{ message: string }>("/api/v1/me/role-requests", {
        method: "POST",
        body: JSON.stringify({ requestedRole, justification }),
      });
      setStatusMsg(res.message);
      loadMe();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Lỗi gửi yêu cầu vai trò.");
    }
  }

  async function handleExportMyData() {
    setError(null);
    try {
      const data = await apiFetch<unknown>("/api/v1/me/data-export");
      const blob = new Blob([JSON.stringify(data, null, 2)], { type: "application/json" });
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = "cococarbon-my-data.json";
      a.click();
      URL.revokeObjectURL(url);
      setStatusMsg("Đã tải xuống bản sao dữ liệu cá nhân (JSON).");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không xuất được dữ liệu cá nhân.");
    }
  }

  async function handleRequestDeletion() {
    setError(null);
    try {
      const res = await apiFetch<{ message: string }>("/api/v1/me/deletion-request", {
        method: "POST",
        body: JSON.stringify({ displayName: "Yêu cầu xóa dữ liệu cá nhân từ Web" }),
      });
      setStatusMsg(res.message);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Lỗi gửi yêu cầu xóa dữ liệu.");
    }
  }

  return (
    <AppShell title="Hồ sơ Cá nhân, Vai trò & Đồng thuận" subtitle="Quản lý thông tin người dùng, consent dữ liệu hiện trường/AI và yêu cầu vai trò (Admin duyệt)">
      {error && <div className="mb-4 rounded-xl border border-red-300 bg-red-50 p-3 text-sm text-red-900">{error}</div>}
      {statusMsg && <div className="mb-4 rounded-xl border border-emerald-300 bg-emerald-50 p-3 text-sm text-emerald-900">{statusMsg}</div>}

      <div className="grid gap-6 lg:grid-cols-2">
        <form onSubmit={saveProfile} className="rounded-xl border border-[#d8e2da] bg-white p-5 space-y-4">
          <h2 className="text-base font-bold text-[#123c29]">Thông tin cá nhân</h2>
          <div>
            <label className="block text-xs font-semibold text-[#526158]">Tên hiển thị</label>
            <input
              value={displayName}
              onChange={(e) => setDisplayName(e.target.value)}
              className="mt-1 w-full rounded-lg border border-[#d8e2da] px-3 py-2 text-sm"
              required
            />
          </div>
          <div>
            <label className="block text-xs font-semibold text-[#526158]">Số điện thoại</label>
            <input
              value={phone}
              onChange={(e) => setPhone(e.target.value)}
              className="mt-1 w-full rounded-lg border border-[#d8e2da] px-3 py-2 text-sm"
            />
          </div>
          <button type="submit" className="rounded-lg bg-[#123c29] px-4 py-2 text-sm font-semibold text-white hover:bg-[#15803d]">
            Lưu hồ sơ
          </button>
        </form>

        <form onSubmit={submitRoleRequest} className="rounded-xl border border-[#d8e2da] bg-white p-5 space-y-4">
          <h2 className="text-base font-bold text-[#123c29]">Yêu cầu nâng vai trò (Cần Admin duyệt)</h2>
          <p className="text-xs text-[#526158]">
            Bảo mật: Hệ thống không tự cấp quyền chỉ vì người dùng chọn trên giao diện. Mọi yêu cầu được lưu ở trạng thái PendingApproval.
          </p>
          <div>
            <label className="block text-xs font-semibold text-[#526158]">Vai trò đề nghị</label>
            <select
              value={requestedRole}
              onChange={(e) => setRequestedRole(e.target.value)}
              className="mt-1 w-full rounded-lg border border-[#d8e2da] px-3 py-2 text-sm"
            >
              <option value="CooperativeManager">CooperativeManager (Quản lý HTX)</option>
              <option value="ProjectDeveloper">ProjectDeveloper (Phát triển dự án)</option>
              <option value="Buyer">Buyer (Doanh nghiệp tìm hiểu)</option>
              <option value="Reviewer">Reviewer (Chuyên viên rà soát)</option>
            </select>
          </div>
          <div>
            <label className="block text-xs font-semibold text-[#526158]">Lý do / Đơn vị công tác</label>
            <input
              value={justification}
              onChange={(e) => setJustification(e.target.value)}
              placeholder="VD: Quản lý HTX Dừa Giồng Trôm"
              className="mt-1 w-full rounded-lg border border-[#d8e2da] px-3 py-2 text-sm"
            />
          </div>
          <button type="submit" className="rounded-lg bg-[#15803d] px-4 py-2 text-sm font-semibold text-white hover:bg-[#123c29]">
            Gửi yêu cầu nâng vai trò
          </button>
          {me && (
            <div className="pt-2 text-xs text-[#526158]">
              Vai trò hiện tại:{" "}
              {me.roles.map((r) => `${r.roleCode} (${r.approvalStatus})`).join(", ")}
            </div>
          )}
        </form>
      </div>

      <section className="mt-6 rounded-xl border border-[#d8e2da] bg-white p-5">
        <h2 className="text-base font-bold text-[#123c29]">Quản lý đồng thuận dữ liệu (Consent Records)</h2>
        <div className="mt-4 divide-y divide-[#e6ede7]">
          {CONSENT_TYPES.map((c) => {
            const latest = me?.consents.filter((x) => x.consentType === c.type).slice(-1)[0];
            return (
              <div key={c.type} className="flex flex-col gap-2 py-3 sm:flex-row sm:items-center sm:justify-between">
                <div>
                  <p className="text-sm font-medium">{c.label}</p>
                  <p className="text-xs text-[#526158]">
                    Trạng thái:{" "}
                    {latest ? (latest.granted ? "Đã đồng ý" : "Đã từ chối") : "Chưa ghi nhận"}
                  </p>
                </div>
                <div className="flex gap-2">
                  <button
                    type="button"
                    onClick={() => recordConsent(c.type, c.label, true)}
                    className="rounded-lg bg-[#e8f5ec] px-3 py-1.5 text-xs font-semibold text-[#15803d] hover:bg-[#d1ebd9]"
                  >
                    Đồng ý
                  </button>
                  <button
                    type="button"
                    onClick={() => recordConsent(c.type, c.label, false)}
                    className="rounded-lg border border-[#d8e2da] px-3 py-1.5 text-xs font-medium text-stone-700 hover:bg-stone-100"
                  >
                    Từ chối
                  </button>
                </div>
              </div>
            );
          })}
        </div>
      </section>

      <section className="mt-6 rounded-xl border border-[#d8e2da] bg-white p-5">
        <h2 className="text-base font-bold text-[#123c29]">Quyền dữ liệu cá nhân (Export / Deletion Request)</h2>
        <p className="mt-1 text-xs text-[#526158]">
          Người dùng có quyền tải bản sao toàn bộ dữ liệu hồ sơ vườn/đồng thuận của mình hoặc gửi yêu cầu xóa dữ liệu cá nhân.
        </p>
        <div className="mt-3 flex flex-wrap gap-3">
          <button type="button" onClick={handleExportMyData} className="rounded-lg border border-[#123c29] px-4 py-2 text-xs font-semibold text-[#123c29] hover:bg-[#e8f5ec]">
            Tải xuống dữ liệu cá nhân (JSON)
          </button>
          <button type="button" onClick={handleRequestDeletion} className="rounded-lg border border-red-300 px-4 py-2 text-xs font-semibold text-red-800 hover:bg-red-50">
            Gửi yêu cầu xóa dữ liệu
          </button>
        </div>
      </section>
    </AppShell>
  );
}
