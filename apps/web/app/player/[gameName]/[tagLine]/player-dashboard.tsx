"use client";

import Link from "next/link";
import { PlayerRanked } from "@/src/player-ranked";
import { ProfileIcon } from "@/src/profile-icon";
import { useCallback, useEffect, useRef, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { apiErrorMessageKey, type ApiErrorMessageKey } from "@/src/i18n/errors";
import {
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
import { formatDashboardDate } from "@/src/dashboard-presentation";
import { formatDate, formatDuration, queueLabel } from "@/src/format";
import { ChampionIcon } from "@/src/champion-icon";
import { platformRegionName } from "@/src/platform-region";
import {
  canContinueRecentMatches,
  isCurrentPlayerRequest,
  nextRecentMatchesStart,
} from "@/src/coverage";

import { SiteHeader } from "@/src/site-header";
import {
  Badge,
  Button,
  Card,
  EmptyState as UiEmptyState,
} from "@/src/design-system";

import {
  CompetitivePerformance,
  CompetitiveRelationships,
  CompetitiveCoverage,
} from "@/src/competitive-sections";
import {
  loadedMatches,
  reconcileMatches,
  filterMatches,
  kda,
  type DashboardWindow,
} from "@/src/competitive-dashboard";

interface PlayerDashboardProps {
  gameName: string;
  tagLine: string;
  platformRegion?: string;
}

interface DashboardData {
  windows: DashboardWindow[];
  player: PlayerLookup;
  summary: PlayerSummary;
  matches: MatchesResponse;
  activeGame: ActiveGame | null;
  coverage: AnalysisCoverage;
}

type LoadingStep = "lookup" | "sync" | "results";
export function PlayerDashboard({
  gameName,
  tagLine,
  platformRegion,
}: PlayerDashboardProps) {
  const t = useTranslations("dashboard");
  const common = useTranslations("common");
  const errorT = useTranslations("errors");
  const locale = useLocale();
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

  const [additionalError, setAdditionalError] = useState<string | null>(null);

  const requestController = useRef<AbortController | null>(null);
  const historyController = useRef<AbortController | null>(null);
  const historyRequestInFlight = useRef(false);

  const load = useCallback(async () => {
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

    try {
      const result = await fetchJson<PlayerSearchResult>(
        playerSearchPath(gameName, tagLine, platformRegion),
        { signal },
      );
      if (signal?.aborted) return;
      setData({
        ...result,
        windows: [{ start: 0, result }],
        matches: {
          ...result.matches,
          items: loadedMatches([{ start: 0, result }]),
        },
        activeGame: null,
      });
      setIsStale(false);
      const hasMore = canContinueRecentMatches(result.coverage);
      setHasMoreMatches(hasMore);
      setRetryingMatchWindow(
        hasMore &&
          (result.coverage.status === "partial" ||
            result.coverage.status === "error"),
      );
      setNextMatchStart(
        hasMore
          ? result.coverage.status === "partial" ||
            result.coverage.status === "error"
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
            current &&
            isCurrentPlayerRequest(
              current.player.puuid,
              result.player.puuid,
              signal.aborted,
            )
              ? { ...current, activeGame }
              : current,
          );
        })
        .catch((activeGameRequestError: unknown) => {
          if (!signal.aborted) {
            setActiveGameError(
              messageFor(activeGameRequestError, t("noActiveError"), errorT),
            );
          }
        })
        .finally(() => {
          if (!signal.aborted) setIsLoadingActiveGame(false);
        });
    } catch (requestError) {
      if (signal?.aborted) return;
      setIsStale(true);
      setError(messageFor(requestError, t("noPlayer"), errorT, "player"));
    } finally {
      if (!signal?.aborted) setIsLoading(false);
    }
  }, [gameName, platformRegion, tagLine, t, errorT]);

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
    if (
      !data ||
      !hasMoreMatches ||
      nextMatchStart === null ||
      historyRequestInFlight.current
    )
      return;
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

      setRetryingMatchWindow(
        result.coverage.status === "partial" ||
          result.coverage.status === "error",
      );
      setNextMatchStart(
        nextRecentMatchesStart(requestedStart, result.coverage),
      );
      setHasMoreMatches(canContinueRecentMatches(result.coverage));

      setData((current) => {
        if (!current || current.player.puuid !== data.player.puuid)
          return current;
        const prior = current.windows.find((w) => w.start === requestedStart);
        const windowResult = {
          ...result,
          matches: {
            ...result.matches,
            items: reconcileMatches([
              ...(prior?.result.matches.items ?? []),
              ...result.matches.items,
            ]),
          },
        };
        const windows = [
          ...current.windows.filter((w) => w.start !== requestedStart),
          { start: requestedStart, result: windowResult },
        ].sort((a, b) => a.start - b.start);
        const items = loadedMatches(windows);
        return {
          ...current,
          windows,
          ...(retryingInitial
            ? {
                summary: result.summary,
                encounters: result.encounters,
                coverage: result.coverage,
              }
            : {}),
          matches: { ...current.matches, items, totalCount: items.length },
        };
      });
    } catch (requestError) {
      if (!controller.signal.aborted) {
        setAdditionalError(messageFor(requestError, t("moreError"), errorT));
        setRetryingMatchWindow(true);
      }
    } finally {
      if (!controller.signal.aborted) setIsLoadingMoreMatches(false);
      if (historyController.current === controller) {
        historyRequestInFlight.current = false;
      }
    }
  }, [data, errorT, hasMoreMatches, nextMatchStart, t]);

  return (
    <>
      <SiteHeader actionLabel={common("newSearch")} />
      <main
        className="dashboard-shell player-dashboard-shell"
        id="main-content"
        tabIndex={-1}
      >
        <div className="dashboard-content">
          <div className="competitive-header">
            <section className="player-heading" aria-labelledby="player-title">
              <Card variant="elevated" className="player-identity-card">
                <div className="competitive-profile-main">
                  <ProfileIcon
                    id={data?.player.profileIconId}
                    observedAt={data?.player.profileIconObservedAt}
                  />
                  <div className="player-identity-copy">
                    <h1 id="player-title">
                      {data?.summary.gameName || gameName}
                      <span className="player-tag">
                        #{data?.summary.tagLine || tagLine}
                      </span>
                    </h1>
                    <Badge variant="info" className="profile-region">
                      {platformRegionName(
                        data?.player.platformRegion || platformRegion,
                        locale,
                      )}
                    </Badge>
                  </div>
                  {data && !isStale ? (
                    <PlayerRanked player={data.player} />
                  ) : null}
                </div>
                <div className="profile-actions">
                  <Button
                    variant="secondary"
                    disabled={isLoading}
                    loading={isLoading}
                    onClick={() => void load()}
                  >
                    {isLoading ? t("updating") : t("update")}
                  </Button>
                  {data ? (
                    <p className="profile-freshness">
                      {data.summary.matchesAnalyzed > 0 ? (
                        <>
                          {isStale ? `${t("previous")} ` : ""}
                          {t("lastMatch")}{" "}
                          <time dateTime={data.summary.dataUpdatedAt}>
                            {formatDashboardDate(
                              data.summary.dataUpdatedAt,
                              locale,
                            )}
                          </time>
                        </>
                      ) : (
                        t("noMatches")
                      )}
                    </p>
                  ) : null}
                </div>
              </Card>
            </section>

            <aside
              className="competitive-session"
              aria-label={t("sessionStatus")}
            >
              {data ? (
                <>
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
                      void fetchJson<ActiveGame | null>(
                        playerActiveGamePath(
                          data.player.puuid,
                          data.player.platformRegion,
                        ),
                        { signal: controller.signal },
                      )
                        .then((game) =>
                          setData((current) =>
                            !controller.signal.aborted &&
                            current?.player.puuid === data.player.puuid
                              ? { ...current, activeGame: game }
                              : current,
                          ),
                        )
                        .catch((activeError: unknown) => {
                          if (!controller.signal.aborted)
                            setActiveGameError(
                              messageFor(
                                activeError,
                                t("noActiveError"),
                                errorT,
                              ),
                            );
                        })
                        .finally(() => {
                          if (!controller.signal.aborted)
                            setIsLoadingActiveGame(false);
                        });
                    }}
                  />
                  <div className="competitive-session-meta">
                    <p>
                      {isLoading
                        ? t("loadingActive")
                        : isStale
                          ? t("staleKept")
                          : t("available")}
                    </p>
                    <a href="#matches-title">
                      <span>
                        {t("loadedMatches", {
                          count: data.matches.items.length,
                        })}
                      </span>
                      <span>{t("historyLink")}</span>
                    </a>
                  </div>
                </>
              ) : (
                <p>{t("waiting")}</p>
              )}
            </aside>
          </div>
          {isLoading && !data ? <LoadingPanel step={step} /> : null}
          {error ? (
            <section className="message-panel error-panel" role="alert">
              <div>
                <p className="eyebrow">
                  {isStale && data ? t("updateFailed") : t("queryFailed")}
                </p>
                <h2>{error}</h2>
                <p>{isStale && data ? t("staleDetails") : t("retryAdvice")}</p>
              </div>
              <Button variant="secondary" onClick={() => void load()}>
                {common("retry")}
              </Button>
            </section>
          ) : null}

          {data ? (
            <>
              {additionalError && (
                <p className="warning-banner" role="status">
                  La última ventana falló. Las métricas conservan solo partidas
                  ya recuperadas.
                </p>
              )}
              <CompetitivePerformance windows={data.windows} />
              <CompetitiveRelationships
                windows={data.windows}
                owner={data.player.puuid}
                region={data.player.platformRegion}
              />

              {additionalError ? (
                <p className="warning-banner" role="status">
                  {additionalError} Puedes volver a intentar este mismo bloque.
                </p>
              ) : null}

              <MatchHistory
                matches={data.matches}
                coverageStatus={data.coverage.status}
                ownerPuuid={data.player.puuid}
                isLoadingMore={isLoadingMoreMatches}
                hasMore={hasMoreMatches}
                retryingWindow={retryingMatchWindow}
                platformRegion={data.player.platformRegion}
                returnProfile={{
                  gameName: data.player.gameName,
                  tagLine: data.player.tagLine,
                }}
                onLoadMore={loadMoreMatches}
                disabled={isLoading}
              />
              <CompetitiveCoverage windows={data.windows} />
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
  const t = useTranslations("dashboard");
  const locale = useLocale();
  if (!game) {
    return (
      <section
        className="active-game-status"
        aria-labelledby="active-game-title"
        aria-live="polite"
      >
        <div>
          <h2 className="eyebrow" id="active-game-title">
            {t("activeGame")}
          </h2>
          <p className="muted-copy">
            {isLoading ? t("checkingActive") : (error ?? t("noActive"))}
          </p>
          {!isLoading && !error ? (
            <span className="sr-only">{t("notDisconnected")}</span>
          ) : null}
        </div>
        {error ? (
          <Button
            variant="secondary"
            disabled={isLoading}
            loading={isLoading}
            onClick={onRetry}
          >
            {t("checkAgain")}
          </Button>
        ) : null}
      </section>
    );
  }

  return (
    <>
      <section
        className="active-game-banner"
        aria-labelledby="active-game-title"
        aria-live="polite"
      >
        <div>
          <h2 className="eyebrow" id="active-game-title">
            {t("activeGame")}
          </h2>
          <p className="muted-copy">
            {queueLabel(game.queueId, locale)} · {t("startedAt")}{" "}
            {formatDate(game.gameStartTime, locale)}
          </p>
        </div>
        <div className="active-game-actions">
          <strong>
            {t("elapsedDuration", {
              duration: formatDuration(game.gameLengthSeconds, locale),
            })}
          </strong>
          <Link
            className="primary-button"
            href={`/match/live?ownerPuuid=${encodeURIComponent(ownerPuuid)}${platformRegion ? `&platformRegion=${encodeURIComponent(platformRegion)}` : ""}`}
          >
            {t("viewActiveGame")} →
          </Link>
        </div>
      </section>
      {error ? (
        <div className="warning-banner" role="status">
          {t("activeCheckStale")}
          <Button
            variant="ghost"
            disabled={isLoading}
            loading={isLoading}
            onClick={onRetry}
          >
            {t("checkAgain")}
          </Button>
        </div>
      ) : null}
    </>
  );
}

function LoadingPanel({ step }: { step: LoadingStep }) {
  const t = useTranslations("dashboard");
  const loadingCopy = {
    lookup: t("lookup"),
    sync: t("sync"),
    results: t("results"),
  };
  return (
    <section className="loading-panel" role="status" aria-live="polite">
      <span className="spinner" aria-hidden="true" />
      <div>
        <p className="eyebrow">{t("preparing")}</p>
        <h2>{loadingCopy[step]}</h2>
        <p>{t("loadingFive")}</p>
        <p>{t("deepAnalysisScope")}</p>
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
  disabled,
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
  disabled: boolean;
}) {
  const t = useTranslations("dashboard");
  const common = useTranslations("common");
  const locale = useLocale();
  const [filter, setFilter] = useState("all");
  const visible = filterMatches(matches.items, filter);
  return (
    <section className="player-match-history" aria-labelledby="matches-title">
      <div className="section-title-row">
        <div>
          <p className="eyebrow">{t("history")}</p>
          <h2 id="matches-title">{t("recentMatches")}</h2>
        </div>
        <p className="section-note">
          {t("loadedMatches", {
            count: matches.items.length.toLocaleString(locale),
          })}
        </p>
      </div>

      <div
        className="competitive-filters"
        role="group"
        aria-label={t("filterResults")}
      >
        {[
          ["all", t("all")],
          ["wins", t("wins")],
          ["losses", t("losses")],
        ].map(([value, label]) => (
          <Button
            key={value}
            variant="ghost"
            aria-pressed={filter === value}
            onClick={() => setFilter(value)}
          >
            {label}
          </Button>
        ))}
      </div>
      {matches.items.length > 0 && !visible.length && (
        <p role="status">{t("filterEmpty")}</p>
      )}
      {matches.items.length === 0 ? (
        <UiEmptyState
          className="player-history-empty"
          title={
            coverageStatus === "error"
              ? t("matchesFailed")
              : t("noMatchesQuery")
          }
          description={
            coverageStatus === "error" || coverageStatus === "partial"
              ? t("partialHistory")
              : t("noEligible")
          }
        />
      ) : (
        <div className="match-list">
          {visible.map((match) => (
            <article key={match.riotMatchId} className="competitive-match">
              <Link
                className={`match-card ${match.win === true ? "win" : match.win === false ? "loss" : "unknown"}`}
                href={matchDetailPath(
                  match.riotMatchId,
                  ownerPuuid,
                  platformRegion,
                  returnProfile,
                )}
                key={match.riotMatchId}
              >
                <Badge
                  variant={
                    match.win === true
                      ? "success"
                      : match.win === false
                        ? "error"
                        : "neutral"
                  }
                  className="result-badge"
                >
                  {match.win === true
                    ? common("victory")
                    : match.win === false
                      ? common("defeat")
                      : common("resultUnavailable")}
                </Badge>
                <div className="match-champion">
                  <ChampionIcon
                    championId={match.championId}
                    championName={match.championName}
                  />
                  <span className="match-champion-copy">
                    <strong>{match.championName}</strong>
                    <span>{queueLabel(match.queueId, locale)}</span>
                  </span>
                </div>
                <div className="kda">
                  <strong>
                    {match.kills} / {match.deaths} / {match.assists}
                  </strong>
                  <span>K / D / A</span>
                  <span className="match-kda-ratio">
                    KDA{" "}
                    {Number.isFinite(kda(match)) ? kda(match).toFixed(2) : "—"}
                  </span>
                </div>
                <div className="match-meta">
                  <span>
                    {formatDuration(match.gameDurationSeconds, locale)}
                  </span>
                  <span>
                    {formatDashboardDate(match.gameStartTimestamp, locale)}
                  </span>
                </div>
                <span className="card-action">
                  {t("analyzeEncounters")} <span aria-hidden="true">→</span>
                </span>
              </Link>
            </article>
          ))}
        </div>
      )}
      {hasMore ? (
        <div className="section-actions">
          <Button
            variant="secondary"
            disabled={isLoadingMore || disabled}
            loading={isLoadingMore}
            onClick={onLoadMore}
          >
            {isLoadingMore
              ? retryingWindow
                ? t("retrying")
                : t("loadingMatches")
              : retryingWindow
                ? t("retryWindow")
                : t("loadMore")}
          </Button>
        </div>
      ) : null}
    </section>
  );
}

function messageFor(
  error: unknown,
  fallback: string,
  translate: (key: ApiErrorMessageKey) => string,
  context: "player" | "match" | "general" = "general",
): string {
  const key = apiErrorMessageKey(error, context);
  return key ? translate(key) : fallback;
}
