"use client";
/* eslint-disable @next/next/no-img-element -- Official static rank emblems, fixed size and same origin. */
import { useEffect, useId, useState } from "react";
import { fetchJson, type PlayerLookup } from "./api";
import { formatDashboardDate } from "./dashboard-presentation";
import { Button } from "./design-system";

export type RankedQueue = "RANKED_SOLO_5x5" | "RANKED_FLEX_SR";
export interface RankedEntry {
  queueType: RankedQueue;
  tier: string;
  division: string;
  leaguePoints: number;
}
export interface PlayerRankedResult {
  entries: RankedEntry[];
  observedAt: string;
}
export const rankNames: Record<string, string> = {
  IRON: "Hierro",
  BRONZE: "Bronce",
  SILVER: "Plata",
  GOLD: "Oro",
  PLATINUM: "Platino",
  EMERALD: "Esmeralda",
  DIAMOND: "Diamante",
  MASTER: "Maestro",
  GRANDMASTER: "Gran Maestro",
  CHALLENGER: "Retador",
};
export function rankedPath(puuid: string, region?: string): string {
  return `/api/v1/players/${encodeURIComponent(puuid)}/ranked${region ? `?platformRegion=${encodeURIComponent(region)}` : ""}`;
}
export function validRankedResult(value: unknown): value is PlayerRankedResult {
  if (!value || typeof value !== "object") return false;
  const v = value as PlayerRankedResult;
  return (
    Array.isArray(v.entries) &&
    typeof v.observedAt === "string" &&
    Number.isFinite(Date.parse(v.observedAt)) &&
    v.entries.every(
      (e) =>
        e &&
        ["RANKED_SOLO_5x5", "RANKED_FLEX_SR"].includes(e.queueType) &&
        Object.hasOwn(rankNames, e.tier) &&
        ["I", "II", "III", "IV"].includes(e.division) &&
        Number.isInteger(e.leaguePoints),
    ) &&
    new Set(v.entries.map((e) => e.queueType)).size === v.entries.length
  );
}
export function RankEmblem({ tier }: { tier: string }) {
  const [failed, setFailed] = useState(false);
  if (failed || !Object.hasOwn(rankNames, tier))
    return (
      <span
        className="rank-emblem-fallback"
        role="img"
        aria-label="Emblema de rango no disponible"
      >
        —
      </span>
    );
  return (
    <img
      src={`/assets/ranks/${tier.toLowerCase()}.png`}
      alt={`Emblema de ${rankNames[tier]}`}
      width={56}
      height={56}
      onError={() => setFailed(true)}
    />
  );
}
export function RankedQueues({ result }: { result: PlayerRankedResult }) {
  const [queue, setQueue] = useState<RankedQueue>(
    result.entries.some((e) => e.queueType === "RANKED_SOLO_5x5")
      ? "RANKED_SOLO_5x5"
      : result.entries.some((e) => e.queueType === "RANKED_FLEX_SR")
        ? "RANKED_FLEX_SR"
        : "RANKED_SOLO_5x5",
  );
  const description = useId();
  const entry = result.entries.find((e) => e.queueType === queue);
  return (
    <>
      <label className="sr-only" htmlFor={`${description}-queue`}>
        Cola clasificatoria
      </label>
      <select
        id={`${description}-queue`}
        aria-describedby={description}
        value={queue}
        onChange={(e) => setQueue(e.target.value as RankedQueue)}
      >
        <option value="RANKED_SOLO_5x5">Solo/Dúo</option>
        <option value="RANKED_FLEX_SR">Flexible</option>
      </select>
      <p id={description} className="sr-only">
        Clasificación recuperada: {formatDashboardDate(result.observedAt)}. La
        caché temporal puede conservarla hasta cinco minutos con la
        configuración predeterminada.
      </p>
      <div className="ranked-value" role="status">
        {entry ? (
          <>
            <RankEmblem key={entry.tier} tier={entry.tier} />
            <div>
              <strong>
                {rankNames[entry.tier]}
                {["MASTER", "GRANDMASTER", "CHALLENGER"].includes(entry.tier)
                  ? ""
                  : ` ${entry.division}`}
              </strong>
              <span>{entry.leaguePoints} LP</span>
            </div>
          </>
        ) : (
          <strong>Sin clasificación</strong>
        )}
      </div>
    </>
  );
}
export function PlayerRanked({ player }: { player: PlayerLookup }) {
  const [attempt, setAttempt] = useState(0);
  const [state, setState] = useState<{
    status: "loading" | "error" | "available";
    result?: PlayerRankedResult;
  }>({ status: "loading" });
  useEffect(() => {
    let alive = true;
    const controller = new AbortController();
    let timeout: ReturnType<typeof setTimeout> | undefined;
    queueMicrotask(() => {
      if (!alive) return;
      setState({ status: "loading" });
      timeout = setTimeout(() => {
        if (alive) setState({ status: "error" });
        controller.abort();
      }, 12_000);
      void fetchJson<unknown>(rankedPath(player.puuid, player.platformRegion), {
        signal: controller.signal,
      })
        .then((result) => {
          if (alive && !controller.signal.aborted) {
            if (!validRankedResult(result))
              throw new Error("Invalid ranked result");
            setState({ status: "available", result });
          }
        })
        .catch(() => {
          if (alive && !controller.signal.aborted)
            setState({ status: "error" });
        })
        .finally(() => clearTimeout(timeout));
    });
    return () => {
      alive = false;
      controller.abort();
      clearTimeout(timeout);
    };
  }, [player, attempt]);
  return (
    <section
      className="player-ranked"
      aria-label="Rango competitivo"
      aria-busy={state.status === "loading"}
    >
      {state.status === "loading" ? (
        <p role="status">Consultando rango…</p>
      ) : state.status === "error" ? (
        <>
          <p role="status">Rango no disponible</p>
          <Button variant="ghost" onClick={() => setAttempt((a) => a + 1)}>
            Reintentar rango
          </Button>
        </>
      ) : (
        <RankedQueues key={state.result!.observedAt} result={state.result!} />
      )}
    </section>
  );
}
