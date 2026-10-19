import assert from "node:assert/strict";
import test from "node:test";

import {
  apiPath,
  analyzeSelectedMatch,
  playerSearchPath,
  recentMatchesPath,
  fetchJson,
  matchDetailPath,
  playerActiveGamePath,
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
import {
  familiarityMessage,
  historyAnalysisStatus,
  knownPlayerCount,
  latestMatchSyncPlan,
  repeatedPlayerCount,
} from "../src/echo.ts";

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
  assert.equal(
    playerActiveGamePath("owner/puuid"),
    "/api/v1/players/owner%2Fpuuid/active-game",
  );
});

test("empty successful responses are treated as null JSON", async () => {
  const originalFetch = globalThis.fetch;
  globalThis.fetch = async () => new Response(null, { status: 200 });
  try {
    assert.equal(
      await fetchJson<null>("/api/v1/players/test/active-game"),
      null,
    );
  } finally {
    globalThis.fetch = originalFetch;
  }
});

test("stateless search and recent-match paths preserve the five-match window", () => {
  assert.equal(
    playerSearchPath("Ana Uno", "LA/N"),
    "/api/v1/players/by-riot-id/Ana%20Uno/LA%2FN/analysis",
  );
  assert.equal(
    recentMatchesPath("owner/puuid", 5, "EUW1"),
    "/api/v1/players/owner%2Fpuuid/recent-matches?count=5&start=5&platformRegion=EUW1",
  );
});

test("selected-match analysis is one independent HTTP request with owner and cancellation", async () => {
  const originalFetch = globalThis.fetch;
  const controller = new AbortController();
  const result = {
    match: { riotMatchId: "LA1_123" },
    coverage: { status: "partial" },
  };
  let calls = 0;
  globalThis.fetch = async (path, init) => {
    calls++;
    assert.equal(path, "/api/v1/matches/LA1_123/analyze");
    assert.equal(init?.method, "POST");
    assert.deepEqual(JSON.parse(String(init?.body)), {
      ownerPuuid: "owner",
      platformRegion: "LA1",
    });
    assert.equal(init?.signal?.aborted, false);
    controller.abort();
    assert.equal(init?.signal?.aborted, true);
    return Response.json(result);
  };
  try {
    assert.deepEqual(
      await analyzeSelectedMatch("LA1_123", "owner", "LA1", controller.signal),
      result,
    );
    assert.equal(calls, 1);
  } finally {
    globalThis.fetch = originalFetch;
  }
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

test("ECHO distinguishes known and repeated players", () => {
  const encounters = [
    { totalMatches: 1, otherPlayerPuuid: "one" },
    { totalMatches: 2, otherPlayerPuuid: "two" },
    { totalMatches: 7, otherPlayerPuuid: "three" },
    { totalMatches: 7, otherPlayerPuuid: "three" },
    { totalMatches: 2, otherPlayerPuuid: "" },
    { totalMatches: 2, otherPlayerPuuid: "owner" },
  ] as Parameters<typeof knownPlayerCount>[0];

  assert.equal(knownPlayerCount(encounters, "owner"), 3);
  assert.equal(repeatedPlayerCount(encounters, "owner"), 2);
});

test("partial HTTP successes and failed syncs never become definitive zero familiarity", () => {
  const complete = {
    matchIdsReturned: 3,
    alreadyStored: 1,
    downloaded: 2,
    notFound: 0,
    failed: 0,
  };
  const fulfilled = { status: "fulfilled" as const, value: complete };
  assert.equal(historyAnalysisStatus([fulfilled]), "complete");
  assert.equal(
    historyAnalysisStatus([
      fulfilled,
      { status: "rejected", reason: new Error("mock") },
    ]),
    "partial",
  );
  assert.equal(
    historyAnalysisStatus([
      { ...fulfilled, value: { ...complete, downloaded: 1, failed: 1 } },
    ]),
    "partial",
  );
  assert.equal(
    historyAnalysisStatus([{ status: "rejected", reason: new Error("mock") }]),
    "error",
  );
  assert.equal(historyAnalysisStatus([]), "insufficient");
  const familiarity = {
    knownPlayers: 0,
    unknownPlayers: 9,
    evaluablePlayers: 9,
    familiarityPercentage: 0,
    status: "Available" as const,
    knownPlayerDetails: [],
  };
  assert.match(familiarityMessage(familiarity, "partial"), /Análisis parcial/);
  assert.match(familiarityMessage(familiarity, "error"), /Error de análisis/);
  assert.match(
    familiarityMessage(familiarity),
    /No se detectaron coincidencias/,
  );
  assert.match(
    familiarityMessage({ ...familiarity, status: "NoPriorHistory" }),
    /Información insuficiente/,
  );
  assert.match(
    familiarityMessage({ ...familiarity, status: "PartialParticipants" }),
    /Análisis parcial/,
  );
});

test("ECHO familiarity copy answers the central match question", () => {
  assert.equal(
    familiarityMessage({
      knownPlayers: 3,
      unknownPlayers: 6,
      evaluablePlayers: 9,
      familiarityPercentage: 33.3,
      status: "Available",
      knownPlayerDetails: [],
    }),
    "3 de 9 jugadores ya habían aparecido antes.",
  );
});

test("last-match sync plan is deterministic and configurable", () => {
  const match = {
    teams: [
      {
        teamId: 100,
        participants: [
          {
            puuid: "owner",
            championId: 1,
            championName: "A",
            kills: 0,
            deaths: 0,
            assists: 0,
          },
          {
            puuid: "one",
            championId: 2,
            championName: "B",
            kills: 0,
            deaths: 0,
            assists: 0,
          },
          {
            puuid: "two",
            championId: 3,
            championName: "C",
            kills: 0,
            deaths: 0,
            assists: 0,
          },
        ],
      },
      {
        teamId: 200,
        participants: [
          {
            puuid: "two",
            championId: 4,
            championName: "D",
            kills: 0,
            deaths: 0,
            assists: 0,
          },
          {
            puuid: "three",
            championId: 5,
            championName: "E",
            kills: 0,
            deaths: 0,
            assists: 0,
          },
        ],
      },
    ],
  } as Parameters<typeof latestMatchSyncPlan>[0];

  assert.deepEqual(
    latestMatchSyncPlan(match, "owner", {
      ownerMatchCount: 7,
      otherMatchCount: 2,
      otherPlayerCount: 2,
    }),
    [
      { puuid: "owner", count: 7 },
      { puuid: "one", count: 2 },
      { puuid: "two", count: 2 },
    ],
  );
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
