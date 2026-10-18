import type {
  MatchDetail,
  MatchFamiliarity,
  MatchSyncResult,
  PlayerEncounter,
} from "./api.ts";

export type HistoryAnalysisStatus =
  "complete" | "partial" | "insufficient" | "error";

export function historyAnalysisStatus(
  results: PromiseSettledResult<MatchSyncResult>[],
): HistoryAnalysisStatus {
  const successful = results.filter((result) => result.status === "fulfilled");
  if (successful.length === 0)
    return results.length > 0 ? "error" : "insufficient";
  const incomplete = successful.some(
    ({ value }) =>
      value.failed > 0 ||
      value.notFound > 0 ||
      value.alreadyStored + value.downloaded < value.matchIdsReturned,
  );
  if (incomplete || successful.length < results.length) return "partial";
  return successful.some(({ value }) => value.matchIdsReturned > 0)
    ? "complete"
    : "insufficient";
}

export interface LatestMatchSyncConfig {
  ownerMatchCount: number;
  otherMatchCount: number;
  otherPlayerCount: number;
}

function configuredCount(
  value: string | undefined,
  fallback: number,
  max: number,
) {
  const parsed = Number(value);
  return Number.isInteger(parsed) && parsed >= 1 && parsed <= max
    ? parsed
    : fallback;
}

/**
 * Public, non-secret knobs for the automatic last-match context refresh.
 * They can be overridden at build time with NEXT_PUBLIC_ECHO_* variables.
 */
export const latestMatchSyncConfig: LatestMatchSyncConfig = {
  ownerMatchCount: configuredCount(
    process.env.NEXT_PUBLIC_ECHO_OWNER_MATCH_COUNT,
    5,
    20,
  ),
  otherMatchCount: configuredCount(
    process.env.NEXT_PUBLIC_ECHO_OTHER_MATCH_COUNT,
    3,
    20,
  ),
  otherPlayerCount: configuredCount(
    process.env.NEXT_PUBLIC_ECHO_OTHER_PLAYER_COUNT,
    9,
    9,
  ),
};

export interface LatestMatchSyncTarget {
  puuid: string;
  count: number;
}

export function latestMatchSyncPlan(
  match: MatchDetail,
  ownerPuuid: string,
  config: LatestMatchSyncConfig = latestMatchSyncConfig,
): LatestMatchSyncTarget[] {
  const participantPuuids = match.teams.flatMap((team) =>
    team.participants.flatMap((participant) =>
      participant.puuid && participant.puuid !== ownerPuuid
        ? [participant.puuid]
        : [],
    ),
  );
  const otherPuuids = [...new Set(participantPuuids)].slice(
    0,
    config.otherPlayerCount,
  );

  return [
    { puuid: ownerPuuid, count: config.ownerMatchCount },
    ...otherPuuids.map((puuid) => ({
      puuid,
      count: config.otherMatchCount,
    })),
  ];
}

export function knownPlayerCount(
  encounters: PlayerEncounter[],
  ownerPuuid?: string,
): number {
  return new Set(
    encounters
      .filter(
        (encounter) =>
          encounter.totalMatches >= 1 &&
          encounter.otherPlayerPuuid?.trim() &&
          encounter.otherPlayerPuuid !== ownerPuuid,
      )
      .map((encounter) => encounter.otherPlayerPuuid),
  ).size;
}

export function repeatedPlayerCount(
  encounters: PlayerEncounter[],
  ownerPuuid?: string,
): number {
  return new Set(
    encounters
      .filter(
        (encounter) =>
          encounter.totalMatches >= 2 &&
          encounter.otherPlayerPuuid?.trim() &&
          encounter.otherPlayerPuuid !== ownerPuuid,
      )
      .map((encounter) => encounter.otherPlayerPuuid),
  ).size;
}

export function familiarityMessage(
  familiarity: MatchFamiliarity,
  analysisStatus: HistoryAnalysisStatus = "complete",
): string {
  if (analysisStatus === "error")
    return "Error de análisis: no se pudo actualizar el historial. La evidencia local no permite concluir que no haya conocidos.";
  if (analysisStatus === "partial")
    return `Análisis parcial: ${familiarity.knownPlayers} coincidencias detectadas en la evidencia disponible. Puede haber más.`;
  if (analysisStatus === "insufficient")
    return "Información insuficiente: no se ha recuperado historial para completar el análisis.";
  const messages: Record<MatchFamiliarity["status"], string> = {
    Available:
      familiarity.knownPlayers > 0
        ? `${familiarity.knownPlayers} de ${familiarity.evaluablePlayers} jugadores ya habían aparecido antes.`
        : `No se detectaron coincidencias entre los ${familiarity.evaluablePlayers} jugadores evaluables en el historial disponible.`,
    NoPriorHistory:
      "Información insuficiente: no hay partidas anteriores a esta en el historial disponible.",
    NoEvaluableParticipants:
      "No hay participantes visibles suficientes para calcular familiaridad.",
    OwnerNotPresent: "El jugador de referencia no aparece en esta partida.",
    PartialParticipants: `Análisis parcial: ${familiarity.knownPlayers} coincidencias entre ${familiarity.evaluablePlayers} jugadores identificables; faltan identidades.`,
  };

  return messages[familiarity.status];
}
