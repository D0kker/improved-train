"use client";

import { useRequestLifetime } from "@/src/use-request-lifetime";

import Link from "next/link";
import { useCallback, useEffect, useState } from "react";
import {
  ApiError,
  apiPath,
  fetchJson,
  analyzeSelectedMatch,
  type MatchDetail,
  type MatchKnownPlayer,
  type MatchPremadeGroup,
  type MatchParticipant,
} from "@/src/api";
import { formatDate, formatDuration, queueLabel } from "@/src/format";
import {
  groupCode,
  groupLabel,
  groupTone,
  groupsForParticipant,
} from "@/src/premade-groups";
import { ChampionIcon } from "@/src/champion-icon";
import { PlayerProfileLink } from "@/src/player-profile-link";
import { familiarityMessage, type HistoryAnalysisStatus } from "@/src/echo";

export function MatchDetailView({
  matchId,
  ownerPuuid,
  platformRegion,
}: {
  matchId: string;
  ownerPuuid?: string;
  platformRegion?: string;
}) {
  const beginRequest = useRequestLifetime();
  const [match, setMatch] = useState<MatchDetail | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [warning, setWarning] = useState<string | null>(null);
  const [isRefreshingHistory, setIsRefreshingHistory] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [historyStatus, setHistoryStatus] =
    useState<HistoryAnalysisStatus>("insufficient");

  const load = useCallback(
    async (signal?: AbortSignal) => {
      signal = beginRequest(signal);
      setIsLoading(true);
      setError(null);
      setWarning(null);
      setHistoryStatus("insufficient");
      try {
        if (ownerPuuid) {
          setIsRefreshingHistory(true);
          const result = await analyzeSelectedMatch(
            matchId,
            ownerPuuid,
            platformRegion,
            signal,
          );
          if (signal?.aborted) return;
          setMatch(result.match);
          setHistoryStatus(result.coverage.status);
          if (result.coverage.status !== "complete")
            setWarning(
              result.coverage.status === "error"
                ? "No se pudo recuperar el historial; la evidencia es insuficiente."
                : "Análisis limitado: faltan partidas elegibles o algunas consultas fallaron. Puede haber más coincidencias.",
            );
        } else {
          const loaded = await fetchJson<MatchDetail>(
            apiPath("matches", matchId) +
              (platformRegion
                ? `?platformRegion=${encodeURIComponent(platformRegion)}`
                : ""),
            { signal },
          );
          if (!signal?.aborted) setMatch(loaded);
        }
      } catch (requestError) {
        if (signal?.aborted) return;
        setError(
          requestError instanceof ApiError || requestError instanceof Error
            ? requestError.message
            : "No pudimos cargar esta partida.",
        );
      } finally {
        if (!signal?.aborted) {
          setIsRefreshingHistory(false);
          setIsLoading(false);
        }
      }
    },
    [matchId, ownerPuuid, platformRegion, beginRequest],
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
          <span className="wordmark-icon">E</span>
          <span>PROJECT ECHO</span>
        </Link>
        <Link className="text-link" href="/">
          Buscar jugador
        </Link>
      </header>

      <div className="dashboard-content detail-content">
        <button
          className="back-link"
          onClick={() => history.back()}
          type="button"
        >
          ← Volver al historial
        </button>

        {isLoading ? (
          <MatchLoading refreshingHistory={isRefreshingHistory} />
        ) : null}
        {error ? (
          <section className="message-panel error-panel" role="alert">
            <div>
              <p className="eyebrow">Detalle no disponible</p>
              <h1>{error}</h1>
              <p>La partida puede no estar guardada todavía.</p>
            </div>
            <button className="secondary-button" onClick={() => void load()}>
              Reintentar
            </button>
          </section>
        ) : null}

        {match ? (
          <>
            {warning ? (
              <p className="warning-banner" role="status">
                {warning}
              </p>
            ) : null}
            <section className="match-heading">
              <div>
                <p className="eyebrow">Detalle de partida</p>
                <h1>Resultado de la partida</h1>
                <p className="muted-copy">
                  {queueLabel(match.queueId)} ·{" "}
                  {formatDate(match.gameStartTimestamp)}
                </p>
              </div>
              <div className="duration-pill">
                <span>Duración</span>
                <strong>{formatDuration(match.gameDurationSeconds)}</strong>
              </div>
            </section>

            <FamiliaritySummary
              familiarity={match.familiarity}
              historyStatus={historyStatus}
            />

            <PremadeSummary
              groups={match.premadeGroups}
              knownPlayers={match.familiarity?.knownPlayerDetails ?? []}
            />

            <div className="teams-grid">
              {match.teams.map((team, index) => (
                <TeamCard
                  key={team.teamId}
                  participants={team.participants}
                  premadeGroups={match.premadeGroups.filter(
                    (group) => group.teamId === team.teamId,
                  )}
                  knownPlayers={match.familiarity?.knownPlayerDetails ?? []}
                  ownerPuuid={ownerPuuid}
                  teamId={team.teamId}
                  title={teamName(team.teamId, index)}
                />
              ))}
            </div>
          </>
        ) : null}
      </div>
    </main>
  );
}

