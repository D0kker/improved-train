export const locales = ["es", "en", "pt-BR"] as const;
export type Locale = (typeof locales)[number];
export const defaultLocale: Locale = "es";

export function isLocale(value: string | undefined): value is Locale {
  return locales.some((locale) => locale === value);
}

export function resolveLocale(
  explicitPreference: string | undefined,
  acceptLanguage: string | null,
): Locale {
  return isLocale(explicitPreference)
    ? explicitPreference
    : negotiateLocale(acceptLanguage);
}

export function localePreferenceCookie(
  locale: Locale,
  protocol: string,
): string {
  const secure = protocol === "https:" ? "; Secure" : "";
  return `echo-locale=${encodeURIComponent(locale)}; Path=/; Max-Age=31536000; SameSite=Lax${secure}`;
}

export function negotiateLocale(acceptLanguage: string | null): Locale {
  if (!acceptLanguage) return defaultLocale;
  const choices = acceptLanguage
    .split(",")
    .map((part, index) => {
      const [tag, ...params] = part.trim().split(";");
      const q = Number(
        params
          .find((p) => p.trim().startsWith("q="))
          ?.trim()
          .slice(2) ?? 1,
      );
      return { tag: tag.toLowerCase(), q, index };
    })
    .filter((choice) => Number.isFinite(choice.q) && choice.q > 0)
    .sort((a, b) => b.q - a.q || a.index - b.index);

  for (const { tag } of choices) {
    if (
      tag === "pt" ||
      tag.startsWith("pt-") ||
      tag === "pt-br" ||
      tag.startsWith("pt-br-")
    )
      return "pt-BR";
    if (tag === "en" || tag.startsWith("en-")) return "en";
    if (tag === "es" || tag.startsWith("es-")) return "es";
  }
  return defaultLocale;
}
