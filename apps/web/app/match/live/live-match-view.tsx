"use client";

import Link from "next/link";
import { useCallback, useEffect, useRef, useState } from "react";
import {
  ApiError,
  fetchJson,
  playerActiveGamePath,
  type ActiveGame,
  type ActiveGameParticipant,
} from "@/src/api";
import { formatDate, formatDuration, queueLabel } from "@/src/format";
import { PlayerProfileLink } from "@/src/player-profile-link";
import { SiteHeader } from "@/src/site-header";
import { platformRegionLabel } from "@/src/platform-region";
import { useRequestLifetime } from "@/src/use-request-lifetime";

export function LiveMatchView({ ownerPuuid, platformRegion }: { ownerPuuid?: string; platformRegion?: string }) {
  const beginRequest = useRequestLifetime();
  const [game, setGame] = useState<ActiveGame | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [hasNoActiveGame, setHasNoActiveGame] = useState(false);
  const [isStale, setIsStale] = useState(false);
  const [checkedAt, setCheckedAt] = useState<string | null>(null);
  const requestController = useRef<AbortController | null>(null);
  const lastOwnerPuuid = useRef(ownerPuuid);

  const load = useCallback(async (parentSignal?: AbortSignal) => {
    requestController.current?.abort();
    const controller = new AbortController();
    requestController.current = controller;
    const signal = beginRequest(parentSignal ?? controller.signal);
    if (lastOwnerPuuid.current !== ownerPuuid) {
      lastOwnerPuuid.current = ownerPuuid;
      setGame(null);
      setCheckedAt(null);
      setIsStale(false);
      setHasNoActiveGame(false);
    }
    if (!ownerPuuid) {
      setError("No se indicó el jugador observado.");
      setLoading(false);
      return;
    }

    setLoading(true);
    setError(null);
    try {
      const activeGame = await fetchJson<ActiveGame | null>(
        playerActiveGamePath(ownerPuuid, platformRegion),
          { signal },
      );
      if (signal.aborted) return;
      if (!activeGame) {
        setHasNoActiveGame(true);
        setGame(null);
      } else {
        setHasNoActiveGame(false);
        setGame(activeGame);
      }
      setIsStale(false);
      setCheckedAt(new Date().toISOString());
    } catch (requestError) {
      if (!signal.aborted) {
        setIsStale(true);
        setError(requestError instanceof ApiError ? requestError.message : "No se pudo comprobar la partida en curso.");
      }
    } finally {
      if (!signal.aborted) setLoading(false);
    }
  }, [ownerPuuid, platformRegion, beginRequest]);

  useEffect(() => {
    let isCurrentEffect = true;
    queueMicrotask(() => {
      if (isCurrentEffect) void load();
    });
    return () => {
      isCurrentEffect = false;
      requestController.current?.abort();
    };
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
    <main className="dashboard-shell" id="main-content" tabIndex={-1}>
      <SiteHeader />

      <div className="dashboard-content detail-content">
        <Link className="back-link" href="/">← Volver a la búsqueda</Link>

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
              <p>{isStale && game ? "Se conserva la información de la consulta anterior." : "Puedes volver a comprobarlo."}</p>
            </div>
            <button className="secondary-button" disabled={loading} onClick={() => void load()}>
              Volver a comprobar
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
                <p className="muted-copy">Región: {platformRegionLabel(platformRegion)}</p>
              </div>
              <div className="duration-pill">
                <span>Transcurrido</span>
                <strong>{formatDuration(game.gameLengthSeconds)}</strong>
              </div>
            </section>
            <p className="profile-freshness">
              {isStale ? "Consulta anterior. " : ""}
              {checkedAt ? `Consultado: ${formatDate(checkedAt)}` : ""}
            </p>
            <div className="section-actions">
              <button className="secondary-button" disabled={loading} onClick={() => void load()} type="button">
                {loading ? "Comprobando…" : "Actualizar jugadores"}
              </button>
            </div>

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
                        platformRegion={platformRegion}
                      />
                    ))}
                  </ul>
                </section>
              ))}
            </div>
          </>
        ) : null}
        {hasNoActiveGame && !loading ? (
          <div className="empty-state">
            <h2>No hay una partida activa</h2>
            <p>Esta consulta no encontró una partida en curso para el jugador seleccionado.</p>
            <button className="secondary-button" onClick={() => void load()} type="button">Volver a comprobar</button>
          </div>
        ) : null}
      </div>
    </main>
  );
}

function LiveParticipant({
  isObserved,
  participant,
  platformRegion,
}: {
  isObserved: boolean;
  participant: ActiveGameParticipant;
  platformRegion?: string;
}) {
  return (
    <li className={isObserved ? "live-observed-player" : undefined}>
      <div className="live-champion-icon">{participant.championId || "?"}</div>
      <div>
        {participant.gameName && participant.tagLine ? (
          <PlayerProfileLink
            gameName={participant.gameName}
            tagLine={participant.tagLine}
            platformRegion={platformRegion}
          />
        ) : (
          <strong>{participant.gameName || "Jugador no identificado"}</strong>
        )}
        <span>
          {participant.tagLine
            ? `#${participant.tagLine} · `
            : "Riot ID no disponible · "}
          Campeón (ID) {participant.championId || "no disponible"}
        </span>
        {isObserved ? <span className="observed-badge">Jugador consultado</span> : null}
      </div>
    </li>
  );
}
