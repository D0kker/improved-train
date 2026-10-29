using System.Security.Cryptography;
using System.Text;
using LolAnalyzer.Application.Analysis;
using LolAnalyzer.Application.Riot;

namespace LolAnalyzer.Application.Stateless;

/// <summary>Pure adapters over the tested T03 algorithms. No repository or shared functional state.</summary>
public sealed class RequestAnalysisCalculator(
    PlayerRelationshipAnalyzer relationshipAnalyzer,
    PossiblePremadeDetector pairDetector,
    PossiblePremadeGroupDetector groupDetector)
{
    private static Guid Id(string value) => new(SHA256.HashData(Encoding.UTF8.GetBytes(value)).AsSpan(0, 16));

    public static DateTimeOffset OccurredAt(RiotMatchData match) => match.GameEndTimestamp;

    public static MatchDetail Detail(RiotMatchData match) => new(
        match.RiotMatchId, match.QueueId, match.GameStartTimestamp, match.GameDurationSeconds,
        match.Participants.GroupBy(player => player.TeamId).OrderBy(team => team.Key)
            .Select(team => new MatchTeamDetail(team.Key, team.OrderBy(player => player.ParticipantId)
                .Select(player => new MatchParticipantDetail(player.Puuid, player.GameName ?? "", player.TagLine ?? "",
                    player.TeamId, player.ParticipantId, player.ChampionId, player.ChampionName,
                    player.TeamPosition, player.Kills, player.Deaths, player.Assists, player.Win)).ToArray())).ToArray());

    public static DashboardMatchEvidence Evidence(RiotMatchData match, string ownerPuuid, string platform)
    {
        var participants = match.Participants.Where(p => !string.IsNullOrWhiteSpace(p.Puuid))
            .GroupBy(p => p.Puuid, StringComparer.Ordinal).OrderBy(g => g.Key == ownerPuuid ? 0 : 1)
            .ThenBy(g => g.Key, StringComparer.Ordinal).Take(10)
            .Select(g => new DashboardParticipant(g.Key, g.First().GameName ?? "", g.First().TagLine ?? "",
                g.Select(p => p.TeamId).Distinct().Count() == 1 && g.First().TeamId is 100 or 200 ? g.First().TeamId : null)).ToArray();
        return new DashboardMatchEvidence(ownerPuuid, platform,
            participants.Length == 10 && match.Participants.Count == 10 && participants.All(p => p.TeamId.HasValue)
                && participants.Any(p => p.Puuid == ownerPuuid) ? "complete" : "partial", participants);
    }

    public static PlayerSummary Summary(PlayerIdentity owner, IReadOnlyCollection<RiotMatchData> matches)
    {
        var own = matches.DistinctBy(match => match.RiotMatchId, StringComparer.Ordinal)
            .Where(match => match.Participants.Any(player => player.Puuid == owner.Puuid)).ToArray();
        var wins = own.Count(match => match.Participants.First(player => player.Puuid == owner.Puuid).Win);
        var participations = own.SelectMany(match => match.Participants
            .Where(player => !string.IsNullOrWhiteSpace(player.Puuid) && player.Puuid != owner.Puuid)
            .DistinctBy(player => player.Puuid).Select(player => player.Puuid)).ToArray();
        var others = participations.GroupBy(puuid => puuid, StringComparer.Ordinal).ToArray();
        return new PlayerSummary(owner.Puuid, owner.GameName, owner.TagLine, own.Length, wins, own.Length - wins,
            own.Length == 0 ? 0 : Math.Round(wins * 100d / own.Length, 1), others.Length,
            others.Count(player => player.Count() >= 2), own.Length == 0 ? DateTimeOffset.UnixEpoch : own.Max(OccurredAt))
        { OtherPlayerParticipations = participations.Length };
    }

    public static IReadOnlyList<PlayerEncounterView> Encounters(string ownerPuuid, IReadOnlyCollection<RiotMatchData> matches)
    {
        var own = matches.DistinctBy(match => match.RiotMatchId, StringComparer.Ordinal)
            .Where(match => match.Participants.Any(player => player.Puuid == ownerPuuid)).ToArray();
        var identities = own.SelectMany(match => match.Participants)
            .Where(player => !string.IsNullOrWhiteSpace(player.Puuid)).DistinctBy(player => player.Puuid)
            .ToDictionary(player => Id(player.Puuid));
        var input = new PlayerAnalysisInput(Id(ownerPuuid), own.Select(match =>
        {
            var owner = match.Participants.First(player => player.Puuid == ownerPuuid);
            return new EncounterMatch(Id(match.RiotMatchId), OccurredAt(match), Id(ownerPuuid), owner.TeamId,
                owner.Win, match.Participants.Where(player => !string.IsNullOrWhiteSpace(player.Puuid))
                    .Select(player => new EncounterParticipant(Id(player.Puuid), player.TeamId)).ToArray());
        }).ToArray());
        return RepeatedPlayerAnalyzer.Analyze(input).Select(item =>
        {
            var identity = identities[item.OtherPlayerId];
            return new PlayerEncounterView(identity.Puuid, identity.GameName ?? "", identity.TagLine ?? "",
                item.TotalMatches, item.SameTeamMatches, item.EnemyTeamMatches, item.WinsTogether,
                item.LossesTogether, item.WinsAgainst, item.LossesAgainst, item.FirstSeenAt, item.LastSeenAt);
        }).OrderByDescending(item => item.TotalMatches).ThenBy(item => item.OtherPlayerPuuid, StringComparer.Ordinal).ToArray();
    }

    public MatchAnalysisResult Analyze(
        RiotMatchData target, string ownerPuuid, string platformRegion,
        IReadOnlyCollection<RiotMatchData> history, AnalysisCoverage coverage, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var owner = target.Participants.Single(player => player.Puuid == ownerPuuid);
        var identity = new PlayerIdentity(ownerPuuid, owner.GameName ?? "", owner.TagLine ?? "", platformRegion);
        var historical = history.Where(match => match.RiotMatchId != target.RiotMatchId)
            .DistinctBy(match => match.RiotMatchId, StringComparer.Ordinal).OrderBy(OccurredAt)
            .ThenBy(match => match.RiotMatchId, StringComparer.Ordinal).ToArray();
        var familiar = MatchFamiliarityCalculator.Calculate(new MatchFamiliarityInput(Id(ownerPuuid), target.RiotMatchId,
            historical.Append(target).Select(match => new FamiliarityMatch(match.RiotMatchId, OccurredAt(match),
                match.Participants.Select(player => new FamiliarityParticipant(
                    string.IsNullOrWhiteSpace(player.Puuid) ? null : Id(player.Puuid), player.Puuid,
                    player.GameName ?? "", player.TagLine ?? "", player.TeamId, player.Win)).ToArray())).ToArray(), 10));
        var participants = target.Participants.Where(player => !string.IsNullOrWhiteSpace(player.Puuid))
            .ToDictionary(player => Id(player.Puuid));
        var relations = relationshipAnalyzer.Analyze(historical.Select(match => new RelationshipMatchSnapshot(
            match.RiotMatchId, OccurredAt(match), match.Participants
                .Where(player => !string.IsNullOrWhiteSpace(player.Puuid)).DistinctBy(player => player.Puuid)
                .Select(player => new RelationshipParticipant(Id(player.Puuid), player.TeamId)).ToArray())).ToArray())
            .Where(pair => participants.ContainsKey(pair.PlayerAId) && participants.ContainsKey(pair.PlayerBId)).ToArray();
        var publicPairs = relations.Select(pair => new HistoricalPairView(participants[pair.PlayerAId].Puuid,
            participants[pair.PlayerBId].Puuid, pair.MatchesTogether, pair.SameTeamMatches, pair.OppositeTeamMatches,
            pair.FirstSeenAt, pair.LastSeenAt, pair.RelationshipScore, pair.RelationshipConfidence))
            .OrderBy(pair => pair.FirstPuuid, StringComparer.Ordinal).ThenBy(pair => pair.SecondPuuid, StringComparer.Ordinal).ToArray();
        var groups = new List<MatchPremadeGroup>();
        foreach (var team in participants.Values.Select(player => player.TeamId).Distinct().Order())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var pairs = relations.Where(pair => participants[pair.PlayerAId].TeamId == team && participants[pair.PlayerBId].TeamId == team)
                .Select(pair =>
                {
                    if (!RelationshipConfidenceExtensions.TryParseLabel(pair.RelationshipConfidence, out var confidence))
                        throw new InvalidOperationException("Relationship confidence is invalid.");
                    var dates = historical.Where(match => match.Participants.Any(player => Id(player.Puuid) == pair.PlayerAId)
                        && match.Participants.Any(player => Id(player.Puuid) == pair.PlayerBId))
                        .Where(match => match.Participants.First(player => Id(player.Puuid) == pair.PlayerAId).TeamId
                            == match.Participants.First(player => Id(player.Puuid) == pair.PlayerBId).TeamId)
                        .Select(match => DateOnly.FromDateTime(OccurredAt(match).UtcDateTime)).Distinct().Count();
                    var detected = pairDetector.Detect(new PremadeDetectionInput(pair.MatchesTogether, pair.SameTeamMatches,
                        confidence, pair.OppositeTeamMatches, dates));
                    return new PremadeGroupPair(pair.PlayerAId, pair.PlayerBId, detected.Classification);
                }).Where(pair => pair.Classification != PremadeClassification.NoEvidence).ToArray();
            var larger = groupDetector.Detect(pairs);
            var all = larger.Concat(pairs.Where(pair => !larger.Any(group => group.PlayerIds.Contains(pair.PlayerAId)
                    && group.PlayerIds.Contains(pair.PlayerBId)))
                .Select(pair => new PossiblePremadeGroup([pair.PlayerAId, pair.PlayerBId], pair.Classification)))
                .OrderByDescending(group => group.PlayerIds.Count).ThenBy(group => string.Join(':', group.PlayerIds));
            foreach (var group in all)
                groups.Add(new MatchPremadeGroup(groups.Count + 1, team, group.Classification.ToString(),
                    group.Classification == PremadeClassification.LikelyPremade ? "possible premade · high evidence" : "possible premade",
                    group.PlayerIds.Select(id => new MatchPremadeGroupMember(participants[id].Puuid,
                        participants[id].GameName ?? "", participants[id].TagLine ?? "")).ToArray()));
        }

        var detail = Detail(target) with
        {
            Familiarity = new MatchFamiliarityView(familiar.KnownPlayers, familiar.UnknownPlayers, familiar.EvaluablePlayers,
                familiar.FamiliarityPercentage, coverage.Status switch
                {
                    "partial" => "PartialHistory",
                    "insufficient" => "InsufficientHistory",
                    "error" => "AnalysisError",
                    _ => familiar.Status.ToString(),
                }, familiar.KnownPlayerDetails),
            PremadeGroups = groups,
        };
        cancellationToken.ThrowIfCancellationRequested();
        return new MatchAnalysisResult(detail, identity, Summary(identity, historical), Encounters(ownerPuuid, historical), publicPairs, coverage);
    }
}
