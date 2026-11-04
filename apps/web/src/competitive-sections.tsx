"use client";
import Link from "next/link";
import { formatDashboardDate } from "./dashboard-presentation";
import { queueLabel } from "./format";
import { platformRegionName } from "./platform-region";
import { matchDetailPath } from "./api";
import { useState } from "react";
import { ChampionIcon } from "./champion-icon";
import { Card, StatCard, SectionHeader, Button } from "./design-system";
import {
  coverageStatus,
  loadedMatches,
  performance,
  relationships,
  type DashboardWindow,
} from "./competitive-dashboard";
import { CoverageSummary } from "./coverage-summary";
import { useLocale, useTranslations } from "next-intl";
const number = (n: number | null, locale: string) =>
  n === null ? "—" : n.toLocaleString(locale, { maximumFractionDigits: 2 });
export function CompetitivePerformance({
  windows,
}: {
  windows: DashboardWindow[];
}) {
  const t = useTranslations("dashboard");
  const locale = useLocale();
  const stats = performance(loadedMatches(windows)),
    status = coverageStatus(windows);
  return (
    <section aria-labelledby="performance-title">
      <SectionHeader title={t("performance")} titleId="performance-title" />
      {status !== "complete" && (
        <p className="warning-banner" role="status">
          {t("sampleMayBeIncomplete")}
        </p>
      )}
      <div className="competitive-performance">
        <Card className="competitive-ring-card">
          <svg
            viewBox="0 0 120 120"
            role="img"
            aria-label={`${t("winRate")}: ${stats.rate === null ? t("noValidPerformance") : number(stats.rate, locale) + "%"}; ${stats.wins} ${t("wins")}; ${stats.losses} ${t("losses")}`}
          >
            <circle cx="60" cy="60" r="48" className="ring-track" />
            <circle
              cx="60"
              cy="60"
              r="48"
              pathLength="100"
              className="ring-value"
              strokeDasharray={`${stats.rate ?? 0} 100`}
              transform="rotate(-90 60 60)"
            />
            <text x="60" y="62" textAnchor="middle">
              {stats.rate === null ? "—" : number(stats.rate, locale) + "%"}
            </text>
          </svg>
          <div className="competitive-ring-copy">
            <strong>{t("winRate")}</strong>
            <p>
              {stats.wins} {t("wins")} · {stats.losses} {t("losses")}
            </p>
            {stats.count > stats.wins + stats.losses && (
              <p>
                {t("unclassified")}: {stats.count - stats.wins - stats.losses}
              </p>
            )}
          </div>
        </Card>
        <div className="competitive-metrics competitive-performance-metrics">
          {[
            [t("analyzed"), stats.count],
            [t("wins"), stats.wins],
            [t("losses"), stats.losses],
            [t("averageKda"), stats.kda],
            [t("averageKills"), stats.kills],
            [t("averageDeaths"), stats.deaths],
            [t("averageAssists"), stats.assists],
          ].map(([label, value]) => (
            <StatCard
              key={String(label)}
              label={String(label)}
              value={number(value as number | null, locale)}
            />
          ))}
        </div>
      </div>
      <header className="competitive-subsection">
        <h3>{t("champions")}</h3>
        <p>{t("basedOnLoaded")}</p>
      </header>
      <div className="competitive-champions">
        {stats.champions.map((c) => (
          <Card key={c.id} className="competitive-champion-card">
            <ChampionIcon championId={c.id} championName={c.name} size={72} />
            <div className="competitive-champion-copy">
              <h4>{c.name}</h4>
              <p>
                {t("championSummary", {
                  count: c.count,
                  wins: c.wins,
                  losses: c.losses,
                })}
              </p>
              <p>
                {t("winRate")}: {number(c.rate, locale)}
                {c.rate !== null ? "%" : ""}
              </p>
            </div>
          </Card>
        ))}
      </div>
      {!stats.count && <p>{t("noValidPerformance")}</p>}
    </section>
  );
}
export function CompetitiveRelationships({
  windows,
  owner,
  region,
}: {
  windows: DashboardWindow[];
  owner: string;
  region?: string;
}) {
  const t = useTranslations("dashboard");
  const locale = useLocale();
  const [expanded, setExpanded] = useState(false),
    stats = relationships(windows, owner);
  const matches = new Map(
    loadedMatches(windows).map((match) => [match.riotMatchId, match]),
  );
  const regions = [
    "br1",
    "eun1",
    "euw1",
    "jp1",
    "kr",
    "la1",
    "la2",
    "na1",
    "oc1",
    "ph2",
    "ru",
    "sg2",
    "th2",
    "tr1",
    "tw2",
    "vn2",
  ];
  return (
    <section aria-labelledby="relationships-title">
      <SectionHeader title={t("relationships")} titleId="relationships-title" />
      <p className="recurrent-explanation">{t("recurrenceDisclaimer")}</p>
      {stats && stats.incompleteMatches > 0 && (
        <p className="warning-banner">
          {t("incompleteParticipants", { count: stats.incompleteMatches })}
        </p>
      )}
      {stats ? (
        <>
          <div className="competitive-metrics">
            {[
              [t("uniquePlayers"), stats.unique],
              [t("recurring"), stats.repeated],
              [t("participations"), stats.participations],
              [t("uniqueAllies"), stats.allies],
              [t("uniqueRivals"), stats.enemies],
            ].map(([label, value]) => (
              <StatCard
                key={String(label)}
                label={String(label)}
                value={String(value)}
              />
            ))}
          </div>
          <h3>{t("recurring")}</h3>
          <ul className="competitive-recurrent">
            {stats.players.slice(0, expanded ? undefined : 5).map((p) => (
              <li key={p.puuid} className="recurrent-player">
                <div className="recurrent-main">
                  <div className="recurrent-identity">
                    <strong>
                      {p.name?.trim() &&
                      p.tag?.trim() &&
                      region &&
                      regions.includes(region.toLowerCase()) ? (
                        <Link
                          href={`/player/${encodeURIComponent(p.name)}/${encodeURIComponent(p.tag)}?platformRegion=${encodeURIComponent(region)}`}
                        >
                          {p.name}
                          <span>#{p.tag}</span>
                        </Link>
                      ) : (
                        <>
                          {p.name || t("noIdentity")}
                          {p.tag ? <span>#{p.tag}</span> : null}
                        </>
                      )}
                    </strong>
                    {region && regions.includes(region.toLowerCase()) ? (
                      <small>{platformRegionName(region, locale)}</small>
                    ) : null}
                  </div>
                  <dl className="recurrent-mini-stats">
                    {[
                      [t("shared"), p.total],
                      [t("allies"), p.ally],
                      [t("rivals"), p.enemy],
                    ].map(([label, value]) => (
                      <div key={label}>
                        <dt>{label}</dt>
                        <dd>{value}</dd>
                      </div>
                    ))}
                  </dl>
                </div>
                {p.sharedMatches.length > 0 ? (
                  <details className="recurrent-shared">
                    <summary>
                      {t("viewMatches", { count: p.sharedMatches.length })}
                      <span className="sr-only">
                        {" "}
                        {t("ofPlayer", { name: p.name || t("noIdentity") })}
                        {p.tag ? `#${p.tag}` : ""}
                      </span>
                    </summary>
                    <ul>
                      {p.sharedMatches.map((m, index) => {
                        const match = matches.get(m.id);
                        return (
                          <li key={m.id}>
                            <div>
                              <strong>
                                {t("matchNumber", { number: index + 1 })}
                                {match
                                  ? ` · ${queueLabel(match.queueId, locale)}`
                                  : ""}
                              </strong>
                              {match ? (
                                <small>
                                  {formatDashboardDate(
                                    match.gameStartTimestamp,
                                    locale,
                                  )}
                                </small>
                              ) : null}
                            </div>
                            <span
                              className={`shared-side shared-side--${m.side}`}
                            >
                              {m.side === "ally"
                                ? t("allies")
                                : m.side === "enemy"
                                  ? t("rivals")
                                  : t("teamUnavailable")}
                            </span>
                            {regions.includes(m.platform.toLowerCase()) ? (
                              <Link
                                href={matchDetailPath(m.id, owner, m.platform)}
                                aria-label={t("analyzeSharedMatch", {
                                  number: index + 1,
                                  player: `${p.name || t("noIdentity")}${p.tag ? `#${p.tag}` : ""}`,
                                })}
                              >
                                {t("analyze")} <span aria-hidden="true">→</span>
                              </Link>
                            ) : (
                              <span>{t("analysisUnavailable")}</span>
                            )}
                          </li>
                        );
                      })}
                    </ul>
                  </details>
                ) : (
                  <p className="recurrent-no-evidence">{t("noEvidence")}</p>
                )}
              </li>
            ))}
          </ul>
          {!stats.players.length && <p>{t("noRecurring")}</p>}
          {stats.players.length > 5 && (
            <Button
              variant="secondary"
              aria-expanded={expanded}
              onClick={() => setExpanded(!expanded)}
            >
              {expanded ? t("less") : t("more")}
            </Button>
          )}
        </>
      ) : (
        <p className="warning-banner">{t("relationshipCoverageUnavailable")}</p>
      )}
    </section>
  );
}
export function CompetitiveCoverage({
  windows,
}: {
  windows: DashboardWindow[];
}) {
  const t = useTranslations("dashboard");
  return (
    <section aria-labelledby="transparency-title">
      <SectionHeader title={t("coverage")} titleId="transparency-title" />
      <p>
        {t("coverageLoadedSummary", {
          matches: loadedMatches(windows).length,
          windows: windows.length,
        })}
      </p>
      <p>{t("coverageScope")}</p>
      <p>{t("relationshipScope")}</p>
      <p>{t("windowScope")}</p>
      <details>
        <summary>{t("coverageDetails")}</summary>
        {windows.map((w) => (
          <CoverageSummary
            key={w.start}
            coverage={w.result.coverage}
            title={t("windowNumber", { start: w.start + 1, end: w.start + 5 })}
          />
        ))}
      </details>
    </section>
  );
}
