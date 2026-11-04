export function formatPercent(value: number, locale = "es-PA"): string {
  return `${new Intl.NumberFormat(locale, { maximumFractionDigits: 1 }).format(value)}%`;
}

export function formatDuration(
  totalSeconds: number | null,
  locale = "es-PA",
): string {
  if (totalSeconds === null) return "—";
  const safeSeconds = Math.max(0, Math.round(totalSeconds));
  const minutes = Math.floor(safeSeconds / 60);
  const seconds = safeSeconds % 60;
  if (locale.startsWith("en")) return `${minutes}m ${seconds}s`;
  if (locale.toLowerCase() === "pt-br") return `${minutes}min ${seconds}s`;
  if (locale !== "es-PA") return `${minutes} min ${seconds} s`;
  return `${minutes}:${seconds.toString().padStart(2, "0")}`;
}

export function formatDate(value: string | null, locale = "es-PA"): string {
  if (!value)
    return locale.startsWith("en")
      ? "Date unavailable"
      : locale.toLowerCase() === "pt-br"
        ? "Data indisponível"
        : "Fecha no disponible";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return locale.startsWith("en")
      ? "Date unavailable"
      : locale.toLowerCase() === "pt-br"
        ? "Data indisponível"
        : "Fecha no disponible";
  }

  return new Intl.DateTimeFormat(locale, {
    day: "numeric",
    month: "short",
    year: "numeric",
    hour: "numeric",
    minute: "2-digit",
    timeZone: "UTC",
    timeZoneName: "short",
  }).format(date);
}

export function winRate(wins: number, games: number, locale = "es-PA"): string {
  return games > 0 ? formatPercent((wins * 100) / games, locale) : "—";
}

export function queueLabel(queueId: number | null, locale = "es-PA"): string {
  const language = locale.toLowerCase();
  if (queueId === null)
    return language.startsWith("en")
      ? "Queue unavailable"
      : language === "pt-br"
        ? "Fila indisponível"
        : "Cola no disponible";
  const names: Record<number, Record<string, string>> = {
    400: {
      es: "Normal reclutamiento",
      en: "Normal Draft",
      "pt-br": "Escolha às cegas normal",
    },
    420: {
      es: "Clasificatoria Solo/Dúo",
      en: "Ranked Solo/Duo",
      "pt-br": "Ranqueada Solo/Duo",
    },
    430: {
      es: "Normal a ciegas",
      en: "Normal Blind Pick",
      "pt-br": "Escolha às cegas normal",
    },
    440: {
      es: "Clasificatoria Flexible",
      en: "Ranked Flex",
      "pt-br": "Ranqueada Flexível",
    },
    450: { es: "ARAM", en: "ARAM", "pt-br": "ARAM" },
    490: { es: "Partida rápida", en: "Quickplay", "pt-br": "Jogo rápido" },
  };
  const languageKey =
    language === "pt-br" ? "pt-br" : language.startsWith("en") ? "en" : "es";
  return (
    names[queueId]?.[languageKey] ??
    `${language.startsWith("en") ? "Queue" : languageKey === "pt-br" ? "Fila" : "Cola"} ${queueId}`
  );
}
