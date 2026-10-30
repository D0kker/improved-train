import "./dom-environment.ts";

import assert from "node:assert/strict";
import { afterEach, test } from "node:test";
import {
  cleanup,
  render,
  screen,
  waitFor,
  within,
} from "@testing-library/react";
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
  render(
    <RiotIdSearchForm
      platformRegion="la1"
      onNavigate={() => assert.fail("No debe navegar")}
    />,
  );

  const gameName = screen.getByRole("textbox", { name: "Nombre de jugador" });
  const tagLine = screen.getByRole("textbox", { name: "Tag" });
  await user.click(screen.getByRole("button", { name: "Buscar jugador" }));

  assert.equal(document.activeElement, gameName);
  assert.equal(gameName.getAttribute("aria-invalid"), "true");
  assert.equal(tagLine.getAttribute("aria-invalid"), "true");
  const gameErrorId = gameName.getAttribute("aria-describedby");
  const tagErrorId = tagLine.getAttribute("aria-describedby");
  assert.equal(
    document.getElementById(gameErrorId!)?.textContent,
    "Escribe el nombre de jugador.",
  );
  assert.equal(
    document.getElementById(tagErrorId!)?.textContent,
    "Escribe el Tag.",
  );
});

test("Riot ID validation focuses Tag when only Tag is missing", async () => {
  const user = userEvent.setup();
  render(
    <RiotIdSearchForm
      platformRegion="la1"
      onNavigate={() => assert.fail("No debe navegar")}
    />,
  );
  const gameName = screen.getByRole("textbox", { name: "Nombre de jugador" });
  const tagLine = screen.getByRole("textbox", { name: "Tag" });

  await user.type(gameName, "Ana");
  await user.click(screen.getByRole("button", { name: "Buscar jugador" }));

  assert.equal(document.activeElement, tagLine);
  assert.equal(gameName.getAttribute("aria-invalid"), "false");
  assert.equal(tagLine.getAttribute("aria-invalid"), "true");
  assert.equal(
    document.getElementById(tagLine.getAttribute("aria-describedby")!)
      ?.textContent,
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

  assert.deepEqual(navigations, [
    "/player/Ana%20Marie/A%2FB?platformRegion=la1",
  ]);
  assert.equal(
    screen.getByRole("status").textContent,
    "Abriendo el perfil del jugador.",
  );
  assert.equal(
    screen
      .getByRole("button", { name: "Abriendo perfil…" })
      .hasAttribute("disabled"),
    true,
  );
});

test("Home presenta la búsqueda como acción principal y conecta el flujo sin llamadas adicionales", async () => {
  const user = userEvent.setup();
  const navigations: string[] = [];
  globalThis.fetch = async () =>
    assert.fail("Home no debe iniciar solicitudes de API");
  render(
    <>
      <SiteHeader
        actionHref="#player-search-form"
        actionLabel="Ir a la búsqueda"
      />
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
  assert.equal(
    screen.getByRole("link", { name: "Ir a la búsqueda" }).getAttribute("href"),
    "#player-search-form",
  );
  assert.ok(document.getElementById("player-search-form"));
  assert.equal(
    screen.getByRole("heading", { level: 1 }).textContent,
    "Cada partida tiene una historia.",
  );
  assert.match(
    screen.getByText(/Busca jugadores, explora sus partidas recientes/)
      .textContent!,
    /encontrado antes/,
  );
  assert.match(
    screen.getByText(/La consulta inicial revisa cinco partidas recientes/)
      .textContent!,
    /no confirma/,
  );
  assert.equal(
    screen.getByLabelText("Región configurada").textContent,
    "Región de búsqueda: Latinoamérica Sur (LAS) · LA2",
  );
  assert.equal(screen.getAllByRole("heading", { level: 3 }).length, 3);

  await user.type(
    screen.getByRole("textbox", { name: "Nombre de jugador" }),
    "Ana Marie",
  );
  await user.type(screen.getByRole("textbox", { name: "Tag" }), "LAN");
  await user.click(screen.getByRole("button", { name: /Buscar jugador/ }));

  assert.deepEqual(navigations, ["/player/Ana%20Marie/LAN?platformRegion=la2"]);
});

test("Home conserva el contenido traducible largo sin abreviarlo", () => {
  const longDescription =
    `${homeCopy.description} ${"Información de contexto ampliada. ".repeat(3)}`.trim();
  const longRegion =
    `Región de juego con nombre extendido ${"y variante regional. ".repeat(2)}`.trim();
  render(
    <HomeContent
      copy={{ ...homeCopy, description: longDescription }}
      platformRegionLabel={longRegion}
      searchForm={<div>Formulario de búsqueda</div>}
    />,
  );

  assert.equal(
    document.querySelector(".home-lede")?.textContent?.trim(),
    longDescription,
  );
  assert.equal(
    screen.getByLabelText("Región configurada").textContent?.trim(),
    `Región de búsqueda: ${longRegion}`,
  );
});

test("skip link has a valid main target and supports keyboard activation", async () => {
  const user = userEvent.setup();
  render(
    <>
      <SkipLink />
      <main id="main-content" tabIndex={-1}>
        Contenido principal
      </main>
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
  const navigation = screen.getByRole("navigation", {
    name: "Navegación principal",
  });
  assert.equal(
    within(navigation)
      .getByRole("link", { name: "PROJECT ECHO — Inicio" })
      .getAttribute("href"),
    "/",
  );
  assert.equal(
    within(navigation)
      .getByRole("link", { name: "Nueva búsqueda" })
      .getAttribute("href"),
    "/",
  );
  await user.tab();
  assert.equal(
    document.activeElement,
    within(navigation).getByRole("link", { name: "PROJECT ECHO — Inicio" }),
  );
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
    win: !id.endsWith("1"),
  };
}

function result(
  ids: string[],
  status: "complete" | "partial" | "insufficient" = "complete",
): PlayerSearchResult {
  const puuid = "owner-puuid";
  const wins = ids.filter((id) => !id.endsWith("1")).length;
  const losses = ids.length - wins;
  return {
    player: { puuid, gameName: "Ana", tagLine: "LAN", platformRegion: "la1" },
    summary: {
      puuid,
      gameName: "Ana",
      tagLine: "LAN",
      matchesAnalyzed: ids.length,
      wins,
      losses,
      winRate:
        ids.length === 0 ? 0 : Math.round((wins * 1000) / ids.length) / 10,
      uniquePlayersEncountered: 4,
      repeatedPlayers: 1,
      dataUpdatedAt: "2026-10-18T10:00:00Z",
    },
    matches: {
      page: 1,
      pageSize: 5,
      totalCount: ids.length,
      items: ids.map(matchItem),
    },
    encounters: [],
    coverage: {
      status,
      participants: 1,
      matchIdsObtained: status === "insufficient" ? 0 : 5,
      uniqueMatchIds: ids.length,
      matchesRecovered: ids.length,
      players: [
        {
          puuid,
          requested: 5,
          matchIds: 5,
          eligibleMatches: ids.length,
          limited: status !== "complete",
        },
      ],
      failures:
        status === "partial"
          ? [{ stage: "match_detail", code: "mock_failure" }]
          : [],
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
    if (path.includes("/analysis?"))
      return Response.json(
        result(["LA1_0", "LA1_1", "LA1_2", "LA1_3", "LA1_4"]),
      );
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
  const history = await screen.findByRole("region", {
    name: "Partidas recientes",
  });
  const summary = await screen.findByRole("region", {
    name: "Rendimiento competitivo",
  });
  const loadMore = () =>
    within(history).getByRole("button", {
      name: /Cargar cinco|Reintentar este bloque/,
    });
  const initialMatchesAnalyzed = () =>
    within(summary)
      .getByText("Partidas analizadas")
      .parentElement?.querySelector("dd")?.textContent;
  assert.equal(
    within(history).getByText("5 partidas cargadas").textContent,
    "5 partidas cargadas",
  );
  assert.equal(initialMatchesAnalyzed(), "5");

  await user.click(loadMore());
  await screen.findAllByText(/Consulta parcial/);
  assert.match(requests[0], /start=5/);
  assert.equal(
    within(history).getByText("8 partidas cargadas").textContent,
    "8 partidas cargadas",
  );
  assert.equal(
    within(history).getByRole("button", {
      name: /Cargar cinco|Reintentar este bloque/,
    }).textContent,
    "Reintentar este bloque de cinco",
  );
  assert.equal(initialMatchesAnalyzed(), "8");

  await user.click(loadMore());
  await screen.findByText(/El servicio de Riot no está disponible/);
  assert.match(requests[1], /start=5/);
  assert.equal(
    within(history).getByText("8 partidas cargadas").textContent,
    "8 partidas cargadas",
  );
  assert.equal(
    within(history).getByRole("button", {
      name: /Cargar cinco|Reintentar este bloque/,
    }).textContent,
    "Reintentar este bloque de cinco",
  );

  await user.click(loadMore());
  await waitFor(() =>
    assert.equal(
      within(history).getByText("10 partidas cargadas").textContent,
      "10 partidas cargadas",
    ),
  );
  assert.match(requests[2], /start=5/);
  assert.equal(document.querySelectorAll(".match-card").length, 10);
  assert.equal(initialMatchesAnalyzed(), "10");

  await user.click(loadMore());
  await waitFor(() =>
    assert.equal(
      within(history).queryByRole("button", {
        name: /Cargar cinco|Reintentar este bloque/,
      }),
      null,
    ),
  );
  assert.match(requests[3], /start=10/);
  assert.equal(document.querySelectorAll(".match-card").length, 10);
});

test("dashboard highlights identity, region, initial metrics and one clear action per match", async () => {
  globalThis.fetch = async (input) => {
    const path = String(input);
    if (path.includes("/analysis?"))
      return Response.json(
        result(["LA1_0", "LA1_1", "LA1_2", "LA1_3", "LA1_4"]),
      );
    if (path.includes("/active-game")) return Response.json(null);
    throw new Error(`Solicitud no esperada: ${path}`);
  };

  const user = userEvent.setup();
  render(<PlayerDashboard gameName="Ana" tagLine="LAN" platformRegion="la1" />);

  const identity = await screen.findByRole("heading", {
    level: 1,
    name: "Ana#LAN",
  });
  assert.ok(identity);
  assert.equal(
    screen
      .getByText("Latinoamérica Norte (LAN)")
      .getAttribute("class")
      ?.includes("ui-badge"),
    true,
  );

  const summary = await screen.findByRole("region", {
    name: "Rendimiento competitivo",
  });
  assert.equal(within(summary).queryByText(/Todas las partidas válidas/), null);
  assert.ok(screen.getByText(/los filtros solo cambian el historial/i));
  assert.ok(within(summary).getByText("Win rate"));
  assert.ok(within(summary).getByRole("img", { name: /Win rate: 80%/ }));
  assert.ok(within(summary).getByText("Victorias"));
  assert.ok(within(summary).getByText("Derrotas"));

  const history = screen.getByRole("region", { name: "Partidas recientes" });
  assert.equal(within(history).getAllByText("Victoria").length, 4);
  assert.equal(within(history).getAllByText("Derrota").length, 1);
  assert.equal(within(history).getAllByText(/Analizar encuentros/).length, 5);
  assert.equal(document.querySelectorAll(".match-card").length, 5);
  assert.equal(screen.queryByText("Analiza esta partida"), null);
  const firstMatchLink = within(history).getAllByRole("link", {
    name: /Victoria.*Annie.*Analizar encuentros/,
  })[0];
  assert.match(firstMatchLink.getAttribute("href") ?? "", /^\/match\/LA1_/);
  for (let i = 0; i < 12 && document.activeElement !== firstMatchLink; i++)
    await user.tab();
  assert.equal(document.activeElement, firstMatchLink);
});

test("dashboard empty history explains the initial window and coverage separately", async () => {
  globalThis.fetch = async (input) => {
    const path = String(input);
    if (path.includes("/analysis?"))
      return Response.json(result([], "insufficient"));
    if (path.includes("/active-game")) return Response.json(null);
    throw new Error(`Solicitud no esperada: ${path}`);
  };

  render(<PlayerDashboard gameName="Ana" tagLine="LAN" platformRegion="la1" />);
  assert.ok(
    await screen.findByText("No hay partidas disponibles en esta consulta"),
  );
  assert.ok(
    screen.getByText(/Esto no describe todo el historial de la cuenta/),
  );
  assert.ok(screen.getAllByText(/Información insuficiente/)[0]);
});

test("dashboard shows history before a pending active-game request resolves", async () => {
  let resolveActiveGame: ((response: Response) => void) | undefined;
  globalThis.fetch = async (input) => {
    const path = String(input);
    if (path.includes("/analysis?"))
      return Response.json(
        result(["LA1_0", "LA1_1", "LA1_2", "LA1_3", "LA1_4"]),
      );
    if (path.includes("/active-game")) {
      return await new Promise<Response>((resolve) => {
        resolveActiveGame = resolve;
      });
    }
    throw new Error(`Solicitud no esperada: ${path}`);
  };
  render(<PlayerDashboard gameName="Ana" tagLine="LAN" platformRegion="la1" />);

  assert.ok(
    await screen.findByRole("region", { name: "Rendimiento competitivo" }),
  );
  assert.match(
    screen.getByText("Comprobando si hay una partida en curso…").textContent!,
    /Comprobando/,
  );
  resolveActiveGame?.(Response.json(null));
  assert.ok(await screen.findByText("No se detectó una partida en curso"));
});

test("competitive filters and expansion use loaded data without changing performance", async () => {
  const fixture = result(["LA1_0", "LA1_2"]);
  let requests = 0;
  globalThis.fetch = async (input) => {
    requests++;
    return Response.json(
      String(input).includes("/active-game") ? null : fixture,
    );
  };
  const user = userEvent.setup();
  render(<PlayerDashboard gameName="Ana" tagLine="LAN" platformRegion="la1" />);
  const history = await screen.findByRole("region", {
    name: "Partidas recientes",
  });
  await user.click(within(history).getByRole("button", { name: "Derrotas" }));
  assert.ok(
    screen.getByText("No hay partidas cargadas que coincidan con este filtro."),
  );
  assert.ok(screen.getByRole("img", { name: /Win rate: 100%/ }));
  await user.click(within(history).getByRole("button", { name: "Todas" }));
  assert.equal(within(history).queryByText(/Ver detalles de/), null);
  assert.equal(history.querySelectorAll("details").length, 0);
  assert.equal(history.querySelectorAll(".match-kda-ratio").length, 2);
  assert.equal(requests, 3);
});

test("recurrent players expand locally and link only complete identities", async () => {
  const fixture = result(["LA1_0", "LA1_2"]);
  fixture.encounters = Array.from({ length: 6 }, (_, i) => ({
    otherPlayerPuuid: `other-${i}`,
    gameName: `Nombre ${i}`,
    tagLine: i === 5 ? "" : "TEST",
    totalMatches: 2,
    sameTeamMatches: 1,
    enemyTeamMatches: 1,
    winsTogether: 1,
    lossesTogether: 0,
    winsAgainst: 1,
    lossesAgainst: 0,
    firstSeenAt: "2026-01-01",
    lastSeenAt: "2026-01-02",
  }));
  let calls = 0;
  globalThis.fetch = async (input) => {
    calls++;
    return Response.json(
      String(input).includes("/active-game") ? null : fixture,
    );
  };
  const user = userEvent.setup();
  render(<PlayerDashboard gameName="Ana" tagLine="LAN" platformRegion="la1" />);
  await screen.findByRole("heading", { name: "Relaciones entre jugadores" });
  assert.equal(
    document.querySelectorAll(".competitive-recurrent li").length,
    5,
  );
  assert.match(
    screen.getByRole("link", { name: "Nombre 0#TEST" }).getAttribute("href")!,
    /platformRegion=la1/,
  );
  await user.click(screen.getByRole("button", { name: "Ver más" }));
  assert.equal(
    document.querySelectorAll(".competitive-recurrent li").length,
    6,
  );
  assert.equal(screen.queryByRole("link", { name: "Nombre 5" }), null);
  assert.equal(calls, 3);
});

test("active game failure is independent and can be retried manually into a live link", async () => {
  let active = 0;
  globalThis.fetch = async (input) =>
    String(input).includes("/active-game")
      ? ++active === 1
        ? new Response("", { status: 503 })
        : Response.json({
            gameId: 1,
            queueId: 420,
            gameStartTime: "2026-01-01",
            gameLengthSeconds: 60,
            participants: [],
          })
      : Response.json(result(["LA1_0"]));
  const user = userEvent.setup();
  render(<PlayerDashboard gameName="Ana" tagLine="LAN" platformRegion="la1" />);
  await screen.findByRole("region", { name: "Rendimiento competitivo" });
  await user.click(
    await screen.findByRole("button", { name: "Volver a comprobar" }),
  );
  const live = await screen.findByRole("link", {
    name: "Ver partida en curso →",
  });
  assert.match(live.getAttribute("href")!, /ownerPuuid=owner-puuid/);
  assert.equal(active, 2);
  assert.equal(document.querySelectorAll(".match-card").length, 1);
});

test("historical shared-match links are backed by deduplicated match evidence", async () => {
  const fixture = result(["LA1_0", "LA1_2"]);
  fixture.matches.items = fixture.matches.items.map((m, i) => ({
    ...m,
    evidence: {
      ownerPuuid: "owner-puuid",
      platformRegion: "la1",
      status: "partial",
      participants: [
        { puuid: "owner-puuid", gameName: "Ana", tagLine: "LAN", teamId: 100 },
        {
          puuid: "other",
          gameName: "Other",
          tagLine: "TEST",
          teamId: i === 0 ? 100 : 200,
        },
      ],
    },
  }));
  let calls = 0;
  globalThis.fetch = async (input) => {
    calls++;
    return Response.json(
      String(input).includes("/active-game") ? null : fixture,
    );
  };
  const user = userEvent.setup();
  render(<PlayerDashboard gameName="Ana" tagLine="LAN" platformRegion="la1" />);
  const summary = await screen.findByText("Ver partidas (2)");
  await user.click(summary);
  const link = screen.getByRole("link", {
    name: "Analizar partida compartida 1 con Other#TEST",
  });
  assert.match(link.getAttribute("href")!, /ownerPuuid=owner-puuid/);
  assert.match(link.closest("li")!.textContent!, /Aliados/);
  assert.match(
    screen
      .getByRole("link", {
        name: "Analizar partida compartida 2 con Other#TEST",
      })
      .closest("li")!.textContent!,
    /Rivales/,
  );
  assert.equal(calls, 3);
});

test("refined dashboard keeps historical profile, compact region, date and seven metrics", async () => {
  const fixture = result(["LA1_0", "LA1_1", "LA1_2", "LA1_3", "LA1_4"]);
  fixture.player.profileIconId = 29;
  fixture.player.profileIconObservedAt = fixture.summary.dataUpdatedAt;
  const calls: string[] = [];
  globalThis.fetch = async (input) => {
    calls.push(String(input));
    return Response.json(
      String(input).includes("active-game") ? null : fixture,
    );
  };
  const user = userEvent.setup();
  render(<PlayerDashboard gameName="Ana" tagLine="LAN" platformRegion="la1" />);
  const region = await screen.findByText("Latinoamérica Norte (LAN)");
  assert.equal(region.textContent, "Latinoamérica Norte (LAN)");
  const icon = await screen.findByRole("img", { name: /invocador observado/ });
  assert.equal(icon.getAttribute("width"), "88");
  assert.equal(screen.queryByText("Ícono histórico"), null);
  assert.ok(icon.getAttribute("aria-describedby"));
  assert.equal(screen.queryByText("PERFIL DEL JUGADOR"), null);
  const date = document.querySelector(".profile-freshness time")!;
  assert.equal(date.getAttribute("datetime"), fixture.summary.dataUpdatedAt);
  assert.match(date.textContent!, /oct/);
  assert.equal(
    document.querySelectorAll(".competitive-performance-metrics dl").length,
    7,
  );
  const champions = screen.getByRole("heading", {
    level: 3,
    name: "Campeones más jugados",
  });
  assert.ok(champions);
  assert.ok(screen.getByText("Según las partidas cargadas"));
  for (const card of document.querySelectorAll(".competitive-champion-card")) {
    assert.ok(card.querySelector(".competitive-champion-copy h4"));
    // This existing JSDOM harness uses champion fallback fixtures; actual
    // Next/Image rendering and dimensions are verified in Chromium.
    assert.equal(
      card.querySelector<HTMLElement>(".champion-icon-fallback")?.style.width,
      "72px",
    );
  }
  assert.equal(
    screen.getByRole("link", { name: /Ver historial/ }).getAttribute("href"),
    "#matches-title",
  );
  await user.click(
    screen.getByRole("button", { name: "Actualizar cinco partidas" }),
  );
  await waitFor(() => assert.equal(calls.length, 6));
  assert.ok(calls.every((path) => path.includes("platformRegion=la1")));
  assert.equal(document.querySelectorAll(".match-card").length, 5);
});

test("pending rank never blocks history, active game or loaded performance", async () => {
  let rankSignal: AbortSignal | undefined;
  globalThis.fetch = async (input, init) => {
    const path = String(input);
    if (path.includes("/ranked")) {
      rankSignal = init!.signal!;
      return new Promise<Response>((_resolve, reject) => {
        init!.signal!.addEventListener(
          "abort",
          () => reject(new DOMException("Cancelled", "AbortError")),
          { once: true },
        );
      });
    }
    if (path.includes("/active-game")) return Response.json(null);
    if (path.includes("/analysis?")) return Response.json(result(["LA1_0"]));
    throw new Error("Unexpected fixture route");
  };
  const view = render(
    <PlayerDashboard gameName="Ana" tagLine="LAN" platformRegion="la1" />,
  );
  await screen.findByText("Consultando rango…");
  assert.ok(screen.getByRole("region", { name: "Rendimiento competitivo" }));
  assert.equal(document.querySelectorAll(".match-card").length, 1);
  await screen.findByText("No se detectó una partida en curso");
  view.unmount();
  assert.ok(rankSignal!.aborted);
});
