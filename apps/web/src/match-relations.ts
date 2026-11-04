import type { MatchAnalysisResult, MatchParticipant, MatchTeam } from "./api";

/** A participant's displayable attributes; PUUID never becomes a visible name. */
export interface MatchRelationIdentity {
  label: string;
  gameName?: string;
  tagLine?: string;
  championName?: string;
  teamId?: number;
}

export interface MatchRelationView {
  /** Internal React key only; do not display or include in a URL. */
  key: string;
  first: MatchRelationIdentity;
  second: MatchRelationIdentity;
  matchesTogether: number;
  allyMatches: number;
  enemyMatches: number;
}

type Relationship = MatchAnalysisResult["relationships"][number];
const compareOrdinal = (a: string, b: string) => (a < b ? -1 : a > b ? 1 : 0);

/**
 * Present the backend's historical evidence without recalculating it.
 * `matchesTogether` is the count of unique previous matches containing both
 * players; `allyMatches` and `enemyMatches` are the same/opposite-team counts.
 * Ties are broken by the canonical PUUID pair using ordinal comparison.
 */
export function presentMatchRelations(
  relationships: readonly Relationship[],
  teams: readonly MatchTeam[],
): MatchRelationView[] {
  const participants = new Map<
    string,
    { participant: MatchParticipant; teamId: number } | null
  >();
  for (const team of teams) {
    for (const participant of team.participants) {
      const puuid = participant.puuid;
      if (!puuid?.trim()) continue;
      // Ambiguous duplicate identity cannot safely establish a team or name.
      if (participants.has(puuid)) participants.set(puuid, null);
      else participants.set(puuid, { participant, teamId: team.teamId });
    }
  }

  const identity = (puuid: string): MatchRelationIdentity => {
    const record = participants.get(puuid);
    if (!record) return { label: "Jugador no identificado" };
    const gameName = record.participant.gameName?.trim() || undefined;
    const tagLine = record.participant.tagLine?.trim() || undefined;
    return {
      label: gameName
        ? tagLine
          ? `${gameName}#${tagLine}`
          : gameName
        : "Jugador no identificado",
      gameName,
      tagLine,
      championName: record.participant.championName?.trim() || undefined,
      teamId: record.teamId,
    };
  };

  return relationships
    .map((relation) => {
      const [firstId, secondId] =
        compareOrdinal(relation.firstPuuid, relation.secondPuuid) <= 0
          ? [relation.firstPuuid, relation.secondPuuid]
          : [relation.secondPuuid, relation.firstPuuid];
      return { relation, firstId, secondId };
    })
    .sort(
      (a, b) =>
        b.relation.matchesTogether - a.relation.matchesTogether ||
        compareOrdinal(a.firstId, b.firstId) ||
        compareOrdinal(a.secondId, b.secondId),
    )
    .map(({ relation, firstId, secondId }) => ({
      key: JSON.stringify([firstId, secondId]),
      first: identity(firstId),
      second: identity(secondId),
      matchesTogether: relation.matchesTogether,
      allyMatches: relation.allyMatches,
      enemyMatches: relation.enemyMatches,
    }));
}
