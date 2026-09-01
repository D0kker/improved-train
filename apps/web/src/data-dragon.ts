const championIdPattern = /^[1-9]\d{0,4}$/;
const versionPattern = /^\d+\.\d+\.\d+$/;

export const defaultDataDragonVersion = "16.17.1";

export function dataDragonVersion(value?: string): string {
  return value && versionPattern.test(value) ? value : defaultDataDragonVersion;
}

export function championIconPath(championId: number): string | null {
  const value = String(championId);
  return championIdPattern.test(value)
    ? `/api/assets/champions/${value}`
    : null;
}

export function championCatalogUrl(version: string): string {
  return `https://ddragon.leagueoflegends.com/cdn/${dataDragonVersion(version)}/data/en_US/champion.json`;
}

export function championImageUrl(
  version: string,
  assetId: string,
): string | null {
  if (!/^[A-Za-z][A-Za-z0-9]+$/.test(assetId)) return null;
  return `https://ddragon.leagueoflegends.com/cdn/${dataDragonVersion(version)}/img/champion/${assetId}.png`;
}
