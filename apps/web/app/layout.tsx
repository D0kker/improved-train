import type { Metadata } from "next";
import { SkipLink } from "@/src/skip-link";
import { NextIntlClientProvider } from "next-intl";
import { getLocale, getMessages } from "next-intl/server";
import type { Locale } from "@/src/i18n/locales";
import "./globals.css";

export async function generateMetadata(): Promise<Metadata> {
  const locale = (await getLocale()) as Locale;
  const descriptions = {
    es: "Descubre con quién ya habías jugado usando tu historial de League of Legends.",
    en: "Explore previous encounters using your League of Legends match history.",
    "pt-BR":
      "Descubra encontros anteriores usando seu histórico de partidas de League of Legends.",
  };
  return { title: "PROJECT ECHO", description: descriptions[locale] };
}

export default async function RootLayout({
  children,
}: Readonly<{ children: React.ReactNode }>) {
  const [locale, messages] = await Promise.all([getLocale(), getMessages()]);
  return (
    <html lang={locale}>
      <body>
        <NextIntlClientProvider locale={locale} messages={messages}>
          <SkipLink />
          {children}
        </NextIntlClientProvider>
      </body>
    </html>
  );
}
