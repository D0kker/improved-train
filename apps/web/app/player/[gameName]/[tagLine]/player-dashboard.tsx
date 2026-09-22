"use client";

import Link from "next/link";
import { useCallback, useEffect, useState } from "react";
import {
  ApiError,
  apiPath,
  fetchJson,
  type MatchDetail,
  type MatchesResponse,
  type PlayerEncounter,
  type PlayerLookup,
  type RelationshipsResponse,
  type PlayerSummary,
  playerLookupPath,
  matchDetailPath,
  playerRelationshipsPath,
  playerSyncPath,
} from "@/src/api";
import {
  formatDate,
  formatDuration,
  formatPercent,
  queueLabel,
  winRate,
} from "@/src/format";
import {
  familiarityMessage,
  knownPlayerCount,
  repeatedPlayerCount,
} from "@/src/echo";
import { PlayerProfileLink } from "@/src/player-profile-link";
import { ChampionIcon } from "@/src/champion-icon";
import { PlayerNetworkSection } from "./player-network-section";

interface PlayerDashboardProps {
  gameName: string;
  tagLine: string;
}

interface DashboardData {
  player: PlayerLookup;
  summary: PlayerSummary;
  encounters: PlayerEncounter[];
  matches: MatchesResponse;
  relationships: RelationshipsResponse;
  latestMatch: MatchDetail | null;
}

type LoadingStep = "lookup" | "sync" | "results";

const loadingCopy: Record<LoadingStep, string> = {
  lookup: "Resolviendo el Riot ID…",
  sync: "Sincronizando hasta 20 partidas…",
  results: "Construyendo el resumen histórico…",
};

export function PlayerDashboard({ gameName, tagLine }: PlayerDashboardProps) {
  const [data, setData] = useState<DashboardData | null>(null);
  const [step, setStep] = useState<LoadingStep>("lookup");
  const [error, setError] = useState<string | null>(null);
  const [warning, setWarning] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

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
        if (synchronize) {
          setStep("sync");
          try {
            await fetchJson<unknown>(playerSyncPath(player.puuid, 20), {
              method: "POST",
              signal,
            });
          } catch (syncError) {
            if (signal?.aborted) return;
            syncWarning = messageFor(
              syncError,
              "No se pudo actualizar el historial.",
            );
          }
        }

        setStep("results");
        const [summary, encounterResponse, matches, relationships] =
          await Promise.all([
            fetchJson<PlayerSummary>(
              apiPath("players", player.puuid, "summary"),
              {
                signal,
              },
            ),
            fetchJson<PlayerEncounter[] | { items: PlayerEncounter[] }>(
              apiPath("players", player.puuid, "known-players"),
              { signal },
            ),
            fetchJson<MatchesResponse>(
              `${apiPath("players", player.puuid, "matches")}?page=1&pageSize=10`,
              { signal },
            ),
            fetchJson<RelationshipsResponse>(
              playerRelationshipsPath(player.puuid),
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
            if (signal?.aborted) return;
            syncWarning ??= messageFor(
              latestMatchError,
              "No se pudo cargar la familiaridad de la última partida.",
            );
          }
        }

        setData({
          player,
          summary,
          encounters: Array.isArray(encounterResponse)
            ? encounterResponse
            : encounterResponse.items,
          matches,
          relationships,
          latestMatch,
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
      () => void load(false, controller.signal),
      0,
    );
    return () => {
      window.clearTimeout(request);
      controller.abort();
    };
  }, [load]);

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
              {isLoading ? "Actualizando…" : "Actualizar 20 partidas"}
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
            <SummaryCards encounters={data.encounters} summary={data.summary} />
            <LatestMatch
              match={data.matches.items[0] ?? null}
              matchDetail={data.latestMatch}
              ownerPuuid={data.player.puuid}
            />
            <Encounters encounters={data.encounters} />
            <MatchHistory
              matches={data.matches}
              ownerPuuid={data.player.puuid}
            />
            <Relationships relationships={data.relationships} />
            <details className="experimental-section">
              <summary>
                <span>
                  <strong>Mapa de conexiones</strong>
                  <small>Experimental · funcional, sin desarrollo activo</small>
                </span>
                <span aria-hidden="true">+</span>
              </summary>
              <PlayerNetworkSection puuid={data.player.puuid} />
            </details>
          </>
        ) : null}
      </div>
    </main>
  );
}

