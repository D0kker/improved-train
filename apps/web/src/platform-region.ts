const platformNames: Record<string, string> = {
  br1: "Brasil",
  eun1: "Europa Nórdica y Este",
  euw1: "Europa Oeste",
  jp1: "Japón",
  kr: "Corea",
  la1: "Latinoamérica Norte (LAN)",
  la2: "Latinoamérica Sur (LAS)",
  na1: "Norteamérica",
  oc1: "Oceanía",
  ph2: "Filipinas",
  ru: "Rusia",
  sg2: "Singapur",
  th2: "Tailandia",
  tr1: "Turquía",
  tw2: "Taiwán",
  vn2: "Vietnam",
};

export function platformRegionLabel(value?: string): string {
  const region = value?.trim();
  if (!region) return "Región no disponible";
  return `${platformNames[region.toLowerCase()] ?? "Región configurada"} · ${region.toUpperCase()}`;
}
