// Isolated visual test. All API and image responses are synthetic.
import { createRequire } from "node:module";
import { mkdir, writeFile } from "node:fs/promises";
import assert from "node:assert/strict";

const { chromium } = createRequire(
  process.env.PLAYWRIGHT_PACKAGE || "/tmp/echo-t0464-visual/package.json",
)("playwright");
const base = process.env.VISUAL_BASE_URL || "http://127.0.0.1:3217";
const output = process.env.CAPTURE_DIR || "docs/releases/t04-7-2-captures";
await mkdir(output, { recursive: true });
const browser = await chromium.launch({
  executablePath: process.env.CHROMIUM_PATH,
  headless: true,
});
const report = {
  synthetic: true,
  revision: process.env.VISUAL_REVISION || "uncommitted",
  matrix: [],
  scenarios: [],
  errors: [],
  unexpectedRequests: [],
};
const words = {
  es: {
    home: "Cada partida tiene una historia.",
    performance: "Rendimiento competitivo",
    teams: "Equipos y participantes",
    live: "Jugadores actuales",
    group: "Grupo A",
    language: "Idioma",
    retry: "Reintentar",
  },
  en: {
    home: "Every match has a story.",
    performance: "Competitive performance",
    teams: "Teams and participants",
    live: "Current players",
    group: "Group A",
    language: "Language",
    retry: "Retry",
  },
  "pt-BR": {
    home: "Toda partida tem uma história.",
    performance: "Desempenho competitivo",
    teams: "Equipes e participantes",
    live: "Jogadores atuais",
    group: "Grupo A",
    language: "Idioma",
    retry: "Tentar novamente",
  },
};
const players = [
  "Ana",
  "Beto",
  "Carla",
  "Diego",
  "Eva",
  "Fabio",
  "Gina",
  "Hugo",
  "Iris",
  "Juan",
].map((name, i) => ({
  puuid: `synthetic-${i}`,
  gameName: name,
  tagLine: "DEMO",
  championId: i + 10,
  championName: ["Ahri", "Ashe", "Lux", "Vi", "Jinx"][i % 5],
  kills: i + 1,
  deaths: i % 4,
  assists: i + 3,
  win: i < 5,
  teamId: i < 5 ? 100 : 200,
}));
const coverage = {
  status: "complete",
  participants: 10,
  matchIdsObtained: 32,
  uniqueMatchIds: 22,
  matchesRecovered: 22,
  players: players.map((p, i) => ({
    puuid: p.puuid,
    requested: i ? 3 : 5,
    matchIds: i ? 3 : 5,
    eligibleMatches: i ? 3 : 5,
    limited: false,
  })),
  failures: [],
};
const pair = (first, second, total, ally, enemy) => ({
  firstPuuid: `synthetic-${first}`,
  secondPuuid: `synthetic-${second}`,
  matchesTogether: total,
  allyMatches: ally,
  enemyMatches: enemy,
  score: 2,
  confidence: "HIGH",
});
const group = (teamId, ids, number) => ({
  groupNumber: number,
  teamId,
  classification: "PossiblePremade",
  label: "possible premade",
  members: ids.map((i) => ({
    puuid: players[i].puuid,
    gameName: players[i].gameName,
    tagLine: players[i].tagLine,
  })),
});
const player = {
  puuid: "synthetic-0",
  gameName: "Ana",
  tagLine: "DEMO",
  platformRegion: "la1",
  profileIconId: null,
};
const summary = {
  puuid: player.puuid,
  gameName: player.gameName,
  tagLine: player.tagLine,
  matchesAnalyzed: 5,
  wins: 3,
  losses: 2,
  winRate: 60,
  uniquePlayersEncountered: 9,
  repeatedPlayers: 2,
  dataUpdatedAt: "2026-10-27T18:00:00Z",
};
const item = (i) => ({
  riotMatchId: `LA1_FIXTURE${i}`,
  queueId: 420,
  gameStartTimestamp: "2026-10-27T18:00:00Z",
  gameDurationSeconds: 1800,
  championId: 103,
  championName: "Ahri",
  kills: 4,
  deaths: 2,
  assists: 7,
  win: i % 2 === 0,
  evidence: {
    ownerPuuid: player.puuid,
    platformRegion: "la1",
    status: "complete",
    participants: players.map((p) => ({
      puuid: p.puuid,
      gameName: p.gameName,
      tagLine: p.tagLine,
      teamId: p.teamId,
    })),
  },
});
const dashboard = {
  player,
  summary,
  matches: {
    page: 1,
    pageSize: 5,
    totalCount: 5,
    items: Array.from({ length: 5 }, (_, i) => item(i)),
  },
  encounters: [],
  coverage,
};
const analysis = {
  owner: player,
  summary,
  encounters: [],
  relationships: [
    pair(0, 1, 3, 2, 1),
    pair(1, 2, 2, 2, 0),
    pair(0, 5, 2, 1, 1),
    pair(5, 6, 2, 2, 0),
  ],
  coverage,
  match: {
    riotMatchId: "LA1_FIXTURE0",
    queueId: 420,
    gameStartTimestamp: "2026-10-27T18:00:00Z",
    gameDurationSeconds: 1800,
    teams: [
      { teamId: 100, participants: players.slice(0, 5) },
      { teamId: 200, participants: players.slice(5) },
    ],
    premadeGroups: [group(100, [0, 1], 1), group(200, [5, 6], 2)],
    familiarity: {
      knownPlayers: 2,
      unknownPlayers: 7,
      evaluablePlayers: 9,
      familiarityPercentage: 22.2,
      status: "Available",
      knownPlayerDetails: [],
    },
  },
};
const artwork =
  '<svg xmlns="http://www.w3.org/2000/svg" width="96" height="96"><rect width="96" height="96" fill="#122237"/><path d="M22 68L48 15 74 68 48 55Z" fill="#d8bd78"/><text x="48" y="88" text-anchor="middle" fill="white" font-size="12">DEMO</text></svg>';

