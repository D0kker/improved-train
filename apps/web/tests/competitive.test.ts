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
