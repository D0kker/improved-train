"use client";

import Link from "next/link";
import { useCallback, useEffect, useRef, useState } from "react";
import {
  ApiError,
  fetchJson,
  playerActiveGamePath,
  type ActiveGame,
  type AnalysisCoverage,
  type MatchesResponse,
  type PlayerLookup,
  type PlayerSummary,
  type PlayerSearchResult,
  playerSearchPath,
  recentMatchesPath,
  matchDetailPath,
} from "@/src/api";
import { formatDate, formatDuration, formatPercent, queueLabel } from "@/src/format";
import { ChampionIcon } from "@/src/champion-icon";
import { platformRegionLabel } from "@/src/platform-region";
import { canContinueRecentMatches, isCurrentPlayerRequest, nextRecentMatchesStart } from "@/src/coverage";
import { CoverageSummary } from "@/src/coverage-summary";
import { SiteHeader } from "@/src/site-header";
import { Badge, Button, Card, EmptyState as UiEmptyState, SectionHeader, StatCard } from "@/src/design-system";

interface PlayerDashboardProps {
  gameName: string;
  tagLine: string;
  platformRegion?: string;
}

interface DashboardData {
  player: PlayerLookup;
  summary: PlayerSummary;
  matches: MatchesResponse;
  activeGame: ActiveGame | null;
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
          activeGame: null,
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
            ? { summary: result.summary, encounters: result.encounters, coverage: result.coverage }
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
    <>
      <SiteHeader actionLabel="Nueva búsqueda" />
      <main className="dashboard-shell player-dashboard-shell" id="main-content" tabIndex={-1}>
      <div className="dashboard-content">
        <section className="player-heading" aria-labelledby="player-title">
          <Card variant="elevated" className="player-identity-card">
            <div className="player-identity-copy">
              <p className="eyebrow">Perfil del jugador</p>
              <h1 id="player-title">
                {data?.summary.gameName || gameName}
                <span className="player-tag">#{data?.summary.tagLine || tagLine}</span>
              </h1>
              <Badge variant="info" className="profile-region">
                Región · {platformRegionLabel(data?.player.platformRegion || platformRegion)}
              </Badge>
              <p className="muted-copy">Consulta de partidas terminadas. El resumen usa hasta cinco partidas iniciales.</p>
            </div>
            <div className="profile-actions">
              <Button variant="secondary" disabled={isLoading} loading={isLoading} onClick={() => void load()}>
                {isLoading ? "Actualizando…" : "Actualizar cinco partidas"}
              </Button>
              {data ? (
                <p className="profile-freshness">
                  {data.summary.matchesAnalyzed > 0
                    ? `${isStale ? "Consulta anterior. " : ""}Partida más reciente: ${formatDate(data.summary.dataUpdatedAt)}`
                    : "Sin partidas recuperadas"}
                </p>
              ) : null}
            </div>
          </Card>
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
            <Button variant="secondary" onClick={() => void load()}>
              Reintentar
            </Button>
          </section>
        ) : null}

        {data ? (
          <>
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
            <CoverageSummary coverage={data.coverage} title="Cobertura · consulta inicial" />
            {additionalCoverage ? <CoverageSummary coverage={additionalCoverage} title="Último bloque de cinco consultado" /> : null}
            {additionalError ? <p className="warning-banner" role="status">{additionalError} Puedes volver a intentar este mismo bloque.</p> : null}
            {hasLoadedAdditionalMatches ? <p className="history-scope-note" role="status">Las partidas adicionales amplían el historial mostrado, pero no modifican este resumen.</p> : null}
            <MatchHistory
              matches={data.matches}
              coverageStatus={data.coverage.status}
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
    </>
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
      <section className="active-game-status" aria-labelledby="active-game-title" aria-live="polite">
        <div>
          <h2 className="eyebrow" id="active-game-title">
            Partida en curso
          </h2>
          <p className="muted-copy">
            {isLoading
              ? "Comprobando si hay una partida en curso…"
              : error ?? "No se detectó una partida en curso en la última consulta."}
          </p>
        </div>
        {error ? (
          <Button variant="secondary" disabled={isLoading} loading={isLoading} onClick={onRetry}>
            Volver a comprobar
          </Button>
        ) : null}
      </section>
    );
  }

  return (
    <>
      <section className="active-game-banner" aria-labelledby="active-game-title" aria-live="polite">
        <div>
          <h2 className="eyebrow" id="active-game-title">
            Partida en curso
          </h2>
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
        <div className="warning-banner" role="status">
          No se pudo actualizar la comprobación. Se conserva el resultado anterior.
          <Button variant="ghost" disabled={isLoading} loading={isLoading} onClick={onRetry}>
            Volver a comprobar
          </Button>
        </div>
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
    { label: "Partidas analizadas", value: summary.matchesAnalyzed.toLocaleString("es-PA"), description: "de hasta cinco iniciales" },
    { label: "Jugadores únicos", value: summary.uniquePlayersEncountered.toLocaleString("es-PA"), description: "Sin contarte a ti" },
    { label: "Recurrentes", value: summary.repeatedPlayers.toLocaleString("es-PA"), description: "En dos o más partidas" },
    { label: "Victorias", value: summary.wins.toLocaleString("es-PA"), description: "En las partidas analizadas" },
    { label: "Derrotas", value: summary.losses.toLocaleString("es-PA"), description: "En las partidas analizadas" },
    {
      label: "Win rate",
      value: summary.matchesAnalyzed > 0 ? formatPercent(summary.winRate) : "—",
      description: summary.matchesAnalyzed > 0 ? "En esta consulta" : "Sin partidas analizadas",
    },
  ];

  return (
    <section className="player-summary" role="region" aria-labelledby="player-summary-title">
      <SectionHeader
        title="Tu resumen inicial"
        titleId="player-summary-title"
        description={`Calculado con ${summary.matchesAnalyzed} de hasta cinco partidas iniciales disponibles.${summary.otherPlayerParticipations !== undefined ? ` Se observaron ${summary.otherPlayerParticipations.toLocaleString("es-PA")} participaciones de otros jugadores, incluidas repeticiones.` : ""}`}
        className="player-summary-header"
      />
      <div className="stat-grid">
        {cards.map((card) => (
          <StatCard
            className="player-stat-card"
            key={card.label}
            label={card.label}
            value={card.value}
            description={card.description}
          />
        ))}
      </div>
    </section>
  );
}