async function contextFor(locale, width, scenario = "complete") {
  const context = await browser.newContext({
    viewport: { width, height: 1000 },
    reducedMotion: "reduce",
    locale,
  });
  await context.addCookies([{ name: "echo-locale", value: locale, url: base }]);
  const page = await context.newPage();
  page.on("pageerror", (error) => report.errors.push(error.message));
  page.on("console", (msg) => {
    if (
      msg.type() === "error" &&
      /hydrat|missing.message|translation/i.test(msg.text())
    )
      report.errors.push(msg.text());
  });
  let attempts = 0;
  await context.route("**/*", async (route) => {
    const url = new URL(route.request().url());
    if (url.origin !== base) {
      report.unexpectedRequests.push(url.origin);
      return route.abort();
    }
    if (
      url.pathname.startsWith("/api/assets/") ||
      url.pathname.startsWith("/_next/image")
    )
      return route.fulfill({
        status: 200,
        contentType: "image/svg+xml",
        body: artwork,
      });
    if (!url.pathname.startsWith("/api/")) return route.continue();
    if (url.pathname.endsWith("/analyze")) {
      attempts++;
      if (scenario === "error" && attempts === 1)
        return route.fulfill({
          status: 503,
          contentType: "application/json",
          body: "{}",
        });
      const response = structuredClone(analysis);
      if (scenario === "partial") {
        response.coverage.status = "partial";
        response.coverage.failures = [
          { stage: "match_detail", code: "fixture" },
        ];
        response.match.teams[1].participants =
          response.match.teams[1].participants.slice(0, 3);
      }
      if (scenario === "empty") {
        response.relationships = [];
        response.match.premadeGroups = [];
        response.coverage.status = "insufficient";
      }
      if (scenario === "overlap")
        response.match.premadeGroups.push(group(100, [1, 2], 3));
      if (scenario === "long")
        for (const team of response.match.teams)
          for (const p of team.participants)
            p.gameName += " synthetic long player name for layout";
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(response),
      });
    }
    if (url.pathname.endsWith("/analysis"))
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(dashboard),
      });
    if (url.pathname.endsWith("/ranked"))
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          entries: [],
          observedAt: "2026-10-29T10:00:00Z",
        }),
      });
    if (url.pathname.includes("active-game"))
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          gameId: 7,
          queueId: 420,
          gameStartTime: "2026-10-28T08:00:00Z",
          gameLengthSeconds: 900,
          participants: players,
        }),
      });
    report.unexpectedRequests.push(url.pathname);
    return route.abort();
  });
  return { context, page };
}
async function noOverflow(page, label) {
  const size = await page.evaluate(() => ({
    viewport: innerWidth,
    scroll: document.documentElement.scrollWidth,
  }));
  assert.ok(
    size.scroll <= size.viewport,
    `${label}: overflow ${size.scroll}/${size.viewport}`,
  );
  return size;
}
async function capture(page, name, locator) {
  await (locator || page).screenshot({
    path: `${output}/${name}.png`,
    ...(!locator ? { fullPage: true } : {}),
  });
}
const matchPath =
  "/match/LA1_FIXTURE0?ownerPuuid=synthetic-0&platformRegion=la1&returnGameName=Ana&returnTagLine=DEMO";
