import type { AnalysisCoverage } from "./api.ts";

export type CoverageStatus = AnalysisCoverage["status"];

export function coverageTitle(status: CoverageStatus): string {
  const labels: Record<CoverageStatus, string> = {
    complete: "Consulta completa",
    partial: "Consulta parcial",
    insufficient: "Información insuficiente",
    error: "No se pudo completar el análisis",
  };
  return labels[status];
}

export function coverageDescription(status: CoverageStatus): string {
  const descriptions: Record<CoverageStatus, string> = {
    complete: "Se recuperó la información prevista para esta consulta.",
    partial: "Algunas partidas no pudieron recuperarse. Los resultados pueden cambiar al volver a intentarlo.",
    insufficient: "No existen suficientes partidas elegibles para completar el análisis solicitado.",
    error: "No fue posible completar el análisis.",
  };
  return descriptions[status];
}

export function canContinueRecentMatches(
  coverage: AnalysisCoverage,
  pageSize = 5,
): boolean {
  return (
    coverage.matchIdsObtained === pageSize ||
    coverage.status === "partial" ||
    coverage.status === "error"
  );
}

export function nextRecentMatchesStart(
  requestedStart: number,
  coverage: AnalysisCoverage,
  pageSize = 5,
): number {
  return coverage.status === "partial" || coverage.status === "error"
    ? requestedStart
    : requestedStart + pageSize;
}

export function isCurrentPlayerRequest(
  currentPuuid: string | undefined,
  requestedPuuid: string,
  aborted: boolean,
): boolean {
  return !aborted && currentPuuid === requestedPuuid;
}
