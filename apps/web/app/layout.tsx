import type { Metadata } from "next";
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
        <a className="skip-link" href="#main-content">Saltar al contenido</a>
        {children}
      </body>
    </html>
  );
}
