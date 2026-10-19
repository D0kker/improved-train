import type { Metadata } from "next";
import { SkipLink } from "@/src/skip-link";
import "./globals.css";

export const metadata: Metadata = {
  title: "PROJECT ECHO",
  description:
    "Descubre con quién ya habías jugado usando tu historial de League of Legends.",
};

export default function RootLayout({
  children,
}: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="es">
      <body>
        <SkipLink />
        {children}
      </body>
    </html>
  );
}
