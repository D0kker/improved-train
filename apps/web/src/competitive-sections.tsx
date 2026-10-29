"use client";
import Link from "next/link";
import { matchDetailPath } from "./api";
import { useState } from "react";
import { ChampionIcon } from "./champion-icon";
import { Card, StatCard, SectionHeader, Button } from "./design-system";
import { coverageTitle } from "./coverage";
import {
  coverageStatus,
  loadedMatches,
  performance,
  relationships,
  type DashboardWindow,
} from "./competitive-dashboard";
import { CoverageSummary } from "./coverage-summary";
const number = (n: number | null) =>
  n === null ? "—" : n.toLocaleString("es-PA", { maximumFractionDigits: 2 });
export function CompetitivePerformance({
  windows,
}: {
  windows: DashboardWindow[];
}) {
  const stats = performance(loadedMatches(windows)),
    status = coverageStatus(windows);
  return (
    <section aria-labelledby="performance-title">
      <SectionHeader
        title="Rendimiento competitivo"
        titleId="performance-title"
        description={`Todas las partidas válidas cargadas: ${stats.count}. Los filtros solo cambian el historial.`}
      />
      <p className="competitive-coverage" role="status">
        {coverageTitle(status)} · La ventana recuperada no equivale al historial
        completo.
      </p>
      {status !== "complete" && (
        <p className="warning-banner">
          Los datos disponibles pueden no representar toda la muestra
          solicitada.
        </p>
      )}
      <div className="competitive-performance">
        <Card className="competitive-ring-card">
          <svg
            viewBox="0 0 120 120"
            role="img"
            aria-label={`Win rate: ${stats.rate === null ? "sin resultados clasificables" : number(stats.rate) + "%"}; ${stats.wins} victorias y ${stats.losses} derrotas`}
          >
            <circle cx="60" cy="60" r="48" className="ring-track" />
            <circle
              cx="60"
              cy="60"
              r="48"
              pathLength="100"
              className="ring-value"
              strokeDasharray={`${stats.rate ?? 0} 100`}
              transform="rotate(-90 60 60)"
            />
            <text x="60" y="62" textAnchor="middle">
              {stats.rate === null ? "—" : number(stats.rate) + "%"}
            </text>
          </svg>
          <div className="competitive-ring-copy">
            <strong>Win rate</strong>
            <p>
              {stats.wins} victorias · {stats.losses} derrotas
            </p>
            {stats.count > stats.wins + stats.losses && (
              <p>
                Resultados sin clasificar:{" "}
                {stats.count - stats.wins - stats.losses}
              </p>
            )}
          </div>
        </Card>
        <div className="competitive-metrics competitive-performance-metrics">
          {[
            ["Partidas analizadas", stats.count],
            ["Victorias", stats.wins],
            ["Derrotas", stats.losses],
            ["KDA promedio", stats.kda],
            ["Asesinatos promedio", stats.kills],
            ["Muertes promedio", stats.deaths],
            ["Asistencias promedio", stats.assists],
          ].map(([label, value]) => (
            <StatCard
              key={String(label)}
              label={String(label)}
              value={number(value as number | null)}
            />
          ))}
        </div>
      </div>
      <header className="competitive-subsection">
        <h3>Campeones más jugados</h3>
        <p>Según las partidas cargadas</p>
      </header>
      <div className="competitive-champions">
        {stats.champions.map((c) => (
          <Card key={c.id} className="competitive-champion-card">
            <ChampionIcon championId={c.id} championName={c.name} size={72} />
            <div className="competitive-champion-copy">
              <h4>{c.name}</h4>
              <p>
                {c.count} partidas · {c.wins} V / {c.losses} D
              </p>
              <p>
                Win rate: {number(c.rate)}
                {c.rate !== null ? "%" : ""}
              </p>
            </div>
          </Card>
        ))}
      </div>
      {!stats.count && <p>Sin partidas válidas para calcular rendimiento.</p>}
    </section>
  );
}
export function CompetitiveRelationships({
  windows,
  owner,
  region,
}: {
  windows: DashboardWindow[];
  owner: string;
  region?: string;
}) {
  const [expanded, setExpanded] = useState(false),
    stats = relationships(windows, owner);
  const regions = [
    "br1",
    "eun1",
    "euw1",
    "jp1",
    "kr",
    "la1",
    "la2",
    "na1",
    "oc1",
    "ph2",
    "ru",
    "sg2",
    "th2",
    "tr1",
    "tw2",
    "vn2",
  ];
  return (
    <section aria-labelledby="relationships-title">
      <SectionHeader
        title="Relaciones entre jugadores"
        titleId="relationships-title"
        description="Coincidencias en las partidas recuperadas. Recurrencia no confirma un premade."
      />
      <p className="competitive-coverage">
        {coverageTitle(coverageStatus(windows))} · Solo evidencia disponible.
      </p>
      {stats && stats.incompleteMatches > 0 && (
        <p className="warning-banner">
          {stats.incompleteMatches} partidas sin evidencia completa de
          participantes. Se cuentan solo coincidencias identificables; algunos
          equipos o encuentros no están disponibles.
        </p>
      )}
      {stats ? (
        <>
          <div className="competitive-metrics">
            {[
              ["Jugadores únicos", stats.unique],
              ["Jugadores recurrentes", stats.repeated],
              ["Participaciones totales", stats.participations],
              ["Aliados únicos", stats.allies],
              ["Rivales únicos", stats.enemies],
            ].map(([label, value]) => (
              <StatCard
                key={String(label)}
                label={String(label)}
                value={String(value)}
              />
            ))}
          </div>
          <p>
            Una persona puede aparecer como aliado y rival; los conjuntos no se
            suman.
          </p>
          <h3>Jugadores recurrentes</h3>
          <ul className="competitive-recurrent">
            {stats.players.slice(0, expanded ? undefined : 5).map((p) => (
              <li key={p.puuid}>
                <strong>
                  {p.name?.trim() &&
                  p.tag?.trim() &&
                  region &&
                  regions.includes(region.toLowerCase()) ? (
                    <Link
                      href={`/player/${encodeURIComponent(p.name)}/${encodeURIComponent(p.tag)}?platformRegion=${encodeURIComponent(region)}`}
                    >
                      {p.name}#{p.tag}
                    </Link>
                  ) : (
                    p.name || "Identidad no disponible"
                  )}
                </strong>
                <span>
                  {p.total} partidas compartidas · {p.ally} como aliados ·{" "}
                  {p.enemy} como rivales
                </span>
                {p.sharedMatches.length > 0 && (
                  <details>
                    <summary>
                      Partidas compartidas ({p.sharedMatches.length})
                    </summary>
                    <ul>
                      {p.sharedMatches.map((m) => (
                        <li key={m.id}>
                          {regions.includes(m.platform.toLowerCase()) ? (
                            <Link
                              href={matchDetailPath(m.id, owner, m.platform)}
                            >
                              Analizar {m.id}
                            </Link>
                          ) : (
                            <span>{m.id}</span>
                          )}{" "}
                          ·{" "}
                          {m.side === "ally"
                            ? "Aliados"
                            : m.side === "enemy"
                              ? "Rivales"
                              : "Equipo no disponible"}
                        </li>
                      ))}
                    </ul>
                  </details>
                )}
              </li>
            ))}
          </ul>
          {!stats.players.length && (
            <p>
              No se observaron jugadores recurrentes en la muestra recuperada.
            </p>
          )}
          {stats.players.length > 5 && (
            <Button
              variant="secondary"
              aria-expanded={expanded}
              onClick={() => setExpanded(!expanded)}
            >
              {expanded ? "Ver menos" : "Ver más"}
            </Button>
          )}
        </>
      ) : (
        <p className="warning-banner">
          Las ventanas contienen solapamientos o detalles sin asociación
          suficiente. El contrato no permite deduplicar sus relaciones con
          exactitud; los conteos acumulados no están disponibles.
        </p>
      )}
      <p className="muted-copy">
        Las partidas compartidas se muestran cuando la respuesta contiene
        evidencia por partida; respuestas antiguas solo permiten conteos
        agregados.
      </p>
    </section>
  );
}
export function CompetitiveCoverage({
  windows,
}: {
  windows: DashboardWindow[];
}) {
  return (
    <section aria-labelledby="transparency-title">
      <SectionHeader
        title="Cobertura y transparencia"
        titleId="transparency-title"
      />
      <p>
        {loadedMatches(windows).length} partidas válidas únicas utilizadas en
        estadísticas · {windows.length} ventanas consultadas.
      </p>
      <p>
        Los contadores de cada ventana no son sumas de partidas únicas: pueden
        incluir solapamientos. Los reintentos sustituyen el reporte de su
        ventana.
      </p>
      <details>
        <summary>Consultar detalle de cobertura</summary>
        {windows.map((w) => (
          <CoverageSummary
            key={w.start}
            coverage={w.result.coverage}
            title={`Ventana ${w.start + 1}–${w.start + 5}`}
          />
        ))}
      </details>
    </section>
  );
}