function FamiliaritySummary({
  familiarity,
  historyStatus,
}: {
  familiarity: MatchDetail["familiarity"];
  historyStatus: HistoryAnalysisStatus;
}) {
  if (!familiarity) {
    return (
      <section className="familiarity-summary">
        <p className="eyebrow">Familiaridad del lobby</p>
        <h2>Contexto no disponible</h2>
        <p className="muted-copy">
          Abre la partida desde el historial de un jugador para comparar con su
          evidencia anterior.
        </p>
      </section>
    );
  }

  return (
    <section
      className="familiarity-summary"
      aria-labelledby="familiarity-title"
    >
      <div>
        <p className="eyebrow">Familiaridad</p>
        <h2 id="familiarity-title">
          {familiarityMessage(familiarity, historyStatus)}
        </h2>
        <p className="muted-copy">
          Solo usa partidas estrictamente anteriores.
        </p>
      </div>
      <div className="familiarity-value">
        <strong>
          {historyStatus === "complete" && familiarity.status === "Available"
            ? `${familiarity.familiarityPercentage}%`
            : "—"}
        </strong>
        <span>{familiarity.evaluablePlayers} jugadores evaluables</span>
        <span>{familiarity.knownPlayers} con antecedentes detectados</span>
      </div>
    </section>
  );
}

function MatchLoading({ refreshingHistory }: { refreshingHistory: boolean }) {
  return (
    <section className="loading-panel" role="status">
      <span className="spinner" aria-hidden="true" />
      <div>
        <p className="eyebrow">Partida</p>
        <h2>
          {refreshingHistory
            ? "Revisando el historial reciente…"
            : "Cargando participantes…"}
        </h2>
      </div>
    </section>
  );
}

function TeamCard({
  participants,
  premadeGroups,
  knownPlayers,
  ownerPuuid,
  teamId,
  title,
}: {
  participants: MatchParticipant[];
  premadeGroups: MatchPremadeGroup[];
  knownPlayers: MatchKnownPlayer[];
  ownerPuuid?: string;
  teamId: number;
  title: string;
}) {
  const won = participants.some((participant) => participant.win === true);
  return (
    <section className={`team-card team-${teamId}`}>
      <header>
        <div>
          <h2>{title}</h2>
        </div>
        <span className={won ? "team-result win" : "team-result loss"}>
          {won ? "Victoria" : "Derrota"}
        </span>
      </header>
      {participants.length === 0 ? (
        <p className="muted-copy">No hay participantes disponibles.</p>
      ) : (
        <ol className="participant-list">
          {participants.map((participant, index) => (
            <ParticipantRow
              key={participant.puuid || `${participant.championName}-${index}`}
              groups={groupsForParticipant(premadeGroups, participant.puuid)}
              participant={participant}
              isObserved={participant.puuid === ownerPuuid}
              history={knownPlayers.find(
                (player) => player.puuid === participant.puuid,
              )}
            />
          ))}
        </ol>
      )}
    </section>
  );
}

