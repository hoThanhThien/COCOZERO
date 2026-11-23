import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "CocoCarbon AI — Nền tảng Dữ liệu Carbon & Chuỗi Giá trị Dừa",
  description:
    "Số hóa vườn dừa, ước tính tham khảo CTU 2015, rà soát hồ sơ AI có dẫn chứng và kết nối dự án vùng dừa Bến Tre - Vĩnh Long.",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="vi">
      <body className="min-h-screen antialiased">{children}</body>
    </html>
  );
}
