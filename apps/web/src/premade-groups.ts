import type { MatchPremadeGroup } from "./api.ts";

const groupToneCount = 5;

export function groupCode(groupNumber: number): string {
  return `P${groupNumber}`;
}

export function groupTone(groupNumber: number): number {
  return ((Math.max(1, groupNumber) - 1) % groupToneCount) + 1;
}

export function groupLabel(group: MatchPremadeGroup): string {
  return group.classification === "LikelyPremade"
    ? "Posible premade · evidencia alta"
    : "Posible premade";
}

export function groupsForParticipant(
  groups: MatchPremadeGroup[],
  puuid?: string,
): MatchPremadeGroup[] {
  if (!puuid) return [];
  return groups.filter((group) =>
    group.members.some((member) => member.puuid === puuid),
  );
}