try {
  for (const locale of ["es", "en", "pt-BR"])
    for (const width of [390, 768, 1440]) {
      const { context, page } = await contextFor(locale, width);
      const w = words[locale];
      await page.goto(base + "/");
      await page.getByRole("heading", { name: w.home }).waitFor();
      assert.equal(await page.locator("html").getAttribute("lang"), locale);
      await noOverflow(page, `${locale}/${width}/home`);
      await capture(page, `${locale}-${width}-home`);
      await page.goto(base + "/player/Ana/DEMO?platformRegion=la1");
      await page.getByRole("heading", { name: w.performance }).waitFor();
      await noOverflow(page, `${locale}/${width}/dashboard`);
      await capture(page, `${locale}-${width}-dashboard`);
      await page.goto(base + matchPath);
      await page.getByRole("heading", { name: w.teams }).waitFor();
      assert.ok((await page.getByText(new RegExp(w.group)).count()) >= 2);
      assert.ok(
        (await page
          .getByText(new RegExp(locale === "en" ? "Group B" : "Grupo B"))
          .count()) >= 2,
      );
      assert.ok(
        !(await page
          .locator("body")
          .innerText()
          .then((text) => text.includes("synthetic-"))),
      );
      await noOverflow(page, `${locale}/${width}/match`);
      await capture(page, `${locale}-${width}-match`);
      if (width === 1440)
        await capture(
          page,
          `${locale}-teams`,
          page.locator('[aria-labelledby="match-teams-title"]'),
        );
      if (locale === "es" && width === 1440)
        await capture(
          page,
          "es-historial-contigo",
          page.locator(".analysis-team--blue"),
        );
      await page.goto(
        base + "/match/live?ownerPuuid=synthetic-0&platformRegion=la1",
      );
      await page.getByRole("heading", { name: w.live }).waitFor();
      await noOverflow(page, `${locale}/${width}/live`);
      await capture(page, `${locale}-${width}-live`);
      report.matrix.push({
        locale,
        width,
        screens: ["home", "dashboard", "match", "live"],
      });
      await context.close();
    }
  for (const width of [320, 1024]) {
    const { context, page } = await contextFor("es", width);
    await page.goto(base + matchPath);
    await page.getByRole("heading", { name: words.es.teams }).waitFor();
    await noOverflow(page, `es/${width}/match`);
    report.matrix.push({ locale: "es", width, screens: ["match"] });
    await context.close();
  }
  const scenarioRuns = [
    ["es", "partial"],
    ["en", "partial"],
    ["pt-BR", "partial"],
    ["es", "empty"],
    ["es", "long"],
    ["es", "error"],
    ["es", "overlap"],
  ];
  for (const [locale, scenario] of scenarioRuns) {
    const { context, page } = await contextFor(locale, 390, scenario);
    await page.goto(base + matchPath);
    if (scenario === "error")
      await page.getByRole("button", { name: words[locale].retry }).click();
    await page.getByRole("heading", { name: words[locale].teams }).waitFor();
    if (scenario === "overlap")
      assert.equal(
        await page
          .locator(".analysis-team--blue li")
          .nth(1)
          .locator(".analysis-group-badge")
          .count(),
        2,
      );
    await noOverflow(page, `${locale}/${scenario}`);
    await capture(page, `${locale}-${scenario}`);
    report.scenarios.push(`${locale}-${scenario}`);
    await context.close();
  }
  // Real selector changes the cookie and reloads; retain the selected locale through navigation and refresh.
  const { context, page } = await contextFor("es", 390);
  await page.goto(base + "/");
  for (const locale of ["en", "pt-BR", "es"]) {
    await page.locator(".locale-control select").selectOption(locale);
    await page.getByRole("heading", { name: words[locale].home }).waitFor();
    assert.equal(await page.locator("html").getAttribute("lang"), locale);
    assert.equal(
      (await context.cookies()).find((c) => c.name === "echo-locale")?.value,
      locale,
    );
  }
  await page.locator("#game-name").fill("Ana");
  await page.locator("#tag-line").fill("DEMO");
  await page.locator("#player-search-form button[type=submit]").click();
  await page.getByRole("heading", { name: words.es.performance }).waitFor();
  await page.locator(".competitive-match a").first().click();
  await page.getByRole("heading", { name: words.es.teams }).waitFor();
  assert.ok(
    page.url().includes("LA1_FIXTURE0") &&
      page.url().includes("ownerPuuid=synthetic-0"),
  );
  const selectedMatchUrl = page.url();
  await page.locator(".locale-control select").selectOption("en");
  await page.getByRole("heading", { name: words.en.teams }).waitFor();
  assert.equal(page.url(), selectedMatchUrl);
  await page.reload();
  await page.getByRole("heading", { name: words.en.teams }).waitFor();
  assert.equal(
    (await context.cookies()).find((c) => c.name === "echo-locale")?.value,
    "en",
  );
  await page.locator(".locale-control select").selectOption("pt-BR");
  await page.getByRole("heading", { name: words["pt-BR"].teams }).waitFor();
  await page.locator(".back-link").click();
  await page
    .getByRole("heading", { name: words["pt-BR"].performance })
    .waitFor();
  assert.equal(await page.locator("html").getAttribute("lang"), "pt-BR");
  await page.locator(".locale-control select").focus();
  const focused = await page.evaluate(() => {
    const e = document.activeElement,
      s = getComputedStyle(e);
    return {
      tag: e.tagName,
      outline: s.outlineStyle,
      width: s.outlineWidth,
      shadow: s.boxShadow,
    };
  });
  assert.equal(focused.tag, "SELECT");
  assert.ok(
    (focused.outline !== "none" && focused.width !== "0px") ||
      focused.shadow !== "none",
  );
  report.scenarios.push("locale-switch-cookie-navigation-reload-focus");
  await context.close();
  const fallback = await browser.newContext({
    extraHTTPHeaders: { "Accept-Language": "fr-FR,fr;q=0.9" },
  });
  await fallback.route("**/*", (route) =>
    new URL(route.request().url()).origin === base
      ? route.continue()
      : route.abort(),
  );
  const fallbackPage = await fallback.newPage();
  await fallbackPage.goto(base + "/");
  assert.equal(await fallbackPage.locator("html").getAttribute("lang"), "es");
  await fallback.close();
  report.scenarios.push("unsupported-browser-fallback");
  assert.deepEqual(report.unexpectedRequests, []);
  assert.deepEqual(report.errors, []);
} finally {
  await writeFile(`${output}/results.json`, JSON.stringify(report, null, 2));
  await browser.close();
}
console.log(JSON.stringify(report));
