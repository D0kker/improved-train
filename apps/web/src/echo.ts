import type { MatchFamiliarity, PlayerEncounter } from "./api.ts";

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
