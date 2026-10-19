"use client";

import Link from "next/link";
import { useCallback, useEffect, useState } from "react";
import {
  ApiError,
  fetchJson,
  playerActiveGamePath,
  type ActiveGame,
  type ActiveGameParticipant,
} from "@/src/api";
import { formatDate, formatDuration, queueLabel } from "@/src/format";
import { PlayerProfileLink } from "@/src/player-profile-link";

export function LiveMatchView({ ownerPuuid }: { ownerPuuid?: string }) {
  const [game, setGame] = useState<ActiveGame | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  const load = useCallback(async () => {
    if (!ownerPuuid) {
      setError("No se indicó el jugador observado.");
      setLoading(false);
      return;
    }

    setLoading(true);
    setError(null);
    try {
      const activeGame = await fetchJson<ActiveGame | null>(
        playerActiveGamePath(ownerPuuid),
      );
      if (!activeGame) {
        setError("La partida en curso ya no está disponible.");
        setGame(null);
      } else {
        setGame(activeGame);
      }
    } catch (requestError) {
      setError(
        requestError instanceof ApiError || requestError instanceof Error
          ? requestError.message
          : "No pudimos cargar la partida en curso.",
      );
    } finally {
      setLoading(false);
    }
  }, [ownerPuuid]);

  useEffect(() => {
    const request = window.setTimeout(() => void load(), 0);
    return () => window.clearTimeout(request);
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
          ← Volver
        </button>

        {loading ? (
          <section className="loading-panel" role="status">
            <span className="spinner" aria-hidden="true" />
            <h2>Cargando jugadores de la partida…</h2>
          </section>
        ) : null}

        {error ? (
          <section className="message-panel error-panel" role="alert">
            <div>
              <p className="eyebrow">Partida en curso</p>
              <h1>{error}</h1>
              <p>La partida puede haber terminado mientras la consultábamos.</p>
            </div>
            <button className="secondary-button" onClick={() => void load()}>
              Actualizar
            </button>
          </section>
        ) : null}

        {game ? (
          <>
            <section className="match-heading">
              <div>
                <p className="eyebrow">Partida en curso</p>
                <h1>Jugadores actuales</h1>
                <p className="muted-copy">
                  {queueLabel(game.queueId)} · iniciada{" "}
                  {formatDate(game.gameStartTime)}
                </p>
              </div>
              <div className="duration-pill">
                <span>Transcurrido</span>
                <strong>{formatDuration(game.gameLengthSeconds)}</strong>
              </div>
            </section>

            <div className="live-teams-grid">
              {teams.map((team) => (
                <section
                  className={`live-team live-team-${team.teamId}`}
                  key={team.teamId}
                >
                  <header>
                    <p className="eyebrow">
                      {team.teamId === 100 ? "Equipo azul" : "Equipo rojo"}
                    </p>
                    <strong>{team.participants.length} jugadores</strong>
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
                      />
                    ))}
                  </ul>
                </section>
              ))}
            </div>
          </>
        ) : null}
      </div>
    </main>
  );
}

function LiveParticipant({
  isObserved,
  participant,
}: {
  isObserved: boolean;
  participant: ActiveGameParticipant;
}) {
  return (
    <li className={isObserved ? "live-observed-player" : undefined}>
      <div className="live-champion-icon">{participant.championId || "?"}</div>
      <div>
        {participant.gameName && participant.tagLine ? (
          <PlayerProfileLink
            gameName={participant.gameName}
            tagLine={participant.tagLine}
          />
        ) : (
          <strong>
            {participant.gameName ||
              (participant.summonerId
                ? `Invocador ${participant.summonerId.slice(0, 8)}…`
                : "Jugador en partida")}
          </strong>
        )}
        <span>
          {participant.tagLine
            ? `#${participant.tagLine} · `
            : participant.gameName
              ? "Riot ID no disponible · "
              : "Identidad no expuesta por Spectator-V5 · "}
          Campeón ID {participant.championId || "?"}
        </span>
      </div>
    </li>
  );
}
