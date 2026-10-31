import "./dom-environment.ts";
import assert from "node:assert/strict";
import { afterEach, test } from "node:test";
import {
  cleanup,
  act,
  render,
  screen,
  waitFor,
  within,
} from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MatchDetailView } from "../app/match/[matchId]/match-detail-view.tsx";
import type { MatchAnalysisResult } from "../src/api.ts";

const originalFetch = globalThis.fetch;
afterEach(() => {
  cleanup();
  globalThis.fetch = originalFetch;
});

const fixture = (): MatchAnalysisResult => ({
  owner: {
    puuid: "owner-fixture",
    gameName: "Ana",
    tagLine: "LAN",
    platformRegion: "la1",
  },
  summary: {
    puuid: "owner-fixture",
    gameName: "Ana",
    tagLine: "LAN",
    matchesAnalyzed: 5,
    wins: 3,
    losses: 2,
    winRate: 60,
    uniquePlayersEncountered: 2,
    repeatedPlayers: 1,
    dataUpdatedAt: "2026-10-27T18:00:00Z",
  },
  encounters: [],
  relationships: [
    {
      firstPuuid: "player-b",
      secondPuuid: "player-c",
      matchesTogether: 2,
      allyMatches: 1,
      enemyMatches: 1,
      score: 1,
      confidence: "LOW",
    },
    {
      firstPuuid: "owner-fixture",
      secondPuuid: "player-b",
      matchesTogether: 4,
      allyMatches: 3,
      enemyMatches: 1,
      score: 1,
      confidence: "HIGH",
    },
    {
      firstPuuid: "owner-fixture",
      secondPuuid: "player-c",
      matchesTogether: 2,
      allyMatches: 0,
      enemyMatches: 2,
      score: 1,
      confidence: "LOW",
    },
  ],
  coverage: {
    status: "complete",
    participants: 10,
    matchIdsObtained: 30,
    uniqueMatchIds: 18,
    matchesRecovered: 18,
    players: [
      {
        puuid: "owner-fixture",
        requested: 5,
        matchIds: 5,
        eligibleMatches: 5,
        limited: false,
      },
    ],
    failures: [],
  },
  match: {
    riotMatchId: "LA1_FIXTURE",
    queueId: 420,
    gameStartTimestamp: "2026-10-27T18:00:00Z",
    gameDurationSeconds: 1800,
    teams: [
      {
        teamId: 100,
        participants: [
          {
            puuid: "owner-fixture",
            gameName: "Ana",
            tagLine: "LAN",
            championId: 0,
            championName: "Ahri",
            kills: 4,
            deaths: 2,
            assists: 7,
            win: true,
          },
          {
            puuid: "player-b",
            gameName: "Beto",
            tagLine: "TEST",
            championId: 0,
            championName: "Ashe",
            kills: 2,
            deaths: 3,
            assists: 6,
            win: true,
          },
          ...Array.from({ length: 3 }, (_, i) => ({
            puuid: `blue-${i}`,
            gameName: `Azul ${i}`,
            tagLine: "TEST",
            championId: 0,
            championName: "Lux",
            kills: 1,
            deaths: 2,
            assists: 3,
            win: true,
          })),
        ],
      },
      {
        teamId: 200,
        participants: [
          {
            puuid: "player-c",
            gameName: "Carla",
            tagLine: "TEST",
            championId: 0,
            championName: "Vi",
            kills: 5,
            deaths: 4,
            assists: 2,
            win: false,
          },
          ...Array.from({ length: 4 }, (_, i) => ({
            puuid: `red-${i}`,
            gameName: `Rojo ${i}`,
            tagLine: "TEST",
            championId: 0,
            championName: "Jinx",
            kills: 1,
            deaths: 2,
            assists: 3,
            win: false,
          })),
        ],
      },
    ],
    premadeGroups: [
      {
        groupNumber: 1,
        teamId: 100,
        classification: "PossiblePremade",
        label: "possible premade",
        members: [
          { puuid: "owner-fixture", gameName: "Ana", tagLine: "LAN" },
          { puuid: "player-b", gameName: "Beto", tagLine: "TEST" },
        ],
      },
    ],
    familiarity: {
      knownPlayers: 2,
      unknownPlayers: 7,
      evaluablePlayers: 9,
      familiarityPercentage: 22.2,
      status: "Available",
      knownPlayerDetails: [],
    },
  },
});

const props = {
  matchId: "LA1_FIXTURE",
  ownerPuuid: "owner-fixture",
  platformRegion: "la1",
  returnProfile: { gameName: "Ana", tagLine: "LAN" },
};
const heading = async (name: string) =>
  screen.findByRole("heading", { name }, { timeout: 6000 });

