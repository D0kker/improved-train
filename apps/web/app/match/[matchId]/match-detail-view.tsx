"use client";

import Link from "next/link";
import { useCallback, useEffect, useState } from "react";
import {
  ApiError,
  apiPath,
  fetchJson,
  playerSyncPath,
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
import { familiarityMessage, latestMatchSyncPlan } from "@/src/echo";

export function MatchDetailView({
  matchId,
  ownerPuuid,
}: {
  matchId: string;
  ownerPuuid?: string;
}) {
  const [match, setMatch] = useState<MatchDetail | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [warning, setWarning] = useState<string | null>(null);
  const [isRefreshingHistory, setIsRefreshingHistory] = useState(false);
  const [isLoading, setIsLoading] = useState(true);

  const load = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    setWarning(null);
    try {
      const path = ownerPuuid
        ? `${apiPath("matches", matchId)}?ownerPuuid=${encodeURIComponent(ownerPuuid)}`
        : apiPath("matches", matchId);
      let loadedMatch = await fetchJson<MatchDetail>(path);
      if (ownerPuuid) {
        const syncPlan = latestMatchSyncPlan(loadedMatch, ownerPuuid);
        let failedTargets = 0;
        setIsRefreshingHistory(syncPlan.length > 0);
        for (const target of syncPlan) {
          try {
            await fetchJson<unknown>(
              playerSyncPath(target.puuid, target.count),
              {
                method: "POST",
              },
            );
          } catch {
            failedTargets += 1;
          }
        }
        setIsRefreshingHistory(false);
        if (failedTargets > 0) {
          setWarning(
            "No se pudo actualizar todo el historial; mostramos la evidencia local disponible.",
          );
        }
        if (failedTargets < syncPlan.length) {
          loadedMatch = await fetchJson<MatchDetail>(path);
        }
      }
      setMatch(loadedMatch);
    } catch (requestError) {
      setError(
        requestError instanceof ApiError || requestError instanceof Error
          ? requestError.message
          : "No pudimos cargar esta partida.",
      );
    } finally {
      setIsRefreshingHistory(false);
      setIsLoading(false);
    }
  }, [matchId, ownerPuuid]);

  useEffect(() => {
    const request = window.setTimeout(() => void load(), 0);
    return () => window.clearTimeout(request);
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

            {ownerPuuid ? (
              <ObservedPlayerBanner
                participant={match.teams
                  .flatMap((team) => team.participants)
                  .find((participant) => participant.puuid === ownerPuuid)}
              />
            ) : null}

            <FamiliaritySummary familiarity={match.familiarity} />

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
}: {
  familiarity: MatchDetail["familiarity"];
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
        <h2 id="familiarity-title">{familiarityMessage(familiarity)}</h2>
        <p className="muted-copy">
          Solo usa partidas estrictamente anteriores.
        </p>
      </div>
      <div className="familiarity-value">
        <strong>{familiarity.familiarityPercentage}%</strong>
        <span>{familiarity.unknownPlayers} no conocidos</span>
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
          <p className="eyebrow">Equipo {teamId}</p>
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
              index={index}
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
  index,
  isObserved,
  participant,
}: {
  groups: MatchPremadeGroup[];
  history?: MatchKnownPlayer;
  index: number;
  isObserved: boolean;
  participant: MatchParticipant;
}) {
  return (
    <li className={isObserved ? "observed-player" : undefined}>
      <span className="position-label">
        {positionName(participant.teamPosition, index)}
      </span>
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
          {isObserved ? (
            <span className="observed-player-badge">Jugador observado</span>
          ) : null}
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

function ObservedPlayerBanner({
  participant,
}: {
  participant?: MatchParticipant;
}) {
  return (
    <section className="observed-player-banner" aria-label="Jugador observado">
      <span className="observed-player-marker" aria-hidden="true">
        ◎
      </span>
      <div>
        <p className="eyebrow">Jugador observado</p>
        <strong>
          {participant?.gameName && participant.tagLine
            ? `${participant.gameName}#${participant.tagLine}`
            : "El jugador desde cuyo historial abriste esta partida"}
        </strong>
        <span>Su fila está resaltada dentro del equipo correspondiente.</span>
      </div>
    </section>
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

function positionName(position: string | undefined, index: number): string {
  const labels: Record<string, string> = {
    TOP: "TOP",
    JUNGLE: "JGL",
    MIDDLE: "MID",
    MID: "MID",
    BOTTOM: "ADC",
    UTILITY: "SUP",
    SUPPORT: "SUP",
  };
  return position
    ? (labels[position.toUpperCase()] ?? position)
    : `${index + 1}`;
}

function teamName(teamId: number, index: number): string {
  if (teamId === 100) return "Equipo azul";
  if (teamId === 200) return "Equipo rojo";
  return `Equipo ${index + 1}`;
}
