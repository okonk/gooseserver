using System.Globalization;
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
                    if (!requirement.Target.HasValue)
                        continue;
                    run.Totals.TryGetValue(requirement.Key, out var contributed);
                    achieved += Math.Min(contributed, requirement.Target.Value);
                    total += requirement.Target.Value;
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
            return null;
        }
    }
}
