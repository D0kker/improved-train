const platformNames: Record<string, Record<string, string>> = {
  br1: { es: "Brasil", en: "Brazil", "pt-BR": "Brasil" },
  eun1: {
    es: "Europa Nórdica y Este",
    en: "Europe Nordic & East",
    "pt-BR": "Europa Nórdica e Oriental",
  },
  euw1: { es: "Europa Oeste", en: "Europe West", "pt-BR": "Europa Ocidental" },
  jp1: { es: "Japón", en: "Japan", "pt-BR": "Japão" },
  kr: { es: "Corea", en: "Korea", "pt-BR": "Coreia" },
  la1: {
    es: "Latinoamérica Norte (LAN)",
    en: "Latin America North (LAN)",
    "pt-BR": "América Latina Norte (LAN)",
  },
  la2: {
    es: "Latinoamérica Sur (LAS)",
    en: "Latin America South (LAS)",
    "pt-BR": "América Latina Sul (LAS)",
  },
  na1: { es: "Norteamérica", en: "North America", "pt-BR": "América do Norte" },
  oc1: { es: "Oceanía", en: "Oceania", "pt-BR": "Oceania" },
  ph2: { es: "Filipinas", en: "Philippines", "pt-BR": "Filipinas" },
  ru: { es: "Rusia", en: "Russia", "pt-BR": "Rússia" },
  sg2: { es: "Singapur", en: "Singapore", "pt-BR": "Singapura" },
  th2: { es: "Tailandia", en: "Thailand", "pt-BR": "Tailândia" },
  tr1: { es: "Turquía", en: "Türkiye", "pt-BR": "Turquia" },
  tw2: { es: "Taiwán", en: "Taiwan", "pt-BR": "Taiwan" },
  vn2: { es: "Vietnam", en: "Vietnam", "pt-BR": "Vietnã" },
};

function translatedRegion(value: string, locale: string) {
  const names = platformNames[value.toLowerCase()];
  return names?.[locale] ?? names?.es;
}

export function platformRegionLabel(value?: string, locale = "es"): string {
  const region = value?.trim();
  if (!region)
    return locale === "en"
      ? "Region unavailable"
      : locale === "pt-BR"
        ? "Região indisponível"
        : "Región no disponible";
  const fallback =
    locale === "en"
      ? "Configured region"
      : locale === "pt-BR"
        ? "Região configurada"
        : "Región configurada";
  return `${translatedRegion(region, locale) ?? fallback} · ${region.toUpperCase()}`;
}

/** Display name only; routing continues using the platform code. */
export function platformRegionName(value?: string, locale = "es"): string {
  const region = value?.trim();
  if (!region)
    return locale === "en"
      ? "Region unavailable"
      : locale === "pt-BR"
        ? "Região indisponível"
        : "Región no disponible";
  return (
    translatedRegion(region, locale) ??
    (locale === "en"
      ? "Configured region"
      : locale === "pt-BR"
        ? "Região configurada"
        : "Región configurada")
  );
}
