"use client";

import Link from "next/link";
import { useCallback, useEffect, useState } from "react";
import {
  ApiError,
  apiPath,
  fetchJson,
  type MatchesResponse,
  type PlayerEncounter,
  type PlayerLookup,
  type RelationshipsResponse,
  type PlayerSummary,
  playerLookupPath,
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
import { PlayerProfileLink } from "@/src/player-profile-link";

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
    async (signal?: AbortSignal) => {
      setIsLoading(true);
      setError(null);
      setWarning(null);
      setStep("lookup");

      try {
        const player = await fetchJson<PlayerLookup>(
          playerLookupPath(gameName, tagLine),
          { signal },
        );

        setStep("sync");
        let syncWarning: string | null = null;
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
              apiPath("players", player.puuid, "encounters"),
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

        setData({
          player,
          summary,
          encounters: Array.isArray(encounterResponse)
            ? encounterResponse
            : encounterResponse.items,
          matches,
          relationships,
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
    const request = window.setTimeout(() => void load(controller.signal), 0);
    return () => {
      window.clearTimeout(request);
      controller.abort();
    };
  }, [load]);

  return (
    <main className="dashboard-shell">
      <header className="topbar">
        <Link className="wordmark" href="/">
          <span className="wordmark-icon">LN</span>
          <span>LoL Network Analyzer</span>
        </Link>
        <Link className="text-link" href="/">
          Nueva búsqueda
        </Link>
      </header>

      <div className="dashboard-content">
        <section className="player-heading">
          <div>
            <p className="eyebrow">Resumen del jugador</p>
            <h1>
              {data?.summary.gameName || gameName}
              <span>#{data?.summary.tagLine || tagLine}</span>
            </h1>
            <p className="muted-copy">
              Encuentros observados únicamente en partidas terminadas y
              almacenadas por el analizador.
            </p>
          </div>
          <button
            className="secondary-button"
            disabled={isLoading}
            onClick={() => void load()}
            type="button"
          >
            {isLoading ? "Actualizando…" : "Actualizar 20 partidas"}
          </button>
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
                {warning} Mostramos los datos locales disponibles.
              </p>
            ) : null}
            <SummaryCards summary={data.summary} />
            <Encounters encounters={data.encounters} />
            <Relationships relationships={data.relationships} />
            <MatchHistory matches={data.matches} />
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

function SummaryCards({ summary }: { summary: PlayerSummary }) {
  const cards = [
    ["Partidas analizadas", summary.matchesAnalyzed.toLocaleString("es-PA")],
    ["Victorias / derrotas", `${summary.wins} / ${summary.losses}`],
    ["Win rate", formatPercent(summary.winRate)],
    [
      "Jugadores encontrados",
      summary.uniquePlayersEncountered.toLocaleString("es-PA"),
    ],
    ["Jugadores repetidos", summary.repeatedPlayers.toLocaleString("es-PA")],
  ];

  return (
    <section aria-labelledby="summary-title">
      <div className="section-title-row">
        <div>
          <p className="eyebrow">Panorama</p>
          <h2 id="summary-title">Tu historial en números</h2>
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

function Encounters({ encounters }: { encounters: PlayerEncounter[] }) {
  return (
    <section aria-labelledby="encounters-title">
      <div className="section-title-row">
        <div>
          <p className="eyebrow">Coincidencias históricas</p>
          <h2 id="encounters-title">Jugadores recurrentes</h2>
        </div>
        <p className="section-note">
          Ordenados por encuentros; no implica que sean un grupo premade.
        </p>
      </div>

      {encounters.length === 0 ? (
        <EmptyState
          title="Aún no hay jugadores repetidos"
          copy="Cuando una persona aparezca en más de una partida analizada, la verás aquí."
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

function MatchHistory({ matches }: { matches: MatchesResponse }) {
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
              href={`/match/${encodeURIComponent(match.riotMatchId)}`}
              key={match.riotMatchId}
            >
              <span className="result-badge">
                {match.win ? "Victoria" : "Derrota"}
              </span>
              <div className="match-champion">
                <strong>{match.championName}</strong>
                <span>{queueLabel(match.queueId)}</span>
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
