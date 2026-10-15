using System.Globalization;
using System.Text.Json.Nodes;
using Goose.CommunityGoals;

namespace Goose.Scripting
{
    public class BaseCommunityGoalScript : ICommunityGoalScript
    {
        public virtual CommunityGoalDefinition Define(CommunityGoal goal, GameWorld world)
        {
            return CommunityGoalDefinitionParser.Parse(goal.PropertiesJson);
        }

        public virtual string? CanContribute(CommunityGoal goal, CommunityGoalRun run, Player player, string key, long amount, GameWorld world)
        {
            return null;
        }

        public virtual void OnContributed(CommunityGoal goal, CommunityGoalRun run, Player? player, string key, long amount, GameWorld world)
        {
        }

        public virtual bool IsComplete(CommunityGoal goal, CommunityGoalRun run, GameWorld world)
        {
            return false;
        }

        public virtual void OnCompleted(CommunityGoal goal, CommunityGoalRun run, GameWorld world)
        {
        }

        public virtual string GetSummaryText(CommunityGoal goal, CommunityGoalRun run, GameWorld world)
        {
            var definition = goal.Definition;
            if (definition is null)
                return "0%";

            if (definition.HasTargets)
            {
                long achieved = 0;
                long total = 0;
                foreach (var requirement in definition.Requirements)
                {
                    if (world.CommunityGoals.TargetFor(requirement, run) is not long target)
                        continue;
                    run.Totals.TryGetValue(requirement.Key, out var contributed);
                    achieved += Math.Min(contributed, target);
                    total += target;
                }
                int percent = total > 0 ? (int)Math.Round(achieved * 100.0 / total) : 0;
                return percent + "%";
            }

            var first = definition.Requirements.FirstOrDefault();
            run.Totals.TryGetValue(first?.Key ?? "", out var amount);
            var text = amount.ToString("N0", CultureInfo.InvariantCulture);
            return first?.Label is null ? text : text + " " + first.Label;
        }

        public virtual GoalBracket? GetBracket(CommunityGoal goal, CommunityGoalRun run, Player player, GameWorld world)
        {
            var definition = goal.Definition;
            if (definition is null || !run.Contributions.TryGetValue(player.PlayerID, out var contribution))
                return null;
            List<int>? places = null;
            if (run.Results is JsonObject results && results["places"] is JsonArray placesArray)
                places = placesArray.Select(node => node!.GetValue<int>()).ToList();
            foreach (var bracket in definition.Brackets)
            {
                if (bracket.Rank is int rank)
                {
                    if (places is not null && places.IndexOf(player.PlayerID) == rank - 1)
                        return bracket;
                }
                else if (bracket.MinScore is double minScore)
                {
                    var value = bracket.Measure is null
                        ? Ranking.Score(definition, contribution)
                        : contribution.Amounts.GetValueOrDefault(bracket.Measure, 0);
                    if (value >= minScore)
                        return bracket;
                }
            }
            return null;
        }
    }
}
