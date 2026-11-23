"use client";

import { useEffect, useState } from "react";
import { CheckCircle2, FileSearch, ShieldAlert } from "lucide-react";
import { AppShell } from "@/components/AppShell";
import { OriginBadge, VerificationNoticeBadge } from "@/components/Badges";
import { apiFetch, getStoredToken } from "@/lib/api";
import type { AiAnalysisDetailDto } from "@/lib/types";

interface AnalysisSummary {
  id: string;
  fileId?: string;
  fileName?: string;
  provider: string;
  modelName: string;
  status: string;
  isFallbackResult: boolean;
  reviewStatus: string;
  createdAt: string;
}

export default function DocumentsAndAuditPage() {
  const [list, setList] = useState<AnalysisSummary[]>([]);
  const [selected, setSelected] = useState<AiAnalysisDetailDto | null>(null);
  const [decision, setDecision] = useState("Accepted");
  const [comment, setComment] = useState("");
  const [msg, setMsg] = useState<string | null>(null);
  const [err, setErr] = useState<string | null>(null);

  async function loadList() {
    if (!getStoredToken()) return;
    try {
      const items = await apiFetch<AnalysisSummary[]>("/api/v1/ai/analyses");
      setList(items);
      const urlParams = new URLSearchParams(window.location.search);
      const targetId = urlParams.get("analysisId") || items[0]?.id;
      if (targetId) {
        const detail = await apiFetch<AiAnalysisDetailDto>(`/api/v1/ai/analyses/${targetId}`);
        setSelected(detail);
      }
    } catch (e) {
      setErr(e instanceof Error ? e.message : "Không thể tải danh sách phân tích AI.");
    }
  }

  useEffect(() => {
    loadList();
  }, []);

  async function selectAnalysis(id: string) {
    setErr(null);
    try {
      const detail = await apiFetch<AiAnalysisDetailDto>(`/api/v1/ai/analyses/${id}`);
      setSelected(detail);
    } catch (e) {
      setErr(e instanceof Error ? e.message : "Không thể mở báo cáo AI.");
    }
  }

  async function handleReviewSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!selected) return;
    setErr(null);
    setMsg(null);
    try {
      await apiFetch(`/api/v1/ai/analyses/${selected.analysisId}/review`, {
        method: "POST",
        body: JSON.stringify({ decision, comment }),
      });
      setMsg("Đã lưu quyết định của Reviewer (giữ nguyên bản gốc AI trích xuất để truy vết).");
      setComment("");
      await selectAnalysis(selected.analysisId);
      await loadList();
    } catch (e2) {
      setErr(e2 instanceof Error ? e2.message : "Chỉ tài khoản Reviewer / Admin mới được phê duyệt.");
    }
  }

  const output = selected?.structuredOutput;

  return (
    <AppShell
      title="Trung tâm Tài liệu & AI Carbon Auditor"
      subtitle="Trích xuất bằng chứng từ PDF/ảnh, phát hiện bất thường/trùng lặp SHA-256 và quy trình Reviewer phê duyệt"
    >
      {err && <div className="mb-4 rounded-xl border border-red-300 bg-red-50 p-3 text-sm text-red-900">{err}</div>}
      {msg && <div className="mb-4 rounded-xl border border-emerald-300 bg-emerald-50 p-3 text-sm text-emerald-900">{msg}</div>}

      <div className="grid gap-6 lg:grid-cols-3">
        <div className="rounded-xl border border-[#d8e2da] bg-white p-4 lg:col-span-1">
          <h2 className="flex items-center gap-2 text-sm font-bold text-[#123c29]">
            <FileSearch className="h-4 w-4 text-[#15803d]" /> Lịch sử AI Audit ({list.length})
          </h2>
          <p className="mt-1 text-xs text-[#526158]">
            Để phân tích tài liệu mới, mở một vườn trong <strong>Hồ sơ Vườn dừa</strong>, tải lên PDF/ảnh và bấm <em>Phân tích AI</em>.
          </p>
          <div className="mt-3 space-y-2">
            {list.map((item) => (
              <button
                key={item.id}
                type="button"
                onClick={() => selectAnalysis(item.id)}
                className={`w-full rounded-lg border p-3 text-left text-xs transition ${
                  selected?.analysisId === item.id ? "border-[#123c29] bg-[#e8f5ec]" : "border-[#e2eae3] hover:bg-stone-50"
                }`}
              >
                <p className="font-bold text-[#123c29]">{item.fileName || item.fileId}</p>
                <p className="mt-0.5 text-[#526158]">{item.modelName} • {item.reviewStatus}</p>
              </button>
            ))}
          </div>
        </div>

        <div className="space-y-4 lg:col-span-2">
          {!selected ? (
            <div className="rounded-xl border border-[#d8e2da] bg-white p-6 text-sm text-[#526158]">
              Chọn một phiên phân tích AI ở danh sách bên trái để xem chi tiết dẫn chứng và rubric.
            </div>
          ) : (
            <>
              <div className="rounded-xl border border-[#d8e2da] bg-white p-5 space-y-3">
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <div className="flex flex-wrap items-center gap-2">
                    <OriginBadge tag="AI_EXTRACTED" />
                    <VerificationNoticeBadge status={`Trạng thái rà soát: ${selected.reviewStatus}`} />
                  </div>
                  <span className="text-xs text-[#526158]">
                    {selected.provider} / {selected.modelName} • {selected.promptVersion}
                  </span>
                </div>

                {selected.readinessRubric && (
                  <div className="rounded-lg border border-[#cfe0d3] bg-[#f7faf7] p-4">
                    <div className="flex items-center justify-between">
                      <p className="text-sm font-bold text-[#123c29]">{selected.readinessRubric.label}</p>
                      <span className="text-lg font-extrabold text-[#15803d]">{selected.readinessRubric.score}/100</span>
                    </div>
                    <p className="mt-1 text-xs text-[#526158]">{selected.readinessRubric.disclaimer}</p>
                    <div className="mt-3 grid gap-2 sm:grid-cols-2">
                      {selected.readinessRubric.items.map((it) => (
                        <div key={it.criterion} className="rounded border border-[#dce6de] bg-white p-2 text-xs">
                          <span className="font-semibold">{it.criterion}</span>: <strong>{it.status}</strong>
                          <p className="text-[11px] text-[#526158]">{it.notes}</p>
                        </div>
                      ))}
                    </div>
                  </div>
                )}

                <div>
                  <h3 className="text-sm font-bold text-[#123c29]">Trường dữ liệu trích xuất (AI_EXTRACTED)</h3>
                  <div className="mt-2 grid gap-2 sm:grid-cols-2">
                    {(output?.extractedFields || []).map((f, i) => (
                      <div key={i} className="rounded-lg border border-[#e2eae3] p-2.5 text-xs">
                        <span className="text-[#526158]">{f.field}:</span> <strong>{f.value ?? "N/A"}</strong> (conf {Math.round(f.confidence * 100)}%)
                      </div>
                    ))}
                  </div>
                </div>

                <div>
                  <h3 className="text-sm font-bold text-[#123c29]">Phát hiện & Cảnh báo</h3>
                  <div className="mt-2 space-y-2">
                    {(output?.findings || []).map((fd, i) => (
                      <div key={i} className="flex items-start gap-2 rounded-lg border border-amber-200 bg-amber-50/70 p-2.5 text-xs">
                        <ShieldAlert className="mt-0.5 h-4 w-4 shrink-0 text-amber-700" />
                        <div>
                          <span className="font-bold">[{fd.code}]</span> {fd.message}
                        </div>
                      </div>
                    ))}
                  </div>
                </div>

                <div>
                  <h3 className="text-sm font-bold text-[#123c29]">Bằng chứng trích dẫn (Evidence)</h3>
                  {selected.evidence.length === 0 ? (
                    <p className="mt-1 text-xs text-[#526158]">Không có trích dẫn trực tiếp — giữ trạng thái Chưa xác minh độc lập.</p>
                  ) : (
                    <div className="mt-2 space-y-2">
                      {selected.evidence.map((ev, i) => (
                        <div key={i} className="rounded-lg border border-[#e2eae3] bg-[#fafcfa] p-3 text-xs">
                          <p className="font-semibold">Trang {ev.pageNumber ?? "?"} • Giá trị: {ev.extractedValue ?? "-"}</p>
                          <p className="mt-1 italic text-[#35443c]">"{ev.snippet}"</p>
                          <p className="mt-1 text-[11px] text-[#526158]">{ev.reason}</p>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              </div>

              <form onSubmit={handleReviewSubmit} className="rounded-xl border border-[#d8e2da] bg-white p-5 space-y-3">
                <h3 className="flex items-center gap-2 text-sm font-bold text-[#123c29]">
                  <CheckCircle2 className="h-4 w-4 text-[#15803d]" /> Quyết định của Reviewer nội bộ (Không thay thế VVB)
                </h3>
                <div className="grid gap-3 sm:grid-cols-3">
                  <select value={decision} onChange={(e) => setDecision(e.target.value)} className="rounded-lg border border-[#d8e2da] px-3 py-2 text-sm">
                    <option value="Accepted">Accepted (Chấp nhận đề xuất AI)</option>
                    <option value="Edited">Edited (Chỉnh sửa)</option>
                    <option value="Rejected">Rejected (Từ chối / Yêu cầu bổ sung)</option>
                  </select>
                  <input
                    value={comment}
                    onChange={(e) => setComment(e.target.value)}
                    placeholder="Ghi chú rà soát..."
                    required
                    className="rounded-lg border border-[#d8e2da] px-3 py-2 text-sm sm:col-span-2"
                  />
                </div>
                <button type="submit" className="rounded-lg bg-[#123c29] px-4 py-2 text-xs font-semibold text-white hover:bg-[#15803d]">
                  Lưu quyết định Reviewer
                </button>

                {selected.reviewerDecisions && selected.reviewerDecisions.length > 0 && (
                  <div className="pt-2 text-xs text-[#526158] space-y-1">
                    <p className="font-semibold">Lịch sử rà soát:</p>
                    {selected.reviewerDecisions.map((d) => (
                      <div key={d.id}>• {d.decision}: {d.comment} ({new Date(d.createdAt).toLocaleString("vi-VN")})</div>
                    ))}
                  </div>
                )}
              </form>
            </>
          )}
        </div>
      </div>
    </AppShell>
  );
}
