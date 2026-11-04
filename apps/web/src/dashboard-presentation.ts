/** Compact localized display for the dashboard, retaining the viewer's timezone. */
export function formatDashboardDate(
  value: string | null,
  locale = "es-PA",
): string {
  if (!value)
    return locale.startsWith("en")
      ? "Date unavailable"
      : locale.toLowerCase() === "pt-br"
        ? "Data indisponível"
        : "Fecha no disponible";
  const date = new Date(value);
  if (Number.isNaN(date.getTime()))
    return locale.startsWith("en")
      ? "Date unavailable"
      : locale.toLowerCase() === "pt-br"
        ? "Data indisponível"
        : "Fecha no disponible";
  return new Intl.DateTimeFormat(locale, {
    day: "numeric",
    month: "short",
    year: "numeric",
    hour: "numeric",
    minute: "2-digit",
    hour12: locale.startsWith("en") || locale === "es-PA",
    timeZone: "UTC",
    timeZoneName: "short",
  }).format(date);
}
