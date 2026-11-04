import assert from "node:assert/strict";
import { test } from "node:test";
import type {
  AnalysisCoverage,
  MatchAnalysisResult,
  MatchDetail,
} from "../src/api.ts";
import { inlineMatchRelations } from "../src/inline-match-relations.ts";

const match = {
  teams: [
    {
      teamId: 100,
      participants: [{ puuid: "owner" }, { puuid: "a" }, { puuid: "b" }],
    },
    { teamId: 200, participants: [{ puuid: "c" }, { puuid: "d" }] },
  ],
  premadeGroups: [
    {
      teamId: 200,
      classification: "PossiblePremade",
      members: [{ puuid: "d" }, { puuid: "c" }],
    },
    {
      teamId: 100,
      classification: "LikelyPremade",
      members: [{ puuid: "a" }, { puuid: "owner" }],
    },
    {
      teamId: 100,
      classification: "PossiblePremade",
      members: [{ puuid: "b" }, { puuid: "a" }],
    },
  ],
} as unknown as MatchDetail;
const coverage = { status: "complete" } as AnalysisCoverage;
const pair = {
  firstPuuid: "owner",
  secondPuuid: "a",
  matchesTogether: 3,
  allyMatches: 2,
  enemyMatches: 1,
  score: 0,
  confidence: "LOW",
} as MatchAnalysisResult["relationships"][number];

test("backend groups retain stable labels, including independent and overlapping groups", () => {
  const one = inlineMatchRelations(match, [pair], coverage, "owner");
  const two = inlineMatchRelations(
    { ...match, premadeGroups: [...match.premadeGroups].reverse() },
    [pair],
    coverage,
    "owner",
  );
  assert.deepEqual([...one.labels], [...two.labels]);
  assert.equal(one.labels.get("a")?.length, 2);
  assert.equal(one.labels.get("c")?.[0], one.labels.get("d")?.[0]);
  assert.notEqual(one.labels.get("owner")?.[0], one.labels.get("c")?.[0]);
});

test("owner pair uses backend counts once and never adds duplicate aggregates", () => {
  const result = inlineMatchRelations(
    match,
    [pair, { ...pair }],
    coverage,
    "owner",
  );
  assert.deepEqual(result.history("a"), {
    kind: "known",
    pair,
    partial: false,
  });
  assert.equal(
    inlineMatchRelations(
      match,
      [pair, { ...pair, matchesTogether: 4 }],
      coverage,
      "owner",
    ).history("a").kind,
    "unavailable",
  );
  assert.equal(result.history("c").kind, "empty");
});

test("partial coverage and missing identities never imply absence", () => {
  const partial = inlineMatchRelations(
    match,
    [pair],
    { status: "partial" } as AnalysisCoverage,
    "owner",
  );
  assert.equal(partial.history("c").kind, "partial");
  assert.equal(partial.history("a").kind, "known");
  assert.equal(
    inlineMatchRelations(
      match,
      [],
      { status: "insufficient" } as AnalysisCoverage,
      "owner",
    ).history("c").kind,
    "partial",
  );
  assert.equal(
    inlineMatchRelations(match, null, null, undefined).history("a").kind,
    "identity",
  );
  assert.equal(
    inlineMatchRelations(
      match,
      [],
      { status: "error" } as AnalysisCoverage,
      "owner",
    ).history("a").kind,
    "unavailable",
  );
});

test("ambiguous identities and invalid group members cannot create labels", () => {
  const duplicate = {
    ...match,
    teams: [...match.teams, { teamId: 200, participants: [{ puuid: "a" }] }],
  } as MatchDetail;
  const result = inlineMatchRelations(duplicate, [pair], coverage, "owner");
  assert.equal(result.history("a").kind, "identity");
  assert.equal(result.labels.has("a"), false);
});
