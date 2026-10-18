"use client";

import Link from "next/link";
import { useCallback, useEffect, useState } from "react";
import {
  ApiError,
  apiPath,
  fetchJson,
  playerActiveGamePath,
  type ActiveGame,
  type MatchDetail,
  type MatchesResponse,
  type PlayerLookup,
  type PlayerSummary,
  type MatchSyncResult,
  playerLookupPath,
  matchDetailPath,
  playerSyncPath,
} from "@/src/api";
import { formatDate, formatDuration, queueLabel } from "@/src/format";
import {
  familiarityMessage,
  historyAnalysisStatus,
  type HistoryAnalysisStatus,
  latestMatchSyncPlan,
  latestMatchSyncConfig,
} from "@/src/echo";
import { ChampionIcon } from "@/src/champion-icon";

interface PlayerDashboardProps {
  gameName: string;
  tagLine: string;
}

interface DashboardData {
  player: PlayerLookup;
  summary: PlayerSummary;
  matches: MatchesResponse;
  latestMatch: MatchDetail | null;
  activeGame: ActiveGame | null;
  historyStatus: HistoryAnalysisStatus;
}

type LoadingStep = "lookup" | "sync" | "results";

const loadingCopy: Record<LoadingStep, string> = {
  lookup: "Resolviendo el Riot ID…",
  sync: "Sincronizando el historial solicitado…",
  results: "Construyendo el resumen histórico…",
};

