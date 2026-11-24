"use client";

import { useParams, useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { Bot, Calculator, Download, FileUp, MapPin, Wallet } from "lucide-react";
import { AppShell } from "@/components/AppShell";
import { OriginBadge, ScientificDisclaimerBanner } from "@/components/Badges";
import { apiFetch, getApiBaseUrl } from "@/lib/api";

interface FinanceLedgerDto {
  summary: {
    totalDeclaredExpenseVnd: number;
    totalDeclaredAgriAndSupportVnd: number;
    netDeclaredFarmCashflowVnd: number;
    scientificBoundaryNotice: string;
  };
  entries: {
    id: string;
    entryType: string;
    category: string;
    amountVnd: number;
    entryDate: string;
    notes: string;
    dataOriginTag: string;
  }[];
}

interface FarmDetailDto {
  id: string;
  name: string;
  province: string;
  commune: string;
  historicalRegion: string;
  areaHa: number;
  treeCount: number;
  variety: string;
  ageYears: number;
  latitude?: number | null;
  longitude?: number | null;
  status: string;
  dataOriginTag: string;
  files: {
    id: string;
    category: string;
    fileName: string;
    mimeType: string;
    sizeBytes: number;
    sha256Hash: string;
    version: number;
    createdAt: string;
  }[];
  calculations: {
    id: string;
    estimatedCO2EquivalentT: number;
    observedTreeDensityPerHa: number;
    factorVersion: string;
    formulaVersion: string;
    sourceCitation: string;
    disclaimer: string;
    createdAt: string;
  }[];
}

export default function FarmDetailPage() {
  const params = useParams<{ id: string }>();
  const router = useRouter();
  const [farm, setFarm] = useState<FarmDetailDto | null>(null);
  const [ledger, setLedger] = useState<FinanceLedgerDto | null>(null);
  const [entryType, setEntryType] = useState("CareExpense");
  const [entryCategory, setEntryCategory] = useState("Ủ phân hữu cơ mụn dừa");
  const [entryAmount, setEntryAmount] = useState("2500000");
  const [file, setFile] = useState<File | null>(null);
  const [category, setCategory] = useState("LandDocument");
  const [busy, setBusy] = useState(false);
  const [msg, setMsg] = useState<string | null>(null);
  const [err, setErr] = useState<string | null>(null);

  async function load() {
    try {
      const [data, fin] = await Promise.all([
        apiFetch<FarmDetailDto>(`/api/v1/farms/${params.id}`),
        apiFetch<FinanceLedgerDto>(`/api/v1/farms/${params.id}/finance-ledger`).catch(() => null),
      ]);
      setFarm(data);
      setLedger(fin);
    } catch (e) {
      setErr(e instanceof Error ? e.message : "Không thể tải chi tiết vườn.");
    }
  }

  async function handleAddFinanceEntry(e: React.FormEvent) {
    e.preventDefault();
    setBusy(true);
    setErr(null);
    setMsg(null);
    try {
      await apiFetch(`/api/v1/farms/${params.id}/finance-ledger`, {
        method: "POST",
        body: JSON.stringify({
          entryType,
          category: entryCategory,
          amountVnd: Number(entryAmount),
          notes: "Ghi nhận trên Sổ Tài chính Xanh nông hộ",
        }),
      });
      setMsg("Đã lưu khoản thu/chi nông nghiệp xanh (USER-DECLARED).");
      await load();
    } catch (e2) {
      setErr(e2 instanceof Error ? e2.message : "Lỗi lưu sổ tài chính.");
    } finally {
      setBusy(false);
    }
  }

  useEffect(() => {
    load();
  }, [params.id]);

  async function handleUpload(e: React.FormEvent) {
    e.preventDefault();
    if (!file) return;
    setBusy(true);
    setErr(null);
    setMsg(null);
    try {
      const fd = new FormData();
      fd.append("file", file);
      fd.append("category", category);
      await apiFetch(`/api/v1/farms/${params.id}/files`, { method: "POST", body: fd });
      setMsg("Đã tải tài liệu lên kho lưu trữ riêng tư (đã kiểm tra định dạng, magic bytes và SHA-256).");
      setFile(null);
      await load();
    } catch (e2) {
      setErr(e2 instanceof Error ? e2.message : "Upload thất bại.");
    } finally {
      setBusy(false);
    }
  }

  async function handleRunAi(fileId: string) {
    setBusy(true);
    setErr(null);
    setMsg(null);
    try {
      const res = await apiFetch<{ analysisId: string; status: string }>("/api/v1/ai/document-analysis", {
        method: "POST",
        body: JSON.stringify({ fileId, farmId: params.id }),
      });
      setMsg(`Đã phân tích tài liệu (${res.status}). Đang chuyển sang báo cáo AI Auditor...`);
      router.push(`/documents?analysisId=${res.analysisId}`);
    } catch (e2) {
      setErr(e2 instanceof Error ? e2.message : "Lỗi chạy AI Auditor.");
    } finally {
      setBusy(false);
    }
  }

  async function handleSignedDownload(fileId: string) {
    try {
      const res = await apiFetch<{ downloadUrl: string }>(`/api/v1/files/${fileId}/signed-url`);
      window.open(`${getApiBaseUrl()}${res.downloadUrl}`, "_blank", "noopener,noreferrer");
    } catch (e2) {
      setErr(e2 instanceof Error ? e2.message : "Không lấy được signed URL.");
    }
  }

  async function handleRunCalculator() {
    if (!farm) return;
    setBusy(true);
    setErr(null);
    setMsg(null);
    try {
      await apiFetch("/api/v1/carbon-calculator/estimate", {
        method: "POST",
        body: JSON.stringify({
          farmId: farm.id,
          areaHa: farm.areaHa,
          treeCount: farm.treeCount,
          variety: farm.variety,
          treeAgeYears: farm.ageYears,
        }),
      });
      setMsg("Đã tính toán tham khảo CTU 2015 qua C# API và lưu bản ghi lịch sử bất biến.");
      await load();
    } catch (e2) {
      setErr(e2 instanceof Error ? e2.message : "Lỗi tính calculator.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <AppShell title={farm ? farm.name : "Chi tiết Vườn dừa"} subtitle="Hồ sơ vườn, tài liệu riêng tư (Signed URL), AI Carbon Auditor và lịch sử tính toán CTU 2015">
      {err && <div className="mb-4 rounded-xl border border-red-300 bg-red-50 p-3 text-sm text-red-900">{err}</div>}
      {msg && <div className="mb-4 rounded-xl border border-emerald-300 bg-emerald-50 p-3 text-sm text-emerald-900">{msg}</div>}

      {farm && (
        <div className="space-y-6">
          <div className="rounded-xl border border-[#d8e2da] bg-white p-5">
            <div className="flex flex-wrap items-center justify-between gap-2">
              <div className="flex items-center gap-2">
                <OriginBadge tag={farm.dataOriginTag} />
                <span className="rounded-full bg-[#e8f5ec] px-2.5 py-0.5 text-xs font-semibold text-[#15803d]">{farm.status}</span>
              </div>
              <button
                type="button"
                disabled={busy}
                onClick={handleRunCalculator}
                className="inline-flex items-center gap-1.5 rounded-lg bg-[#123c29] px-3.5 py-2 text-xs font-semibold text-white hover:bg-[#15803d]"
              >
                <Calculator className="h-4 w-4" /> Chạy Calculator CTU 2015 cho vườn này
              </button>
            </div>
            <div className="mt-4 grid gap-3 text-sm sm:grid-cols-3">
              <div><span className="text-[#526158]">Địa bàn:</span> {farm.commune}, {farm.province} ({farm.historicalRegion})</div>
              <div><span className="text-[#526158]">Diện tích / Số cây:</span> <strong>{farm.areaHa} ha</strong> / <strong>{farm.treeCount} cây</strong></div>
              <div><span className="text-[#526158]">Giống / Tuổi:</span> {farm.variety} / {farm.ageYears} năm</div>
              <div className="flex items-center gap-1 sm:col-span-3 text-xs text-[#526158]">
                <MapPin className="h-3.5 w-3.5" /> Tọa độ:{" "}
                {farm.latitude && farm.longitude ? `${farm.latitude}, ${farm.longitude}` : "Chưa khai báo"}
              </div>
            </div>
          </div>

          <div className="grid gap-6 lg:grid-cols-2">
            <div className="rounded-xl border border-[#d8e2da] bg-white p-5">
              <h2 className="flex items-center gap-2 text-base font-bold text-[#123c29]">
                <FileUp className="h-5 w-5 text-[#15803d]" /> Trung tâm Tài liệu Riêng tư
              </h2>
              <form onSubmit={handleUpload} className="mt-3 space-y-3 rounded-lg border border-[#e2eae3] bg-[#f9fbf9] p-3">
                <select value={category} onChange={(e) => setCategory(e.target.value)} className="w-full rounded-lg border border-[#d8e2da] bg-white px-3 py-2 text-sm">
                  <option value="LandDocument">Giấy tờ đất / Quyền sử dụng (PDF/Ảnh)</option>
                  <option value="FarmPhoto">Ảnh hiện trường vườn dừa</option>
                  <option value="MonitoringReport">Báo cáo / Ghi chép chăm sóc</option>
                </select>
                <input
                  type="file"
                  accept=".pdf,.jpg,.jpeg,.png,application/pdf,image/jpeg,image/png"
                  onChange={(e) => setFile(e.target.files?.[0] ?? null)}
                  className="block w-full text-xs"
                />
                <button
                  type="submit"
                  disabled={!file || busy}
                  className="rounded-lg bg-[#15803d] px-4 py-2 text-xs font-semibold text-white hover:bg-[#123c29] disabled:opacity-50"
                >
                  Tải lên tài liệu
                </button>
              </form>

              <div className="mt-4 space-y-2">
                {farm.files.length === 0 ? (
                  <p className="text-sm text-[#526158]">Chưa có tài liệu nào được tải lên.</p>
                ) : (
                  farm.files.map((f) => (
                    <div key={f.id} className="flex flex-col gap-2 rounded-lg border border-[#e2eae3] p-3 sm:flex-row sm:items-center sm:justify-between">
                      <div>
                        <p className="text-sm font-semibold text-[#123c29]">{f.fileName} (v{f.version})</p>
                        <p className="text-[11px] text-[#526158]">{f.mimeType} • {(f.sizeBytes / 1024).toFixed(1)} KB • SHA256: {f.sha256Hash.slice(0, 12)}...</p>
                      </div>
                      <div className="flex gap-2">
                        <button
                          type="button"
                          onClick={() => handleSignedDownload(f.id)}
                          className="inline-flex items-center gap-1 rounded-lg border border-[#d8e2da] px-2.5 py-1.5 text-xs font-medium hover:bg-stone-100"
                        >
                          <Download className="h-3.5 w-3.5" /> Tải
                        </button>
                        <button
                          type="button"
                          disabled={busy}
                          onClick={() => handleRunAi(f.id)}
                          className="inline-flex items-center gap-1 rounded-lg bg-[#123c29] px-2.5 py-1.5 text-xs font-semibold text-white hover:bg-[#15803d]"
                        >
                          <Bot className="h-3.5 w-3.5" /> Phân tích AI
                        </button>
                      </div>
                    </div>
                  ))
                )}
              </div>
            </div>

            <div className="rounded-xl border border-[#d8e2da] bg-white p-5">
              <h2 className="text-base font-bold text-[#123c29]">Lịch sử tính toán tham khảo CTU 2015</h2>
              <div className="mt-3 space-y-3">
                {farm.calculations.length === 0 ? (
                  <p className="text-sm text-[#526158]">Chưa có bản tính nào cho vườn này.</p>
                ) : (
                  farm.calculations.map((c) => (
                    <div key={c.id} className="rounded-lg border border-[#e2eae3] p-3">
                      <p className="text-lg font-bold text-[#15803d]">{c.estimatedCO2EquivalentT} tCO2 tương đương tham khảo</p>
                      <p className="mt-0.5 text-xs text-[#526158]">
                        Mật độ quan sát: {c.observedTreeDensityPerHa} cây/ha • Phiên bản: {c.factorVersion} ({c.formulaVersion})
                      </p>
                      <p className="mt-1 text-[11px] text-[#526158]">{c.sourceCitation}</p>
                    </div>
                  ))
                )}
              </div>
            </div>
          </div>

          <div className="rounded-xl border border-[#d8e2da] bg-white p-5 space-y-4">
            <div className="flex flex-wrap items-center justify-between gap-2">
              <h2 className="flex items-center gap-2 text-base font-bold text-[#123c29]">
                <Wallet className="h-5 w-5 text-[#15803d]" /> Sổ Tài chính Xanh & Ngân sách Chuẩn bị MRV (USER-DECLARED)
              </h2>
              {ledger && (
                <div className="flex flex-wrap gap-3 text-xs">
                  <span>Chi phí xanh: <strong>{ledger.summary.totalDeclaredExpenseVnd.toLocaleString("vi-VN")} đ</strong></span>
                  <span>Thu nông sản/phụ phẩm: <strong>{ledger.summary.totalDeclaredAgriAndSupportVnd.toLocaleString("vi-VN")} đ</strong></span>
                  <span>Dòng tiền ròng: <strong className="text-[#15803d]">{ledger.summary.netDeclaredFarmCashflowVnd.toLocaleString("vi-VN")} đ</strong></span>
                </div>
              )}
            </div>
            <p className="text-xs text-[#526158]">
              {ledger?.summary.scientificBoundaryNotice || "Chỉ ghi nhận chi phí chăm sóc thực tế, phụ phẩm dừa và ngân sách chuẩn bị hồ sơ; không phải doanh thu tín chỉ carbon."}
            </p>
            <form onSubmit={handleAddFinanceEntry} className="grid gap-2 sm:grid-cols-4">
              <select value={entryType} onChange={(e) => setEntryType(e.target.value)} className="rounded-lg border border-[#d8e2da] px-3 py-2 text-xs">
                <option value="CareExpense">Chi phí chăm sóc xanh</option>
                <option value="MrvPreparationExpense">Chi phí chuẩn bị hồ sơ MRV</option>
                <option value="CoconutHarvestRevenue">Thu nông sản dừa</option>
                <option value="ByProductRevenue">Thu phụ phẩm (gáo/mụn/xơ)</option>
                <option value="GreenSupportGrantOrLoan">Vốn ưu đãi / Hỗ trợ xanh</option>
              </select>
              <input value={entryCategory} onChange={(e) => setEntryCategory(e.target.value)} required placeholder="Hạng mục" className="rounded-lg border border-[#d8e2da] px-3 py-2 text-xs" />
              <input type="number" min="1000" step="1000" value={entryAmount} onChange={(e) => setEntryAmount(e.target.value)} required placeholder="Số tiền (VND)" className="rounded-lg border border-[#d8e2da] px-3 py-2 text-xs" />
              <button type="submit" disabled={busy} className="rounded-lg bg-[#123c29] px-3 py-2 text-xs font-semibold text-white hover:bg-[#15803d]">
                + Ghi nhận thu/chi
              </button>
            </form>
            {ledger && ledger.entries.length > 0 && (
              <div className="divide-y divide-[#eef3ef] text-xs">
                {ledger.entries.map((en) => (
                  <div key={en.id} className="flex flex-wrap items-center justify-between gap-2 py-2">
                    <span><strong>{en.category}</strong> ({en.entryType})</span>
                    <span className="font-semibold text-[#123c29]">{en.amountVnd.toLocaleString("vi-VN")} đ</span>
                  </div>
                ))}
              </div>
            )}
          </div>

          <ScientificDisclaimerBanner />
        </div>
      )}
    </AppShell>
  );
}
