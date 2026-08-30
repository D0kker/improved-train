import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "LoL Network Analyzer",
  description:
    "Análisis histórico de jugadores recurrentes en League of Legends.",
};

export default function RootLayout({
  children,
}: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="es">
      <body>{children}</body>
    </html>
  );
}
