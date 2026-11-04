import assert from "node:assert/strict";
import { test } from "node:test";
import { presentMatchRelations } from "../src/match-relations.ts";
import type {
  MatchAnalysisResult,
  MatchParticipant,
  MatchTeam,
} from "../src/api.ts";

type Relationship = MatchAnalysisResult["relationships"][number];
const participant = (
  puuid: string,
  gameName?: string,
  tagLine?: string,
): MatchParticipant => ({
  puuid,
  gameName,
  tagLine,
  championId: 1,
  championName: "Annie",
  kills: 1,
  deaths: 2,
  assists: 3,
});
const teams: MatchTeam[] = [
  {
    teamId: 100,
    participants: [
      participant("owner-id", "Ana", "LAN"),
      participant("ally-id", "Beto", "TEST"),
    ],
  },
  {
    teamId: 200,
    participants: [
      participant("enemy-id", "Cris", "TEST"),
      participant("incomplete-id", "Dani"),
    ],
  },
];
const pair = (
  firstPuuid: string,
  secondPuuid: string,
  matchesTogether: number,
  allyMatches = 0,
): Relationship => ({
  firstPuuid,
  secondPuuid,
  matchesTogether,
  allyMatches,
  enemyMatches: matchesTogether - allyMatches,
  score: 0,
  confidence: "LOW",
});

test("pairs resolve participants by PUUID and preserve historical counts", () => {
  const [row] = presentMatchRelations(
    [pair("owner-id", "ally-id", 4, 3)],
    teams,
  );
  assert.equal(row.first.label, "Beto#TEST");
  assert.equal(row.second.label, "Ana#LAN");
  assert.equal(row.first.teamId, 100);
  assert.deepEqual(
    [row.matchesTogether, row.allyMatches, row.enemyMatches],
    [4, 3, 1],
  );
});

test("orders descending by encounters with deterministic canonical pair tie breaks", () => {
  const input = [
    pair("owner-id", "enemy-id", 2),
    pair("owner-id", "ally-id", 2),
    pair("enemy-id", "ally-id", 7, 4),
  ];
  const forward = presentMatchRelations(input, teams);
  const reverse = presentMatchRelations(
    input
      .map((p) => ({
        ...p,
        firstPuuid: p.secondPuuid,
        secondPuuid: p.firstPuuid,
      }))
      .reverse(),
    teams,
  );
  assert.deepEqual(
    forward.map((p) => p.matchesTogether),
    [7, 2, 2],
  );
  assert.deepEqual(
    forward.map((p) => p.key),
    reverse.map((p) => p.key),
  );
  assert.deepEqual(
    forward.map((p) => [p.first.label, p.second.label]),
    reverse.map((p) => [p.first.label, p.second.label]),
  );
  assert.deepEqual([forward[0].allyMatches, forward[0].enemyMatches], [4, 3]);
});

test("missing identities never expose PUUID or invent profile attributes", () => {
  const [row] = presentMatchRelations(
    [pair("unknown-secret-id", "incomplete-id", 1)],
    teams,
  );
  assert.equal(row.first.label, "Dani");
  assert.equal(row.first.tagLine, undefined);
  assert.equal(row.second.label, "Jugador no identificado");
  assert.equal(row.second.teamId, undefined);
  assert.ok(!row.second.label.includes("unknown-secret-id"));
});

test("duplicate participant identities become unknown and empty evidence stays empty", () => {
  const duplicate = [
    ...teams,
    {
      teamId: 200,
      participants: [participant("owner-id", "Duplicate", "TAG")],
    },
  ];
  const row = presentMatchRelations(
    [pair("owner-id", "ally-id", 1)],
    duplicate,
  )[0];
  assert.equal(row.second.label, "Jugador no identificado");
  assert.equal(row.second.teamId, undefined);
  assert.deepEqual(presentMatchRelations([], teams), []);
});
