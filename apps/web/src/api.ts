export interface PlayerLookup {
  puuid: string;
  gameName: string;
  tagLine: string;
  platformRegion?: string;
}

export interface PlayerSummary {
  puuid: string;
  gameName: string;
  tagLine: string;
  matchesAnalyzed: number;
  wins: number;
  losses: number;
  winRate: number;
  uniquePlayersEncountered: number;
  repeatedPlayers: number;
  dataUpdatedAt: string;
}

export interface PlayerEncounter {
  otherPlayerPuuid: string;
  gameName: string;
  tagLine: string;
  totalMatches: number;
  sameTeamMatches: number;
  enemyTeamMatches: number;
  winsTogether: number;
  lossesTogether: number;
  winsAgainst: number;
  lossesAgainst: number;
  firstSeenAt: string;
  lastSeenAt: string;
}

export interface MatchListItem {
  riotMatchId: string;
  queueId: number | null;
  gameStartTimestamp: string | null;
  gameDurationSeconds: number | null;
  championId: number;
  championName: string;
  kills: number;
  deaths: number;
  assists: number;
  win: boolean;
}

export interface MatchesResponse {
  page: number;
  pageSize: number;
  totalCount: number;
  items: MatchListItem[];
}

export interface PlayerRelationship {
  otherPlayerPuuid: string;
  gameName: string;
  tagLine: string;
  matchesTogether: number;
  sameTeamMatches: number;
  oppositeTeamMatches: number;
  sameTeamRatio: number;
  recentMatchesTogether: number;
  consecutiveMatches: number;
  firstSeenAt: string;
  lastSeenAt: string;
  relationshipScore: number;
  relationshipConfidence: "LOW" | "MEDIUM" | "HIGH" | "VERY_HIGH";
  premadeLabel: "possible premade" | "likely premade" | null;
}

export interface RelationshipsResponse {
  page: number;
  pageSize: number;
  totalCount: number;
  items: PlayerRelationship[];
}

export type RelationshipConfidence = "LOW" | "MEDIUM" | "HIGH" | "VERY_HIGH";

export interface PlayerNetworkNode {
  puuid: string;
  gameName: string;
  tagLine: string;
  isCenter: boolean;
}

export interface PlayerNetworkEdge {
  sourcePuuid: string;
  targetPuuid: string;
  matchesTogether: number;
  sameTeamMatches: number;
  oppositeTeamMatches: number;
  sameTeamRatio: number;
  relationshipScore: number;
  relationshipConfidence: RelationshipConfidence;
  premadeLabel: "possible premade" | "likely premade" | null;
}

export interface PlayerNetworkResponse {
  center: PlayerNetworkNode;
  nodes: PlayerNetworkNode[];
  edges: PlayerNetworkEdge[];
  metadata: {
    depth: number;
    truncated: boolean;
    totalAvailableNodes: number;
    totalAvailableEdges: number;
    appliedMaxNodes: number;
    appliedMaxEdges: number;
  };
}

export interface MatchParticipant {
  puuid?: string;
  gameName?: string;
  tagLine?: string;
  championId: number;
  championName: string;
  teamPosition?: string;
  kills: number;
  deaths: number;
  assists: number;
  win?: boolean;
}

export interface MatchTeam {
  teamId: number;
  participants: MatchParticipant[];
}

export interface MatchPremadeGroupMember {
  puuid: string;
  gameName: string;
  tagLine: string;
}

export interface MatchPremadeGroup {
  groupNumber: number;
  teamId: number;
  classification: "PossiblePremade" | "LikelyPremade";
  label: "possible premade" | "possible premade · high evidence";
  members: MatchPremadeGroupMember[];
}

export interface MatchFamiliarity {
  knownPlayers: number;
  unknownPlayers: number;
  evaluablePlayers: number;
  familiarityPercentage: number;
  status:
    | "Available"
    | "NoPriorHistory"
    | "NoEvaluableParticipants"
    | "OwnerNotPresent";
}

export interface MatchDetail {
  riotMatchId: string;
  queueId: number | null;
  gameStartTimestamp: string | null;
  gameDurationSeconds: number | null;
  teams: MatchTeam[];
  premadeGroups: MatchPremadeGroup[];
  familiarity: MatchFamiliarity | null;
}

export class ApiError extends Error {
  readonly status: number;

  constructor(message: string, status: number) {
    super(message);
    this.name = "ApiError";
    this.status = status;
  }
}

export function apiPath(...segments: Array<string | number>): string {
  return `/api/v1/${segments
    .map((segment) => encodeURIComponent(String(segment)))
    .join("/")}`;
}

export function playerLookupPath(gameName: string, tagLine: string): string {
  return apiPath("players", "by-riot-id", gameName, tagLine);
}

export function playerProfilePath(gameName: string, tagLine: string): string {
  return `/player/${encodeURIComponent(gameName)}/${encodeURIComponent(tagLine)}`;
}

export function matchDetailPath(matchId: string, ownerPuuid?: string): string {
  const path = `/match/${encodeURIComponent(matchId)}`;
  return ownerPuuid
    ? `${path}?ownerPuuid=${encodeURIComponent(ownerPuuid)}`
    : path;
}

export function playerSyncPath(puuid: string, count = 20): string {
  if (!Number.isInteger(count) || count < 1 || count > 20) {
    throw new RangeError("El número de partidas debe estar entre 1 y 20.");
  }

  return `${apiPath("players", puuid, "matches", "sync")}?count=${count}`;
}

export function playerRelationshipsPath(
  puuid: string,
  page = 1,
  pageSize = 20,
): string {
  if (
    !Number.isInteger(page) ||
    page < 1 ||
    !Number.isInteger(pageSize) ||
    pageSize < 1 ||
    pageSize > 100
  ) {
    throw new RangeError("La paginación de relaciones no es válida.");
  }

  return `${apiPath("players", puuid, "relationships")}?page=${page}&pageSize=${pageSize}`;
}

export function playerNetworkPath(puuid: string): string {
  return `${apiPath("players", puuid, "network")}?maxNodes=50&maxEdges=100`;
}

export async function fetchJson<T>(
  path: string,
  init?: RequestInit,
): Promise<T> {
  const response = await fetch(path, {
    ...init,
    headers: {
      Accept: "application/json",
      ...init?.headers,
    },
  });

  if (!response.ok) {
    throw new ApiError(await errorMessage(response), response.status);
  }

  return (await response.json()) as T;
}

async function errorMessage(response: Response): Promise<string> {
  const fallback =
    response.status === 404
      ? "No encontramos esos datos."
      : "No pudimos completar la solicitud.";

  try {
    const body = (await response.json()) as {
      detail?: unknown;
      title?: unknown;
      message?: unknown;
    };
    const candidate = body.detail ?? body.message ?? body.title;
    return typeof candidate === "string" && candidate.trim()
      ? candidate
      : fallback;
  } catch {
    return fallback;
  }
}
