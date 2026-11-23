"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { ShoppingBag } from "lucide-react";
import { AppShell } from "@/components/AppShell";
import { apiFetch, getStoredToken } from "@/lib/api";
import type { RfqDto } from "@/lib/types";

export default function BuyerWorkspacePage() {
  const [rfqs, setRfqs] = useState<RfqDto[]>([]);
  const [savedIds, setSavedIds] = useState<string[]>([]);
  const [replyMap, setReplyMap] = useState<Record<string, string>>({});
  const [msg, setMsg] = useState<string | null>(null);
  const [err, setErr] = useState<string | null>(null);

  async function load() {
    setSavedIds(JSON.parse(localStorage.getItem("cococarbon_saved_projects") || "[]"));
    if (!getStoredToken()) return;
    try {
      const data = await apiFetch<RfqDto[]>("/api/v1/rfqs");
      setRfqs(data);
    } catch (e) {
      setErr(e instanceof Error ? e.message : "Không tải được danh sách RFQ.");
    }
  }

  useEffect(() => {
    load();
  }, []);

  async function respondRfq(id: string) {
    setErr(null);
    setMsg(null);
    try {
      await apiFetch(`/api/v1/rfqs/${id}/respond`, {
        method: "POST",
        body: JSON.stringify({
          status: "Responded",
          sellerResponse: replyMap[id] || "Đã tiếp nhận yêu cầu tìm hiểu thông tin.",
        }),
      });
      setMsg("Đã phản hồi RFQ.");
      await load();
    } catch (e) {
      setErr(e instanceof Error ? e.message : "Không có quyền phản hồi RFQ này.");
    }
  }

  return (
    <AppShell
      title="Không gian Buyer & Theo dõi RFQ"
      subtitle="Theo dõi dự án đã lưu, yêu cầu tìm hiểu thông tin (RFQ) và phản hồi của bên phát triển dự án"
    >
      {err && <div className="mb-4 rounded-xl border border-red-300 bg-red-50 p-3 text-sm text-red-900">{err}</div>}
      {msg && <div className="mb-4 rounded-xl border border-emerald-300 bg-emerald-50 p-3 text-sm text-emerald-900">{msg}</div>}

      <div className="space-y-6">
        <div className="rounded-xl border border-[#d8e2da] bg-white p-5">
          <h2 className="text-sm font-bold text-[#123c29]">Dự án đã đánh dấu ({savedIds.length})</h2>
          {savedIds.length === 0 ? (
            <p className="mt-2 text-xs text-[#526158]">
              Chưa lưu dự án nào. Duyệt <Link href="/projects" className="font-semibold text-[#15803d] underline">Danh mục Dự án</Link>.
            </p>
          ) : (
            <div className="mt-2 flex flex-wrap gap-2">
              {savedIds.map((id) => (
                <Link key={id} href={`/projects/${id}`} className="rounded-lg border border-[#d8e2da] px-3 py-1.5 text-xs font-medium hover:bg-[#e8f5ec]">
                  Xem dự án {id.slice(0, 8)}...
                </Link>
              ))}
            </div>
          )}
        </div>

        <div className="rounded-xl border border-[#d8e2da] bg-white p-5">
          <h2 className="flex items-center gap-2 text-base font-bold text-[#123c29]">
            <ShoppingBag className="h-5 w-5 text-[#15803d]" /> Danh sách RFQ ({rfqs.length})
          </h2>
          <div className="mt-4 space-y-3">
            {rfqs.map((r) => (
              <div key={r.id} className="rounded-xl border border-[#e2eae3] p-4 space-y-2 text-xs">
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <Link href={`/projects/${r.projectId}`} className="text-sm font-bold text-[#123c29] hover:underline">
                    {r.projectName}
                  </Link>
                  <span className="rounded-full bg-[#e8f5ec] px-2.5 py-0.5 font-semibold text-[#15803d]">{r.status}</span>
                </div>
                <p><strong>Đơn vị gửi:</strong> {r.buyerOrganization} • <strong>Phạm vi:</strong> {r.inquiryScope}</p>
                <p className="text-[#35443c]">{r.message}</p>
                {r.sellerResponse && (
                  <div className="rounded-lg border border-emerald-200 bg-emerald-50 p-2.5">
                    <strong>Phản hồi bên dự án:</strong> {r.sellerResponse}
                  </div>
                )}
                <div className="flex gap-2 pt-1">
                  <input
                    value={replyMap[r.id] || ""}
                    onChange={(e) => setReplyMap({ ...replyMap, [r.id]: e.target.value })}
                    placeholder="Phản hồi (dành cho Project Developer)..."
                    className="flex-1 rounded-lg border border-[#d8e2da] px-2.5 py-1.5 text-xs"
                  />
                  <button type="button" onClick={() => respondRfq(r.id)} className="rounded-lg bg-[#123c29] px-3 py-1.5 font-semibold text-white">
                    Gửi phản hồi
                  </button>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </AppShell>
  );
}