function ParticipantRow({
  groups,
  history,
  isObserved,
  participant,
}: {
  groups: MatchPremadeGroup[];
  history?: MatchKnownPlayer;
  isObserved: boolean;
  participant: MatchParticipant;
}) {
  return (
    <li className={isObserved ? "observed-player" : undefined}>
      <div className="participant-identity">
        <ChampionIcon
          championId={participant.championId}
          championName={participant.championName}
        />
        <div>
          <strong>{participant.championName}</strong>
          <PlayerProfileLink
            gameName={participant.gameName}
            tagLine={participant.tagLine}
          />
          {history ? <HistoryBadge player={history} /> : null}
          {groups.length > 0 ? (
            <span className="premade-memberships">
              {groups.map((group) => (
                <span
                  className={`premade-code tone-${groupTone(group.groupNumber)}`}
                  aria-label={`Grupo ${groupCode(group.groupNumber)}: ${groupLabel(group)}`}
                  key={group.groupNumber}
                  title={groupLabel(group)}
                >
                  {groupCode(group.groupNumber)}
                </span>
              ))}
            </span>
          ) : null}
        </div>
      </div>
      <p>
        <strong>
          {participant.kills} / {participant.deaths} / {participant.assists}
        </strong>
        <span>K / D / A</span>
      </p>
    </li>
  );
}

function PremadeSummary({
  groups,
  knownPlayers,
}: {
  groups: MatchPremadeGroup[];
  knownPlayers: MatchKnownPlayer[];
}) {
  return (
    <section className="premade-summary" aria-labelledby="premade-title">
      <div className="section-title-row">
        <div>
          <p className="eyebrow">Inferencia histórica</p>
          <h2 id="premade-title">Posibles premades en esta partida</h2>
        </div>
        <p className="section-note">No confirma que hayan entrado juntos.</p>
      </div>
      {groups.length === 0 ? (
        <p className="muted-copy">
          No hay evidencia suficiente para señalar un grupo recurrente.
        </p>
      ) : (
        <ul className="premade-group-list">
          {groups.map((group) => (
            <li key={group.groupNumber}>
              <span
                className={`premade-code tone-${groupTone(group.groupNumber)}`}
              >
                {groupCode(group.groupNumber)}
              </span>
              <div>
                <strong>{groupLabel(group)}</strong>
                <span>Equipo {group.teamId}</span>
                <span className="premade-member-links">
                  {group.members.map((member) => (
                    <PlayerProfileLink
                      compact
                      gameName={member.gameName}
                      key={member.puuid}
                      tagLine={member.tagLine}
                    />
                  ))}
                </span>
              </div>
            </li>
          ))}
        </ul>
      )}
      {knownPlayers.length > 0 ? (
        <div className="history-evidence">
          <strong>Jugadores conocidos</strong>
          <div className="history-evidence-grid">
            {knownPlayers.map((player) => (
              <article className="history-evidence-card" key={player.puuid}>
                <PlayerProfileLink
                  compact
                  gameName={player.gameName}
                  tagLine={player.tagLine}
                />
                <HistoryBadge player={player} />
                <span>
                  {player.allyMatches} como equipo · {player.enemyMatches} como
                  rival
                </span>
              </article>
            ))}
          </div>
        </div>
      ) : null}
    </section>
  );
}

function HistoryBadge({ player }: { player: MatchKnownPlayer }) {
  const playedTogether = player.allyMatches > 0;
  const playedAgainst = player.enemyMatches > 0;
  const label =
    playedTogether && playedAgainst
      ? "Aliado y rival"
      : playedTogether
        ? "Ya jugó junto"
        : "Ya jugó en contra";
  const tone =
    playedTogether && playedAgainst
      ? "mixed"
      : playedTogether
        ? "ally"
        : "opponent";

  return (
    <span
      className={`history-badge ${tone}`}
      title={`${label}: ${player.winsAgainst}V / ${player.lossesAgainst}D contra el owner`}
    >
      {label}
    </span>
  );
}

function teamName(teamId: number, index: number): string {
  if (teamId === 100) return "Equipo azul";
  if (teamId === 200) return "Equipo rojo";
  return `Equipo ${index + 1}`;
}
