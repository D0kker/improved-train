import assert from "node:assert/strict";
import test from "node:test";

import { apiPath, playerLookupPath, playerSyncPath } from "../src/api.ts";
import { formatDuration, formatPercent, winRate } from "../src/format.ts";

test("API paths remain same-origin and encode user-controlled segments", () => {
  assert.equal(
    playerLookupPath("Ana Uno", "LA/N"),
    "/api/v1/players/by-riot-id/Ana%20Uno/LA%2FN",
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

test("presentation helpers normalize percentages and durations", () => {
  assert.equal(formatDuration(605), "10:05");
  assert.equal(formatPercent(62.5), "62.5%");
  assert.equal(formatPercent(1), "1%");
  assert.equal(winRate(3, 4), "75%");
  assert.equal(winRate(0, 0), "—");
});
