import assert from "node:assert/strict";
import test from "node:test";

import {
  apiPath,
  playerLookupPath,
  playerProfilePath,
  playerRelationshipsPath,
  playerSyncPath,
} from "../src/api.ts";
import { formatDuration, formatPercent, winRate } from "../src/format.ts";
import {
  groupCode,
  groupLabel,
  groupTone,
  groupsForParticipant,
} from "../src/premade-groups.ts";

test("API paths remain same-origin and encode user-controlled segments", () => {
  assert.equal(
    playerLookupPath("Ana Uno", "LA/N"),
    "/api/v1/players/by-riot-id/Ana%20Uno/LA%2FN",
  );
  assert.equal(
    playerProfilePath("Ana Uno", "LA/N"),
    "/player/Ana%20Uno/LA%2FN",
  );
  assert.equal(
    apiPath("matches", "LA1_123/456"),
    "/api/v1/matches/LA1_123%2F456",
  );
});

test("match synchronization is bounded to twenty matches", () => {
  assert.equal(
    playerSyncPath("test-puuid", 20),
    "/api/v1/players/test-puuid/matches/sync?count=20",
  );
  assert.throws(() => playerSyncPath("test-puuid", 21), RangeError);
  assert.throws(() => playerSyncPath("test-puuid", 0), RangeError);
});

test("relationship paths are same-origin and pagination is bounded", () => {
  assert.equal(
    playerRelationshipsPath("test/puuid", 2, 50),
    "/api/v1/players/test%2Fpuuid/relationships?page=2&pageSize=50",
  );
  assert.throws(() => playerRelationshipsPath("test", 0, 20), RangeError);
  assert.throws(() => playerRelationshipsPath("test", 1, 101), RangeError);
});

test("presentation helpers normalize percentages and durations", () => {
  assert.equal(formatDuration(605), "10:05");
  assert.equal(formatPercent(62.5), "62.5%");
  assert.equal(formatPercent(1), "1%");
  assert.equal(winRate(3, 4), "75%");
  assert.equal(winRate(0, 0), "—");
});

test("premade groups remain distinguishable without relying on color", () => {
  const groups = [
    {
      groupNumber: 1,
      teamId: 100,
      classification: "LikelyPremade" as const,
      label: "possible premade · high evidence" as const,
      members: [{ puuid: "a", gameName: "Ana", tagLine: "LAN" }],
    },
    {
      groupNumber: 6,
      teamId: 200,
      classification: "PossiblePremade" as const,
      label: "possible premade" as const,
      members: [{ puuid: "a", gameName: "Ana", tagLine: "LAN" }],
    },
  ];

  assert.equal(groupCode(1), "P1");
  assert.equal(groupCode(6), "P6");
  assert.equal(groupTone(1), groupTone(6));
  assert.equal(groupLabel(groups[0]), "Posible premade · evidencia alta");
  assert.equal(groupsForParticipant(groups, "a").length, 2);
  assert.deepEqual(groupsForParticipant(groups, "unknown"), []);
});
