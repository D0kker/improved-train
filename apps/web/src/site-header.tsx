"use client";

import Link from "next/link";
import { useLocale, useTranslations } from "next-intl";
import { localePreferenceCookie, locales, type Locale } from "./i18n/locales";

export function SiteHeader({
  actionLabel,
  actionHref = "/",
  onLocaleChange,
}: {
  actionLabel?: string;
  actionHref?: string;
  onLocaleChange?: (locale: Locale) => void;
}) {
  const locale = useLocale() as Locale;
  const t = useTranslations("common");

  function changeLocale(nextLocale: Locale) {
    document.cookie = localePreferenceCookie(
      nextLocale,
      window.location.protocol,
    );
    if (onLocaleChange) onLocaleChange(nextLocale);
    else window.location.reload();
  }

  return (
    <header className="topbar">
      <nav aria-label={t("navigation")}>
        <Link
          className="wordmark"
          href="/"
          aria-label={`PROJECT ECHO — ${t("home")}`}
        >
          <span className="wordmark-icon" aria-hidden="true">
            E
          </span>
          <span>PROJECT ECHO</span>
        </Link>
        <div className="topbar-actions">
          <label className="locale-control">
            <span className="sr-only">{t("language")}</span>
            <select
              aria-label={t("language")}
              value={locale}
              onChange={(event) => changeLocale(event.target.value as Locale)}
            >
              {locales.map((item) => (
                <option key={item} value={item}>
                  {item === "es"
                    ? "Español"
                    : item === "en"
                      ? "English"
                      : "Português (Brasil)"}
                </option>
              ))}
            </select>
          </label>
          <Link className="text-link" href={actionHref}>
            {actionLabel ?? t("search")}
          </Link>
        </div>
      </nav>
    </header>
  );
}
