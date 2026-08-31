"use client";

import { useCallback, useEffect, useMemo, useState } from "react";

import {
  ApiError,
  fetchJson,
  type PlayerNetworkResponse,
  type RelationshipConfidence,
  playerNetworkPath,
} from "@/src/api";
import {
  filterNetwork,
  networkEdgeWidth,
  networkNodeRadius,
  positionNetworkNodes,
} from "@/src/network";
import { PlayerProfileLink } from "@/src/player-profile-link";

export function PlayerNetworkSection({ puuid }: { puuid: string }) {
  const [network, setNetwork] = useState<PlayerNetworkResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [minimumScore, setMinimumScore] = useState(0);
  const [minimumConfidence, setMinimumConfidence] =
    useState<RelationshipConfidence>("LOW");
  const [zoom, setZoom] = useState(1);
  const [pan, setPan] = useState({ x: 0, y: 0 });
  const [selectedPuuid, setSelectedPuuid] = useState<string | null>(null);

  const load = useCallback(
    async (signal?: AbortSignal) => {
      setIsLoading(true);
      setError(null);
      try {
        setNetwork(
          await fetchJson<PlayerNetworkResponse>(playerNetworkPath(puuid), {
            signal,
          }),
        );
      } catch (requestError) {
        if (!signal?.aborted) {
          setError(
            requestError instanceof ApiError || requestError instanceof Error
              ? requestError.message
              : "No pudimos cargar la red.",
          );
        }
      } finally {
        if (!signal?.aborted) setIsLoading(false);
      }
    },
    [puuid],
  );

  useEffect(() => {
    const controller = new AbortController();
    const request = window.setTimeout(() => void load(controller.signal), 0);
    return () => {
      window.clearTimeout(request);
      controller.abort();
    };
  }, [load]);

  const filtered = useMemo(
    () =>
      network
        ? filterNetwork(network, minimumScore, minimumConfidence)
        : { nodes: [], edges: [] },
    [minimumConfidence, minimumScore, network],
  );
  const positioned = useMemo(
    () => positionNetworkNodes(filtered.nodes, filtered.edges),
    [filtered],
  );
  const positions = new Map(positioned.map((node) => [node.puuid, node]));
  const selected = positioned.find((node) => node.puuid === selectedPuuid);

  function resetView() {
    setZoom(1);
    setPan({ x: 0, y: 0 });
    setSelectedPuuid(null);
  }

  return (
    <section aria-labelledby="network-title">
      <div className="section-title-row">
        <div>
          <p className="eyebrow">Red histórica</p>
          <h2 id="network-title">Mapa de conexiones</h2>
        </div>
        <p className="section-note">Profundidad 1 · datos locales acotados</p>
      </div>

      {isLoading ? <p role="status">Cargando red…</p> : null}
      {error ? (
        <div className="network-message" role="alert">
          <span>{error}</span>
          <button
            className="secondary-button"
            onClick={() => void load()}
            type="button"
          >
            Reintentar
          </button>
        </div>
      ) : null}
      {network ? (
        <>
          <div className="network-filters">
            <label>
              Score mínimo: <strong>{minimumScore}</strong>
              <input
                max="100"
                min="0"
                onChange={(event) =>
                  setMinimumScore(Number(event.target.value))
                }
                type="range"
                value={minimumScore}
              />
            </label>
            <label>
              Confianza mínima
              <select
                onChange={(event) =>
                  setMinimumConfidence(
                    event.target.value as RelationshipConfidence,
                  )
                }
                value={minimumConfidence}
              >
                <option value="LOW">LOW</option>
                <option value="MEDIUM">MEDIUM</option>
                <option value="HIGH">HIGH</option>
                <option value="VERY_HIGH">VERY HIGH</option>
              </select>
            </label>
          </div>

          {network.metadata.truncated ? (
            <p className="warning-banner" role="status">
              Red truncada: se muestran hasta {network.metadata.appliedMaxNodes}{" "}
              nodos y {network.metadata.appliedMaxEdges} conexiones de{" "}
              {network.metadata.totalAvailableNodes} nodos disponibles.
            </p>
          ) : null}

          {filtered.edges.length === 0 ? (
            <div className="empty-state">
              <h3>No hay conexiones con estos filtros</h3>
              <p>
                Reduce el score o la confianza mínima para ampliar la tabla y el
                mapa.
              </p>
            </div>
          ) : (
            <div className="network-layout">
              <div className="network-canvas-panel">
                <div
                  className="network-toolbar"
                  aria-label="Controles del mapa"
                >
                  <button
                    onClick={() =>
                      setZoom((value) => Math.min(1.8, value + 0.2))
                    }
                    type="button"
                  >
                    Acercar
                  </button>
                  <button
                    onClick={() =>
                      setZoom((value) => Math.max(0.6, value - 0.2))
                    }
                    type="button"
                  >
                    Alejar
                  </button>
                  <button
                    onClick={() =>
                      setPan((value) => ({ ...value, x: value.x - 35 }))
                    }
                    type="button"
                  >
                    ←
                  </button>
                  <button
                    onClick={() =>
                      setPan((value) => ({ ...value, x: value.x + 35 }))
                    }
                    type="button"
                  >
                    →
                  </button>
                  <button
                    onClick={() =>
                      setPan((value) => ({ ...value, y: value.y - 35 }))
                    }
                    type="button"
                  >
                    ↑
                  </button>
                  <button
                    onClick={() =>
                      setPan((value) => ({ ...value, y: value.y + 35 }))
                    }
                    type="button"
                  >
                    ↓
                  </button>
                  <button onClick={resetView} type="button">
                    Restablecer
                  </button>
                </div>
                <svg
                  aria-label="Mapa visual de conexiones. La tabla siguiente contiene la misma información."
                  className="network-canvas"
                  role="img"
                  viewBox="0 0 800 500"
                >
                  <g transform={`translate(${pan.x} ${pan.y}) scale(${zoom})`}>
                    {filtered.edges.map((edge) => {
                      const source = positions.get(edge.sourcePuuid);
                      const target = positions.get(edge.targetPuuid);
                      return source && target ? (
                        <line
                          key={`${edge.sourcePuuid}:${edge.targetPuuid}`}
                          strokeWidth={networkEdgeWidth(edge.relationshipScore)}
                          x1={source.x}
                          x2={target.x}
                          y1={source.y}
                          y2={target.y}
                        />
                      ) : null;
                    })}
                    {positioned.map((node) => (
                      <g
                        className={`network-node${selectedPuuid === node.puuid ? " selected" : ""}`}
                        key={node.puuid}
                        onClick={() => setSelectedPuuid(node.puuid)}
                        role="button"
                        tabIndex={0}
                        transform={`translate(${node.x} ${node.y})`}
                        onKeyDown={(event) => {
                          if (event.key === "Enter" || event.key === " ") {
                            event.preventDefault();
                            setSelectedPuuid(node.puuid);
                          }
                        }}
                      >
                        <circle
                          r={networkNodeRadius(node.score, node.isCenter)}
                        />
                        <text textAnchor="middle" y="4">
                          {shortName(node.gameName)}
                        </text>
                      </g>
                    ))}
                  </g>
                </svg>
                <p className="network-legend">
                  Nodo mayor = score más alto. Línea más gruesa = relación más
                  fuerte. La tabla muestra valores exactos.
                </p>
                {selected ? (
                  <p className="network-selection" role="status">
                    Seleccionado:{" "}
                    <PlayerProfileLink
                      compact
                      gameName={selected.gameName}
                      tagLine={selected.tagLine}
                    />{" "}
                    · score {selected.score}
                  </p>
                ) : null}
              </div>

              <NetworkTable network={network} edges={filtered.edges} />
            </div>
          )}
        </>
      ) : null}
    </section>
  );
}

