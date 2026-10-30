import "./dom-environment.ts";
import assert from "node:assert/strict";
import { afterEach, test } from "node:test";
import {
  cleanup,
  render,
  screen,
  fireEvent,
  waitFor,
  act,
} from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import {
  PlayerRanked,
  RankedQueues,
  RankEmblem,
  validRankedResult,
  rankedPath,
} from "../src/player-ranked.tsx";
const originalFetch = globalThis.fetch;
afterEach(() => {
  cleanup();
  globalThis.fetch = originalFetch;
});
const solo = {
  queueType: "RANKED_SOLO_5x5" as const,
  tier: "GOLD",
  division: "II",
  leaguePoints: 54,
};
const flex = {
  queueType: "RANKED_FLEX_SR" as const,
  tier: "SILVER",
  division: "I",
  leaguePoints: 20,
};
const observedAt = "2026-10-29T10:00:00Z";
const player = {
  puuid: "owner",
  gameName: "Ana",
  tagLine: "LAN",
  platformRegion: "la1",
};
test("ranked contract rejects malformed data rather than claiming unranked", () => {
  assert.ok(validRankedResult({ entries: [], observedAt }));
  for (const value of [
    null,
    {},
    { entries: [], observedAt: "bad" },
    { entries: [solo, solo], observedAt },
    { entries: [{ ...solo, tier: "UNKNOWN" }], observedAt },
  ])
    assert.equal(validRankedResult(value), false);
  assert.equal(
    rankedPath("a/b", "la1"),
    "/api/v1/players/a%2Fb/ranked?platformRegion=la1",
  );
});
test("Solo and Flex switch by keyboard without another request", async () => {
  const user = userEvent.setup();
  render(<RankedQueues result={{ entries: [flex, solo], observedAt }} />);
  assert.ok(screen.getByText("Oro II"));
  const select = screen.getByRole("combobox", { name: "Cola clasificatoria" });
  select.focus();
  assert.equal(document.activeElement, select);
  await user.selectOptions(select, "RANKED_FLEX_SR");
  assert.ok(screen.getByText("Plata I"));
  assert.ok(screen.getByText("20 LP"));
});
test("Flex alone is selected initially and missing Solo is explicitly unranked", async () => {
  render(<RankedQueues result={{ entries: [flex], observedAt }} />);
  assert.ok(screen.getByText("Plata I"));
  await userEvent
    .setup()
    .selectOptions(screen.getByRole("combobox"), "RANKED_SOLO_5x5");
  assert.ok(screen.getByText("Sin clasificación"));
});
test("successful empty result means unranked; apex omits meaningless division", () => {
  const view = render(<RankedQueues result={{ entries: [], observedAt }} />);
  assert.ok(screen.getByText("Sin clasificación"));
  view.unmount();
  render(
    <RankedQueues
      result={{ entries: [{ ...solo, tier: "MASTER" }], observedAt }}
    />,
  );
  assert.ok(screen.getByText("Maestro"));
  assert.equal(screen.queryByText("Maestro II"), null);
});
test("official emblem has actual tier, safe path and neutral failure fallback", () => {
  const view = render(<RankEmblem tier="GOLD" />);
  const image = screen.getByRole("img", { name: "Emblema de Oro" });
  assert.equal(image.getAttribute("src"), "/assets/ranks/gold.png");
  fireEvent.error(image);
  assert.ok(
    screen.getByRole("img", { name: "Emblema de rango no disponible" }),
  );
  view.unmount();
  render(<RankEmblem tier="../../invalid" />);
  assert.equal(document.querySelector("img"), null);
});
test("rank loading is independent, one request per identity, error retry does not claim unranked", async () => {
  let calls = 0;
  globalThis.fetch = async (input) => {
    calls++;
    assert.match(String(input), /owner\/ranked\?platformRegion=la1/);
    return calls === 1
      ? new Response("", { status: 429 })
      : Response.json({ entries: [solo], observedAt });
  };
  const view = render(<PlayerRanked player={player} />);
  assert.ok(screen.getByText("Consultando rango…"));
  await screen.findByText("Rango no disponible");
  assert.equal(screen.queryByText("Sin clasificación"), null);
  await userEvent
    .setup()
    .click(screen.getByRole("button", { name: "Reintentar rango" }));
  await screen.findByText("Oro II");
  view.rerender(<PlayerRanked player={player} />);
  assert.equal(calls, 2);
});
test("leaving cancels ranked fetch and older identities cannot overwrite new result", async () => {
  const signals: AbortSignal[] = [];
  let resolveOld: (r: Response) => void = () => {};
  globalThis.fetch = async (input, init) => {
    signals.push(init!.signal!);
    if (String(input).includes("/players/owner/ranked"))
      return new Promise<Response>((r) => {
        resolveOld = r;
      });
    return Response.json({ entries: [flex], observedAt });
  };
  const view = render(<PlayerRanked player={player} />);
  await waitFor(() => assert.equal(signals.length, 1));
  view.rerender(<PlayerRanked player={{ ...player, puuid: "new-owner" }} />);
  await screen.findByText("Plata I");
  assert.ok(signals[0].aborted);
  resolveOld(Response.json({ entries: [solo], observedAt }));
  await waitFor(() => assert.equal(screen.queryByText("Oro II"), null));
  view.unmount();
  assert.ok(signals[1].aborted);
});

test("rank timeout ends loading and cancels its own request", async (t) => {
  const originalTimeout = globalThis.setTimeout;
  let expire: (() => void) | undefined;
  t.mock.method(
    globalThis,
    "setTimeout",
    (
      callback: (...args: unknown[]) => void,
      milliseconds?: number,
      ...args: unknown[]
    ) => {
      if (milliseconds === 12_000) expire = () => callback(...args);
      return originalTimeout(callback, milliseconds, ...args);
    },
  );
  let signal: AbortSignal | undefined;
  globalThis.fetch = async (_input, init) => {
    signal = init!.signal!;
    return new Promise<Response>((_resolve, reject) => {
      init!.signal!.addEventListener(
        "abort",
        () => reject(new DOMException("Cancelled", "AbortError")),
        { once: true },
      );
    });
  };
  render(<PlayerRanked player={player} />);
  await waitFor(() => assert.ok(expire));
  act(() => expire!());
  assert.ok(screen.getByText("Rango no disponible"));
  assert.ok(signal!.aborted);
  assert.equal(screen.queryByText("Sin clasificación"), null);
});
