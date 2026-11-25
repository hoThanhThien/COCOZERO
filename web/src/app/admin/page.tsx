"use client";

import { useEffect, useState } from "react";
import { Cpu, ShieldCheck } from "lucide-react";
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

interface SecurityPostureDto {
  firebaseTokenVerification: string;
  devDemoModeEnabled: boolean;
  httpsRedirectEnforced: boolean;
  securityHeaders: string[];
  uploadPolicy: string;
  rateLimitPolicy: string;
  pendingReviews: number;
  pendingDeletionCount: number;
  totalAuditLogs: number;
}

interface AiProviderItemDto {
  id: string;
  label: string;
  active: boolean;
  hasApiKey: boolean;
  maskedApiKey: string;
  modelName: string;
  suggestedModels: string[];
  baseUrl: string;
  requiresKey: boolean;
}

interface AiProviderStatusDto {
  activeProvider: string;
  activeModelName: string;
  activeHasApiKey: boolean;
  isUsingFallbackEngine: boolean;
  providers: AiProviderItemDto[];
  securityNote: string;
}

export default function AdminConsolePage() {
  const [users, setUsers] = useState<AdminUserRow[]>([]);
  const [logs, setLogs] = useState<AuditLogRow[]>([]);
  const [posture, setPosture] = useState<SecurityPostureDto | null>(null);
  const [aiStatus, setAiStatus] = useState<AiProviderStatusDto | null>(null);
  const [selectedProvider, setSelectedProvider] = useState("gemini");
  const [aiModelName, setAiModelName] = useState("gemini-4-hydrogen");
  const [aiBaseUrl, setAiBaseUrl] = useState("https://generativelanguage.googleapis.com");
  const [aiApiKey, setAiApiKey] = useState("");
  const [aiBusy, setAiBusy] = useState(false);
  const [aiTestMsg, setAiTestMsg] = useState<string | null>(null);
  const [msg, setMsg] = useState<string | null>(null);
  const [err, setErr] = useState<string | null>(null);

  async function load() {
    if (!getStoredToken()) return;
    try {
      const [u, l, p, ai] = await Promise.all([
        apiFetch<AdminUserRow[]>("/api/v1/admin/users"),
        apiFetch<AuditLogRow[]>("/api/v1/admin/audit-logs?limit=30"),
        apiFetch<SecurityPostureDto>("/api/v1/admin/security-posture"),
        apiFetch<AiProviderStatusDto>("/api/v1/admin/ai-provider").catch(() => null),
      ]);
      setUsers(u);
      setLogs(l);
      setPosture(p);
      if (ai) {
        setAiStatus(ai);
        setSelectedProvider(ai.activeProvider);
        const cur = ai.providers.find((x) => x.id === ai.activeProvider);
        if (cur) {
          setAiModelName(cur.modelName);
          setAiBaseUrl(cur.baseUrl);
        }
      }
    } catch (e) {
      setErr(e instanceof Error ? e.message : "Chỉ tài khoản Admin mới được truy cập bảng quản trị.");
    }
  }

  function handleSelectProvider(pid: string) {
    setSelectedProvider(pid);
    setAiApiKey("");
    setAiTestMsg(null);
    const preset = aiStatus?.providers.find((x) => x.id === pid);
    if (preset) {
      setAiModelName(preset.modelName);
      setAiBaseUrl(preset.baseUrl);
    }
  }

  async function saveAiConfig(clearApiKey = false) {
    setAiBusy(true);
    setErr(null);
    setMsg(null);
    try {
      const updated = await apiFetch<AiProviderStatusDto>("/api/v1/admin/ai-provider", {
        method: "POST",
        body: JSON.stringify({
          activeProvider: selectedProvider,
          modelName: aiModelName,
          baseUrl: aiBaseUrl,
          apiKey: aiApiKey.trim() || null,
          clearApiKey,
        }),
      });
      setAiStatus(updated);
      setAiApiKey("");
      setMsg(`Đã kích hoạt mô hình suy luận AI: ${updated.activeProvider} (${updated.activeModelName}).`);
    } catch (e) {
      setErr(e instanceof Error ? e.message : "Không thể cập nhật cấu hình AI Provider.");
    } finally {
      setAiBusy(false);
    }
  }

  async function testAiConfig() {
    setAiBusy(true);
    setAiTestMsg(null);
    try {
      const res = await apiFetch<{ ok: boolean; latencyMs: number; replyPreview?: string; error?: string }>(
        "/api/v1/admin/ai-provider/test",
        {
          method: "POST",
          body: JSON.stringify({
            provider: selectedProvider,
            modelName: aiModelName,
            baseUrl: aiBaseUrl,
            apiKey: aiApiKey.trim() || null,
          }),
        }
      );
      setAiTestMsg(
        res.ok
          ? `Kết nối thành công (${res.latencyMs}ms): "${res.replyPreview || "OK"}"`
          : res.error || "Kiểm tra kết nối thất bại."
      );
    } catch (e) {
      setAiTestMsg(e instanceof Error ? e.message : "Không thể kiểm tra kết nối AI.");
    } finally {
      setAiBusy(false);
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

      <div className="mb-6 rounded-xl border border-[#cfe0d3] bg-white p-5 space-y-4">
        <div className="flex flex-wrap items-center justify-between gap-2">
          <div>
            <h2 className="flex items-center gap-2 text-base font-bold text-[#123c29]">
              <Cpu className="h-5 w-5 text-[#15803d]" /> Cấu hình Đa Mô hình AI &amp; Quản lý API Key (Multi-AI Reasoning)
            </h2>
            <p className="mt-0.5 text-xs text-[#526158]">
              Cho phép Admin thêm API Key của Google Gemini, OpenAI, OpenRouter, Groq, Anthropic Claude hoặc endpoint OpenAI-Compatible để Trợ lý AI và AI Auditor suy luận + Tool Calling.
            </p>
          </div>
          {aiStatus && (
            <span className={`rounded-full px-3 py-1 text-xs font-bold ${aiStatus.isUsingFallbackEngine ? "bg-amber-100 text-amber-900" : "bg-emerald-100 text-emerald-900"}`}>
              Đang chạy: {aiStatus.activeProvider} ({aiStatus.activeModelName}) {aiStatus.isUsingFallbackEngine ? "• Fallback" : "• API Key Sẵn sàng"}
            </span>
          )}
        </div>

        <div className="grid gap-3 text-xs sm:grid-cols-2 lg:grid-cols-4">
          <div>
            <label className="font-semibold text-[#526158]">Chọn AI Provider</label>
            <select
              value={selectedProvider}
              onChange={(e) => handleSelectProvider(e.target.value)}
              className="mt-1 w-full rounded-lg border border-[#d8e2da] bg-white px-3 py-2"
            >
              {(aiStatus?.providers || [
                { id: "gemini", label: "Google Gemini (Interactions API)" },
                { id: "openai", label: "OpenAI (GPT Models)" },
                { id: "openrouter", label: "OpenRouter (Multi-Model)" },
                { id: "groq", label: "Groq (Llama / Qwen)" },
                { id: "anthropic", label: "Anthropic Claude" },
                { id: "custom", label: "Custom OpenAI-Compatible / Ollama" },
                { id: "heuristic", label: "Heuristic Fallback (Offline)" },
              ]).map((p) => (
                <option key={p.id} value={p.id}>
                  {p.label}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="font-semibold text-[#526158]">Model Name</label>
            <input
              value={aiModelName}
              onChange={(e) => setAiModelName(e.target.value)}
              placeholder="Ví dụ: gemini-4-hydrogen, gpt-4o-mini..."
              className="mt-1 w-full rounded-lg border border-[#d8e2da] px-3 py-2"
            />
          </div>

          <div>
            <label className="font-semibold text-[#526158]">Endpoint Base URL</label>
            <input
              value={aiBaseUrl}
              onChange={(e) => setAiBaseUrl(e.target.value)}
              placeholder="https://api.openai.com/v1"
              className="mt-1 w-full rounded-lg border border-[#d8e2da] px-3 py-2"
            />
          </div>

          <div>
            <label className="font-semibold text-[#526158]">
              API Key mới{" "}
              {aiStatus?.providers.find((x) => x.id === selectedProvider)?.hasApiKey && (
                <span className="text-emerald-700">
                  (Đã lưu: {aiStatus.providers.find((x) => x.id === selectedProvider)?.maskedApiKey})
                </span>
              )}
            </label>
            <input
              type="password"
              value={aiApiKey}
              onChange={(e) => setAiApiKey(e.target.value)}
              placeholder="Nhập API Key để thêm hoặc thay thế..."
              className="mt-1 w-full rounded-lg border border-[#d8e2da] px-3 py-2"
            />
          </div>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <button
            type="button"
            disabled={aiBusy}
            onClick={() => saveAiConfig(false)}
            className="rounded-lg bg-[#123c29] px-4 py-2 text-xs font-semibold text-white hover:bg-[#15803d] disabled:opacity-50"
          >
            Lưu Key &amp; Kích hoạt Provider
          </button>
          <button
            type="button"
            disabled={aiBusy}
            onClick={testAiConfig}
            className="rounded-lg border border-[#123c29] px-4 py-2 text-xs font-semibold text-[#123c29] hover:bg-[#e8f5ec] disabled:opacity-50"
          >
            Kiểm tra kết nối (Ping AI)
          </button>
          {aiStatus?.providers.find((x) => x.id === selectedProvider)?.hasApiKey && (
            <button
              type="button"
              disabled={aiBusy}
              onClick={() => saveAiConfig(true)}
              className="rounded-lg border border-red-300 px-3 py-2 text-xs font-semibold text-red-800 hover:bg-red-50 disabled:opacity-50"
            >
              Xóa Key của Provider này
            </button>
          )}
          <span className="text-[11px] text-[#526158]">
            {aiStatus?.securityNote || "API key chỉ được lưu an toàn ở phía server, không trả key gốc về trình duyệt."}
          </span>
        </div>

        {aiTestMsg && (
          <div className="rounded-lg border border-[#d8e2da] bg-[#f8fbf8] p-2.5 text-xs text-[#123c29]">
            {aiTestMsg}
          </div>
        )}
      </div>

      {posture && (
        <div className="mb-6 rounded-xl border border-[#d8e2da] bg-white p-5">
          <h2 className="text-sm font-bold text-[#123c29]">Trạng thái An ninh mạng & Bảo vệ Dữ liệu (Security Posture)</h2>
          <div className="mt-3 grid gap-3 text-xs sm:grid-cols-3">
            <div className="rounded-lg border border-[#e2eae3] p-3">
              <strong>Auth:</strong> {posture.firebaseTokenVerification}
              <p className="text-[#526158]">Dev Demo Mode: {posture.devDemoModeEnabled ? "Bật (Development)" : "Tắt (Strict)"}</p>
            </div>
            <div className="rounded-lg border border-[#e2eae3] p-3">
              <strong>Upload & Rate Limit:</strong> {posture.rateLimitPolicy}
              <p className="text-[#526158]">{posture.uploadPolicy}</p>
            </div>
            <div className="rounded-lg border border-[#e2eae3] p-3">
              <strong>Hàng đợi Rà soát & Privacy:</strong>
              <p className="text-[#526158]">AI chờ duyệt: {posture.pendingReviews} • Yêu cầu xóa DL: {posture.pendingDeletionCount}</p>
            </div>
          </div>
        </div>
      )}

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