test("match analysis presents owner, teams, historical pairs, cautious groups and coverage", async () => {
  const requests: { path: string; method?: string }[] = [];
  globalThis.fetch = async (input, init) => {
    requests.push({ path: String(input), method: init?.method });
    return Response.json(fixture());
  };
  render(<MatchDetailView {...props} />);
  for (const name of [
    "Resumen de partida",
    "Equipos y participantes",
    "Relaciones históricas",
    "Grupos con coincidencias históricas",
    "Cobertura y transparencia",
  ])
    assert.ok(await heading(name));
  const section = screen
    .getByRole("heading", { name: "Relaciones históricas" })
    .closest("section")!;
  const rows = within(section).getAllByRole("listitem");
  assert.equal(rows.length, 3);
  assert.match(rows[0].textContent!, /Ana/);
  assert.match(rows[0].textContent!, /Beto/);
  assert.match(rows[0].textContent!, /4/);
  const groups = screen
    .getByRole("heading", { name: "Grupos con coincidencias históricas" })
    .closest("section")!;
  assert.match(groups.textContent!, /Ana/);
  assert.match(groups.textContent!, /Beto/);
  assert.match(groups.textContent!, /no confirma/i);
  assert.match(groups.textContent!, /aliados/i);
  assert.match(groups.textContent!, /rivales/i);
  assert.match(groups.textContent!, /3/);
  assert.doesNotMatch(groups.textContent!, /\d+\s*%/);
  const summary = screen
    .getByRole("heading", { name: "Resumen de partida" })
    .closest("section")!;
  assert.ok(within(summary).getByText("5.50"));
  assert.ok(screen.getByText("Jugador consultado"));
  assert.ok(screen.getByRole("heading", { name: "Equipo azul" }));
  assert.ok(screen.getByRole("heading", { name: "Equipo rojo" }));
  assert.match(
    screen
      .getByRole("link", { name: /Volver al perfil/ })
      .getAttribute("href")!,
    /\/player\/Ana\/LAN\?platformRegion=la1/,
  );
  assert.doesNotMatch(
    document.body.textContent!,
    /owner-fixture|player-b|player-c/,
  );
  assert.equal(requests.length, 1);
  assert.match(requests[0].path, /LA1_FIXTURE\/analyze$/);
  assert.equal(requests[0].method, "POST");
});

test("partial roster and partial coverage remain explicit rather than inventing participants", async () => {
  const data = fixture();
  data.coverage.status = "partial";
  data.coverage.failures = [
    { stage: "match_detail", code: "temporary_failure" },
  ];
  data.match.teams[0].participants = data.match.teams[0].participants.slice(
    0,
    2,
  );
  data.match.teams[1].participants = [];
  globalThis.fetch = async () => Response.json(data);
  render(<MatchDetailView {...props} />);
  await heading("Equipos y participantes");
  assert.ok(screen.getAllByText(/parcial/i).length > 0);
  assert.ok(screen.getByText(/No hay participantes disponibles/i));
  assert.equal(screen.queryByText("Rojo 0"), null);
});

test("failed request offers retry; successful retry restores the result", async () => {
  let attempts = 0;
  globalThis.fetch = async () =>
    ++attempts === 1
      ? new Response("", { status: 503 })
      : Response.json(fixture());
  const user = userEvent.setup();
  render(<MatchDetailView {...props} />);
  const retry = await screen.findByRole("button", { name: /Reintentar/ });
  assert.ok(screen.getByRole("alert"));
  await user.click(retry);
  assert.ok(await heading("Relaciones históricas"));
  assert.equal(attempts, 2);
});

test("direct match without owner returns to search and never claims a historical zero", async () => {
  const data = fixture();
  data.match.familiarity = null;
  data.match.premadeGroups = [];
  let requestMethod: string | undefined;
  globalThis.fetch = async (_input, init) => {
    requestMethod = init?.method;
    return Response.json(data.match);
  };
  render(<MatchDetailView matchId="LA1_FIXTURE" />);
  assert.ok(await screen.findByRole("link", { name: /Volver a la búsqueda/ }));
  await heading("Equipos y participantes");
  assert.ok(!requestMethod || requestMethod === "GET");
  assert.match(
    document.body.textContent!,
    /no disponible|sin análisis|no solicitado/i,
  );
});