export function PlayerDashboard({ gameName, tagLine }: PlayerDashboardProps) {
  const [data, setData] = useState<DashboardData | null>(null);
  const [step, setStep] = useState<LoadingStep>("lookup");
  const [error, setError] = useState<string | null>(null);
  const [warning, setWarning] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isLoadingMoreMatches, setIsLoadingMoreMatches] = useState(false);

  const load = useCallback(
    async (synchronize: boolean, signal?: AbortSignal) => {
      setIsLoading(true);
      setError(null);
      setWarning(null);
      setStep("lookup");

      try {
        const player = await fetchJson<PlayerLookup>(
          playerLookupPath(gameName, tagLine),
          { signal },
        );

        let syncWarning: string | null = null;
        let initialSyncIncomplete = false;
        if (synchronize) {
          setStep("sync");
          try {
            const syncResult = await fetchJson<MatchSyncResult>(
              playerSyncPath(
                player.puuid,
                latestMatchSyncConfig.ownerMatchCount,
              ),
              {
                method: "POST",
                signal,
              },
            );
            initialSyncIncomplete =
              historyAnalysisStatus([
                { status: "fulfilled", value: syncResult },
              ]) !== "complete";
            if (initialSyncIncomplete)
              syncWarning = "El historial del jugador está incompleto.";
          } catch (syncError) {
            if (signal?.aborted) return;
            syncWarning = messageFor(
              syncError,
              "No se pudo actualizar el historial.",
            );
            initialSyncIncomplete = true;
          }
        }

        setStep("results");
        const readResults = async () => {
          const [summary, matches] = await Promise.all([
            fetchJson<PlayerSummary>(
              apiPath("players", player.puuid, "summary"),
              {
                signal,
              },
            ),
            fetchJson<MatchesResponse>(
              `${apiPath("players", player.puuid, "matches")}?page=1&pageSize=10`,
              { signal },
            ),
          ]);

          let latestMatch: MatchDetail | null = null;
          if (matches.items[0]) {
            try {
              latestMatch = await fetchJson<MatchDetail>(
                `${apiPath("matches", matches.items[0].riotMatchId)}?ownerPuuid=${encodeURIComponent(player.puuid)}`,
                { signal },
              );
            } catch (latestMatchError) {
              if (signal?.aborted) throw latestMatchError;
              syncWarning ??= messageFor(
                latestMatchError,
                "No se pudo cargar la familiaridad de la última partida.",
              );
            }
          }

          let activeGame: ActiveGame | null = null;
          try {
            activeGame = await fetchJson<ActiveGame | null>(
              playerActiveGamePath(player.puuid),
              { signal },
            );
          } catch (activeGameError) {
            if (signal?.aborted) throw activeGameError;
            syncWarning ??= messageFor(
              activeGameError,
              "No se pudo comprobar si hay una partida en curso.",
            );
          }

          return {
            summary,
            matches,
            latestMatch,
            activeGame,
          };
        };

        let results = await readResults();
        let historyStatus: HistoryAnalysisStatus = "insufficient";
        if (results.latestMatch) {
          const syncPlan = latestMatchSyncPlan(
            results.latestMatch,
            player.puuid,
          );
          const syncResults = await Promise.allSettled(
            syncPlan.map((target) =>
              fetchJson<MatchSyncResult>(
                playerSyncPath(
                  target.puuid,
                  target.count,
                  results.latestMatch?.riotMatchId,
                ),
                {
                  method: "POST",
                  signal,
                },
              ),
            ),
          );
          if (signal?.aborted) return;
          historyStatus = historyAnalysisStatus(syncResults);
          if (initialSyncIncomplete && historyStatus === "complete")
            historyStatus = "partial";
          const failedTargets = syncResults.filter(
            (result) => result.status === "rejected",
          );
          if (failedTargets.length > 0) {
            syncWarning ??= messageFor(
              failedTargets[0].reason,
              "No se pudo actualizar automáticamente el contexto de la última partida.",
            );
          }
          if (historyStatus === "partial")
            syncWarning ??=
              "Análisis parcial: algunas partidas no se pudieron recuperar.";
          if (failedTargets.length < syncPlan.length) {
            try {
              results = await readResults();
            } catch (refreshError) {
              if (signal?.aborted) return;
              historyStatus = "partial";
              syncWarning ??= messageFor(
                refreshError,
                "No se pudo actualizar el resultado del análisis.",
              );
            }
          }
        }

        setData({
          player,
          ...results,
          historyStatus,
        });
        setWarning(syncWarning);
      } catch (requestError) {
        if (signal?.aborted) return;
        setError(messageFor(requestError, "No pudimos cargar este jugador."));
      } finally {
        if (!signal?.aborted) setIsLoading(false);
      }
    },
    [gameName, tagLine],
  );

  useEffect(() => {
    const controller = new AbortController();
    const request = window.setTimeout(
      () => void load(true, controller.signal),
      0,
    );
    return () => {
      window.clearTimeout(request);
      controller.abort();
    };
  }, [load]);

  const loadMoreMatches = useCallback(async () => {
    if (
      !data ||
      isLoadingMoreMatches ||
      data.matches.items.length >= data.matches.totalCount
    ) {
      return;
    }

    setIsLoadingMoreMatches(true);
    try {
      const nextPage = data.matches.page + 1;
      const nextMatches = await fetchJson<MatchesResponse>(
        `${apiPath("players", data.player.puuid, "matches")}?page=${nextPage}&pageSize=10`,
      );
      setData((current) =>
        current
          ? {
              ...current,
              matches: {
                ...nextMatches,
                items: [...current.matches.items, ...nextMatches.items],
              },
            }
          : current,
      );
    } catch (requestError) {
      setWarning(
        messageFor(requestError, "No se pudieron cargar más partidas."),
      );
    } finally {
      setIsLoadingMoreMatches(false);
    }
  }, [data, isLoadingMoreMatches]);

  return (
    <main className="dashboard-shell">
      <header className="topbar">
        <Link className="wordmark" href="/">
          <span className="wordmark-icon">E</span>
          <span>PROJECT ECHO</span>
        </Link>
        <Link className="text-link" href="/">
          Nueva búsqueda
        </Link>
      </header>

      <div className="dashboard-content">
        <section className="player-heading">
          <div>
            <p className="eyebrow">Who have you played with before?</p>
            <h1>
              {data?.summary.gameName || gameName}
              <span>#{data?.summary.tagLine || tagLine}</span>
            </h1>
            <p className="muted-copy">
              Encuentros observados únicamente en partidas terminadas y
              almacenadas por el analizador.
            </p>
          </div>
          <div className="profile-actions">
            <button
              className="secondary-button"
              disabled={isLoading}
              onClick={() => void load(true)}
              type="button"
            >
              {isLoading
                ? "Actualizando…"
                : `Actualizar ${latestMatchSyncConfig.ownerMatchCount} partidas`}
            </button>
            {data ? (
              <p className="profile-freshness">
                Datos locales actualizados:{" "}
                {formatDate(data.summary.dataUpdatedAt)}
              </p>
            ) : null}
          </div>
        </section>

        {isLoading && !data ? <LoadingPanel step={step} /> : null}
        {error ? (
          <section className="message-panel error-panel" role="alert">
            <div>
              <p className="eyebrow">No se pudo completar el análisis</p>
              <h2>{error}</h2>
              <p>
                Verifica el Riot ID o intenta de nuevo. No se muestran trazas
                internas ni credenciales.
              </p>
            </div>
            <button
              className="secondary-button"
              onClick={() => void load(false)}
            >
              Reintentar
            </button>
          </section>
        ) : null}

        {data ? (
          <>
            {warning ? (
              <p className="warning-banner" role="status">
                {warning} Mostramos los datos locales disponibles.
              </p>
            ) : null}
            <SummaryCards summary={data.summary} />
            {data.activeGame ? (
              <ActiveGameBanner
                game={data.activeGame}
                ownerPuuid={data.player.puuid}
              />
            ) : null}
            <LatestMatch
              match={data.matches.items[0] ?? null}
              matchDetail={data.latestMatch}
              ownerPuuid={data.player.puuid}
              historyStatus={data.historyStatus}
            />
            <MatchHistory
              matches={data.matches}
              ownerPuuid={data.player.puuid}
              isLoadingMore={isLoadingMoreMatches}
              onLoadMore={loadMoreMatches}
            />
          </>
        ) : null}
      </div>
    </main>
  );
}

