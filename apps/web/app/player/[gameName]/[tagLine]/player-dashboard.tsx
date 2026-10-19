"use client";

import Link from "next/link";
import { useCallback, useEffect, useState } from "react";
import {
  ApiError,
  fetchJson,
  playerActiveGamePath,
  type ActiveGame,
  type MatchDetail,
  type MatchesResponse,
  type PlayerLookup,
  type PlayerSummary,
  type PlayerSearchResult,
  playerSearchPath,
  recentMatchesPath,
  matchDetailPath,
} from "@/src/api";
import { formatDate, formatDuration, queueLabel } from "@/src/format";
import { familiarityMessage, type HistoryAnalysisStatus } from "@/src/echo";
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
  sync: "Consultando tus últimas 5 partidas…",
  results: "Construyendo el resumen…",
};

export function PlayerDashboard({ gameName, tagLine }: PlayerDashboardProps) {
  const [data, setData] = useState<DashboardData | null>(null);
  const [step, setStep] = useState<LoadingStep>("lookup");
  const [error, setError] = useState<string | null>(null);
  const [warning, setWarning] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isLoadingMoreMatches, setIsLoadingMoreMatches] = useState(false);
  const [hasMoreMatches, setHasMoreMatches] = useState(false);

  const load = useCallback(
    async (signal?: AbortSignal) => {
      setIsLoading(true);
      setError(null);
      setWarning(null);
      setStep("sync");
      try {
        const result = await fetchJson<PlayerSearchResult>(
          playerSearchPath(gameName, tagLine),
          { signal },
        );
        let activeGame: ActiveGame | null = null;
        try {
          activeGame = await fetchJson<ActiveGame | null>(
            playerActiveGamePath(result.player.puuid),
            { signal },
          );
        } catch {
          if (signal?.aborted) return;
          setWarning("No se pudo comprobar si hay una partida en curso.");
        }
        if (signal?.aborted) return;
        setData({
          ...result,
          latestMatch: null,
          activeGame,
          historyStatus: result.coverage.status,
        });
        setHasMoreMatches(result.matches.items.length === 5);
        if (result.coverage.status !== "complete")
          setWarning(
            result.coverage.status === "error"
              ? "No se pudieron recuperar las partidas. Puedes reintentar."
              : "Se muestran las partidas recuperadas; la cobertura del historial es limitada.",
          );
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
    const request = window.setTimeout(() => void load(controller.signal), 0);
    return () => {
      window.clearTimeout(request);
      controller.abort();
    };
  }, [load]);

  const loadMoreMatches = useCallback(async () => {
    if (!data || isLoadingMoreMatches || !hasMoreMatches) return;
    setIsLoadingMoreMatches(true);
    try {
      const result = await fetchJson<PlayerSearchResult>(
        recentMatchesPath(
          data.player.puuid,
          data.matches.items.length,
          data.player.platformRegion,
        ),
      );
      setHasMoreMatches(result.matches.items.length === 5);
      setData((current) =>
        current
          ? {
              ...current,
              matches: {
                ...result.matches,
                items: [
                  ...new Map(
                    [...current.matches.items, ...result.matches.items].map(
                      (match) => [match.riotMatchId, match],
                    ),
                  ).values(),
                ],
                totalCount:
                  current.matches.items.length + result.matches.items.length,
              },
            }
          : current,
      );
      if (
        result.coverage.status === "partial" ||
        result.coverage.status === "error"
      ) {
        setWarning("No se pudo completar la siguiente consulta del historial.");
        setHasMoreMatches(true);
      }
    } catch (requestError) {
      setWarning(
        messageFor(requestError, "No se pudieron cargar más partidas."),
      );
    } finally {
      setIsLoadingMoreMatches(false);
    }
  }, [data, isLoadingMoreMatches, hasMoreMatches]);

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
              onClick={() => void load()}
              type="button"
            >
              {isLoading ? "Actualizando…" : "Actualizar 5 partidas"}
            </button>
            {data ? (
              <p className="profile-freshness">
                {data.summary.matchesAnalyzed > 0
                  ? `Fecha de la última partida: ${formatDate(data.summary.dataUpdatedAt)}`
                  : "Sin partidas recuperadas"}
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
            <button className="secondary-button" onClick={() => void load()}>
              Reintentar
            </button>
          </section>
        ) : null}

        {data ? (
          <>
            {warning ? (
              <p className="warning-banner" role="status">
                {warning}
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
              hasMore={hasMoreMatches}
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
        <p>Consultando las últimas 5 partidas del jugador.</p>
        <p>
          Al abrir una partida se analizarán 5 partidas anteriores del jugador y
          3 de cada uno de los otros 9 participantes.
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
        Tus últimas 5 partidas recuperadas. Cada jugador se cuenta una sola vez,
        sin incluirte; recurrentes: aparecen en al menos dos partidas.
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
          <h2 id="last-match-title">Analiza esta partida</h2>
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
              : "Abre la partida para analizar sus participantes y encuentros anteriores."}
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
  hasMore,
  onLoadMore,
}: {
  matches: MatchesResponse;
  ownerPuuid: string;
  isLoadingMore: boolean;
  hasMore: boolean;
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
          {matches.items.length.toLocaleString("es-PA")} partidas cargadas
        </p>
      </div>

      {matches.items.length === 0 ? (
        <EmptyState
          title="No hay partidas disponibles"
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
      {matches.items.length > 0 && hasMore ? (
        <div className="section-actions">
          <button
            className="secondary-button"
            disabled={isLoadingMore}
            onClick={onLoadMore}
            type="button"
          >
            {isLoadingMore ? "Cargando partidas…" : "Cargar 5 partidas más"}
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