test("leaving cancels an in-flight match analysis", async () => {
  let signal: AbortSignal | undefined;
  globalThis.fetch = async (_input, init) => {
    signal = init?.signal as AbortSignal;
    return new Promise<Response>((_resolve, reject) =>
      signal!.addEventListener(
        "abort",
        () => reject(new DOMException("cancelled", "AbortError")),
        { once: true },
      ),
    );
  };
  const view = render(<MatchDetailView {...props} />);
  await waitFor(() => assert.ok(signal));
  assert.ok(screen.getByRole("status"));
  assert.match(document.body.textContent!, /Analizando partida/i);
  view.unmount();
  assert.ok(signal!.aborted);
});

test("missing participant identity is never displayed as a raw PUUID", async () => {
  const data = fixture();
  data.relationships.push({
    firstPuuid: "owner-fixture",
    secondPuuid: "private-unknown-fixture",
    matchesTogether: 2,
    allyMatches: 1,
    enemyMatches: 1,
    score: 0,
    confidence: "LOW",
  });
  globalThis.fetch = async () => Response.json(data);
  render(<MatchDetailView {...props} />);
  await heading("Relaciones históricas");
  assert.doesNotMatch(document.body.textContent!, /private-unknown-fixture/);
});

test("ties in historical pair ordering do not depend on response order", async () => {
  const data = fixture();
  globalThis.fetch = async () => Response.json(data);
  const view = render(<MatchDetailView {...props} />);
  await heading("Relaciones históricas");
  const rows = () =>
    within(
      screen
        .getByRole("heading", { name: "Relaciones históricas" })
        .closest("section")!,
    )
      .getAllByRole("listitem")
      .map((li) => li.textContent);
  const ordered = rows();
  view.unmount();
  data.relationships.reverse();
  render(<MatchDetailView {...props} />);
  await heading("Relaciones históricas");
  assert.deepEqual(rows(), ordered);
});

test("KDA handles zero deaths and missing combat fields without Infinity or invented values", async () => {
  const data = fixture();
  data.match.teams[0].participants[0].deaths = 0;
  globalThis.fetch = async () => Response.json(data);
  const view = render(<MatchDetailView {...props} />);
  await heading("Resumen de partida");
  let summary = screen
    .getByRole("heading", { name: "Resumen de partida" })
    .closest("section")!;
  assert.ok(within(summary).getByText("11.00"));
  assert.doesNotMatch(summary.textContent!, /Infinity|NaN/);
  view.unmount();
  data.match.teams[0].participants[0].assists = null as unknown as number;
  data.match.teams[0].participants[0].championName = "";
  render(<MatchDetailView {...props} />);
  await heading("Resumen de partida");
  summary = screen
    .getByRole("heading", { name: "Resumen de partida" })
    .closest("section")!;
  assert.ok(within(summary).getAllByText("—").length >= 1);
  assert.ok(within(summary).getByText("Campeón no disponible"));
  assert.doesNotMatch(summary.textContent!, /Infinity|NaN/);
});

test("changing match and owner cancels the prior request and ignores its late response", async () => {
  let resolveOld: ((response: Response) => void) | undefined;
  let oldSignal: AbortSignal | undefined;
  const next = fixture();
  next.match.riotMatchId = "LA1_NEW";
  next.owner = {
    puuid: "player-b",
    gameName: "Beto",
    tagLine: "TEST",
    platformRegion: "la1",
  };
  globalThis.fetch = async (input, init) => {
    if (String(input).includes("LA1_OLD")) {
      oldSignal = init!.signal as AbortSignal;
      // Deliberately ignore abort: this simulates an uncooperative transport.
      return new Promise<Response>((resolve) => {
        resolveOld = resolve;
      });
    }
    return Response.json(next);
  };
  const view = render(<MatchDetailView {...props} matchId="LA1_OLD" />);
  await waitFor(() => assert.ok(resolveOld));
  view.rerender(
    <MatchDetailView
      {...props}
      matchId="LA1_NEW"
      ownerPuuid="player-b"
      returnProfile={{ gameName: "Beto", tagLine: "TEST" }}
    />,
  );
  await heading("Resumen de partida");
  assert.ok(oldSignal!.aborted);
  let summary = screen
    .getByRole("heading", { name: "Resumen de partida" })
    .closest("section")!;
  assert.match(summary.textContent!, /Beto/);
  await act(async () => {
    resolveOld!(Response.json(fixture()));
    await new Promise((resolve) => setTimeout(resolve, 20));
  });
  summary = screen
    .getByRole("heading", { name: "Resumen de partida" })
    .closest("section")!;
  assert.match(summary.textContent!, /Beto/);
  assert.doesNotMatch(summary.textContent!, /Ana/);
});
