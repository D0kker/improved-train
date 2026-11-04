import type { AnalysisCoverage, MatchAnalysisResult, MatchDetail } from "./api";

type Pair = MatchAnalysisResult["relationships"][number];
type History =
  | { kind: "known"; pair: Pair; partial: boolean }
  | { kind: "empty" | "partial" | "unavailable" | "identity" };
const compare = (a: string, b: string) => (a < b ? -1 : a > b ? 1 : 0);

/** The API owns historical deduplication and target exclusion; never sum its pair aggregates. */
export function inlineMatchRelations(
  match: MatchDetail,
  pairs: readonly Pair[] | null,
  coverage: AnalysisCoverage | null,
  owner?: string,
) {
  const people = new Map<string, number[]>();
  const names = new Map<string, string | null>();
  for (const team of match.teams)
    for (const person of team.participants) {
      if (person.puuid?.trim()) {
        people.set(person.puuid, [
          ...(people.get(person.puuid) ?? []),
          team.teamId,
        ]);
        const name = person.gameName?.trim();
        const tag = person.tagLine?.trim();
        names.set(person.puuid, name ? `${name}${tag ? `#${tag}` : ""}` : null);
      }
    }
  const validOwner = !!owner?.trim() && people.get(owner)?.length === 1;
  const available = !!pairs && !!coverage && coverage.status !== "error";
  const candidates = available
    ? match.premadeGroups.flatMap((group) => {
        const ids = [
          ...new Set(
            group.members.map((member) => member.puuid).filter(Boolean),
          ),
        ].sort(compare);
        return (group.classification === "PossiblePremade" ||
          group.classification === "LikelyPremade") &&
          ids.length >= 2 &&
          ids.length === group.members.length &&
          ids.every(
            (id) =>
              people.get(id)?.length === 1 &&
              people.get(id)?.[0] === group.teamId,
          )
          ? [{ key: JSON.stringify([group.teamId, ids]), ids }]
          : [];
      })
    : [];
  const uniqueGroups = [
    ...new Map(candidates.map((group) => [group.key, group])).values(),
  ].sort((a, b) => compare(a.key, b.key));
  const labels = new Map<string, string[]>();
  uniqueGroups.forEach((group, index) => {
    let n = index + 1,
      label = "";
    while (n) {
      n--;
      label = String.fromCharCode(65 + (n % 26)) + label;
      n = Math.floor(n / 26);
    }
    for (const id of group.ids)
      labels.set(id, [...(labels.get(id) ?? []), label]);
  });
  function matching(firstId: string, secondId: string) {
    return pairs!.filter(
      (pair) =>
        (pair.firstPuuid === firstId && pair.secondPuuid === secondId) ||
        (pair.secondPuuid === firstId && pair.firstPuuid === secondId),
    );
  }
  function consistent(found: Pair[]) {
    const first = found[0];
    return (
      !!first &&
      found.every(
        (p) =>
          Number.isInteger(p.matchesTogether) &&
          p.matchesTogether > 0 &&
          Number.isInteger(p.allyMatches) &&
          p.allyMatches >= 0 &&
          Number.isInteger(p.enemyMatches) &&
          p.enemyMatches >= 0 &&
          p.allyMatches + p.enemyMatches === p.matchesTogether &&
          p.matchesTogether === first.matchesTogether &&
          p.allyMatches === first.allyMatches &&
          p.enemyMatches === first.enemyMatches,
      )
    );
  }
  function history(puuid?: string): History {
    if (!puuid?.trim() || people.get(puuid)?.length !== 1 || !validOwner)
      return { kind: "identity" };
    if (!available) return { kind: "unavailable" };
    const found = matching(owner!, puuid);
    if (found.length === 0)
      return { kind: coverage!.status === "complete" ? "empty" : "partial" };
    const first = found[0];
    if (!consistent(found)) return { kind: "unavailable" };
    return {
      kind: "known",
      pair: first,
      partial: coverage!.status !== "complete",
    };
  }
  function priorAllies(puuid?: string): (string | null)[] {
    if (
      !available ||
      !validOwner ||
      !puuid ||
      puuid === owner ||
      people.get(puuid)?.length !== 1
    )
      return [];
    return [...people.keys()]
      .filter(
        (id) => id !== puuid && id !== owner && people.get(id)?.length === 1,
      )
      .sort(compare)
      .filter((id) => {
        const found = matching(puuid, id);
        return consistent(found) && found[0].allyMatches > 0;
      })
      .map((id) => names.get(id)!);
  }
  return { labels, history, priorAllies };
}
