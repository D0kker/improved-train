import { MatchDetailView } from "./match-detail-view";

interface MatchPageProps {
  params: Promise<{ matchId: string }>;
  searchParams: Promise<{
    ownerPuuid?: string;
    platformRegion?: string;
    returnGameName?: string;
    returnTagLine?: string;
  }>;
}

export default async function MatchPage({
  params,
  searchParams,
}: MatchPageProps) {
  const { matchId } = await params;
  const { ownerPuuid, platformRegion, returnGameName, returnTagLine } = await searchParams;
  return (
    <MatchDetailView
      matchId={matchId}
      ownerPuuid={ownerPuuid}
      platformRegion={platformRegion}
      returnProfile={returnGameName && returnTagLine ? { gameName: returnGameName, tagLine: returnTagLine } : undefined}
    />
  );
}
