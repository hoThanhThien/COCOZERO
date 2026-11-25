"use client";

import { useEffect, useState } from "react";
import { Building2, MapPin } from "lucide-react";
import { AppShell } from "@/components/AppShell";
import { OriginBadge, ScientificDisclaimerBanner } from "@/components/Badges";
import { apiFetch, getStoredToken } from "@/lib/api";

interface CooperativeSummary {
  id: string;
  name: string;
  code: string;
  province: string;
  commune: string;
  historicalRegion: string;
}

interface CooperativeDashboardDto {
  cooperative: CooperativeSummary & { dataOriginTag: string };
  summary: {
    authorizedMemberCount: number;
    authorizedFarmCount: number;
    totalDeclaredAreaHa: number;
    totalDeclaredTrees: number;
    farmsReadyForReview: number;
    farmsNeedingRevision: number;
    scientificNotice: string;
  };
  farms: {
    id: string;
    name: string;
    commune: string;
    areaHa: number;
    treeCount: number;
    variety: string;
    ageYears: number;
    latitude?: number | null;
    longitude?: number | null;
    status: string;
    dataOriginTag: string;
  }[];
}

export default function CooperativePage() {
  const [coops, setCoops] = useState<CooperativeSummary[]>([]);
  const [dashboard, setDashboard] = useState<CooperativeDashboardDto | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!getStoredToken()) return;
    apiFetch<CooperativeSummary[]>("/api/v1/cooperatives")
      .then((list) => {
        setCoops(list);
        if (list[0]) {
          return apiFetch<CooperativeDashboardDto>(`/api/v1/cooperatives/${list[0].id}/dashboard`).then(setDashboard);
        }
      })
      .catch((e) => setError(e instanceof Error ? e.message : "Chỉ Quản lý HTX được ủy quyền hoặc Admin mới xem được dashboard HTX."));
  }, []);

  return (
    <AppShell
      title="Không gian Hợp tác xã (Cooperative Workspace)"
      subtitle="Tổng hợp vùng nguyên liệu theo ủy quyền; KHÔNG tự động coi là dự án carbon đủ điều kiện hay tín chỉ đã phát hành"
    >
      {error && <div className="mb-4 rounded-xl border border-amber-300 bg-amber-50 p-3 text-sm text-amber-950">{error}</div>}

      {!dashboard ? (
        <div className="rounded-xl border border-[#d8e2da] bg-white p-6 text-sm text-[#526158]">
          {coops.length === 0
            ? "Đăng nhập bằng tài khoản Quản lý HTX (hoặc Admin) để xem dashboard tổng hợp vùng nguyên liệu."
            : "Đang tải dashboard hợp tác xã..."}
        </div>
      ) : (
        <div className="space-y-6">
          <div className="rounded-xl border border-[#d8e2da] bg-white p-5">
            <div className="flex flex-wrap items-center justify-between gap-2">
              <div>
                <h2 className="flex items-center gap-2 text-lg font-bold text-[#123c29]">
                  <Building2 className="h-5 w-5 text-[#15803d]" /> {dashboard.cooperative.name}
                </h2>
                <p className="text-xs text-[#526158]">
                  Mã HTX: {dashboard.cooperative.code} • {dashboard.cooperative.commune}, {dashboard.cooperative.province} ({dashboard.cooperative.historicalRegion})
                </p>
              </div>
              <OriginBadge tag={dashboard.cooperative.dataOriginTag} />
            </div>

            <div className="mt-4 grid grid-cols-2 gap-3 lg:grid-cols-4">
              <div className="rounded-lg border border-[#e2eae3] p-3">
                <p className="text-xs text-[#526158]">Thành viên ủy quyền</p>
                <p className="text-2xl font-bold text-[#123c29]">{dashboard.summary.authorizedMemberCount}</p>
              </div>
              <div className="rounded-lg border border-[#e2eae3] p-3">
                <p className="text-xs text-[#526158]">Số vườn ủy quyền</p>
                <p className="text-2xl font-bold text-[#123c29]">{dashboard.summary.authorizedFarmCount}</p>
              </div>
              <div className="rounded-lg border border-[#e2eae3] p-3">
                <p className="text-xs text-[#526158]">Tổng diện tích khai báo</p>
                <p className="text-2xl font-bold text-[#123c29]">{dashboard.summary.totalDeclaredAreaHa} ha</p>
              </div>
              <div className="rounded-lg border border-[#e2eae3] p-3">
                <p className="text-xs text-[#526158]">Tổng số cây khai báo</p>
                <p className="text-2xl font-bold text-[#123c29]">{dashboard.summary.totalDeclaredTrees}</p>
              </div>
            </div>
            <p className="mt-3 text-xs font-medium text-amber-900">{dashboard.summary.scientificNotice}</p>
          </div>

          <div className="rounded-xl border border-[#d8e2da] bg-white p-5">
            <h3 className="text-base font-bold text-[#123c29]">Danh sách vườn được ủy quyền & Tọa độ hiện trường</h3>
            <div className="mt-3 overflow-x-auto">
              <table className="w-full min-w-[600px] text-left text-xs">
                <thead>
                  <tr className="border-b border-[#e2eae3] text-[#526158]">
                    <th className="py-2 pr-3">Tên vườn</th>
                    <th className="py-2 pr-3">Xã</th>
                    <th className="py-2 pr-3">Diện tích</th>
                    <th className="py-2 pr-3">Số cây</th>
                    <th className="py-2 pr-3">Tọa độ</th>
                    <th className="py-2">Trạng thái</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-[#eef3ef]">
                  {dashboard.farms.map((f) => (
                    <tr key={f.id}>
                      <td className="py-2.5 pr-3 font-semibold text-[#123c29]">{f.name}</td>
                      <td className="py-2.5 pr-3">{f.commune}</td>
                      <td className="py-2.5 pr-3">{f.areaHa} ha</td>
                      <td className="py-2.5 pr-3">{f.treeCount}</td>
                      <td className="py-2.5 pr-3">
                        <span className="inline-flex items-center gap-1">
                          <MapPin className="h-3 w-3 text-[#15803d]" />
                          {f.latitude && f.longitude ? `${f.latitude.toFixed(4)}, ${f.longitude.toFixed(4)}` : "Chưa có"}
                        </span>
                      </td>
                      <td className="py-2.5">{f.status}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>

          <ScientificDisclaimerBanner text={dashboard.summary.scientificNotice} />
        </div>
      )}
    </AppShell>
  );
}
