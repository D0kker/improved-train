"use client";

import Link from "next/link";
import { useCallback, useEffect, useRef, useState } from "react";
import {
  ApiError,
  fetchJson,
  playerActiveGamePath,
  type ActiveGame,
  type AnalysisCoverage,
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
import { platformRegionLabel } from "@/src/platform-region";
import { canContinueRecentMatches, isCurrentPlayerRequest, nextRecentMatchesStart } from "@/src/coverage";
import { CoverageSummary } from "@/src/coverage-summary";
import { SiteHeader } from "@/src/site-header";

interface PlayerDashboardProps {
  gameName: string;
  tagLine: string;
  platformRegion?: string;
}

interface DashboardData {
  player: PlayerLookup;
  summary: PlayerSummary;
  matches: MatchesResponse;
  latestMatch: MatchDetail | null;
  activeGame: ActiveGame | null;
  historyStatus: HistoryAnalysisStatus;
  coverage: AnalysisCoverage;
}

type LoadingStep = "lookup" | "sync" | "results";
const loadingCopy: Record<LoadingStep, string> = {
  lookup: "Resolviendo el Riot ID…",
  sync: "Consultando tus últimas 5 partidas…",
  results: "Construyendo el resumen…",
};

export function PlayerDashboard({ gameName, tagLine, platformRegion }: PlayerDashboardProps) {
  const [data, setData] = useState<DashboardData | null>(null);
  const [step, setStep] = useState<LoadingStep>("lookup");
  const [error, setError] = useState<string | null>(null);
  const [activeGameError, setActiveGameError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isLoadingActiveGame, setIsLoadingActiveGame] = useState(false);
  const [isStale, setIsStale] = useState(false);
  const [isLoadingMoreMatches, setIsLoadingMoreMatches] = useState(false);
  const [hasMoreMatches, setHasMoreMatches] = useState(false);
  const [nextMatchStart, setNextMatchStart] = useState<number | null>(null);
  const [retryingMatchWindow, setRetryingMatchWindow] = useState(false);
  const [additionalCoverage, setAdditionalCoverage] = useState<AnalysisCoverage | null>(null);
  const [additionalError, setAdditionalError] = useState<string | null>(null);
  const [hasLoadedAdditionalMatches, setHasLoadedAdditionalMatches] = useState(false);
  const requestController = useRef<AbortController | null>(null);
  const historyController = useRef<AbortController | null>(null);
  const historyRequestInFlight = useRef(false);

  const load = useCallback(
    async () => {
      requestController.current?.abort();
      historyController.current?.abort();
      historyRequestInFlight.current = false;
      const controller = new AbortController();
      requestController.current = controller;
      const { signal } = controller;
      setIsLoading(true);
      setError(null);
      setActiveGameError(null);
      setIsLoadingActiveGame(false);
      setStep("sync");
      setIsLoadingMoreMatches(false);
      setAdditionalError(null);
      setAdditionalCoverage(null);
      setHasLoadedAdditionalMatches(false);
      try {
        const result = await fetchJson<PlayerSearchResult>(
          playerSearchPath(gameName, tagLine, platformRegion),
          { signal },
        );
        if (signal?.aborted) return;
        setData({
          ...result,
          latestMatch: null,
          activeGame: null,
          historyStatus: result.coverage.status,
        });
        setIsStale(false);
        const hasMore = canContinueRecentMatches(result.coverage);
        setHasMoreMatches(hasMore);
        setRetryingMatchWindow(
          hasMore && (result.coverage.status === "partial" || result.coverage.status === "error"),
        );
        setNextMatchStart(
          hasMore
            ? result.coverage.status === "partial" || result.coverage.status === "error"
              ? 0
              : 5
            : null,
        );

        setIsLoadingActiveGame(true);
        void fetchJson<ActiveGame | null>(
          playerActiveGamePath(result.player.puuid, result.player.platformRegion),
          { signal },
        )
          .then((activeGame) => {
            if (signal.aborted) return;
            setData((current) =>
              current && isCurrentPlayerRequest(current.player.puuid, result.player.puuid, signal.aborted)
                ? { ...current, activeGame }
                : current,
            );
          })
          .catch((activeGameRequestError: unknown) => {
            if (!signal.aborted) {
              setActiveGameError(
                messageFor(activeGameRequestError, "No se pudo comprobar si hay una partida en curso."),
              );
            }
          })
          .finally(() => {
            if (!signal.aborted) setIsLoadingActiveGame(false);
          });
      } catch (requestError) {
        if (signal?.aborted) return;
        setIsStale(true);
        setError(messageFor(requestError, "No pudimos cargar este jugador."));
      } finally {
        if (!signal?.aborted) setIsLoading(false);
      }
    },
    [gameName, platformRegion, tagLine],
  );

  useEffect(() => {
    let isCurrentEffect = true;
    queueMicrotask(() => {
      if (isCurrentEffect) void load();
    });
    return () => {
      isCurrentEffect = false;
      requestController.current?.abort();
      historyController.current?.abort();
    };
  }, [load]);

  const loadMoreMatches = useCallback(async () => {
    if (!data || !hasMoreMatches || nextMatchStart === null || historyRequestInFlight.current) return;
    historyRequestInFlight.current = true;
    const controller = new AbortController();
    historyController.current?.abort();
    historyController.current = controller;
    const requestedStart = nextMatchStart;
    setIsLoadingMoreMatches(true);
    setAdditionalError(null);
    try {
      const result = await fetchJson<PlayerSearchResult>(
        recentMatchesPath(
          data.player.puuid,
          requestedStart,
          data.player.platformRegion,
        ),
        { signal: controller.signal },
      );
      if (controller.signal.aborted) return;
      const retryingInitial = requestedStart === 0;
      setAdditionalCoverage(retryingInitial ? null : result.coverage);
      setRetryingMatchWindow(
        result.coverage.status === "partial" || result.coverage.status === "error",
      );
      setNextMatchStart(
        nextRecentMatchesStart(requestedStart, result.coverage),
      );
      setHasMoreMatches(canContinueRecentMatches(result.coverage));
      if (!retryingInitial) setHasLoadedAdditionalMatches(true);
      setData((current) => {
        if (!current || current.player.puuid !== data.player.puuid) return current;
        const currentItems = retryingInitial ? current.matches.items.slice(5) : current.matches.items;
        const items = [
          ...new Map(
            [...(retryingInitial ? result.matches.items : currentItems), ...(retryingInitial ? currentItems : result.matches.items)]
              .map((match) => [match.riotMatchId, match]),
          ).values(),
        ];
        return {
          ...current,
          ...(retryingInitial
            ? { summary: result.summary, encounters: result.encounters, coverage: result.coverage, historyStatus: result.coverage.status }
            : {}),
          matches: { ...current.matches, items, totalCount: items.length },
        };
      });
    } catch (requestError) {
      if (!controller.signal.aborted) {
        setAdditionalError(messageFor(requestError, "No se pudieron cargar más partidas."));
        setRetryingMatchWindow(true);
      }
    } finally {
      if (!controller.signal.aborted) setIsLoadingMoreMatches(false);
      if (historyController.current === controller) {
        historyRequestInFlight.current = false;
      }
    }
  }, [data, hasMoreMatches, nextMatchStart]);

  return (
    <main className="dashboard-shell" id="main-content" tabIndex={-1}>
      <SiteHeader actionLabel="Nueva búsqueda" />

      <div className="dashboard-content">
        <section className="player-heading">
          <div>
            <p className="eyebrow">Perfil del jugador</p>
            <h1>
              {data?.summary.gameName || gameName}
              <span>#{data?.summary.tagLine || tagLine}</span>
            </h1>
            <p className="muted-copy">
              Consulta de partidas terminadas. Los análisis no se guardan como historial permanente.
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
                  ? `${isStale ? "Consulta anterior. " : ""}Partida más reciente: ${formatDate(data.summary.dataUpdatedAt)}`
                  : "Sin partidas recuperadas"}
              </p>
            ) : null}
            {data ? <p className="profile-region">Región: {platformRegionLabel(data.player.platformRegion || platformRegion)}</p> : null}
          </div>
        </section>

        {isLoading && !data ? <LoadingPanel step={step} /> : null}
        {error ? (
          <section className="message-panel error-panel" role="alert">
            <div>
              <p className="eyebrow">{isStale && data ? "No se pudo actualizar" : "No se pudo completar la consulta"}</p>
              <h2>{error}</h2>
              <p>
                {isStale && data
                  ? "Se conserva la consulta anterior. La fecha mostrada corresponde a la partida más reciente, no al momento de esta actualización."
                  : "Revisa el Riot ID y la región seleccionada; puedes volver a intentarlo."}
              </p>
            </div>
            <button className="secondary-button" onClick={() => void load()}>
              Reintentar
            </button>
          </section>
        ) : null}

        {data ? (
          <>
            <CoverageSummary coverage={data.coverage} title="Consulta inicial · cinco partidas" />
            <SummaryCards summary={data.summary} />
            <ActiveGameStatus
              game={data.activeGame}
              error={activeGameError}
              isLoading={isLoadingActiveGame}
              ownerPuuid={data.player.puuid}
              platformRegion={data.player.platformRegion}
              onRetry={() => {
                const controller = requestController.current;
                if (!controller || controller.signal.aborted) return;
                setActiveGameError(null);
                setIsLoadingActiveGame(true);
                void fetchJson<ActiveGame | null>(playerActiveGamePath(data.player.puuid, data.player.platformRegion), { signal: controller.signal })
                  .then((game) => setData((current) => current?.player.puuid === data.player.puuid ? { ...current, activeGame: game } : current))
                  .catch((activeError: unknown) => {
                    if (!controller.signal.aborted) setActiveGameError(messageFor(activeError, "No se pudo comprobar si hay una partida en curso."));
                  })
                  .finally(() => { if (!controller.signal.aborted) setIsLoadingActiveGame(false); });
              }}
            />
            {additionalCoverage ? <CoverageSummary coverage={additionalCoverage} title="Último bloque de cinco consultado" /> : null}
            {additionalError ? <p className="warning-banner" role="status">{additionalError} Puedes volver a intentar este mismo bloque.</p> : null}
            {hasLoadedAdditionalMatches ? <p className="history-scope-note" role="status">Las partidas adicionales amplían el historial mostrado, pero no modifican este resumen.</p> : null}
            <LatestMatch
              match={data.matches.items[0] ?? null}
              matchDetail={data.latestMatch}
              ownerPuuid={data.player.puuid}
              historyStatus={data.historyStatus}
              platformRegion={data.player.platformRegion}
              returnProfile={{ gameName: data.player.gameName, tagLine: data.player.tagLine }}
            />
            <MatchHistory
              matches={data.matches}
              ownerPuuid={data.player.puuid}
              isLoadingMore={isLoadingMoreMatches}
              hasMore={hasMoreMatches}
              retryingWindow={retryingMatchWindow}
              platformRegion={data.player.platformRegion}
              returnProfile={{ gameName: data.player.gameName, tagLine: data.player.tagLine }}
              onLoadMore={loadMoreMatches}
            />
          </>
        ) : null}
      </div>
    </main>
  );
}

