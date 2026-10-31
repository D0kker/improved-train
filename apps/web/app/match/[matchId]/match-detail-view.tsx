"use client";

import Link from "next/link";
import { useCallback, useEffect, useRef, useState } from "react";
import {
  ApiError,
  apiPath,
  fetchJson,
  analyzeSelectedMatch,
  type MatchDetail,
  type MatchAnalysisResult,
  type AnalysisCoverage,
} from "@/src/api";
import { formatDuration, queueLabel } from "@/src/format";
import { formatDashboardDate } from "@/src/dashboard-presentation";
import { coverageTitle, coverageDescription } from "@/src/coverage";
import { CoverageSummary } from "@/src/coverage-summary";
import { ChampionIcon } from "@/src/champion-icon";
import { PlayerProfileLink } from "@/src/player-profile-link";
import { SiteHeader } from "@/src/site-header";
import { Button, Card } from "@/src/design-system";
import { platformRegionName } from "@/src/platform-region";
import { familiarityMessage } from "@/src/echo";
import { useRequestLifetime } from "@/src/use-request-lifetime";
import { presentMatchRelations } from "@/src/match-relations";

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
        setError(
          requestError instanceof ApiError
            ? requestError.message
            : "No se pudo cargar la partida. Comprueba la conexión e inténtalo de nuevo.",
        );
      } finally {
        if (!signal.aborted) setIsLoading(false);
      }
    },
    [matchId, ownerPuuid, platformRegion, beginRequest],
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
      ? presentMatchRelations(relationships, match.teams)
      : [];
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
          {returnProfile ? "← Volver al perfil" : "← Volver a la búsqueda"}
        </Link>
        {isLoading && !match && (
          <section className="loading-panel" role="status">
            <span className="spinner" aria-hidden="true" />
            <div>
              <h1>
                {ownerPuuid
                  ? "Analizando partida e historial anterior…"
                  : "Cargando participantes…"}
              </h1>
              <p>La consulta puede tardar unos momentos.</p>
            </div>
          </section>
        )}
        {error && (
          <section className="message-panel error-panel" role="alert">
            <div>
              {match ? <h2>{error}</h2> : <h1>{error}</h1>}
              <p>
                {match && isStale
                  ? "Se conserva el resultado de la consulta anterior."
                  : "Puedes volver al perfil o a la búsqueda e intentarlo de nuevo."}
              </p>
            </div>
            <Button
              variant="secondary"
              onClick={() => void load()}
              disabled={isLoading}
            >
              Reintentar
            </Button>
          </section>
        )}
        {match && (
          <>
            {isLoading && (
              <p role="status" className="warning-banner">
                Actualizando el análisis…
              </p>
            )}
            {isStale && (
              <p className="warning-banner" role="status">
                Se conserva la información de la consulta anterior.
              </p>
            )}
            <section
              aria-labelledby="match-summary-title"
              className="match-summary-section"
            >
              <header className="analysis-section-heading">
                <div>
                  <p className="eyebrow">Análisis de partida</p>
                  <h1 id="match-summary-title">Resumen de partida</h1>
                </div>
                {coverage ? (
                  <a
                    className={`analysis-coverage analysis-coverage--${coverage.status}`}
                    href="#match-coverage"
                  >
                    {coverageTitle(coverage.status)}
                  </a>
                ) : (
                  <span className="analysis-coverage">
                    Análisis histórico no solicitado
                  </span>
                )}
              </header>
              {coverage && coverage.status !== "complete" && (
                <p role="status" className="warning-banner">
                  {coverageDescription(coverage.status)} Las coincidencias
                  disponibles pueden estar incompletas.
                </p>
              )}
              <Card className="analysis-match-summary">
                <div className="analysis-owner">
                  {owner && (
                    <ChampionIcon
                      championId={owner.championId}
                      championName={
                        owner.championName || "Campeón no disponible"
                      }
                    />
                  )}
                  <div>
                    <strong>
                      {owner
                        ? owner.championName || "Campeón no disponible"
                        : "Jugador consultado no disponible"}
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
                        ? "Victoria"
                        : owner?.win === false
                          ? "Derrota"
                          : "Resultado no disponible"}
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
                    <span>Duración</span>
                    <strong>
                      {typeof match.gameDurationSeconds === "number" &&
                      Number.isFinite(match.gameDurationSeconds)
                        ? formatDuration(match.gameDurationSeconds)
                        : "—"}
                    </strong>
                  </div>
                </div>
                <p>
                  {queueLabel(match.queueId)} ·{" "}
                  {formatDashboardDate(match.gameStartTimestamp)}
                  {platformRegion
                    ? ` · ${platformRegionName(platformRegion)}`
                    : ""}
                </p>
              </Card>
            </section>
            <section aria-labelledby="match-teams-title">
              <header className="analysis-section-heading">
                <div>
                  <p className="eyebrow">Participantes</p>
                  <h2 id="match-teams-title">Equipos y participantes</h2>
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
                          ? "Victoria"
                          : "Derrota"
                        : "Resultado no disponible";
                    return (
                      <section
                        className={`analysis-team analysis-team--${team.teamId === 100 ? "blue" : team.teamId === 200 ? "red" : "neutral"}`}
                        key={team.teamId}
                        aria-label={
                          team.teamId === 100
                            ? "Equipo azul"
                            : team.teamId === 200
                              ? "Equipo rojo"
                              : `Equipo ${index + 1}`
                        }
                      >
                        <header>
                          <h3>
                            {team.teamId === 100
                              ? "Equipo azul"
                              : team.teamId === 200
                                ? "Equipo rojo"
                                : `Equipo ${index + 1}`}
                          </h3>
                          <span>{result}</span>
                        </header>
                        {team.participants.length ? (
                          <ol>
                            {team.participants.map((p, i) => (
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
                                    p.championName || "Campeón no disponible"
                                  }
                                />
                                <div className="analysis-participant">
                                  <strong>
                                    {p.championName || "Campeón no disponible"}
                                  </strong>
                                  <PlayerProfileLink
                                    gameName={p.gameName}
                                    tagLine={p.tagLine}
                                    platformRegion={platformRegion}
                                    compact
                                  />
                                  {p.puuid && p.puuid === ownerPuuid && (
                                    <span>Jugador consultado</span>
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
                            ))}
                          </ol>
                        ) : (
                          <p>No hay participantes disponibles.</p>
                        )}
                      </section>
                    );
                  })}
                </div>
              ) : (
                <p>No hay equipos disponibles.</p>
              )}
            </section>
            <section aria-labelledby="match-relationships-title">
              <header className="analysis-section-heading">
                <div>
                  <p className="eyebrow">Evidencia anterior</p>
                  <h2 id="match-relationships-title">Relaciones históricas</h2>
                </div>
              </header>
              <p>
                Coincidencias en las partidas anteriores disponibles; no
                representan todo el historial.
              </p>
              {match.familiarity && coverage && (
                <p>
                  {familiarityMessage(match.familiarity, coverage.status)}
                  {coverage.status === "complete" &&
                  match.familiarity.status === "Available"
                    ? ` Familiaridad: ${match.familiarity.familiarityPercentage}%.`
                    : ""}
                </p>
              )}
              {coverage && coverage.status !== "complete" && (
                <p className="warning-banner" role="status">
                  La información es incompleta; la ausencia de coincidencias no
                  es concluyente.
                </p>
              )}
              {relationships === null ? (
                <p>No hay análisis histórico disponible para esta partida.</p>
              ) : pairs.length === 0 ? (
                <p>
                  {coverage?.status === "complete"
                    ? "No se encontraron coincidencias en las partidas disponibles."
                    : "No hay información suficiente para confirmar coincidencias."}
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
                          <dt>Encuentros</dt>
                          <dd>{pair.matchesTogether}</dd>
                        </div>
                        <div>
                          <dt>Aliados</dt>
                          <dd>{pair.allyMatches}</dd>
                        </div>
                        <div>
                          <dt>Rivales</dt>
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
                  <p className="eyebrow">Interpretación prudente</p>
                  <h2 id="match-groups-title">
                    Grupos con coincidencias históricas
                  </h2>
                </div>
              </header>
              <p>
                La recurrencia no confirma que los jugadores sean premades ni
                que hayan entrado juntos.
              </p>
              {match.premadeGroups.length ? (
                <ul className="analysis-groups">
                  {match.premadeGroups.map((group) => (
                    <li key={`${group.teamId}-${group.groupNumber}`}>
                      <strong>
                        {group.teamId === 100
                          ? "Equipo azul"
                          : group.teamId === 200
                            ? "Equipo rojo"
                            : "Equipo"}
                      </strong>
                      <span>
                        {group.classification === "LikelyPremade"
                          ? "Evidencia histórica alta"
                          : "Coincidencias históricas"}
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
                      ).map((pair) => (
                        <p key={pair.key}>
                          {pair.first.label} y {pair.second.label}:{" "}
                          {pair.allyMatches} como aliados · {pair.enemyMatches}{" "}
                          como rivales
                        </p>
                      ))}
                    </li>
                  ))}
                </ul>
              ) : (
                <p>
                  {coverage?.status === "complete"
                    ? "No se identificaron grupos en la muestra disponible."
                    : "No hay información suficiente para identificar grupos."}
                </p>
              )}
            </section>
            <section id="match-coverage" aria-labelledby="match-coverage-title">
              <header className="analysis-section-heading">
                <div>
                  <p className="eyebrow">Alcance del análisis</p>
                  <h2 id="match-coverage-title">Cobertura y transparencia</h2>
                </div>
              </header>
              <p>
                Se consultan partidas anteriores a la seleccionada. La muestra
                no representa todo el historial.
              </p>
              {coverage ? (
                <details className="analysis-coverage-details">
                  <summary>Ver detalle de cobertura</summary>
                  <CoverageSummary
                    coverage={coverage}
                    title="Historial anterior a esta partida"
                  />
                </details>
              ) : (
                <p>
                  El análisis histórico no se solicitó. Abre la partida desde el
                  historial de un jugador.
                </p>
              )}
            </section>
          </>
        )}
      </div>
    </main>
  );
}
