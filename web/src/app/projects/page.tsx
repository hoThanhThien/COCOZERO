"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { FolderKanban, Plus, Search } from "lucide-react";
import { AppShell } from "@/components/AppShell";
import { OriginBadge, VerificationNoticeBadge } from "@/components/Badges";
import { apiFetch, getStoredToken } from "@/lib/api";
import type { ProjectDto } from "@/lib/types";

export default function ProjectsDirectoryPage() {
  const [projects, setProjects] = useState<ProjectDto[]>([]);
  const [notice, setNotice] = useState("");
  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState("");
  const [showCreate, setShowCreate] = useState(false);
  const [name, setName] = useState("");
  const [code, setCode] = useState("");
  const [area, setArea] = useState("2.0");
  const [methodology, setMethodology] = useState("Đang đánh giá IPCC AFOLU / Verra VM0042 v2.2 (chưa đăng ký)");
  const [msg, setMsg] = useState<string | null>(null);
  const [err, setErr] = useState<string | null>(null);

  async function loadProjects() {
    const qs = new URLSearchParams();
    if (search) qs.set("search", search);
    if (statusFilter) qs.set("status", statusFilter);
    const data = await apiFetch<{ directoryNotice: string; projects: ProjectDto[] }>(`/api/v1/projects?${qs.toString()}`);
    setNotice(data.directoryNotice);
    setProjects(data.projects);
  }

  useEffect(() => {
    loadProjects().catch((e) => setErr(e instanceof Error ? e.message : "Lỗi tải danh mục dự án."));
  }, [statusFilter]);

  async function handleCreateProject(e: React.FormEvent) {
    e.preventDefault();
    setErr(null);
    setMsg(null);
    try {
      await apiFetch("/api/v1/projects", {
        method: "POST",
        body: JSON.stringify({
          name,
          code,
          projectType: "Coconut Agroforestry & Soil Management",
          province: "Vĩnh Long",
          historicalRegion: "Vùng dừa Bến Tre",
          declaredTotalAreaHa: Number(area),
          intendedMethodology: methodology,
        }),
      });
      setMsg("Đã tạo hồ sơ dự án mới ở trạng thái DataCollection.");
      setShowCreate(false);
      setName("");
      setCode("");
      await loadProjects();
    } catch (e2) {
      setErr(e2 instanceof Error ? e2.message : "Chỉ ProjectDeveloper hoặc Admin mới được tạo dự án.");
    }
  }

  return (
    <AppShell
      title="Danh mục Dự án & Kết nối RFQ (Project Directory)"
      subtitle="Kênh công bố thông tin giai đoạn chuẩn bị dự án & tiếp nhận yêu cầu tìm hiểu; KHÔNG phải sàn giao dịch được cấp phép"
    >
      {notice && (
        <div className="mb-4 rounded-xl border border-[#cfe0d3] bg-[#e8f5ec] p-3 text-xs font-medium text-[#123c29]">
          {notice}
        </div>
      )}
      {err && <div className="mb-4 rounded-xl border border-red-300 bg-red-50 p-3 text-sm text-red-900">{err}</div>}
      {msg && <div className="mb-4 rounded-xl border border-emerald-300 bg-emerald-50 p-3 text-sm text-emerald-900">{msg}</div>}

      <div className="mb-5 flex flex-col gap-3 sm:flex-row sm:flex-wrap sm:items-center sm:justify-between">
        <div className="flex flex-col gap-2 sm:flex-row sm:flex-wrap sm:items-center">
          <div className="relative w-full sm:w-56">
            <Search className="absolute left-3 top-2.5 h-4 w-4 text-[#526158]" />
            <input
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Tìm tên, mã hoặc phương pháp..."
              className="w-full rounded-lg border border-[#d8e2da] bg-white py-2 pl-9 pr-3 text-sm"
            />
          </div>
          <select value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)} className="w-full rounded-lg border border-[#d8e2da] bg-white px-3 py-2 text-sm sm:w-auto">
            <option value="">Tất cả giai đoạn</option>
            <option value="Concept">Concept</option>
            <option value="DataCollection">DataCollection</option>
            <option value="Feasibility">Feasibility</option>
            <option value="ValidationPreparation">ValidationPreparation</option>
            <option value="RegisteredExternalEvidenceOnly">RegisteredExternalEvidenceOnly</option>
          </select>
          <button type="button" onClick={() => loadProjects()} className="rounded-lg bg-[#123c29] px-3.5 py-2 text-sm font-semibold text-white">
            Lọc
          </button>
        </div>

        {getStoredToken() && (
          <button
            type="button"
            onClick={() => setShowCreate(!showCreate)}
            className="inline-flex items-center gap-1.5 rounded-lg bg-[#15803d] px-3.5 py-2 text-sm font-semibold text-white"
          >
            <Plus className="h-4 w-4" /> Tạo hồ sơ dự án (Developer)
          </button>
        )}
      </div>

      {showCreate && (
        <form onSubmit={handleCreateProject} className="mb-6 rounded-xl border border-[#d8e2da] bg-white p-5 space-y-3">
          <h2 className="text-sm font-bold text-[#123c29]">Tạo hồ sơ dự án chuẩn bị</h2>
          <div className="grid gap-3 sm:grid-cols-3">
            <input value={name} onChange={(e) => setName(e.target.value)} required placeholder="Tên dự án" className="rounded-lg border border-[#d8e2da] px-3 py-2 text-sm" />
            <input value={code} onChange={(e) => setCode(e.target.value)} required placeholder="Mã dự án (VD: PRJ-VL-002)" className="rounded-lg border border-[#d8e2da] px-3 py-2 text-sm" />
            <input type="number" step="0.1" value={area} onChange={(e) => setArea(e.target.value)} required placeholder="Diện tích (ha)" className="rounded-lg border border-[#d8e2da] px-3 py-2 text-sm" />
          </div>
          <input value={methodology} onChange={(e) => setMethodology(e.target.value)} className="w-full rounded-lg border border-[#d8e2da] px-3 py-2 text-sm" />
          <button type="submit" className="rounded-lg bg-[#123c29] px-4 py-2 text-xs font-semibold text-white">Lưu dự án</button>
        </form>
      )}

      <div className="grid gap-4 md:grid-cols-2">
        {projects.map((p) => (
          <div key={p.id} className="rounded-xl border border-[#d8e2da] bg-white p-5 flex flex-col justify-between">
            <div>
              <div className="flex flex-wrap items-center justify-between gap-2">
                <span className="text-xs font-mono font-bold text-[#15803d]">{p.code}</span>
                <div className="flex flex-wrap gap-1.5">
                  <OriginBadge tag={p.dataOriginTag} />
                  <span className="rounded-full bg-[#e8f5ec] px-2.5 py-0.5 text-xs font-semibold text-[#123c29]">{p.projectStatus}</span>
                </div>
              </div>
              <h2 className="mt-2 flex items-center gap-2 text-base font-bold text-[#123c29]">
                <FolderKanban className="h-4 w-4 shrink-0 text-[#15803d]" /> {p.name}
              </h2>
              <p className="mt-1 text-xs text-[#526158]">{p.province} ({p.historicalRegion}) • Diện tích khai báo: <strong>{p.declaredTotalAreaHa} ha</strong></p>
              <p className="mt-2 text-xs text-[#35443c]"><strong>Phương pháp dự kiến:</strong> {p.intendedMethodology}</p>
              <div className="mt-3 flex flex-wrap items-center gap-2">
                <VerificationNoticeBadge status={p.verificationStatus} />
                <span className="rounded-full border border-[#d8e2da] px-2.5 py-0.5 text-xs">
                  {p.readinessLabel}: <strong>{p.readinessScore}/100</strong>
                </span>
              </div>
            </div>
            <div className="mt-4 border-t border-[#eef3ef] pt-3 flex items-center justify-between">
              <span className="text-[11px] text-[#526158]">Chưa phát hành tín chỉ</span>
              <Link href={`/projects/${p.id}`} className="rounded-lg bg-[#123c29] px-3 py-1.5 text-xs font-semibold text-white hover:bg-[#15803d]">
                Xem chi tiết & Gửi RFQ →
              </Link>
            </div>
          </div>
        ))}
      </div>
    </AppShell>
  );
}