function ActiveGameStatus({
  game,
  error,
  isLoading,
  ownerPuuid,
  platformRegion,
  onRetry,
}: {
  game: ActiveGame | null;
  error: string | null;
  isLoading: boolean;
  ownerPuuid: string;
  platformRegion?: string;
  onRetry: () => void;
}) {
  if (!game) {
    return (
      <section className="active-game-status" aria-live="polite">
        <div>
          <p className="eyebrow">Partida en curso</p>
          <p className="muted-copy">
            {isLoading
              ? "Comprobando si hay una partida en curso…"
              : error ?? "No se detectó una partida en curso en la última consulta."}
          </p>
        </div>
        {error ? (
          <button className="secondary-button" disabled={isLoading} onClick={onRetry} type="button">
            Volver a comprobar
          </button>
        ) : null}
      </section>
    );
  }

  return (
    <>
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
          href={`/match/live?ownerPuuid=${encodeURIComponent(ownerPuuid)}${platformRegion ? `&platformRegion=${encodeURIComponent(platformRegion)}` : ""}`}
        >
          Ver partida en curso →
        </Link>
      </div>
      </section>
      {error ? (
        <p className="warning-banner" role="status">
          No se pudo actualizar la comprobación. Se conserva el resultado anterior.
          <button className="inline-retry" disabled={isLoading} onClick={onRetry} type="button">
            Volver a comprobar
          </button>
        </p>
      ) : null}
    </>
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
          <p className="eyebrow">Resumen de las cinco partidas iniciales</p>
          <h2 id="summary-title">Tu historial de encuentros</h2>
        </div>
      </div>
      <p className="section-note">
        Esta consulta incluye hasta cinco partidas iniciales. Cada jugador se cuenta una sola vez,
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
  platformRegion,
  returnProfile,
}: {
  match: MatchesResponse["items"][number] | null;
  matchDetail: MatchDetail | null;
  ownerPuuid: string;
  historyStatus: HistoryAnalysisStatus;
  platformRegion?: string;
  returnProfile: { gameName: string; tagLine: string };
}) {
  if (!match) {
    return (
      <section aria-labelledby="last-match-title">
        <div className="section-title-row">
          <div>
            <p className="eyebrow">Última partida</p>
        <h2 id="last-match-title">Todavía no hay partidas recuperadas</h2>
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
        href={matchDetailPath(match.riotMatchId, ownerPuuid, platformRegion, returnProfile)}
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
  retryingWindow,
  platformRegion,
  returnProfile,
  onLoadMore,
}: {
  matches: MatchesResponse;
  ownerPuuid: string;
  isLoadingMore: boolean;
  hasMore: boolean;
  retryingWindow: boolean;
  platformRegion?: string;
  returnProfile: { gameName: string; tagLine: string };
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
          title="No se recuperaron partidas"
          copy="Revisa el estado de la consulta y vuelve a intentarlo si fue parcial o no pudo completarse."
        />
      ) : (
        <div className="match-list">
          {matches.items.map((match) => (
            <Link
              className={`match-card ${match.win ? "win" : "loss"}`}
              href={matchDetailPath(match.riotMatchId, ownerPuuid, platformRegion, returnProfile)}
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
      {hasMore ? (
        <div className="section-actions">
          <button
            className="secondary-button"
            disabled={isLoadingMore}
            onClick={onLoadMore}
            type="button"
          >
            {isLoadingMore
              ? retryingWindow ? "Reintentando este bloque…" : "Cargando partidas…"
              : retryingWindow ? "Reintentar este bloque de cinco" : "Cargar cinco partidas más"}
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
  return error instanceof ApiError ? error.message : fallback;
}
