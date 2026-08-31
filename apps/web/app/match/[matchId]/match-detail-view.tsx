"use client";

import Link from "next/link";
import { useCallback, useEffect, useState } from "react";
import {
  ApiError,
  apiPath,
  fetchJson,
  type MatchDetail,
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

export function MatchDetailView({ matchId }: { matchId: string }) {
  const [match, setMatch] = useState<MatchDetail | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const load = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      setMatch(await fetchJson<MatchDetail>(apiPath("matches", matchId)));
    } catch (requestError) {
      setError(
        requestError instanceof ApiError || requestError instanceof Error
          ? requestError.message
          : "No pudimos cargar esta partida.",
      );
    } finally {
      setIsLoading(false);
    }
  }, [matchId]);

  useEffect(() => {
    const request = window.setTimeout(() => void load(), 0);
    return () => window.clearTimeout(request);
  }, [load]);

  return (
    <main className="dashboard-shell">
      <header className="topbar">
        <Link className="wordmark" href="/">
          <span className="wordmark-icon">LN</span>
          <span>LoL Network Analyzer</span>
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

        {isLoading ? <MatchLoading /> : null}
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
            <section className="match-heading">
              <div>
                <p className="eyebrow">Detalle de partida</p>
                <h1>{match.riotMatchId}</h1>
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

            <PremadeSummary groups={match.premadeGroups} />

            <div className="teams-grid">
              {match.teams.map((team, index) => (
                <TeamCard
                  key={team.teamId}
                  participants={team.participants}
                  premadeGroups={match.premadeGroups.filter(
                    (group) => group.teamId === team.teamId,
                  )}
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

function MatchLoading() {
  return (
    <section className="loading-panel" role="status">
      <span className="spinner" aria-hidden="true" />
      <div>
        <p className="eyebrow">Partida</p>
        <h2>Cargando participantes…</h2>
      </div>
    </section>
  );
}

function TeamCard({
  participants,
  premadeGroups,
  teamId,
  title,
}: {
  participants: MatchParticipant[];
  premadeGroups: MatchPremadeGroup[];
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
            />
          ))}
        </ol>
      )}
    </section>
  );
}

function ParticipantRow({
  groups,
  index,
  participant,
}: {
  groups: MatchPremadeGroup[];
  index: number;
  participant: MatchParticipant;
}) {
  return (
    <li>
      <span className="position-label">
        {positionName(participant.teamPosition, index)}
      </span>
      <div>
        <strong>{participant.championName}</strong>
        <span>{playerName(participant)}</span>
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
      <p>
        <strong>
          {participant.kills} / {participant.deaths} / {participant.assists}
        </strong>
        <span>K / D / A</span>
      </p>
    </li>
  );
}

function PremadeSummary({ groups }: { groups: MatchPremadeGroup[] }) {
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
                <span>
                  Equipo {group.teamId} ·{" "}
                  {group.members.map(memberName).join(", ")}
                </span>
              </div>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}

function memberName(member: MatchPremadeGroup["members"][number]): string {
  return member.tagLine
    ? `${member.gameName}#${member.tagLine}`
    : member.gameName || "Jugador identificado";
}

function playerName(participant: MatchParticipant): string {
  if (!participant.gameName) return "Jugador no identificado";
  return participant.tagLine
    ? `${participant.gameName}#${participant.tagLine}`
    : participant.gameName;
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
