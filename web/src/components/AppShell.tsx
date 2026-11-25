"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import {
  Bot,
  Building2,
  Calculator,
  FileCheck2,
  FolderKanban,
  LayoutDashboard,
  LogOut,
  Menu,
  ShieldCheck,
  ShoppingBag,
  Sprout,
  Trees,
  UserCheck,
  X,
} from "lucide-react";
import { apiFetch, getStoredToken } from "@/lib/api";
import { signOutFirebaseIfActive } from "@/lib/firebase";
import type { UserProfileDto } from "@/lib/types";

const NAV_ITEMS = [
  { href: "/dashboard", label: "Bảng điều khiển", icon: LayoutDashboard },
  { href: "/farms", label: "Hồ sơ Vườn dừa", icon: Trees },
  { href: "/calculator", label: "Calculator CTU 2015", icon: Calculator },
  { href: "/documents", label: "Tài liệu & AI Auditor", icon: FileCheck2 },
  { href: "/cooperative", label: "Hợp tác xã", icon: Building2 },
  { href: "/projects", label: "Danh mục Dự án", icon: FolderKanban },
  { href: "/buyer", label: "Không gian Buyer / RFQ", icon: ShoppingBag },
  { href: "/financial-assistant", label: "Trợ lý Tài chính AI", icon: Calculator },
  { href: "/assistant", label: "Trợ lý Khoa học RAG", icon: Bot },
  { href: "/onboarding", label: "Hồ sơ & Đồng thuận", icon: UserCheck },
  { href: "/admin", label: "Quản trị & Audit", icon: ShieldCheck },
];

