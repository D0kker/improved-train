"use client";

import Link from "next/link";
import { useCallback, useEffect, useRef, useState } from "react";
import {
  fetchJson,
  playerActiveGamePath,
  type ActiveGame,
  type ActiveGameParticipant,
} from "@/src/api";
import { formatDate, formatDuration, queueLabel } from "@/src/format";
import { PlayerProfileLink } from "@/src/player-profile-link";
import { SiteHeader } from "@/src/site-header";
import { platformRegionLabel } from "@/src/platform-region";
import { useRequestLifetime } from "@/src/use-request-lifetime";
import { useLocale, useTranslations } from "next-intl";
import { apiErrorMessageKey } from "@/src/i18n/errors";

export function LiveMatchView({
  ownerPuuid,
  platformRegion,
}: {
  ownerPuuid?: string;
  platformRegion?: string;
}) {
  const t = useTranslations("live");
  const common = useTranslations("common");
  const errorT = useTranslations("errors");
  const locale = useLocale();
  const beginRequest = useRequestLifetime();
  const [game, setGame] = useState<ActiveGame | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [hasNoActiveGame, setHasNoActiveGame] = useState(false);
  const [isStale, setIsStale] = useState(false);
  const [checkedAt, setCheckedAt] = useState<string | null>(null);
  const requestController = useRef<AbortController | null>(null);
  const lastOwnerPuuid = useRef(ownerPuuid);

  const load = useCallback(
    async (parentSignal?: AbortSignal) => {
      requestController.current?.abort();
      const controller = new AbortController();
      requestController.current = controller;
      const signal = beginRequest(parentSignal ?? controller.signal);
      if (lastOwnerPuuid.current !== ownerPuuid) {
        lastOwnerPuuid.current = ownerPuuid;
        setGame(null);
        setCheckedAt(null);
        setIsStale(false);
        setHasNoActiveGame(false);
      }
      if (!ownerPuuid) {
        setError(t("missingOwner"));
        setLoading(false);
        return;
      }

      setLoading(true);
      setError(null);
      try {
        const activeGame = await fetchJson<ActiveGame | null>(
          playerActiveGamePath(ownerPuuid, platformRegion),
          { signal },
        );
        if (signal.aborted) return;
        if (!activeGame) {
          setHasNoActiveGame(true);
          setGame(null);
        } else {
          setHasNoActiveGame(false);
          setGame(activeGame);
        }
        setIsStale(false);
        setCheckedAt(new Date().toISOString());
      } catch (requestError) {
        if (!signal.aborted) {
          setIsStale(true);
          const errorKey = apiErrorMessageKey(requestError);
          setError(errorKey ? errorT(errorKey) : t("error"));
        }
      } finally {
        if (!signal.aborted) setLoading(false);
      }
    },
    [ownerPuuid, platformRegion, beginRequest, t, errorT],
  );

  useEffect(() => {
    let isCurrentEffect = true;
    queueMicrotask(() => {
      if (isCurrentEffect) void load();
    });
    return () => {
      isCurrentEffect = false;
      requestController.current?.abort();
    };
  }, [load]);

  const teams = game
    ? [100, 200].map((teamId) => ({
        teamId,
        participants: game.participants.filter(
          (participant) => participant.teamId === teamId,
        ),
      }))
    : [];

  return (
    <main className="dashboard-shell" id="main-content" tabIndex={-1}>
      <SiteHeader />

      <div className="dashboard-content detail-content">
        <Link className="back-link" href="/">
          {common("backSearch")}
        </Link>

        {loading ? (
          <section className="loading-panel" role="status">
            <span className="spinner" aria-hidden="true" />
            <h2>{t("loading")}</h2>
          </section>
        ) : null}

        {error ? (
          <section className="message-panel error-panel" role="alert">
            <div>
              <p className="eyebrow">{t("current")}</p>
              <h1>{error}</h1>
              <p>{isStale && game ? t("stale") : t("checkAgain")}</p>
            </div>
            <button
              className="secondary-button"
              disabled={loading}
              onClick={() => void load()}
            >
              Volver a comprobar
            </button>
          </section>
        ) : null}

        {game ? (
          <>
            <section className="match-heading">
              <div>
                <p className="eyebrow">{t("current")}</p>
                <h1>{t("title")}</h1>
                <p className="muted-copy">
                  {queueLabel(game.queueId, locale)} · {t("startedAt")}{" "}
                  {formatDate(game.gameStartTime, locale)}
                </p>
                <p className="muted-copy">
                  {common("region")}:{" "}
                  {platformRegionLabel(platformRegion, locale)}
                </p>
              </div>
              <div className="duration-pill">
                <span>{t("elapsed")}</span>
                <strong>
                  {formatDuration(game.gameLengthSeconds, locale)}
                </strong>
              </div>
            </section>
            <p className="profile-freshness">
              {isStale ? `${t("previous")} ` : ""}
              {checkedAt
                ? `${t("checkedAt")}: ${formatDate(checkedAt, locale)}`
                : ""}
            </p>
            <div className="section-actions">
              <button
                className="secondary-button"
                disabled={loading}
                onClick={() => void load()}
                type="button"
              >
                {loading ? t("checking") : t("update")}
              </button>
            </div>

            <div className="live-teams-grid">
              {teams.map((team) => (
                <section
                  className={`live-team live-team-${team.teamId}`}
                  key={team.teamId}
                >
                  <header>
                    <p className="eyebrow">
                      {team.teamId === 100
                        ? common("blueTeam")
                        : common("redTeam")}
                    </p>
                    <strong>
                      {t("playerCount", { count: team.participants.length })}
                    </strong>
                  </header>
                  <ul className="live-participant-list">
                    {team.participants.map((participant, index) => (
                      <LiveParticipant
                        key={
                          participant.puuid ??
                          `${participant.championId}-${index}`
                        }
                        isObserved={participant.puuid === ownerPuuid}
                        participant={participant}
                        platformRegion={platformRegion}
                      />
                    ))}
                  </ul>
                </section>
              ))}
            </div>
          </>
        ) : null}
        {hasNoActiveGame && !loading ? (
          <div className="empty-state">
            <h2>{t("noGameTitle")}</h2>
            <p>{t("noGameDescription")}</p>
            <button
              className="secondary-button"
              onClick={() => void load()}
              type="button"
            >
              {t("checkAgain")}
            </button>
          </div>
        ) : null}
      </div>
    </main>
  );
}

function LiveParticipant({
  isObserved,
  participant,
  platformRegion,
}: {
  isObserved: boolean;
  participant: ActiveGameParticipant;
  platformRegion?: string;
}) {
  const t = useTranslations("live");
  const common = useTranslations("common");
  return (
    <li className={isObserved ? "live-observed-player" : undefined}>
      <div className="live-champion-icon">{participant.championId || "?"}</div>
      <div>
        {participant.gameName && participant.tagLine ? (
          <PlayerProfileLink
            gameName={participant.gameName}
            tagLine={participant.tagLine}
            platformRegion={platformRegion}
          />
        ) : (
          <strong>{participant.gameName || t("identifiedPlayer")}</strong>
        )}
        <span>
          {participant.tagLine
            ? `#${participant.tagLine} · `
            : `${t("riotIdUnavailable")} · `}
          {t("championId")} {participant.championId || t("championUnavailable")}
        </span>
        {isObserved ? (
          <span className="observed-badge">{common("observedPlayer")}</span>
        ) : null}
      </div>
    </li>
  );
}
