"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { Calculator, FileCheck2, FolderKanban, Plus, Trees } from "lucide-react";
import { AppShell } from "@/components/AppShell";
import { OriginBadge, ScientificDisclaimerBanner } from "@/components/Badges";
import { apiFetch, getStoredToken } from "@/lib/api";
import type { FarmDto, UserProfileDto } from "@/lib/types";

export default function DashboardPage() {
  const [me, setMe] = useState<UserProfileDto | null>(null);
  const [farms, setFarms] = useState<FarmDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!getStoredToken()) {
      setLoading(false);
      return;
    }
    Promise.all([apiFetch<UserProfileDto>("/api/v1/me"), apiFetch<FarmDto[]>("/api/v1/farms")])
      .then(([profile, farmList]) => {
        setMe(profile);
        setFarms(farmList);
      })
      .catch((e) => setError(e instanceof Error ? e.message : "Không thể tải bảng điều khiển."))
      .finally(() => setLoading(false));
  }, []);

  const totalArea = farms.reduce((acc, f) => acc + f.areaHa, 0);
  const totalTrees = farms.reduce((acc, f) => acc + f.treeCount, 0);

  return (
    <AppShell
      title="Bảng điều khiển Nông hộ & Dữ liệu Vườn"
      subtitle="Tổng quan hồ sơ vườn dừa, trạng thái rà soát và các tác vụ cần hoàn thiện"
    >
      {!getStoredToken() ? (
        <div className="rounded-xl border border-[#d8e2da] bg-white p-6 text-center">
          <p className="text-sm text-[#526158]">Bạn cần đăng nhập Google hoặc chế độ Demo để xem vườn dừa của mình.</p>
          <Link href="/login" className="mt-3 inline-block rounded-lg bg-[#15803d] px-4 py-2 text-sm font-semibold text-white">
            Đến trang đăng nhập
          </Link>
        </div>
      ) : loading ? (
        <div className="rounded-xl border border-[#d8e2da] bg-white p-6 text-sm text-[#526158]">Đang tải dữ liệu...</div>
      ) : error ? (
        <div className="rounded-xl border border-red-300 bg-red-50 p-4 text-sm text-red-900">{error}</div>
      ) : (
        <div className="space-y-6">
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            <div className="rounded-xl border border-[#d8e2da] bg-white p-4">
              <p className="text-xs font-medium text-[#526158]">Số vườn đang quản lý / truy cập</p>
              <p className="mt-1 text-2xl font-bold text-[#123c29]">{farms.length}</p>
            </div>
            <div className="rounded-xl border border-[#d8e2da] bg-white p-4">
              <p className="text-xs font-medium text-[#526158]">Tổng diện tích khai báo</p>
              <p className="mt-1 text-2xl font-bold text-[#123c29]">{totalArea.toFixed(2)} ha</p>
            </div>
            <div className="rounded-xl border border-[#d8e2da] bg-white p-4">
              <p className="text-xs font-medium text-[#526158]">Tổng số cây khai báo</p>
              <p className="mt-1 text-2xl font-bold text-[#123c29]">{totalTrees.toLocaleString("vi-VN")}</p>
            </div>
            <div className="rounded-xl border border-[#d8e2da] bg-white p-4">
              <p className="text-xs font-medium text-[#526158]">Đồng thuận đã ghi nhận</p>
              <p className="mt-1 text-2xl font-bold text-[#123c29]">{me?.consents.filter((c) => c.granted).length ?? 0}</p>
            </div>
          </div>

          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
            <Link href="/farms" className="flex items-center gap-2 rounded-xl bg-[#123c29] p-4 text-sm font-semibold text-white hover:bg-[#15803d]">
              <Plus className="h-4 w-4" /> Khai báo vườn dừa mới
            </Link>
            <Link href="/calculator" className="flex items-center gap-2 rounded-xl border border-[#d8e2da] bg-white p-4 text-sm font-semibold text-[#123c29] hover:bg-[#e8f5ec]">
              <Calculator className="h-4 w-4 text-[#15803d]" /> Tính ước tính CTU 2015
            </Link>
            <Link href="/documents" className="flex items-center gap-2 rounded-xl border border-[#d8e2da] bg-white p-4 text-sm font-semibold text-[#123c29] hover:bg-[#e8f5ec]">
              <FileCheck2 className="h-4 w-4 text-[#15803d]" /> Xem báo cáo AI Auditor
            </Link>
            <Link href="/projects" className="flex items-center gap-2 rounded-xl border border-[#d8e2da] bg-white p-4 text-sm font-semibold text-[#123c29] hover:bg-[#e8f5ec]">
              <FolderKanban className="h-4 w-4 text-[#15803d]" /> Danh mục Dự án
            </Link>
          </div>

          <div className="rounded-xl border border-[#cfe0d3] bg-white p-5">
            <h2 className="text-sm font-bold text-[#123c29]">Lộ trình 4 bước hướng dẫn (Human-Centered Guided Flow)</h2>
            <div className="mt-3 grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
              {[
                { step: "Bước 1", title: "Hồ sơ & Đồng thuận", href: "/onboarding", desc: "Xác nhận GPS, ảnh và bảo vệ dữ liệu cá nhân." },
                { step: "Bước 2", title: "Vườn dừa & Tài chính xanh", href: "/farms", desc: "Khai báo diện tích, số cây, chi phí chăm sóc thực tế." },
                { step: "Bước 3", title: "Calculator & AI Chống gian lận", href: "/calculator", desc: "Tính CTU 2015 và kiểm tra bất thường chứng từ." },
                { step: "Bước 4", title: "HTX & Kết nối RFQ", href: "/projects", desc: "Tổng hợp vùng nguyên liệu & tiếp nhận đối tác." },
              ].map((s) => (
                <Link key={s.step} href={s.href} className="rounded-lg border border-[#e2eae3] bg-[#f8fbf8] p-3 hover:border-[#15803d]">
                  <p className="text-[11px] font-bold uppercase text-[#15803d]">{s.step}</p>
                  <p className="mt-0.5 text-xs font-bold text-[#123c29]">{s.title}</p>
                  <p className="mt-1 text-[11px] text-[#526158]">{s.desc}</p>
                </Link>
              ))}
            </div>
            <div className="mt-3 grid gap-2 rounded-lg border border-[#e6ede7] bg-[#fafcfa] p-3 text-[11px] text-[#435149] sm:grid-cols-2 lg:grid-cols-4">
              <div><strong>Biomass:</strong> Khối lượng sinh khối cây dừa (tươi/khô).</div>
              <div><strong>Carbon stock:</strong> Trữ lượng C tại 1 thời điểm (chưa phải tín chỉ).</div>
              <div><strong>CO2e tham khảo:</strong> Giá trị CTU 2015 theo tuổi 4 hoặc 10 năm.</div>
              <div><strong>Issued credit:</strong> Chỉ do registry chính thức phát hành sau VVB.</div>
            </div>
          </div>

          <div className="rounded-xl border border-[#d8e2da] bg-white p-5">
            <div className="flex items-center justify-between">
              <h2 className="flex items-center gap-2 text-base font-bold text-[#123c29]">
                <Trees className="h-5 w-5 text-[#15803d]" /> Vườn dừa gần đây
              </h2>
              <Link href="/farms" className="text-xs font-semibold text-[#15803d] hover:underline">
                Xem tất cả →
              </Link>
            </div>
            {farms.length === 0 ? (
              <p className="mt-4 text-sm text-[#526158]">Chưa có hồ sơ vườn nào. Hãy tạo vườn đầu tiên để số hóa dữ liệu.</p>
            ) : (
              <div className="mt-4 divide-y divide-[#e6ede7]">
                {farms.slice(0, 5).map((farm) => (
                  <div key={farm.id} className="flex flex-col gap-2 py-3 sm:flex-row sm:items-center sm:justify-between">
                    <div>
                      <div className="flex flex-wrap items-center gap-2">
                        <Link href={`/farms/${farm.id}`} className="font-semibold text-[#123c29] hover:underline">
                          {farm.name}
                        </Link>
                        <OriginBadge tag={farm.dataOriginTag} />
                      </div>
                      <p className="mt-0.5 text-xs text-[#526158]">
                        {farm.commune}, {farm.province} ({farm.historicalRegion}) • {farm.areaHa} ha • {farm.treeCount} cây • {farm.variety} ({farm.ageYears} năm)
                      </p>
                    </div>
                    <Link
                      href={`/farms/${farm.id}`}
                      className="rounded-lg border border-[#d8e2da] px-3 py-1.5 text-xs font-semibold text-[#123c29] hover:bg-[#e8f5ec]"
                    >
                      Chi tiết & Upload tài liệu
                    </Link>
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
