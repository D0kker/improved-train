import { MatchDetailView } from "./match-detail-view";

interface MatchPageProps {
  params: Promise<{ matchId: string }>;
}

export default async function MatchPage({ params }: MatchPageProps) {
  const { matchId } = await params;
  return <MatchDetailView matchId={matchId} />;
}