function ActiveGameBanner({
  game,
  ownerPuuid,
}: {
  game: ActiveGame;
  ownerPuuid: string;
}) {
  return (
    <section className="active-game-banner" aria-live="polite">
      <div>
        <p className="eyebrow">Partida en curso</p>
        <p className="muted-copy">
          {queueLabel(game.queueId)} · iniciada {formatDate(game.gameStartTime)}
        </p>
      </div>
      <div className="active-game-actions">
        <strong>{formatDuration(game.gameLengthSeconds)} transcurridos</strong>
        <Link
          className="primary-button"
          href={`/match/live?ownerPuuid=${encodeURIComponent(ownerPuuid)}`}
        >
          Ver partida en curso →
        </Link>
      </div>
    </section>
  );
}

function LoadingPanel({ step }: { step: LoadingStep }) {
  return (
    <section className="loading-panel" role="status" aria-live="polite">
      <span className="spinner" aria-hidden="true" />
      <div>
        <p className="eyebrow">Preparando el análisis</p>
        <h2>{loadingCopy[step]}</h2>
        <p>
          La actualización del jugador buscado revisa{" "}
          {latestMatchSyncConfig.ownerMatchCount} partidas.
        </p>
        <p>
          Última partida: {latestMatchSyncConfig.ownerMatchCount} del jugador y{" "}
          {latestMatchSyncConfig.otherMatchCount} de hasta{" "}
          {latestMatchSyncConfig.otherPlayerCount} participantes.
        </p>
      </div>
    </section>
  );
}

function SummaryCards({ summary }: { summary: PlayerSummary }) {
  const cards = [
    ["Partidas analizadas", summary.matchesAnalyzed.toLocaleString("es-PA")],
    [
      "Jugadores únicos encontrados",
      summary.uniquePlayersEncountered.toLocaleString("es-PA"),
    ],
    ["Jugadores recurrentes", summary.repeatedPlayers.toLocaleString("es-PA")],
    ["Victorias", summary.wins.toLocaleString("es-PA")],
    ["Derrotas", summary.losses.toLocaleString("es-PA")],
  ];

  return (
    <section aria-labelledby="summary-title">
      <div className="section-title-row">
        <div>
          <p className="eyebrow">ECHO overview</p>
          <h2 id="summary-title">Tu historial de encuentros</h2>
        </div>
      </div>
      <p className="section-note">
        Todas tus partidas analizadas disponibles. Cada jugador se cuenta una
        sola vez, sin incluirte; recurrentes: aparecen en al menos dos partidas.
        {summary.otherPlayerParticipations !== undefined
          ? ` ${summary.otherPlayerParticipations} participaciones de otros jugadores (incluye repeticiones).`
          : ""}
      </p>
      <dl className="stat-grid">
        {cards.map(([label, value]) => (
          <div className="stat-card" key={label}>
            <dt>{label}</dt>
            <dd>{value}</dd>
          </div>
        ))}
      </dl>
    </section>
  );
}