function Relationships({
  relationships,
}: {
  relationships: RelationshipsResponse;
}) {
  return (
    <section aria-labelledby="relationships-title">
      <div className="section-title-row">
        <div>
          <p className="eyebrow">Relaciones históricas</p>
          <h2 id="relationships-title">Posibles conexiones recurrentes</h2>
        </div>
        <p className="section-note">
          Confidence es una heurística, no una probabilidad ni confirmación
          oficial.
        </p>
      </div>

      {relationships.items.length === 0 ? (
        <EmptyState
          title="Aún no hay relaciones suficientes"
          copy="Las relaciones aparecerán al coincidir jugadores en las partidas terminadas almacenadas."
        />
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Jugador</th>
                <th>Score</th>
                <th>Evidencia</th>
                <th>Mismo equipo</th>
                <th>Recientes</th>
                <th>Consecutivas</th>
                <th>Inferencia</th>
              </tr>
            </thead>
            <tbody>
              {relationships.items.map((relationship) => (
                <tr key={relationship.otherPlayerPuuid}>
                  <td>
                    <PlayerProfileLink
                      gameName={relationship.gameName}
                      tagLine={relationship.tagLine}
                    />
                  </td>
                  <td>
                    <strong>{relationship.relationshipScore}/100</strong>
                    <span>{relationship.relationshipConfidence}</span>
                  </td>
                  <td>
                    {relationship.matchesTogether} partidas
                    <span>{relationship.oppositeTeamMatches} como rivales</span>
                  </td>
                  <td>
                    {formatPercent(relationship.sameTeamRatio * 100)}
                    <span>{relationship.sameTeamMatches} partidas</span>
                  </td>
                  <td>{relationship.recentMatchesTogether}</td>
                  <td>{relationship.consecutiveMatches}</td>
                  <td>
                    {relationship.premadeLabel ? (
                      <span
                        className={`inference-badge ${relationship.premadeLabel === "likely premade" ? "likely" : "possible"}`}
                      >
                        {relationship.premadeLabel}
                      </span>
                    ) : (
                      <span>Evidencia insuficiente</span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
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
        <p>La sincronización está limitada a 20 partidas por solicitud.</p>
      </div>
    </section>
  );
}

function SummaryCards({
  encounters,
  summary,
}: {
  encounters: PlayerEncounter[];
  summary: PlayerSummary;
}) {
  const cards = [
    ["Partidas analizadas", summary.matchesAnalyzed.toLocaleString("es-PA")],
    [
      "Jugadores conocidos",
      knownPlayerCount(encounters).toLocaleString("es-PA"),
    ],
    [
      "Jugadores recurrentes",
      repeatedPlayerCount(encounters).toLocaleString("es-PA"),
    ],
  ];

  return (
    <section aria-labelledby="summary-title">
      <div className="section-title-row">
        <div>
          <p className="eyebrow">ECHO overview</p>
          <h2 id="summary-title">Tu historial de encuentros</h2>
        </div>
      </div>
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
}: {
  match: MatchesResponse["items"][number] | null;
  matchDetail: MatchDetail | null;
  ownerPuuid: string;
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
              ? familiarityMessage(matchDetail.familiarity)
              : "Familiaridad no disponible."}
          </strong>
          <span>{formatDate(match.gameStartTimestamp)}</span>
        </div>
        <span className="latest-match-action">Ver partida →</span>
      </Link>
    </section>
  );
}

function Encounters({ encounters }: { encounters: PlayerEncounter[] }) {
  return (
    <section aria-labelledby="encounters-title">
      <div className="section-title-row">
        <div>
          <p className="eyebrow">Known players</p>
          <h2 id="encounters-title">Jugadores que ya habías visto</h2>
        </div>
        <p className="section-note">
          Incluye desde el primer encuentro; recurrente significa 2 o más.
        </p>
      </div>

      {encounters.length === 0 ? (
        <EmptyState
          title="Aún no hay jugadores conocidos"
          copy="Cuando otro jugador aparezca en una partida analizada, lo verás aquí."
        />
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Jugador</th>
                <th>Encuentros</th>
                <th>Aliado</th>
                <th>Rival</th>
                <th>WR juntos</th>
                <th>WR en contra</th>
                <th>Última vez</th>
              </tr>
            </thead>
            <tbody>
              {encounters.map((encounter) => (
                <tr key={encounter.otherPlayerPuuid}>
                  <td>
                    <PlayerProfileLink
                      gameName={encounter.gameName}
                      tagLine={encounter.tagLine}
                    />
                  </td>
                  <td>{encounter.totalMatches}</td>
                  <td>{encounter.sameTeamMatches}</td>
                  <td>{encounter.enemyTeamMatches}</td>
                  <td>
                    {winRate(encounter.winsTogether, encounter.sameTeamMatches)}
                    <span>
                      {encounter.winsTogether}V · {encounter.lossesTogether}D
                    </span>
                  </td>
                  <td>
                    {winRate(encounter.winsAgainst, encounter.enemyTeamMatches)}
                    <span>
                      {encounter.winsAgainst}V · {encounter.lossesAgainst}D
                    </span>
                  </td>
                  <td>{formatDate(encounter.lastSeenAt)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}

function MatchHistory({
  matches,
  ownerPuuid,
}: {
  matches: MatchesResponse;
  ownerPuuid: string;
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
