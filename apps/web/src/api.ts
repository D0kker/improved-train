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
  otherPlayerParticipations?: number;
  dataUpdatedAt: string;
}

export interface ActiveGame {
  gameId: number;
  queueId: number;
  gameStartTime: string;
  gameLengthSeconds: number;
  participants: ActiveGameParticipant[];
}

export interface ActiveGameParticipant {
  puuid?: string;
  gameName?: string;
  tagLine?: string;
  summonerId?: string;
  championId: number;
  teamId: number;
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
    | "OwnerNotPresent"
    | "PartialParticipants"
    | "PartialHistory"
    | "InsufficientHistory"
    | "AnalysisError";
  knownPlayerDetails: MatchKnownPlayer[];
}

export interface MatchKnownPlayer {
  puuid: string;
  gameName: string;
  tagLine: string;
  playedBefore: number;
  allyMatches: number;
  enemyMatches: number;
  winsTogether: number;
  lossesTogether: number;
  winsAgainst: number;
  lossesAgainst: number;
  firstEncounterAt: string;
  lastEncounterAt: string;
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

export interface AnalysisCoverage {
  status: "complete" | "partial" | "insufficient" | "error";
  participants: number;
  matchIdsObtained: number;
  uniqueMatchIds: number;
  matchesRecovered: number;
  players: {
    puuid: string;
    requested: number;
    matchIds: number;
    eligibleMatches: number;
    limited: boolean;
  }[];
  failures: { stage: string; code: string; puuid?: string; matchId?: string }[];
}

export interface PlayerSearchResult {
  player: PlayerLookup;
  summary: PlayerSummary;
  matches: MatchesResponse;
  encounters: PlayerEncounter[];
  coverage: AnalysisCoverage;
}

export interface MatchAnalysisResult {
  match: MatchDetail;
  owner: PlayerLookup;
  summary: PlayerSummary;
  encounters: PlayerEncounter[];
  relationships: {
    firstPuuid: string;
    secondPuuid: string;
    matchesTogether: number;
    allyMatches: number;
    enemyMatches: number;
    score: number;
    confidence: string;
  }[];
  coverage: AnalysisCoverage;
}

export function playerSearchPath(
  gameName: string,
  tagLine: string,
  platformRegion?: string,
): string {
  const path = `${playerLookupPath(gameName, tagLine)}/analysis`;
  return platformRegion
    ? `${path}?platformRegion=${encodeURIComponent(platformRegion)}`
    : path;
}

export function recentMatchesPath(
  puuid: string,
  start = 0,
  platformRegion?: string,
): string {
  const path = `${apiPath("players", puuid, "recent-matches")}?count=5&start=${start}`;
  return platformRegion
    ? `${path}&platformRegion=${encodeURIComponent(platformRegion)}`
    : path;
}

export function matchAnalyzePath(matchId: string): string {
  return apiPath("matches", matchId, "analyze");
}

export function analyzeSelectedMatch(
  matchId: string,
  ownerPuuid: string,
  platformRegion?: string,
  signal?: AbortSignal,
): Promise<MatchAnalysisResult> {
  return fetchJson<MatchAnalysisResult>(matchAnalyzePath(matchId), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ ownerPuuid, platformRegion }),
    signal,
  });
}

export interface MatchSyncResult {
  matchIdsReturned: number;
  alreadyStored: number;
  downloaded: number;
  notFound: number;
  failed: number;
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

export function playerProfilePath(
  gameName: string,
  tagLine: string,
  platformRegion?: string,
): string {
  const path = `/player/${encodeURIComponent(gameName)}/${encodeURIComponent(tagLine)}`;
  return platformRegion
    ? `${path}?platformRegion=${encodeURIComponent(platformRegion)}`
    : path;
}

export function playerActiveGamePath(
  puuid: string,
  platformRegion?: string,
): string {
  const path = apiPath("players", puuid, "active-game");
  return platformRegion
    ? `${path}?platformRegion=${encodeURIComponent(platformRegion)}`
    : path;
}

export function matchDetailPath(
  matchId: string,
  ownerPuuid?: string,
  platformRegion?: string,
  returnProfile?: { gameName: string; tagLine: string },
): string {
  const path = `/match/${encodeURIComponent(matchId)}`;
  const query = new URLSearchParams();
  if (ownerPuuid) query.set("ownerPuuid", ownerPuuid);
  if (platformRegion) query.set("platformRegion", platformRegion);
  if (ownerPuuid && returnProfile?.gameName && returnProfile.tagLine) {
    query.set("returnGameName", returnProfile.gameName);
    query.set("returnTagLine", returnProfile.tagLine);
  }
  const search = query.toString();
  return search ? `${path}?${search}` : path;
}

export function playerSyncPath(
  puuid: string,
  count = 20,
  excludeRiotMatchId?: string,
): string {
  if (!Number.isInteger(count) || count < 1 || count > 20) {
    throw new RangeError("El número de partidas debe estar entre 1 y 20.");
  }

  const path = `${apiPath("players", puuid, "matches", "sync")}?count=${count}`;
  return excludeRiotMatchId
    ? `${path}&excludeRiotMatchId=${encodeURIComponent(excludeRiotMatchId)}`
    : path;
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
  // API budget 180s < browser 200s < Next proxy 210s. No automatic retries.
  const timeout = new AbortController();
  const timer = setTimeout(
    () =>
      timeout.abort(
        new DOMException(
          "La solicitud excedió el tiempo disponible.",
          "TimeoutError",
        ),
      ),
    200_000,
  );
  const signal = init?.signal
    ? AbortSignal.any([init.signal, timeout.signal])
    : timeout.signal;
  try {
    const response = await fetch(path, {
      ...init,
      signal,
      headers: {
        Accept: "application/json",
        ...init?.headers,
      },
    });

    if (!response.ok) {
      throw new ApiError(await errorMessage(response, path), response.status);
    }

    const body = await response.text();
    return (body.trim() ? JSON.parse(body) : null) as T;
  } finally {
    clearTimeout(timer);
  }
}

async function errorMessage(response: Response, path: string): Promise<string> {
  const isPlayerLookup = path.includes("/players/by-riot-id/");
  const isMatchLookup = path.includes("/matches/") || path.startsWith("/match/");
  switch (response.status) {
    case 400:
      return "No pudimos procesar la solicitud. Revisa los datos e inténtalo de nuevo.";
    case 404:
      if (isPlayerLookup) return "No encontramos ese Riot ID en la región seleccionada.";
      if (isMatchLookup) return "No encontramos la partida solicitada.";
      return "No encontramos la información solicitada.";
    case 429:
      return "La consulta está limitada temporalmente. Espera un momento y vuelve a intentarlo.";
    case 502:
    case 503:
      return "El servicio de Riot no está disponible en este momento.";
    case 504:
      return "La consulta tardó demasiado. Puedes volver a intentarlo.";
    default:
      return "Ocurrió un error inesperado. Inténtalo de nuevo.";
  }
}
