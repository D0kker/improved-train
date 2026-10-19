"use client";

import { useRequestLifetime } from "@/src/use-request-lifetime";

import Link from "next/link";
import { useCallback, useEffect, useRef, useState } from "react";
import {
  ApiError,
  apiPath,
  fetchJson,
  analyzeSelectedMatch,
  type MatchDetail,
  type MatchKnownPlayer,
  type MatchPremadeGroup,
  type MatchParticipant,
  type AnalysisCoverage,
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
import { CoverageSummary } from "@/src/coverage-summary";
import { SiteHeader } from "@/src/site-header";
import { platformRegionLabel } from "@/src/platform-region";

export function MatchDetailView({
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
  const [error, setError] = useState<string | null>(null);
  const [coverage, setCoverage] = useState<AnalysisCoverage | null>(null);
  const [isStale, setIsStale] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [historyStatus, setHistoryStatus] =
    useState<HistoryAnalysisStatus>("insufficient");

  const hasPreviousMatch = useRef(false);
  const load = useCallback(
    async (signal?: AbortSignal) => {
      signal = beginRequest(signal);
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
          if (signal?.aborted) return;
          setMatch(result.match);
          hasPreviousMatch.current = true;
          setCoverage(result.coverage);
          setHistoryStatus(result.coverage.status);
          setIsStale(false);
        } else {
          const loaded = await fetchJson<MatchDetail>(
            apiPath("matches", matchId) +
              (platformRegion
                ? `?platformRegion=${encodeURIComponent(platformRegion)}`
                : ""),
            { signal },
          );
          if (!signal?.aborted) {
            setMatch(loaded);
            hasPreviousMatch.current = true;
            setCoverage(null);
            setIsStale(false);
          }
        }
      } catch (requestError) {
        if (signal?.aborted) return;
        setIsStale(true);
        setError(requestError instanceof ApiError ? requestError.message : "No se pudo cargar la partida. Comprueba la conexión e inténtalo de nuevo.");
      } finally {
        if (!signal?.aborted) {
          setIsLoading(false);
        }
      }
    },
    [matchId, ownerPuuid, platformRegion, beginRequest],
  );

  useEffect(() => {
    let isCurrentEffect = true;
    queueMicrotask(() => {
      if (isCurrentEffect) void load();
    });
    return () => {
      isCurrentEffect = false;
    };
  }, [load]);

  return (
    <main className="dashboard-shell" id="main-content" tabIndex={-1}>
      <SiteHeader />

      <div className="dashboard-content detail-content">
        <Link className="back-link" href={returnProfile
          ? `/player/${encodeURIComponent(returnProfile.gameName)}/${encodeURIComponent(returnProfile.tagLine)}${platformRegion ? `?platformRegion=${encodeURIComponent(platformRegion)}` : ""}`
          : "/"}>
          {returnProfile ? "← Volver al perfil" : "← Volver a la búsqueda"}
        </Link>

        {isLoading && !match ? <MatchLoading refreshingHistory={Boolean(ownerPuuid)} /> : null}
        {error ? (
          <section className="message-panel error-panel" role="alert">
            <div>
              <p className="eyebrow">Detalle no disponible</p>
              <h1>{error}</h1>
              <p>{isStale && match ? "Se conserva el resultado de la consulta anterior." : "Puedes volver al perfil o a la búsqueda e intentarlo de nuevo."}</p>
            </div>
            <button className="secondary-button" onClick={() => void load()}>
              Reintentar
            </button>
          </section>
        ) : null}

        {match ? (
          <>
            {isStale ? <p className="warning-banner" role="status">Se conserva la información de la consulta anterior.</p> : null}
            {coverage ? <CoverageSummary coverage={coverage} title="Historial anterior a esta partida" /> : null}
            <section className="match-heading">
              <div>
                <p className="eyebrow">Detalle de partida</p>
                <h1>Resultado de la partida</h1>
                <p className="muted-copy">
                  {queueLabel(match.queueId)} ·{" "}
                  {formatDate(match.gameStartTimestamp)}
                </p>
                {ownerPuuid && returnProfile ? <p className="muted-copy">Jugador consultado: {returnProfile.gameName} #{returnProfile.tagLine}</p> : null}
                {platformRegion ? <p className="muted-copy">Región: {platformRegionLabel(platformRegion)}</p> : null}
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
              platformRegion={platformRegion}
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
                  platformRegion={platformRegion}
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
          Este análisis utiliza partidas anteriores a la seleccionada para identificar encuentros y posibles grupos habituales. No incluye la partida seleccionada ni partidas posteriores.
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
  platformRegion,
}: {
  participants: MatchParticipant[];
  premadeGroups: MatchPremadeGroup[];
  knownPlayers: MatchKnownPlayer[];
  ownerPuuid?: string;
  teamId: number;
  title: string;
  platformRegion?: string;
}) {
  const outcomes = new Set(participants.map((participant) => participant.win).filter((value) => value !== undefined));
  const result = outcomes.size === 1 ? (outcomes.has(true) ? "win" : "loss") : "unknown";
  return (
    <section className={`team-card team-${teamId}`}>
      <header>
        <div>
          <h2>{title}</h2>
        </div>
        {result === "unknown" ? <span className="team-result">Resultado no disponible</span> : (
          <span className={`team-result ${result}`}>{result === "win" ? "Victoria" : "Derrota"}</span>
        )}
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
              platformRegion={platformRegion}
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
  platformRegion,
}: {
  groups: MatchPremadeGroup[];
  history?: MatchKnownPlayer;
  isObserved: boolean;
  participant: MatchParticipant;
  platformRegion?: string;
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
            platformRegion={platformRegion}
          />
          {isObserved ? <span className="observed-badge">Jugador consultado</span> : null}
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
  platformRegion,
}: {
  groups: MatchPremadeGroup[];
  knownPlayers: MatchKnownPlayer[];
  platformRegion?: string;
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
                      platformRegion={platformRegion}
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
                  platformRegion={platformRegion}
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