function NetworkTable({
  edges,
  network,
}: {
  edges: PlayerNetworkResponse["edges"];
  network: PlayerNetworkResponse;
}) {
  const nodes = new Map(network.nodes.map((node) => [node.puuid, node]));
  return (
    <div className="table-wrap network-table">
      <table>
        <caption>Conexiones equivalentes al mapa</caption>
        <thead>
          <tr>
            <th>Jugador</th>
            <th>Score</th>
            <th>Confianza</th>
            <th>Partidas</th>
            <th>Inferencia</th>
          </tr>
        </thead>
        <tbody>
          {edges.map((edge) => {
            const otherPuuid =
              edge.sourcePuuid === network.center.puuid
                ? edge.targetPuuid
                : edge.sourcePuuid;
            const node = nodes.get(otherPuuid);
            return node ? (
              <tr key={otherPuuid}>
                <td>
                  <PlayerProfileLink
                    gameName={node.gameName}
                    tagLine={node.tagLine}
                  />
                </td>
                <td>{edge.relationshipScore}/100</td>
                <td>{edge.relationshipConfidence}</td>
                <td>{edge.matchesTogether}</td>
                <td>{edge.premadeLabel ?? "Evidencia insuficiente"}</td>
              </tr>
            ) : null;
          })}
        </tbody>
      </table>
    </div>
  );
}

function shortName(gameName: string): string {
  return gameName.length > 12 ? `${gameName.slice(0, 11)}…` : gameName;
}
