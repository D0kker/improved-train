import { PlayerDashboard } from "./player-dashboard";

interface PlayerPageProps {
  params: Promise<{ gameName: string; tagLine: string }>;
  searchParams: Promise<{ platformRegion?: string }>;
}

export default async function PlayerPage({ params, searchParams }: PlayerPageProps) {
  const { gameName, tagLine } = await params;
  const { platformRegion } = await searchParams;

  return (
    <PlayerDashboard
      key={`${gameName}:${tagLine}:${platformRegion ?? ""}`}
      gameName={decodeRouteSegment(gameName)}
      tagLine={decodeRouteSegment(tagLine)}
      platformRegion={platformRegion}
    />
  );
}

function decodeRouteSegment(value: string): string {
  try {
    return decodeURIComponent(value);
  } catch {
    return value;
  }
}
