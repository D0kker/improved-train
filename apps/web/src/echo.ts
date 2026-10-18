import type { MatchDetail, MatchFamiliarity, PlayerEncounter } from "./api.ts";

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
    10,
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

export function knownPlayerCount(encounters: PlayerEncounter[]): number {
  return encounters.filter((encounter) => encounter.totalMatches >= 1).length;
}

export function repeatedPlayerCount(encounters: PlayerEncounter[]): number {
  return encounters.filter((encounter) => encounter.totalMatches >= 2).length;
}

export function familiarityMessage(familiarity: MatchFamiliarity): string {
  const messages: Record<MatchFamiliarity["status"], string> = {
    Available: `${familiarity.knownPlayers} de ${familiarity.evaluablePlayers} jugadores ya habían aparecido antes.`,
    NoPriorHistory: "Esta es la primera partida evaluable del historial.",
    NoEvaluableParticipants:
      "No hay participantes visibles suficientes para calcular familiaridad.",
    OwnerNotPresent: "El jugador de referencia no aparece en esta partida.",
  };

  return messages[familiarity.status];
}
