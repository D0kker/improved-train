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

export interface MatchParticipant {
  puuid?: string;
  gameName?: string;
  tagLine?: string;
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

export interface MatchDetail {
  riotMatchId: string;
  queueId: number | null;
  gameStartTimestamp: string | null;
  gameDurationSeconds: number | null;
  teams: MatchTeam[];
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

export function playerSyncPath(puuid: string, count = 20): string {
  if (!Number.isInteger(count) || count < 1 || count > 20) {
    throw new RangeError("El número de partidas debe estar entre 1 y 20.");
  }

  return `${apiPath("players", puuid, "matches", "sync")}?count=${count}`;
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
