import assert from "node:assert/strict";
import { test } from "node:test";
import {
  performance,
  loadedMatches,
  relationships,
  coverageStatus,
  filterMatches,
  kda,
} from "../src/competitive-dashboard.ts";
import type { MatchListItem, PlayerSearchResult } from "../src/api.ts";
const match = (
  id: string,
  win: boolean | undefined = true,
  championId = 1,
): MatchListItem => ({
  riotMatchId: id,
  queueId: null,
  gameStartTimestamp: null,
  gameDurationSeconds: null,
  championId,
  championName: "Campeón largo",
  kills: 4,
  deaths: 0,
  assists: 6,
  win: win as boolean,
});
const window = (
  start: number,
  items: MatchListItem[],
  encounters: PlayerSearchResult["encounters"] = [],
) => ({
  start,
  result: {
    matches: { items },
    encounters,
    coverage: { status: "complete" },
  } as PlayerSearchResult,
});
test("performance grows across loaded matches, excludes unknown results and deduplicates", () => {
  assert.equal(performance([]).rate, null);
  const a = match("a"),
    b = match("b", false),
    c = { ...match("c"), win: undefined } as unknown as MatchListItem;
  const stats = performance([a, a, b, c]);
  assert.equal(stats.count, 3);
  assert.equal(stats.rate, 50);
  assert.equal(stats.kills, 4);
  assert.equal(stats.kda, 10);
  assert.equal(kda(a), 10);
  assert.equal(
    performance(Array.from({ length: 10 }, (_, i) => match(String(i)))).count,
    10,
  );
  assert.equal(filterMatches([a, b, c], "losses").length, 1);
  assert.equal(
    performance([
      match("a", true, 3),
      match("b", true, 1),
      match("c", true, 2),
      match("d", true, 4),
    ]).champions[0].id,
    1,
  );
});
test("invalid details are excluded and coverage preserves partial states", () => {
  const w = window(0, [match("a"), { ...match("b"), kills: NaN }]);
  assert.equal(loadedMatches([w]).length, 1);
  w.result.coverage.status = "partial";
  assert.equal(coverageStatus([w]), "partial");
});
test("relationships combine by PUUID across disjoint windows, with overlapping ally/rival sets", () => {
  const p = {
    otherPlayerPuuid: "other",
    gameName: "Nombre",
    tagLine: "TAG",
    totalMatches: 1,
    sameTeamMatches: 1,
    enemyTeamMatches: 0,
  } as PlayerSearchResult["encounters"][number];
  const a = window(0, [match("a")], [p]),
    b = window(
      5,
      [match("b")],
      [{ ...p, sameTeamMatches: 0, enemyTeamMatches: 1 }],
    );
  const stats = relationships([a, b], "owner")!;
  assert.equal(stats.unique, 1);
  assert.equal(stats.repeated, 1);
  assert.equal(stats.allies, 1);
  assert.equal(stats.enemies, 1);
  assert.equal(stats.participations, 2);
  assert.equal(relationships([a, a], "owner"), null);
});

test("KDA is the mean of per-match ratios and not the aggregate ratio", () => {
  const stats = performance([
    { ...match("a"), kills: 4, deaths: 2, assists: 0 },
    { ...match("b"), kills: 10, deaths: 0, assists: 0 },
  ]);
  assert.equal(stats.kda, 6);
  assert.equal(stats.kills, 7);
  assert.equal(stats.deaths, 1);
});

test("per-match evidence deduplicates overlapping windows, preserves sides, and is reception-order invariant", () => {
  const a = {
    ...match("LA1_A"),
    evidence: {
      ownerPuuid: "owner",
      platformRegion: "la1",
      status: "complete" as const,
      participants: [
        { puuid: "owner", gameName: "Owner", tagLine: "T", teamId: 100 },
        { puuid: "p", gameName: "Player", tagLine: "T", teamId: 100 },
      ],
    },
  };
  const b = {
    ...match("LA1_B"),
    evidence: {
      ...a.evidence,
      participants: [
        a.evidence.participants[0],
        { ...a.evidence.participants[1], teamId: 200 },
      ],
    },
  };
  const wa = window(0, [a, b]),
    wb = window(5, [a]);
  const stats = relationships([wa, wb], "owner")!;
  assert.equal(stats.participations, 2);
  assert.equal(stats.repeated, 1);
  assert.equal(stats.allies, 1);
  assert.equal(stats.enemies, 1);
  assert.deepEqual(
    stats.players[0].sharedMatches.map((m) => m.id),
    ["LA1_A", "LA1_B"],
  );
  assert.deepEqual(stats, relationships([wb, wa], "owner"));
  const incomplete = {
    ...b,
    evidence: {
      ...b.evidence,
      status: "partial" as const,
      participants: [
        b.evidence.participants[0],
        { ...b.evidence.participants[1], teamId: null },
        { puuid: "", gameName: "", tagLine: "", teamId: 100 },
      ],
    },
  };
  const partial = relationships([window(0, [incomplete])], "owner")!;
  assert.equal(partial.participations, 1);
  assert.equal(partial.enemies, 0);
  assert.equal(partial.incompleteMatches, 1);
  const missing = { ...a, evidence: { ...a.evidence, participants: [] } };
  assert.equal(relationships([window(0, [missing])], "owner")!.unique, 0);
});

test("retries retain complete evidence instead of downgrading it", async () => {
  const { reconcileMatches } = await import("../src/competitive-dashboard.ts");
  const full = {
    ...match("a"),
    evidence: {
      ownerPuuid: "owner",
      platformRegion: "la1",
      status: "complete" as const,
      participants: [
        { puuid: "owner", gameName: "O", tagLine: "T", teamId: 100 },
      ],
    },
  };
  const partial = {
    ...full,
    evidence: {
      ...full.evidence,
      status: "partial" as const,
      participants: [],
    },
  };
  assert.deepEqual(
    reconcileMatches([full, partial, full]),
    reconcileMatches([partial, full]),
  );
  assert.equal(
    reconcileMatches([full, partial])[0].evidence?.status,
    "complete",
  );
});
