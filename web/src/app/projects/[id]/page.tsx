"use client";

import { useParams } from "next/navigation";
import { useEffect, useState } from "react";
import { Bookmark, Send } from "lucide-react";
import { AppShell } from "@/components/AppShell";
import { OriginBadge, ScientificDisclaimerBanner, VerificationNoticeBadge } from "@/components/Badges";
import { apiFetch, getStoredToken } from "@/lib/api";

interface ProjectDetailDto {
  id: string;
  code: string;
  name: string;
  cooperativeName?: string | null;
  projectType: string;
  province: string;
  historicalRegion: string;
  declaredTotalAreaHa: number;
  participatingFarmCount: number;
  intendedMethodology: string;
  methodologyNotes: string;
  baselineSummary: string;
  monitoringPlanSummary: string;
  readinessScore: number;
  readinessLabel: string;
  verificationStatus: string;
  projectStatus: string;
  dataOriginTag: string;
  disclaimer: string;
}

export default function ProjectDetailPage() {
  const params = useParams<{ id: string }>();
  const [project, setProject] = useState<ProjectDetailDto | null>(null);
  const [org, setOrg] = useState("");
  const [scope, setScope] = useState("Tìm hiểu hồ sơ chuẩn bị dự án và hợp tác vùng nguyên liệu");
  const [message, setMessage] = useState("");
  const [saved, setSaved] = useState(false);
  const [statusMsg, setStatusMsg] = useState<string | null>(null);
  const [err, setErr] = useState<string | null>(null);

  useEffect(() => {
    apiFetch<ProjectDetailDto>(`/api/v1/projects/${params.id}`)
      .then((p) => {
        setProject(p);
        const list = JSON.parse(localStorage.getItem("cococarbon_saved_projects") || "[]") as string[];
        setSaved(list.includes(p.id));
      })
      .catch((e) => setErr(e instanceof Error ? e.message : "Không tải được chi tiết dự án."));
  }, [params.id]);

  function toggleSave() {
    if (!project) return;
    const list = JSON.parse(localStorage.getItem("cococarbon_saved_projects") || "[]") as string[];
    const next = saved ? list.filter((id) => id !== project.id) : [...new Set([...list, project.id])];
    localStorage.setItem("cococarbon_saved_projects", JSON.stringify(next));
    setSaved(!saved);
  }

  async function sendRfq(e: React.FormEvent) {
    e.preventDefault();
    setErr(null);
    setStatusMsg(null);
    try {
      await apiFetch("/api/v1/rfqs", {
        method: "POST",
        body: JSON.stringify({
          projectId: params.id,
          buyerOrganization: org,
          inquiryScope: scope,
          message,
        }),
      });
      setStatusMsg("Đã gửi yêu cầu tìm hiểu / báo giá (RFQ) tới Project Developer. Không phát sinh giao dịch tài chính trên nền tảng.");
      setMessage("");
    } catch (e2) {
      setErr(e2 instanceof Error ? e2.message : "Vui lòng đăng nhập để gửi RFQ.");
    }
  }

  return (
    <AppShell title={project ? project.name : "Chi tiết Dự án"} subtitle="Thông tin chuẩn bị hồ sơ MRV & Kênh tiếp nhận yêu cầu tìm hiểu (RFQ)">
      {err && <div className="mb-4 rounded-xl border border-red-300 bg-red-50 p-3 text-sm text-red-900">{err}</div>}
      {statusMsg && <div className="mb-4 rounded-xl border border-emerald-300 bg-emerald-50 p-3 text-sm text-emerald-900">{statusMsg}</div>}

      {project && (
        <div className="space-y-6">
          <div className="rounded-xl border border-[#d8e2da] bg-white p-5 space-y-3">
            <div className="flex flex-wrap items-center justify-between gap-2">
              <div className="flex flex-wrap items-center gap-2">
                <OriginBadge tag={project.dataOriginTag} />
                <VerificationNoticeBadge status={project.verificationStatus} />
                <span className="rounded-full bg-[#e8f5ec] px-2.5 py-0.5 text-xs font-semibold text-[#123c29]">{project.projectStatus}</span>
              </div>
              <button type="button" onClick={toggleSave} className="inline-flex items-center gap-1.5 rounded-lg border border-[#d8e2da] px-3 py-1.5 text-xs font-semibold">
                <Bookmark className="h-3.5 w-3.5" /> {saved ? "Đã lưu dự án" : "Lưu dự án"}
              </button>
            </div>
            <div className="grid gap-3 text-sm sm:grid-cols-3">
              <div><span className="text-[#526158]">Mã:</span> {project.code}</div>
              <div><span className="text-[#526158]">Địa bàn:</span> {project.province} ({project.historicalRegion})</div>
              <div><span className="text-[#526158]">Diện tích / Số vườn:</span> {project.declaredTotalAreaHa} ha / {project.participatingFarmCount} vườn</div>
            </div>
            <div className="text-xs text-[#35443c] space-y-1">
              <p><strong>Phương pháp dự kiến:</strong> {project.intendedMethodology}</p>
              <p><strong>Baseline:</strong> {project.baselineSummary || "Đang hoàn thiện"}</p>
              <p><strong>Monitoring:</strong> {project.monitoringPlanSummary || "Đang hoàn thiện"}</p>
              <p><strong>{project.readinessLabel}:</strong> {project.readinessScore}/100</p>
            </div>
          </div>

          <form onSubmit={sendRfq} className="rounded-xl border border-[#d8e2da] bg-white p-5 space-y-3">
            <h2 className="flex items-center gap-2 text-base font-bold text-[#123c29]">
              <Send className="h-4 w-4 text-[#15803d]" /> Gửi yêu cầu tìm hiểu / Báo giá (RFQ)
            </h2>
            <p className="text-xs text-[#526158]">
              Lưu ý: RFQ chỉ là đầu mối trao đổi thông tin giữa doanh nghiệp và đơn vị dự án, không thực hiện thanh toán hoặc chuyển quyền tín chỉ trong MVP.
            </p>
            <input value={org} onChange={(e) => setOrg(e.target.value)} required placeholder="Tên doanh nghiệp / tổ chức của bạn" className="w-full rounded-lg border border-[#d8e2da] px-3 py-2 text-sm" />
            <input value={scope} onChange={(e) => setScope(e.target.value)} required placeholder="Phạm vi quan tâm" className="w-full rounded-lg border border-[#d8e2da] px-3 py-2 text-sm" />
            <textarea value={message} onChange={(e) => setMessage(e.target.value)} required rows={3} placeholder="Nội dung trao đổi..." className="w-full rounded-lg border border-[#d8e2da] px-3 py-2 text-sm" />
            <button
              type="submit"
              disabled={!getStoredToken()}
              className="rounded-lg bg-[#123c29] px-4 py-2 text-sm font-semibold text-white hover:bg-[#15803d] disabled:opacity-50"
            >
              Gửi RFQ
            </button>
          </form>

          <ScientificDisclaimerBanner text={project.disclaimer} />
        </div>
      )}
    </AppShell>
  );
}
