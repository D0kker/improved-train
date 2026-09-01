import assert from "node:assert/strict";
import test from "node:test";

import {
  apiPath,
  matchDetailPath,
  playerNetworkPath,
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
import {
  filterNetwork,
  networkEdgeWidth,
  networkNodeRadius,
  positionNetworkNodes,
} from "../src/network.ts";
import {
  championCatalogUrl,
  championIconPath,
  championImageUrl,
  dataDragonVersion,
} from "../src/data-dragon.ts";
import { browserSecurityHeaders } from "../src/security-headers.ts";

test("API paths remain same-origin and encode user-controlled segments", () => {
  assert.equal(
    playerLookupPath("Ana Uno", "LA/N"),
    "/api/v1/players/by-riot-id/Ana%20Uno/LA%2FN",
  );
  assert.equal(
    matchDetailPath("LA1_123/456", "owner/puuid"),
    "/match/LA1_123%2F456?ownerPuuid=owner%2Fpuuid",
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
  assert.equal(
    playerNetworkPath("test/puuid"),
    "/api/v1/players/test%2Fpuuid/network?maxNodes=50&maxEdges=100",
  );
});

test("network helpers filter, position and scale deterministically", () => {
  const network = {
    center: { puuid: "owner", gameName: "Ana", tagLine: "LAN", isCenter: true },
    nodes: [
      { puuid: "owner", gameName: "Ana", tagLine: "LAN", isCenter: true },
      { puuid: "high", gameName: "Bea", tagLine: "LAN", isCenter: false },
      { puuid: "low", gameName: "Caro", tagLine: "LAN", isCenter: false },
    ],
    edges: [
      {
        sourcePuuid: "owner",
        targetPuuid: "high",
        matchesTogether: 5,
        sameTeamMatches: 4,
        oppositeTeamMatches: 1,
        sameTeamRatio: 0.8,
        relationshipScore: 70,
        relationshipConfidence: "HIGH" as const,
        premadeLabel: "likely premade" as const,
      },
      {
        sourcePuuid: "owner",
        targetPuuid: "low",
        matchesTogether: 2,
        sameTeamMatches: 1,
        oppositeTeamMatches: 1,
        sameTeamRatio: 0.5,
        relationshipScore: 20,
        relationshipConfidence: "LOW" as const,
        premadeLabel: null,
      },
    ],
    metadata: {
      depth: 1,
      truncated: false,
      totalAvailableNodes: 3,
      totalAvailableEdges: 2,
      appliedMaxNodes: 50,
      appliedMaxEdges: 100,
    },
  };

  const filtered = filterNetwork(network, 50, "MEDIUM");
  assert.deepEqual(
    filtered.nodes.map((node) => node.puuid),
    ["owner", "high"],
  );
  assert.equal(filtered.edges.length, 1);
  assert.deepEqual(positionNetworkNodes(filtered.nodes, filtered.edges)[0], {
    ...network.center,
    x: 400,
    y: 250,
    score: 100,
  });
  assert.equal(networkNodeRadius(70, false), 23);
  assert.equal(networkEdgeWidth(100), 5);
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

test("Data Dragon assets remain same-origin, validated and versioned", () => {
  assert.equal(championIconPath(1), "/api/assets/champions/1");
  assert.equal(championIconPath(0), null);
  assert.equal(dataDragonVersion("16.17.1"), "16.17.1");
  assert.equal(dataDragonVersion("latest/../../"), "16.17.1");
  assert.equal(
    championCatalogUrl("16.17.1"),
    "https://ddragon.leagueoflegends.com/cdn/16.17.1/data/en_US/champion.json",
  );
  assert.equal(
    championImageUrl("16.17.1", "MonkeyKing"),
    "https://ddragon.leagueoflegends.com/cdn/16.17.1/img/champion/MonkeyKing.png",
  );
  assert.equal(championImageUrl("16.17.1", "../secret"), null);
});

test("browser security policy blocks framing and external connections", () => {
  const headers = Object.fromEntries(
    browserSecurityHeaders.map(({ key, value }) => [key, value]),
  );
  assert.match(headers["Content-Security-Policy"], /frame-ancestors 'none'/);
  assert.match(headers["Content-Security-Policy"], /connect-src 'self'/);
  assert.equal(headers["X-Content-Type-Options"], "nosniff");
  assert.equal(headers["X-Frame-Options"], "DENY");
});
