"use client";

import Link from "next/link";
import { useCallback, useEffect, useRef, useState } from "react";
import {
  apiPath,
  fetchJson,
  analyzeSelectedMatch,
  type MatchDetail,
  type MatchAnalysisResult,
  type AnalysisCoverage,
} from "@/src/api";
import { formatDuration, queueLabel } from "@/src/format";
import { formatDashboardDate } from "@/src/dashboard-presentation";
import { CoverageSummary } from "@/src/coverage-summary";
import { ChampionIcon } from "@/src/champion-icon";
import { PlayerProfileLink } from "@/src/player-profile-link";
import { SiteHeader } from "@/src/site-header";
import { Button, Card } from "@/src/design-system";
import { platformRegionName } from "@/src/platform-region";
import { useRequestLifetime } from "@/src/use-request-lifetime";
import { presentMatchRelations } from "@/src/match-relations";
import { useLocale, useTranslations } from "next-intl";
import { apiErrorMessageKey } from "@/src/i18n/errors";
import { inlineMatchRelations } from "@/src/inline-match-relations";

type MatchDetailProps = {
  matchId: string;
  ownerPuuid?: string;
  platformRegion?: string;
  returnProfile?: { gameName: string; tagLine: string };
};
export function MatchDetailView(props: MatchDetailProps) {
  return (
    <MatchDetailContent
      key={JSON.stringify([
        props.matchId,
        props.ownerPuuid,
        props.platformRegion,
      ])}
      {...props}
    />
  );
}
function MatchDetailContent({
  matchId,
  ownerPuuid,
  platformRegion,
  returnProfile,
}: {
  matchId: string;
  ownerPuuid?: string;
  platformRegion?: string;
  returnProfile?: { gameName: string; tagLine: string };
}) {
  const t = useTranslations("match");
  const common = useTranslations("common");
  const coverageText = useTranslations("coverage");
  const errorT = useTranslations("errors");
  const locale = useLocale();
  const beginRequest = useRequestLifetime();
  const [match, setMatch] = useState<MatchDetail | null>(null);
  const [coverage, setCoverage] = useState<AnalysisCoverage | null>(null);
  const [relationships, setRelationships] = useState<
    MatchAnalysisResult["relationships"] | null
  >(null);
  const [error, setError] = useState<string | null>(null);
  const [isStale, setIsStale] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const hasPreviousMatch = useRef(false);
  const load = useCallback(
    async (parent?: AbortSignal) => {
      const signal = beginRequest(parent);
      setIsLoading(true);
      setError(null);
      setIsStale(hasPreviousMatch.current);
      try {
        if (ownerPuuid) {
          const result = await analyzeSelectedMatch(
            matchId,
            ownerPuuid,
            platformRegion,
            signal,
          );
          if (signal.aborted) return;
          setMatch(result.match);
          setCoverage(result.coverage);
          setRelationships(result.relationships ?? null);
        } else {
          const loaded = await fetchJson<MatchDetail>(
            apiPath("matches", matchId) +
              (platformRegion
                ? `?platformRegion=${encodeURIComponent(platformRegion)}`
                : ""),
            { signal },
          );
          if (signal.aborted) return;
          setMatch(loaded);
          setCoverage(null);
          setRelationships(null);
        }
        hasPreviousMatch.current = true;
        setIsStale(false);
      } catch (requestError) {
        if (signal.aborted) return;
        setIsStale(hasPreviousMatch.current);
        const errorKey = apiErrorMessageKey(requestError, "match");
        setError(errorKey ? errorT(errorKey) : t("noMatch"));
      } finally {
        if (!signal.aborted) setIsLoading(false);
      }
    },
    [matchId, ownerPuuid, platformRegion, beginRequest, t, errorT],
  );
  useEffect(() => {
    let current = true;
    queueMicrotask(() => {
      if (current) void load();
    });
    return () => {
      current = false;
    };
  }, [load]);
  const owner = ownerPuuid
    ? match?.teams
        .flatMap((team) => team.participants)
        .find((p) => p.puuid === ownerPuuid)
    : undefined;
  const combatValid =
    owner &&
    [owner.kills, owner.deaths, owner.assists].every(
      (n) => typeof n === "number" && Number.isFinite(n) && n >= 0,
    );
  const kda =
    combatValid && owner
      ? ((owner.kills + owner.assists) / Math.max(1, owner.deaths)).toFixed(2)
      : null;
  const pairs =
    match && relationships
      ? presentMatchRelations(
          relationships,
          match.teams,
          common("unidentifiedPlayer"),
        )
      : [];
  const familiarityCopy = (() => {
    const familiarity = match?.familiarity;
    if (!familiarity || !coverage) return "";
    if (coverage.status === "error") return t("familiarityError");
    if (coverage.status === "partial")
      return t("familiarityPartial", {
        count: familiarity.knownPlayers,
        evaluable: familiarity.evaluablePlayers,
      });
    if (coverage.status === "insufficient") return t("familiarityInsufficient");
    switch (familiarity.status) {
      case "Available":
        return familiarity.knownPlayers > 0
          ? t("familiarityAvailable", {
              count: familiarity.knownPlayers,
              evaluable: familiarity.evaluablePlayers,
            })
          : t("familiarityNone", { evaluable: familiarity.evaluablePlayers });
      case "NoPriorHistory":
        return t("familiarityNoPrior");
      case "NoEvaluableParticipants":
        return t("familiarityNoParticipants");
      case "OwnerNotPresent":
        return t("familiarityOwnerMissing");
      case "PartialParticipants":
        return t("familiarityPartialParticipants", {
          count: familiarity.knownPlayers,
          evaluable: familiarity.evaluablePlayers,
        });
      case "PartialHistory":
        return t("familiarityPartialHistory", {
          count: familiarity.knownPlayers,
        });
      case "InsufficientHistory":
        return t("familiarityInsufficient");
      case "AnalysisError":
        return t("familiarityError");
    }
  })();
  const inline = match
    ? inlineMatchRelations(match, relationships, coverage, ownerPuuid)
    : null;
  return (
    <main className="dashboard-shell" id="main-content" tabIndex={-1}>
      <SiteHeader />
      <div className="dashboard-content detail-content match-analysis">
        <Link
          className="back-link"
          href={
            returnProfile
              ? `/player/${encodeURIComponent(returnProfile.gameName)}/${encodeURIComponent(returnProfile.tagLine)}${platformRegion ? `?platformRegion=${encodeURIComponent(platformRegion)}` : ""}`
              : "/"
          }
        >
          {returnProfile ? common("backProfile") : common("backSearch")}
        </Link>
        {isLoading && !match && (
          <section className="loading-panel" role="status">
            <span className="spinner" aria-hidden="true" />
            <div>
              <h1>{ownerPuuid ? t("analyzing") : t("participantsLoading")}</h1>
              <p>{t("mayTake")}</p>
            </div>
          </section>
        )}
        {error && (
          <section className="message-panel error-panel" role="alert">
            <div>
              {match ? <h2>{error}</h2> : <h1>{error}</h1>}
              <p>{match && isStale ? t("previousResult") : t("tryAgain")}</p>
            </div>
            <Button
              variant="secondary"
              onClick={() => void load()}
              disabled={isLoading}
            >
              {common("retry")}
            </Button>
          </section>
        )}
        {match && (
          <>
            {isLoading && (
              <p role="status" className="warning-banner">
                {t("updating")}
              </p>
            )}
            {isStale && (
              <p className="warning-banner" role="status">
                {t("previousResult")}
              </p>
            )}
            <section
              aria-labelledby="match-summary-title"
              className="match-summary-section"
            >
              <header className="analysis-section-heading">
                <div>
                  <p className="eyebrow">{t("analysis")}</p>
                  <h1 id="match-summary-title">{t("summary")}</h1>
                </div>
                {coverage ? (
                  <a
                    className={`analysis-coverage analysis-coverage--${coverage.status}`}
                    href="#match-coverage"
                  >
                    {coverageText(coverage.status)}
                  </a>
                ) : (
                  <span className="analysis-coverage">{t("notRequested")}</span>
                )}
              </header>
              {coverage && coverage.status !== "complete" && (
                <p role="status" className="warning-banner">
                  {coverageText(coverage.status)}. {t("partialWarning")}
                </p>
              )}
              <Card className="analysis-match-summary">
                <div className="analysis-owner">
                  {owner && (
                    <ChampionIcon
                      championId={owner.championId}
                      championName={
                        owner.championName || t("championUnavailable")
                      }
                    />
                  )}
                  <div>
                    <strong>
                      {owner
                        ? owner.championName || t("championUnavailable")
                        : t("ownerUnavailable")}
                    </strong>
                    {owner && (
                      <PlayerProfileLink
                        gameName={owner.gameName}
                        tagLine={owner.tagLine}
                        platformRegion={platformRegion}
                        compact
                      />
                    )}
                    <span>
                      {owner?.win === true
                        ? common("victory")
                        : owner?.win === false
                          ? common("defeat")
                          : common("resultUnavailable")}
                    </span>
                  </div>
                </div>
                <div className="analysis-summary-stats">
                  <div>
                    <span>K / D / A</span>
                    <strong>
                      {combatValid && owner
                        ? `${owner.kills} / ${owner.deaths} / ${owner.assists}`
                        : "—"}
                    </strong>
                  </div>
                  <div>
                    <span>KDA</span>
                    <strong>{kda ?? "—"}</strong>
                  </div>
                  <div>
                    <span>{t("duration")}</span>
                    <strong>
                      {typeof match.gameDurationSeconds === "number" &&
                      Number.isFinite(match.gameDurationSeconds)
                        ? formatDuration(match.gameDurationSeconds, locale)
                        : "—"}
                    </strong>
                  </div>
                </div>
                <p>
                  {queueLabel(match.queueId, locale)} ·{" "}
                  {formatDashboardDate(match.gameStartTimestamp, locale)}
                  {platformRegion
                    ? ` · ${platformRegionName(platformRegion, locale)}`
                    : ""}
                </p>
              </Card>
            </section>
            <section aria-labelledby="match-teams-title">
              <header className="analysis-section-heading">
                <div>
                  <p className="eyebrow">{t("participants")}</p>
                  <h2 id="match-teams-title">{t("teams")}</h2>
                </div>
              </header>
              {match.teams.length ? (
                <div className="analysis-teams-grid">
                  {match.teams.map((team, index) => {
                    const outcomes = new Set(
                      team.participants.map((p) => p.win),
                    );
                    const result =
                      outcomes.size === 1 &&
                      team.participants.every((p) => typeof p.win === "boolean")
                        ? outcomes.has(true)
                          ? common("victory")
                          : common("defeat")
                        : common("resultUnavailable");
                    return (
                      <section
                        className={`analysis-team analysis-team--${team.teamId === 100 ? "blue" : team.teamId === 200 ? "red" : "neutral"}`}
                        key={team.teamId}
                        aria-label={
                          team.teamId === 100
                            ? common("blueTeam")
                            : team.teamId === 200
                              ? common("redTeam")
                              : `${t("team")} ${index + 1}`
                        }
                      >
                        <header>
                          <h3>
                            {team.teamId === 100
                              ? common("blueTeam")
                              : team.teamId === 200
                                ? common("redTeam")
                                : `${t("team")} ${index + 1}`}
                          </h3>
                          <span>{result}</span>
                        </header>
                        {team.participants.length ? (
                          <ol>
                            {team.participants.map((p, i) => {
                              const history = inline?.history(p.puuid);
                              const allies = inline?.priorAllies(p.puuid) ?? [];
                              const isOwner =
                                !!p.puuid && p.puuid === ownerPuuid;
                              return (
                                <li
                                  key={p.puuid || `${p.championId}-${i}`}
                                  className={
                                    p.puuid && p.puuid === ownerPuuid
                                      ? "analysis-owner-row"
                                      : undefined
                                  }
                                >
                                  <ChampionIcon
                                    championId={p.championId}
                                    championName={
                                      p.championName || t("championUnavailable")
                                    }
                                  />
                                  <div className="analysis-participant">
                                    <strong>
                                      {p.championName ||
                                        t("championUnavailable")}
                                    </strong>
                                    <PlayerProfileLink
                                      gameName={p.gameName}
                                      tagLine={p.tagLine}
                                      platformRegion={platformRegion}
                                      compact
                                    />
                                    {p.puuid && p.puuid === ownerPuuid && (
                                      <span>{common("observedPlayer")}</span>
                                    )}
                                    {p.puuid &&
                                      inline?.labels
                                        .get(p.puuid)
                                        ?.map((label) => (
                                          <span
                                            className="analysis-group-badge"
                                            key={label}
                                          >
                                            {t("inlineGroup", { label })}
                                          </span>
                                        ))}
                                    {!isOwner && history && (
                                      <span className="analysis-inline-history">
                                        {history.kind === "known"
                                          ? `${t("inlineHistory", { count: history.pair.matchesTogether, allies: history.pair.allyMatches, rivals: history.pair.enemyMatches })}${history.partial ? ` · ${t("inlinePartial")}` : ""}`
                                          : history.kind === "empty"
                                            ? t("inlineEmpty")
                                            : history.kind === "partial"
                                              ? t("inlineIncomplete")
                                              : history.kind === "identity"
                                                ? t("inlineIdentity")
                                                : t("inlineUnavailable")}
                                      </span>
                                    )}
                                    {allies.length > 0 && (
                                      <span className="analysis-inline-history">
                                        {t("inlineAllies")}:{" "}
                                        {allies
                                          .map(
                                            (name) =>
                                              name ??
                                              common("unidentifiedPlayer"),
                                          )
                                          .join(", ")}
                                      </span>
                                    )}
                                  </div>
                                  <div className="analysis-player-kda">
                                    <strong>
                                      {[p.kills, p.deaths, p.assists].every(
                                        (n) =>
                                          typeof n === "number" &&
                                          Number.isFinite(n) &&
                                          n >= 0,
                                      )
                                        ? `${p.kills} / ${p.deaths} / ${p.assists}`
                                        : "—"}
                                    </strong>
                                    <span>K / D / A</span>
                                  </div>
                                </li>
                              );
                            })}
                          </ol>
                        ) : (
                          <p>{t("noParticipants")}</p>
                        )}
                      </section>
                    );
                  })}
                </div>
              ) : (
                <p>{t("noTeams")}</p>
              )}
            </section>
            <section aria-labelledby="match-relationships-title">
              <header className="analysis-section-heading">
                <div>
                  <p className="eyebrow">{t("historicalEvidence")}</p>
                  <h2 id="match-relationships-title">
                    {t("historicalRelations")}
                  </h2>
                </div>
              </header>
              <p>{t("previousMatchesNote")}</p>
              {match.familiarity && coverage && (
                <p>
                  {familiarityCopy}
                  {coverage.status === "complete" &&
                  match.familiarity.status === "Available"
                    ? ` ${t("familiarityPercent", { value: match.familiarity.familiarityPercentage })}`
                    : ""}
                </p>
              )}
              {coverage && coverage.status !== "complete" && (
                <p className="warning-banner" role="status">
                  {t("incompleteWarning")}
                </p>
              )}
              {relationships === null ? (
                <p>{t("noAnalysis")}</p>
              ) : pairs.length === 0 ? (
                <p>
                  {coverage?.status === "complete"
                    ? t("noMatchesFound")
                    : t("insufficient")}
                </p>
              ) : (
                <ul className="analysis-relationships">
                  {pairs.map((pair) => (
                    <li key={pair.key}>
                      <div>
                        <strong>{pair.first.label}</strong>
                        <span aria-hidden="true"> ↔ </span>
                        <strong>{pair.second.label}</strong>
                      </div>
                      <dl>
                        <div>
                          <dt>{t("encounters")}</dt>
                          <dd>{pair.matchesTogether}</dd>
                        </div>
                        <div>
                          <dt>{t("allies")}</dt>
                          <dd>{pair.allyMatches}</dd>
                        </div>
                        <div>
                          <dt>{t("rivals")}</dt>
                          <dd>{pair.enemyMatches}</dd>
                        </div>
                      </dl>
                    </li>
                  ))}
                </ul>
              )}
            </section>
            <section aria-labelledby="match-groups-title">
              <header className="analysis-section-heading">
                <div>
                  <p className="eyebrow">{t("cautious")}</p>
                  <h2 id="match-groups-title">{t("historicalMatches")}</h2>
                </div>
              </header>
              <p>{t("groupsDisclaimer")}</p>
              {match.premadeGroups.length ? (
                <ul className="analysis-groups">
                  {match.premadeGroups.map((group) => (
                    <li key={`${group.teamId}-${group.groupNumber}`}>
                      <strong>
                        {group.teamId === 100
                          ? common("blueTeam")
                          : group.teamId === 200
                            ? common("redTeam")
                            : t("team")}
                      </strong>
                      <span>
                        {group.classification === "LikelyPremade"
                          ? t("strongEvidence")
                          : t("historicalMatches")}
                      </span>
                      <div>
                        {group.members.map((member, i) => (
                          <span key={member.puuid}>
                            {i > 0 && " · "}
                            <PlayerProfileLink
                              compact
                              gameName={member.gameName}
                              tagLine={member.tagLine}
                              platformRegion={platformRegion}
                            />
                          </span>
                        ))}
                      </div>
                      {presentMatchRelations(
                        (relationships ?? []).filter(
                          (r) =>
                            group.members.some(
                              (m) => m.puuid === r.firstPuuid,
                            ) &&
                            group.members.some(
                              (m) => m.puuid === r.secondPuuid,
                            ),
                        ),
                        match.teams,
                        common("unidentifiedPlayer"),
                      ).map((pair) => (
                        <p key={pair.key}>
                          {pair.first.label} y {pair.second.label}:{" "}
                          {pair.allyMatches} {t("allyHistory")} ·{" "}
                          {pair.enemyMatches} {t("rivalHistory")}
                        </p>
                      ))}
                    </li>
                  ))}
                </ul>
              ) : (
                <p>
                  {coverage?.status === "complete"
                    ? t("noGroups")
                    : t("insufficientGroups")}
                </p>
              )}
            </section>
            <section id="match-coverage" aria-labelledby="match-coverage-title">
              <header className="analysis-section-heading">
                <div>
                  <p className="eyebrow">{t("scope")}</p>
                  <h2 id="match-coverage-title">{t("coverage")}</h2>
                </div>
              </header>
              <p>{t("searchedBefore")}</p>
              {coverage ? (
                <details className="analysis-coverage-details">
                  <summary>{t("coverageDetail")}</summary>
                  <CoverageSummary
                    coverage={coverage}
                    title={t("priorHistory")}
                  />
                </details>
              ) : (
                <p>{t("historyNotRequested")}</p>
              )}
            </section>
          </>
        )}
      </div>
    </main>
  );
}