function LatestMatch({
  match,
  matchDetail,
  ownerPuuid,
  historyStatus,
}: {
  match: MatchesResponse["items"][number] | null;
  matchDetail: MatchDetail | null;
  ownerPuuid: string;
  historyStatus: HistoryAnalysisStatus;
}) {
  if (!match) {
    return (
      <section aria-labelledby="last-match-title">
        <div className="section-title-row">
          <div>
            <p className="eyebrow">Última partida</p>
            <h2 id="last-match-title">Todavía no hay partidas analizadas</h2>
          </div>
        </div>
      </section>
    );
  }

  return (
    <section aria-labelledby="last-match-title">
      <div className="section-title-row">
        <div>
          <p className="eyebrow">Última partida</p>
          <h2 id="last-match-title">Familiaridad del lobby</h2>
        </div>
        <p className="section-note">Solo evidencia anterior a esta partida.</p>
      </div>
      <Link
        className="latest-match-card"
        href={matchDetailPath(match.riotMatchId, ownerPuuid)}
      >
        <div>
          <span className="latest-match-id">{match.riotMatchId}</span>
          <strong>
            {matchDetail?.familiarity
              ? familiarityMessage(matchDetail.familiarity, historyStatus)
              : "Familiaridad no disponible."}
          </strong>
          {matchDetail?.familiarity?.knownPlayerDetails.some(
            (player) => player.enemyMatches > 0,
          ) ? (
            <span>
              Contra conocidos:{" "}
              {matchDetail.familiarity.knownPlayerDetails
                .filter((player) => player.enemyMatches > 0)
                .slice(0, 3)
                .map(
                  (player) =>
                    `${player.gameName} (${player.winsAgainst}V/${player.lossesAgainst}D)`,
                )
                .join(" · ")}
            </span>
          ) : null}
          <span>{formatDate(match.gameStartTimestamp)}</span>
        </div>
        <span className="latest-match-action">Ver partida →</span>
      </Link>
    </section>
  );
}

function MatchHistory({
  matches,
  ownerPuuid,
  isLoadingMore,
  onLoadMore,
}: {
  matches: MatchesResponse;
  ownerPuuid: string;
  isLoadingMore: boolean;
  onLoadMore: () => void;
}) {
  return (
    <section aria-labelledby="matches-title">
      <div className="section-title-row">
        <div>
          <p className="eyebrow">Historial</p>
          <h2 id="matches-title">Partidas recientes</h2>
        </div>
        <p className="section-note">
          {matches.totalCount.toLocaleString("es-PA")} partidas disponibles
        </p>
      </div>

      {matches.items.length === 0 ? (
        <EmptyState
          title="No hay partidas guardadas"
          copy="Actualiza el historial cuando la API de Riot esté disponible."
        />
      ) : (
        <div className="match-list">
          {matches.items.map((match) => (
            <Link
              className={`match-card ${match.win ? "win" : "loss"}`}
              href={matchDetailPath(match.riotMatchId, ownerPuuid)}
              key={match.riotMatchId}
            >
              <span className="result-badge">
                {match.win ? "Victoria" : "Derrota"}
              </span>
              <div className="match-champion">
                <ChampionIcon
                  championId={match.championId}
                  championName={match.championName}
                />
                <span className="match-champion-copy">
                  <strong>{match.championName}</strong>
                  <span>{queueLabel(match.queueId)}</span>
                </span>
              </div>
              <div className="kda">
                <strong>
                  {match.kills} / {match.deaths} / {match.assists}
                </strong>
                <span>K / D / A</span>
              </div>
              <div className="match-meta">
                <span>{formatDuration(match.gameDurationSeconds)}</span>
                <span>{formatDate(match.gameStartTimestamp)}</span>
              </div>
              <span className="card-arrow" aria-hidden="true">
                →
              </span>
            </Link>
          ))}
        </div>
      )}
      {matches.items.length > 0 && matches.items.length < matches.totalCount ? (
        <div className="section-actions">
          <button
            className="secondary-button"
            disabled={isLoadingMore}
            onClick={onLoadMore}
            type="button"
          >
            {isLoadingMore ? "Cargando partidas…" : "Cargar 10 partidas más"}
          </button>
        </div>
      ) : null}
    </section>
  );
}

function EmptyState({ title, copy }: { title: string; copy: string }) {
  return (
    <div className="empty-state">
      <div className="empty-icon" aria-hidden="true">
        ···
      </div>
      <h3>{title}</h3>
      <p>{copy}</p>
    </div>
  );
}

function messageFor(error: unknown, fallback: string): string {
  if (error instanceof ApiError || error instanceof Error) {
    return error.message;
  }
  return fallback;
}
