import { LiveMatchView } from "./live-match-view";

interface LiveMatchPageProps {
  searchParams: Promise<{ ownerPuuid?: string }>;
}

export default async function LiveMatchPage({
  searchParams,
}: LiveMatchPageProps) {
  const { ownerPuuid } = await searchParams;
  return <LiveMatchView ownerPuuid={ownerPuuid} />;
}
