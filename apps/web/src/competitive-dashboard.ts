import type { MatchListItem, PlayerSearchResult } from "./api";
export type DashboardWindow = { start: number; result: PlayerSearchResult };
export function loadedMatches(windows: DashboardWindow[]): MatchListItem[] {
  return [
    ...new Map(
      windows
        .flatMap((w) => w.result.matches.items)
        .filter(
          (m) =>
            m.riotMatchId &&
            [m.kills, m.deaths, m.assists].every(
              (n) => Number.isFinite(n) && n >= 0,
            ),
        )
        .map((m) => [m.riotMatchId, m]),
    ).values(),
  ];
}
export function kda(m: Pick<MatchListItem, "kills" | "deaths" | "assists">) {
  return (m.kills + m.assists) / Math.max(1, m.deaths);
}
export function performance(items: MatchListItem[]) {
  const matches = [...new Map(items.map((m) => [m.riotMatchId, m])).values()];
  const wins = matches.filter((m) => m.win === true).length;
  const losses = matches.filter((m) => m.win === false).length;
  const avg = (fn: (m: MatchListItem) => number) =>
    matches.length
      ? matches.reduce((s, m) => s + fn(m), 0) / matches.length
      : null;
  const champions = [...new Set(matches.map((m) => m.championId))]
    .map((id) => {
      const sample = matches.filter((m) => m.championId === id);
      const w = sample.filter((m) => m.win === true).length,
        l = sample.filter((m) => m.win === false).length;
      return {
        id,
        name: sample[0].championName,
        count: sample.length,
        wins: w,
        losses: l,
        rate: w + l ? (w / (w + l)) * 100 : null,
      };
    })
    .sort((a, b) => b.count - a.count || a.id - b.id)
    .slice(0, 3);
  return {
    count: matches.length,
    wins,
    losses,
    rate: wins + losses ? (wins / (wins + losses)) * 100 : null,
    kills: avg((m) => m.kills),
    deaths: avg((m) => m.deaths),
    assists: avg((m) => m.assists),
    kda: avg(kda),
    champions,
  };
}
export function relationships(windows: DashboardWindow[], owner: string) {
  const ids = windows.flatMap((w) =>
    w.result.matches.items.map((m) => m.riotMatchId),
  );
  // Aggregated encounter contracts cannot attribute counts to overlapping IDs.
  if (
    windows.some(
      (w) =>
        w.result.summary &&
        w.result.summary.matchesAnalyzed !== w.result.matches.items.length,
    ) ||
    new Set(ids).size !== ids.length ||
    loadedMatches(windows).length !== ids.length
  )
    return null;
  const players = new Map<
    string,
    {
      puuid: string;
      name: string;
      tag: string;
      total: number;
      ally: number;
      enemy: number;
    }
  >();
  for (const w of windows)
    for (const p of new Map(
      w.result.encounters.map((p) => [p.otherPlayerPuuid, p]),
    ).values()) {
      if (!p.otherPlayerPuuid || p.otherPlayerPuuid === owner) continue;
      const prev = players.get(p.otherPlayerPuuid);
      players.set(p.otherPlayerPuuid, {
        puuid: p.otherPlayerPuuid,
        name: p.gameName,
        tag: p.tagLine,
        total: (prev?.total ?? 0) + p.totalMatches,
        ally: (prev?.ally ?? 0) + p.sameTeamMatches,
        enemy: (prev?.enemy ?? 0) + p.enemyTeamMatches,
      });
    }
  const all = [...players.values()];
  return {
    unique: all.length,
    repeated: all.filter((p) => p.total >= 2).length,
    participations: all.reduce((s, p) => s + p.total, 0),
    allies: all.filter((p) => p.ally > 0).length,
    enemies: all.filter((p) => p.enemy > 0).length,
    players: all
      .filter((p) => p.total >= 2)
      .sort(
        (a, b) => b.total - a.total || a.puuid.localeCompare(b.puuid, "en"),
      ),
  };
}
export function coverageStatus(windows: DashboardWindow[]) {
  const statuses = windows.map((w) => w.result.coverage.status);
  return statuses.includes("error")
    ? "error"
    : statuses.includes("partial")
      ? "partial"
      : statuses.includes("insufficient")
        ? "insufficient"
        : "complete";
}
export function filterMatches(items: MatchListItem[], filter: string) {
  return items.filter(
    (m) =>
      filter === "all" ||
      (filter === "wins" ? m.win === true : m.win === false),
  );
}
