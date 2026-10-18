import { PlayerDashboard } from "./player-dashboard";

interface PlayerPageProps {
  params: Promise<{ gameName: string; tagLine: string }>;
}

export default async function PlayerPage({ params }: PlayerPageProps) {
  const { gameName, tagLine } = await params;

  return (
    <PlayerDashboard
      gameName={decodeRouteSegment(gameName)}
      tagLine={decodeRouteSegment(tagLine)}
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
