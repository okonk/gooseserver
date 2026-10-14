namespace Goose.CommunityGoals
{
    public static class Ranking
    {
        public static double Score(CommunityGoalDefinition definition, Contribution contribution)
        {
            double score = 0;
            foreach (var requirement in definition.Requirements)
            {
                if (!contribution.Amounts.TryGetValue(requirement.Key, out var amount)) continue;
                score += definition.ScoreWeights.GetValueOrDefault(requirement.Key, 1.0) * amount;
            }
            return score;
        }

        public static long Entries(CommunityGoalDefinition definition, Contribution contribution)
        {
            if (definition.EntryCost is null) return 0;
            long entries = 0;
            foreach (var (key, cost) in definition.EntryCost)
            {
                if (cost <= 0 || !contribution.Amounts.TryGetValue(key, out var amount)) continue;
                entries += amount / cost;
            }
            return entries;
        }

        public static List<int> Freeze(CommunityGoalDefinition definition, CommunityGoalRun run, Random random)
        {
            var places = new List<int>();
            var count = definition.Brackets
                .Where(b => b.Rank.HasValue)
                .Select(b => b.Rank!.Value)
                .DefaultIfEmpty(0)
                .Max();
            var pool = run.Contributions.Values.ToList();
            for (var i = 0; i < count && pool.Count > 0; i++)
            {
                Contribution? winner = definition.Ranking == RankingKind.Lottery
                    ? DrawLottery(definition, pool, random)
                    : pool
                        .Where(c => Score(definition, c) > 0)
                        .OrderByDescending(c => Score(definition, c))
                        .ThenBy(c => c.LastAt)
                        .ThenBy(c => c.PlayerId)
                        .FirstOrDefault();
                if (winner is null) break;
                places.Add(winner.PlayerId);
                pool.Remove(winner);
            }
            return places;
        }

        private static Contribution? DrawLottery(CommunityGoalDefinition definition, List<Contribution> pool, Random random)
        {
            var eligible = pool.Where(c => Entries(definition, c) > 0).ToList();
            if (eligible.Count == 0) return null;
            var total = eligible.Sum(c => Entries(definition, c));
            var roll = random.NextInt64(total);
            foreach (var contribution in eligible)
            {
                var entries = Entries(definition, contribution);
                if (roll < entries) return contribution;
                roll -= entries;
            }
            return eligible[^1];
        }
    }
}
