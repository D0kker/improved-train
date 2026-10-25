import type { AnalysisCoverage } from "./api";
import { coverageDescription, coverageTitle } from "./coverage";

export function CoverageSummary({
  coverage,
  title,
}: {
  coverage: AnalysisCoverage;
  title: string;
}) {
  const windows = coverage.players ?? [];
  const requestedWindows = windows.reduce((total, player) => total + player.requested, 0);
  const evaluableProfiles = windows.filter((player) => player.eligibleMatches > 0).length;
  const incompleteWindows = windows.filter((player) => player.limited).length;
  const headingId = `coverage-${title.toLowerCase().replace(/[^a-z0-9]+/g, "-")}`;

  return (
    <section
      className={`coverage-panel coverage-${coverage.status}`}
      aria-labelledby={headingId}
      role={coverage.status === "error" ? "alert" : "status"}
    >
      <div>
        <p className="eyebrow">{title}</p>
        <h2 id={headingId}>{coverageTitle(coverage.status)}</h2>
        <p>{coverageDescription(coverage.status)}</p>
      </div>
      <dl className="coverage-stats">
        {requestedWindows > 0 ? (
          <div>
            <dt>Partidas solicitadas</dt>
            <dd>{requestedWindows}</dd>
          </div>
        ) : null}
        <div>
          <dt>IDs de partidas obtenidos</dt>
          <dd>{coverage.matchIdsObtained}</dd>
        </div>
        <div>
          <dt>Partidas recuperadas</dt>
          <dd>{coverage.matchesRecovered}</dd>
        </div>
        <div>
          <dt>IDs de partidas únicos</dt>
          <dd>{coverage.uniqueMatchIds}</dd>
        </div>
        {windows.length > 0 ? (
          <div>
            <dt>Perfiles con historial utilizable</dt>
            <dd>{evaluableProfiles} de {windows.length}</dd>
          </div>
        ) : null}
        {windows.length > 0 ? (
          <div>
            <dt>Ventanas con historial incompleto</dt>
            <dd>{incompleteWindows} de {windows.length}</dd>
          </div>
        ) : null}
        {coverage.failures.length > 0 ? (
          <div>
            <dt>Fallos reportados</dt>
            <dd>{coverage.failures.length}</dd>
          </div>
        ) : null}
      </dl>
    </section>
  );
}