function MatchHistory({
  matches,
  coverageStatus,
  ownerPuuid,
  isLoadingMore,
  hasMore,
  retryingWindow,
  platformRegion,
  returnProfile,
  onLoadMore,
}: {
  matches: MatchesResponse;
  coverageStatus: AnalysisCoverage["status"];
  ownerPuuid: string;
  isLoadingMore: boolean;
  hasMore: boolean;
  retryingWindow: boolean;
  platformRegion?: string;
  returnProfile: { gameName: string; tagLine: string };
  onLoadMore: () => void;
}) {
  return (
    <section className="player-match-history" aria-labelledby="matches-title">
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
        <UiEmptyState
          className="player-history-empty"
          title={coverageStatus === "error" ? "No se pudieron cargar partidas" : "No hay partidas disponibles en esta consulta"}
          description={coverageStatus === "error" || coverageStatus === "partial"
            ? "La consulta no pudo recuperar toda la información. Revisa la cobertura y vuelve a intentarlo."
            : "No hay partidas elegibles en la ventana consultada. Esto no describe todo el historial de la cuenta."}
        />
      ) : (
        <div className="match-list">
          {matches.items.map((match) => (
            <Link
              className={`match-card ${match.win ? "win" : "loss"}`}
              href={matchDetailPath(match.riotMatchId, ownerPuuid, platformRegion, returnProfile)}
              key={match.riotMatchId}
            >
              <Badge variant={match.win ? "success" : "error"} className="result-badge">
                {match.win ? "Victoria" : "Derrota"}
              </Badge>
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
              <span className="card-action">
                Analizar partida <span aria-hidden="true">→</span>
              </span>
            </Link>
          ))}
        </div>
      )}
      {hasMore ? (
        <div className="section-actions">
          <Button
            variant="secondary"
            disabled={isLoadingMore}
            loading={isLoadingMore}
            onClick={onLoadMore}
          >
            {isLoadingMore
              ? retryingWindow ? "Reintentando este bloque…" : "Cargando partidas…"
              : retryingWindow ? "Reintentar este bloque de cinco" : "Cargar cinco partidas más"}
          </Button>
        </div>
      ) : null}
    </section>
  );
}

function messageFor(error: unknown, fallback: string): string {
  return error instanceof ApiError ? error.message : fallback;
}