export function AppShell({
  title,
  subtitle,
  children,
}: {
  title: string;
  subtitle?: string;
  children: React.ReactNode;
}) {
  const pathname = usePathname();
  const router = useRouter();
  const [me, setMe] = useState<UserProfileDto | null>(null);
  const [mobileOpen, setMobileOpen] = useState(false);

  useEffect(() => {
    if (!getStoredToken()) return;
    apiFetch<UserProfileDto>("/api/v1/me")
      .then(setMe)
      .catch(() => {
        // Public routes like /projects or /calculator still work even if session expired
      });
  }, []);

  async function handleLogout() {
    await signOutFirebaseIfActive();
    setMe(null);
    router.push("/login");
  }

  return (
    <div className="min-h-screen bg-[#f6f8f5] text-[#1b241f]">
      {/* Mobile Header */}
      <header className="sticky top-0 z-30 flex items-center justify-between border-b border-[#d8e2da] bg-white px-4 py-3 lg:hidden">
        <Link href="/" className="flex items-center gap-2 font-bold text-[#123c29]">
          <Sprout className="h-5 w-5 text-[#15803d]" />
          <span>CocoCarbon AI</span>
        </Link>
        <button
          type="button"
          onClick={() => setMobileOpen(!mobileOpen)}
          aria-label="Mở menu điều hướng"
          className="rounded-lg border border-[#d8e2da] p-2 text-[#123c29]"
        >
          {mobileOpen ? <X className="h-5 w-5" /> : <Menu className="h-5 w-5" />}
        </button>
      </header>

      <div className="mx-auto flex max-w-[1440px]">
        {mobileOpen && (
          <div
            onClick={() => setMobileOpen(false)}
            aria-hidden="true"
            className="fixed inset-0 z-30 bg-black/40 backdrop-blur-[1px] lg:hidden"
          />
        )}

        {/* Sidebar */}
        <aside
          className={`${
            mobileOpen ? "fixed inset-y-0 left-0 z-40 block w-72 shadow-xl" : "hidden"
          } overflow-y-auto border-r border-[#d8e2da] bg-white lg:sticky lg:top-0 lg:block lg:h-screen lg:w-64 lg:shrink-0`}
        >
          <div className="flex min-h-full flex-col justify-between p-4">
            <div>
              <div className="mb-2 flex items-center justify-between lg:hidden">
                <Link href="/" onClick={() => setMobileOpen(false)} className="flex items-center gap-2 font-bold text-[#123c29]">
                  <Sprout className="h-5 w-5 text-[#15803d]" />
                  <span>CocoCarbon AI</span>
                </Link>
                <button
                  type="button"
                  onClick={() => setMobileOpen(false)}
                  aria-label="Đóng menu"
                  className="rounded-lg border border-[#d8e2da] p-1.5 text-[#123c29]"
                >
                  <X className="h-4 w-4" />
                </button>
              </div>
              <Link href="/" className="hidden items-center gap-2.5 px-2 py-2 lg:flex">
                <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-[#123c29] text-white">
                  <Sprout className="h-5 w-5" />
                </div>
                <div>
                  <p className="text-sm font-bold text-[#123c29]">CocoCarbon AI</p>
                  <p className="text-[11px] text-[#526158]">Vùng dừa Bến Tre • Vĩnh Long</p>
                </div>
              </Link>

              <nav className="mt-4 space-y-1">
                {NAV_ITEMS.map((item) => {
                  const Icon = item.icon;
                  const active = pathname === item.href || pathname?.startsWith(`${item.href}/`);
                  return (
                    <Link
                      key={item.href}
                      href={item.href}
                      onClick={() => setMobileOpen(false)}
                      className={`flex items-center gap-2.5 rounded-xl px-3 py-2.5 text-sm font-medium transition ${
                        active
                          ? "bg-[#123c29] text-white"
                          : "text-[#35443c] hover:bg-[#e8f5ec] hover:text-[#123c29]"
                      }`}
                    >
                      <Icon className="h-4 w-4 shrink-0" />
                      <span>{item.label}</span>
                    </Link>
                  );
                })}
              </nav>
            </div>

            <div className="mt-4 rounded-xl border border-[#d8e2da] bg-[#f7faf7] p-3">
              {me ? (
                <div className="space-y-2">
                  <div>
                    <p className="truncate text-xs font-bold text-[#123c29]">{me.displayName}</p>
                    <p className="truncate text-[11px] text-[#526158]">{me.email}</p>
                    <div className="mt-1 flex flex-wrap gap-1">
                      {me.roles
                        .filter((r) => r.approvalStatus === "Approved")
                        .map((r) => (
                          <span
                            key={r.roleCode}
                            className="rounded bg-[#e8f5ec] px-1.5 py-0.5 text-[10px] font-semibold text-[#15803d]"
                          >
                            {r.roleCode}
                          </span>
                        ))}
                    </div>
                  </div>
                  <button
                    type="button"
                    onClick={handleLogout}
                    className="flex w-full items-center justify-center gap-1.5 rounded-lg border border-[#d8e2da] bg-white px-2.5 py-1.5 text-xs font-medium text-stone-700 hover:bg-stone-100"
                  >
                    <LogOut className="h-3.5 w-3.5" />
                    Đăng xuất
                  </button>
                </div>
              ) : (
                <div className="space-y-2 text-xs">
                  <p className="text-[#526158]">Chưa đăng nhập phiên làm việc.</p>
                  <Link
                    href="/login"
                    className="block w-full rounded-lg bg-[#15803d] py-2 text-center font-semibold text-white hover:bg-[#123c29]"
                  >
                    Đăng nhập Google / Demo
                  </Link>
                </div>
              )}
            </div>
          </div>
        </aside>

        {/* Main Content */}
        <main className="min-w-0 flex-1 p-3 sm:p-6 lg:p-8">
          <div className="mb-5 flex flex-col gap-2 border-b border-[#d8e2da] pb-4 sm:mb-6 sm:flex-row sm:items-end sm:justify-between">
            <div className="min-w-0">
              <h1 className="text-xl font-bold tracking-tight text-[#123c29] sm:text-2xl">{title}</h1>
              {subtitle && <p className="mt-1 text-xs leading-relaxed text-[#526158] sm:text-sm">{subtitle}</p>}
            </div>
            <div className="shrink-0 text-[11px] text-[#526158] sm:text-xs">
              Nền tảng chuẩn hóa dữ liệu • Không thay thế tổ chức thẩm định/registry
            </div>
          </div>
          {children}
        </main>
      </div>
    </div>
  );
}
