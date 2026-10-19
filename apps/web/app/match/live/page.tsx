import { LiveMatchView } from "./live-match-view";

interface LiveMatchPageProps {
  searchParams: Promise<{ ownerPuuid?: string; platformRegion?: string }>;
}

export default async function LiveMatchPage({
  searchParams,
}: LiveMatchPageProps) {
  const { ownerPuuid, platformRegion } = await searchParams;
  return (
    <LiveMatchView
      key={`${ownerPuuid ?? "missing"}:${platformRegion ?? ""}`}
      ownerPuuid={ownerPuuid}
      platformRegion={platformRegion}
    />
  );
}
