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
  if (windows.some((w) => w.result.matches.items.some((m) => m.evidence)))
    return evidenceRelationships(windows, owner);
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
      sharedMatches: { id: string; platform: string; side: string }[];
    }
  >();
  for (const w of windows)
    for (const p of new Map(
      w.result.encounters.map((p) => [p.otherPlayerPuuid, p]),
    ).values()) {
      if (!p.otherPlayerPuuid || p.otherPlayerPuuid === owner) continue;
      const prev = players.get(p.otherPlayerPuuid);
      players.set(p.otherPlayerPuuid, {
        sharedMatches: [],
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
    incompleteMatches: loadedMatches(windows).length,
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

function evidenceRelationships(windows: DashboardWindow[], owner: string) {
  const validIds = new Set(loadedMatches(windows).map((m) => m.riotMatchId));
  const candidates = windows
    .flatMap((w) => w.result.matches.items)
    .filter((m) => validIds.has(m.riotMatchId));
  const canonical = (m: MatchListItem) =>
    JSON.stringify(
      m.evidence
        ? {
            ...m.evidence,
            participants: [...m.evidence.participants].sort((a, b) =>
              a.puuid.localeCompare(b.puuid, "en"),
            ),
          }
        : null,
    );
  const quality = (m: MatchListItem) =>
    m.evidence?.ownerPuuid === owner
      ? (m.evidence.status === "complete" ? 100 : 0) +
        m.evidence.participants.filter((p) => p.puuid).length
      : -1;
  candidates.sort(
    (a, b) =>
      quality(b) - quality(a) || canonical(a).localeCompare(canonical(b), "en"),
  );
  const unique = new Map<string, MatchListItem>();
  for (const m of candidates)
    if (!unique.has(m.riotMatchId)) unique.set(m.riotMatchId, m);
  const players = new Map<
    string,
    {
      puuid: string;
      name: string;
      tag: string;
      total: number;
      ally: number;
      enemy: number;
      sharedMatches: { id: string; platform: string; side: string }[];
    }
  >();
  let incompleteMatches = 0;
  for (const m of [...unique.values()].sort((a, b) =>
    a.riotMatchId.localeCompare(b.riotMatchId, "en"),
  )) {
    const e = m.evidence,
      own = e?.participants.find((p) => p.puuid === owner);
    if (!e || e.ownerPuuid !== owner || !own) {
      incompleteMatches++;
      continue;
    }
    if (e.status !== "complete") incompleteMatches++;
    const grouped = new Map<string, typeof e.participants>();
    for (const p of e.participants)
      if (p.puuid && p.puuid !== owner)
        grouped.set(p.puuid, [...(grouped.get(p.puuid) ?? []), p]);
    for (const [puuid, rows] of grouped) {
      const p = rows[0],
        teams = new Set(rows.map((r) => r.teamId));
      const side =
        (own.teamId === 100 || own.teamId === 200) &&
        teams.size === 1 &&
        (p.teamId === 100 || p.teamId === 200)
          ? own.teamId === p.teamId
            ? "ally"
            : "enemy"
          : "unknown";
      const prev = players.get(puuid) ?? {
        puuid,
        name: p.gameName,
        tag: p.tagLine,
        total: 0,
        ally: 0,
        enemy: 0,
        sharedMatches: [],
      };
      players.set(puuid, {
        ...prev,
        total: prev.total + 1,
        ally: prev.ally + (side === "ally" ? 1 : 0),
        enemy: prev.enemy + (side === "enemy" ? 1 : 0),
        sharedMatches: [
          ...prev.sharedMatches,
          { id: m.riotMatchId, platform: e.platformRegion, side },
        ],
      });
    }
  }
  const all = [...players.values()];
  return {
    unique: all.length,
    repeated: all.filter((p) => p.total >= 2).length,
    participations: all.reduce((s, p) => s + p.total, 0),
    allies: all.filter((p) => p.ally > 0).length,
    enemies: all.filter((p) => p.enemy > 0).length,
    incompleteMatches,
    players: all
      .filter((p) => p.total >= 2)
      .sort(
        (a, b) => b.total - a.total || a.puuid.localeCompare(b.puuid, "en"),
      ),
  };
}

export function reconcileMatches(items: MatchListItem[]): MatchListItem[] {
  const quality = (m: MatchListItem) =>
    (m.evidence?.status === "complete" ? 100 : 0) +
    (m.evidence?.participants.length ?? 0);
  const rows = items
    .filter(
      (m) =>
        m.riotMatchId &&
        [m.kills, m.deaths, m.assists].every(
          (n) => Number.isFinite(n) && n >= 0,
        ),
    )
    .sort(
      (a, b) =>
        quality(b) - quality(a) ||
        JSON.stringify(a).localeCompare(JSON.stringify(b), "en"),
    );
  const map = new Map<string, MatchListItem>();
  for (const m of rows) if (!map.has(m.riotMatchId)) map.set(m.riotMatchId, m);
  return [...map.values()].sort(
    (a, b) =>
      (b.gameStartTimestamp ?? "").localeCompare(a.gameStartTimestamp ?? "") ||
      a.riotMatchId.localeCompare(b.riotMatchId, "en"),
  );
}
