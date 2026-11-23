"use client";

import { useEffect, useState } from "react";
import { ShieldCheck } from "lucide-react";
import { AppShell } from "@/components/AppShell";
import { apiFetch, getStoredToken } from "@/lib/api";

interface AdminUserRow {
  id: string;
  email: string;
  displayName: string;
  roles: { id: string; roleCode: string; approvalStatus: string }[];
}

interface AuditLogRow {
  id: string;
  actorEmail: string;
  action: string;
  entityType: string;
  entityId: string;
  createdAt: string;
}

export default function AdminConsolePage() {
  const [users, setUsers] = useState<AdminUserRow[]>([]);
  const [logs, setLogs] = useState<AuditLogRow[]>([]);
  const [msg, setMsg] = useState<string | null>(null);
  const [err, setErr] = useState<string | null>(null);

  async function load() {
    if (!getStoredToken()) return;
    try {
      const [u, l] = await Promise.all([
        apiFetch<AdminUserRow[]>("/api/v1/admin/users"),
        apiFetch<AuditLogRow[]>("/api/v1/admin/audit-logs?limit=30"),
      ]);
      setUsers(u);
      setLogs(l);
    } catch (e) {
      setErr(e instanceof Error ? e.message : "Chỉ tài khoản Admin mới được truy cập bảng quản trị.");
    }
  }

  useEffect(() => {
    load();
  }, []);

  async function approveRole(userId: string, roleCode: string) {
    setErr(null);
    setMsg(null);
    try {
      await apiFetch(`/api/v1/admin/users/${userId}/roles`, {
        method: "POST",
        body: JSON.stringify({ roleCode, approvalStatus: "Approved", scopeType: "Global" }),
      });
      setMsg(`Đã phê duyệt vai trò ${roleCode}.`);
      await load();
    } catch (e) {
      setErr(e instanceof Error ? e.message : "Không thể cập nhật vai trò.");
    }
  }

  return (
    <AppShell title="Quản trị Hệ thống & Audit Log" subtitle="Kiểm duyệt phân quyền người dùng, quản lý phiên bản hệ số nghiên cứu và nhật ký kiểm toán">
      {err && <div className="mb-4 rounded-xl border border-red-300 bg-red-50 p-3 text-sm text-red-900">{err}</div>}
      {msg && <div className="mb-4 rounded-xl border border-emerald-300 bg-emerald-50 p-3 text-sm text-emerald-900">{msg}</div>}

      <div className="grid gap-6 lg:grid-cols-2">
        <div className="rounded-xl border border-[#d8e2da] bg-white p-5">
          <h2 className="flex items-center gap-2 text-base font-bold text-[#123c29]">
            <ShieldCheck className="h-5 w-5 text-[#15803d]" /> Người dùng & Kiểm duyệt Vai trò
          </h2>
          <div className="mt-3 divide-y divide-[#eef3ef] text-xs">
            {users.map((u) => (
              <div key={u.id} className="py-3 space-y-1.5">
                <p className="font-bold text-[#123c29]">{u.displayName} <span className="font-normal text-[#526158]">({u.email})</span></p>
                <div className="flex flex-wrap gap-1.5">
                  {u.roles.map((r) => (
                    <span key={r.id} className="inline-flex items-center gap-1 rounded border border-[#d8e2da] px-2 py-0.5">
                      {r.roleCode}: <strong>{r.approvalStatus}</strong>
                      {r.approvalStatus !== "Approved" && (
                        <button
                          type="button"
                          onClick={() => approveRole(u.id, r.roleCode)}
                          className="ml-1 rounded bg-[#15803d] px-1.5 py-0.5 text-[10px] text-white"
                        >
                          Duyệt
                        </button>
                      )}
                    </span>
                  ))}
                </div>
              </div>
            ))}
          </div>
        </div>

        <div className="rounded-xl border border-[#d8e2da] bg-white p-5">
          <h2 className="text-base font-bold text-[#123c29]">Nhật ký Kiểm toán (Audit Logs)</h2>
          <div className="mt-3 divide-y divide-[#eef3ef] text-xs">
            {logs.map((l) => (
              <div key={l.id} className="py-2 flex items-center justify-between gap-2">
                <div>
                  <span className="font-bold text-[#123c29]">{l.action}</span> • {l.entityType}
                  <p className="text-[11px] text-[#526158]">{l.actorEmail}</p>
                </div>
                <span className="text-[11px] text-[#526158]">{new Date(l.createdAt).toLocaleString("vi-VN")}</span>
              </div>
            ))}
          </div>
        </div>
      </div>
    </AppShell>
  );
}
