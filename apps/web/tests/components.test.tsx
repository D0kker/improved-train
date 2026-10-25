import "./dom-environment.ts";

import assert from "node:assert/strict";
import { afterEach, test } from "node:test";
import { cleanup, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { RiotIdSearchForm } from "../src/riot-id-search-form.tsx";
import { HomeContent } from "../app/home-content.tsx";
import { homeCopy } from "../app/home-copy.ts";
import { SiteHeader } from "../src/site-header.tsx";
import { SkipLink } from "../src/skip-link.tsx";
import { PlayerDashboard } from "../app/player/[gameName]/[tagLine]/player-dashboard.tsx";
import type { PlayerSearchResult } from "../src/api.ts";

const originalFetch = globalThis.fetch;

afterEach(() => {
  cleanup();
  globalThis.fetch = originalFetch;
  window.history.replaceState(null, "", "/");
});

test("Riot ID validation associates field errors and focuses the first invalid input", async () => {
  const user = userEvent.setup();
  render(<RiotIdSearchForm platformRegion="la1" onNavigate={() => assert.fail("No debe navegar")} />);

  const gameName = screen.getByRole("textbox", { name: "Nombre de jugador" });
  const tagLine = screen.getByRole("textbox", { name: "Tag" });
  await user.click(screen.getByRole("button", { name: "Buscar jugador" }));

  assert.equal(document.activeElement, gameName);
  assert.equal(gameName.getAttribute("aria-invalid"), "true");
  assert.equal(tagLine.getAttribute("aria-invalid"), "true");
  const gameErrorId = gameName.getAttribute("aria-describedby");
  const tagErrorId = tagLine.getAttribute("aria-describedby");
  assert.equal(document.getElementById(gameErrorId!)?.textContent, "Escribe el nombre de jugador.");
  assert.equal(document.getElementById(tagErrorId!)?.textContent, "Escribe el Tag.");
});

test("Riot ID validation focuses Tag when only Tag is missing", async () => {
  const user = userEvent.setup();
  render(<RiotIdSearchForm platformRegion="la1" onNavigate={() => assert.fail("No debe navegar")} />);
  const gameName = screen.getByRole("textbox", { name: "Nombre de jugador" });
  const tagLine = screen.getByRole("textbox", { name: "Tag" });

  await user.type(gameName, "Ana");
  await user.click(screen.getByRole("button", { name: "Buscar jugador" }));

  assert.equal(document.activeElement, tagLine);
  assert.equal(gameName.getAttribute("aria-invalid"), "false");
  assert.equal(tagLine.getAttribute("aria-invalid"), "true");
  assert.equal(
    document.getElementById(tagLine.getAttribute("aria-describedby")!)?.textContent,
    "Escribe el Tag.",
  );
});

test("Riot ID form works by keyboard and announces navigation without rejecting punctuation", async () => {
  const user = userEvent.setup();
  const navigations: string[] = [];
  render(
    <RiotIdSearchForm
      platformRegion="la1"
      onNavigate={(href) => navigations.push(href)}
    />,
  );
  const gameName = screen.getByRole("textbox", { name: "Nombre de jugador" });
  const tagLine = screen.getByRole("textbox", { name: "Tag" });

  await user.tab();
  assert.equal(document.activeElement, gameName);
  await user.type(gameName, "Ana Marie");
  await user.tab();
  assert.equal(document.activeElement, tagLine);
  await user.type(tagLine, "A/B");
  await user.keyboard("{Enter}");

  assert.deepEqual(navigations, ["/player/Ana%20Marie/A%2FB?platformRegion=la1"]);
  assert.equal(screen.getByRole("status").textContent, "Abriendo el perfil del jugador.");
  assert.equal(screen.getByRole("button", { name: "Abriendo perfil…" }).hasAttribute("disabled"), true);
});

test("Home presenta la búsqueda como acción principal y conecta el flujo sin llamadas adicionales", async () => {
  const user = userEvent.setup();
  const navigations: string[] = [];
  globalThis.fetch = async () => assert.fail("Home no debe iniciar solicitudes de API");
  render(
    <>
      <SiteHeader actionHref="#player-search-form" actionLabel="Ir a la búsqueda" />
      <HomeContent
        copy={homeCopy}
        platformRegionLabel="Latinoamérica Sur (LAS) · LA2"
        searchForm={
          <RiotIdSearchForm
            platformRegion="la2"
            onNavigate={(href) => navigations.push(href)}
          />
        }
      />
    </>,
  );

  assert.equal(screen.getAllByRole("main").length, 1);
  assert.equal(screen.getByRole("link", { name: "Ir a la búsqueda" }).getAttribute("href"), "#player-search-form");
  assert.ok(document.getElementById("player-search-form"));
  assert.equal(screen.getByRole("heading", { level: 1 }).textContent, "Cada partida tiene una historia.");
  assert.match(screen.getByText(/Busca jugadores, explora sus partidas recientes/).textContent!, /encontrado antes/);
  assert.match(screen.getByText(/La consulta inicial revisa cinco partidas recientes/).textContent!, /no confirma/);
  assert.equal(screen.getByLabelText("Región configurada").textContent, "Región de búsqueda: Latinoamérica Sur (LAS) · LA2");
  assert.equal(screen.getAllByRole("heading", { level: 3 }).length, 3);

  await user.type(screen.getByRole("textbox", { name: "Nombre de jugador" }), "Ana Marie");
  await user.type(screen.getByRole("textbox", { name: "Tag" }), "LAN");
  await user.click(screen.getByRole("button", { name: /Buscar jugador/ }));

  assert.deepEqual(navigations, ["/player/Ana%20Marie/LAN?platformRegion=la2"]);
});

test("Home conserva el contenido traducible largo sin abreviarlo", () => {
  const longDescription = `${homeCopy.description} ${"Información de contexto ampliada. ".repeat(3)}`.trim();
  const longRegion = `Región de juego con nombre extendido ${"y variante regional. ".repeat(2)}`.trim();
  render(
    <HomeContent
      copy={{ ...homeCopy, description: longDescription }}
      platformRegionLabel={longRegion}
      searchForm={<div>Formulario de búsqueda</div>}
    />,
  );

  assert.equal(document.querySelector(".home-lede")?.textContent?.trim(), longDescription);
  assert.equal(screen.getByLabelText("Región configurada").textContent?.trim(), `Región de búsqueda: ${longRegion}`);
});

test("skip link has a valid main target and supports keyboard activation", async () => {
  const user = userEvent.setup();
  render(
    <>
      <SkipLink />
      <main id="main-content" tabIndex={-1}>Contenido principal</main>
    </>,
  );
  const skipLink = screen.getByRole("link", { name: "Saltar al contenido" });
  assert.equal(skipLink.getAttribute("href"), "#main-content");
  assert.ok(document.querySelector(skipLink.getAttribute("href")!));

  await user.tab();
  assert.equal(document.activeElement, skipLink);
  await user.keyboard("{Enter}");
  await waitFor(() => assert.equal(window.location.hash, "#main-content"));
});

test("site header exposes a named navigation landmark and keyboard-reachable links", async () => {
  const user = userEvent.setup();
  render(<SiteHeader actionLabel="Nueva búsqueda" />);
  const navigation = screen.getByRole("navigation", { name: "Navegación principal" });
  assert.equal(within(navigation).getByRole("link", { name: "PROJECT ECHO — Inicio" }).getAttribute("href"), "/");
  assert.equal(within(navigation).getByRole("link", { name: "Nueva búsqueda" }).getAttribute("href"), "/");
  await user.tab();
  assert.equal(document.activeElement, within(navigation).getByRole("link", { name: "PROJECT ECHO — Inicio" }));
});

function matchItem(id: string) {
  return {
    riotMatchId: id,
    queueId: 420,
    gameStartTimestamp: "2026-10-18T10:00:00Z",
    gameDurationSeconds: 1800,
    championId: 0,
    championName: "Annie",
    kills: 3,
    deaths: 2,
    assists: 4,
    win: true,
  };
}

function result(ids: string[], status: "complete" | "partial" | "insufficient" = "complete"): PlayerSearchResult {
  const puuid = "owner-puuid";
  return {
    player: { puuid, gameName: "Ana", tagLine: "LAN", platformRegion: "la1" },
    summary: {
      puuid,
      gameName: "Ana",
      tagLine: "LAN",
      matchesAnalyzed: ids.length,
      wins: ids.length,
      losses: 0,
      winRate: 100,
      uniquePlayersEncountered: 4,
      repeatedPlayers: 1,
      dataUpdatedAt: "2026-10-18T10:00:00Z",
    },
    matches: { page: 1, pageSize: 5, totalCount: ids.length, items: ids.map(matchItem) },
    encounters: [],
    coverage: {
      status,
      participants: 1,
      matchIdsObtained: status === "insufficient" ? 0 : 5,
      uniqueMatchIds: ids.length,
      matchesRecovered: ids.length,
      players: [{ puuid, requested: 5, matchIds: 5, eligibleMatches: ids.length, limited: status !== "complete" }],
      failures: status === "partial" ? [{ stage: "match_detail", code: "mock_failure" }] : [],
    },
  };
}

test("dashboard pagination retries partial and failed windows, deduplicates IDs, and stops at exhaustion", async () => {
  const requests: string[] = [];
  const pages = [
    result(["LA1_5", "LA1_6", "LA1_7"], "partial"),
    new Response("service unavailable", { status: 503 }),
    result(["LA1_5", "LA1_6", "LA1_7", "LA1_8", "LA1_9"]),
    result([], "insufficient"),
  ];
  globalThis.fetch = async (input) => {
    const path = String(input);
    if (path.includes("/analysis?")) return Response.json(result(["LA1_0", "LA1_1", "LA1_2", "LA1_3", "LA1_4"]));
    if (path.includes("/active-game")) return Response.json(null);
    if (path.includes("/recent-matches")) {
      requests.push(path);
      const page = pages.shift();
      if (!page) throw new Error("No hay fixture para la ventana solicitada");
      return page instanceof Response ? page : Response.json(page);
    }
    throw new Error(`Solicitud no esperada: ${path}`);
  };

  const user = userEvent.setup();
  render(<PlayerDashboard gameName="Ana" tagLine="LAN" platformRegion="la1" />);
  const history = await screen.findByRole("region", { name: "Partidas recientes" });
  const summary = screen.getByRole("region", { name: "Tu historial de encuentros" });
  const loadMore = () => within(history).getByRole("button");
  const initialMatchesAnalyzed = () =>
    within(summary).getByText("Partidas analizadas").parentElement?.querySelector("dd")?.textContent;
  assert.equal(within(history).getByText("5 partidas cargadas").textContent, "5 partidas cargadas");
  assert.equal(initialMatchesAnalyzed(), "5");

  await user.click(loadMore());
  await screen.findByText("Consulta parcial");
  assert.match(requests[0], /start=5/);
  assert.equal(within(history).getByText("8 partidas cargadas").textContent, "8 partidas cargadas");
  assert.equal(within(history).getByRole("button").textContent, "Reintentar este bloque de cinco");
  assert.equal(initialMatchesAnalyzed(), "5");

  await user.click(loadMore());
  await screen.findByText(/El servicio de Riot no está disponible/);
  assert.match(requests[1], /start=5/);
  assert.equal(within(history).getByText("8 partidas cargadas").textContent, "8 partidas cargadas");
  assert.equal(within(history).getByRole("button").textContent, "Reintentar este bloque de cinco");

  await user.click(loadMore());
  await waitFor(() => assert.equal(within(history).getByText("10 partidas cargadas").textContent, "10 partidas cargadas"));
  assert.match(requests[2], /start=5/);
  assert.equal(document.querySelectorAll(".match-card").length, 10);
  assert.equal(initialMatchesAnalyzed(), "5");

  await user.click(loadMore());
  await waitFor(() => assert.equal(within(history).queryByRole("button"), null));
  assert.match(requests[3], /start=10/);
  assert.equal(document.querySelectorAll(".match-card").length, 10);
});

test("dashboard shows history before a pending active-game request resolves", async () => {
  let resolveActiveGame: ((response: Response) => void) | undefined;
  globalThis.fetch = async (input) => {
    const path = String(input);
    if (path.includes("/analysis?")) return Response.json(result(["LA1_0", "LA1_1", "LA1_2", "LA1_3", "LA1_4"]));
    if (path.includes("/active-game")) {
      return await new Promise<Response>((resolve) => { resolveActiveGame = resolve; });
    }
    throw new Error(`Solicitud no esperada: ${path}`);
  };
  render(<PlayerDashboard gameName="Ana" tagLine="LAN" platformRegion="la1" />);

  assert.ok(await screen.findByRole("region", { name: "Tu historial de encuentros" }));
  assert.match(screen.getByText("Comprobando si hay una partida en curso…").textContent!, /Comprobando/);
  resolveActiveGame?.(Response.json(null));
  assert.ok(await screen.findByText("No se detectó una partida en curso en la última consulta."));
});
