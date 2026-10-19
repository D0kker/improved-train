import { MatchDetailView } from "./match-detail-view";

interface MatchPageProps {
  params: Promise<{ matchId: string }>;
  searchParams: Promise<{ ownerPuuid?: string; platformRegion?: string }>;
}

export default async function MatchPage({
  params,
  searchParams,
}: MatchPageProps) {
  const { matchId } = await params;
  const { ownerPuuid, platformRegion } = await searchParams;
  return (
    <MatchDetailView
      matchId={matchId}
      ownerPuuid={ownerPuuid}
      platformRegion={platformRegion}
    />
  );
}
