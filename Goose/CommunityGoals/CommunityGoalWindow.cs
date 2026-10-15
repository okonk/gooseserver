using System.Text.Json.Nodes;

namespace Goose.CommunityGoals
{
    public static class CommunityGoalWindow
    {
        public static void Handle(NPC npc, Player player, GameWorld world)
        {
            var handler = world.CommunityGoals;
            foreach (var w in player.Windows.Where(w => w.NPC == npc
                && (w.Type == Window.WindowTypes.CommunityGoal || w.Type == Window.WindowTypes.OptionList || w.Type == Window.WindowTypes.Amount)).ToList())
                w.Close(player, world);
            player.TalkedTo(npc, world);

            var prizes = handler.PendingPrizes(player, npc.NPCTemplateID);
            var active = new List<(CommunityGoal Goal, CommunityGoalRun Run)>();
            var now = handler.UtcNow();
            var finished = new List<(CommunityGoal Goal, CommunityGoalRun Run)>();
            foreach (var goal in handler.GoalsForNpc(npc.NPCTemplateID))
            {
                var run = handler.CurrentRun(goal.Id);
                if (run is { Status: RunStatus.Active })
                {
                    active.Add((goal, run));
                    continue;
                }
                if (run is not { Status: not RunStatus.Active, CompletedAt: DateTime completedAt }) continue;
                var window = goal.Definition?.EffectiveClaimWindow is TimeSpan claimWindow && claimWindow > TimeSpan.FromDays(14)
                    ? claimWindow
                    : TimeSpan.FromDays(14);
                if (completedAt + window > now)
                    finished.Add((goal, run));
            }

            var lines = new List<string>();
            foreach (var prize in prizes)
                lines.Add($"Claim: {prize.Goal.Name} (Run {prize.Run.Run})");
            foreach (var (goal, run) in active)
                lines.Add(ActiveLine(goal, run, world));
            foreach (var (goal, run) in finished)
                lines.Add($"{goal.Name} - {(run.Status == RunStatus.Completed ? "Completed" : "Failed")}");

            if (lines.Count == 0) return;
            if (prizes.Count == 0 && active.Count == 1 && finished.Count == 0)
            {
                OpenDescription(active[0].Goal, active[0].Run, npc, player, world, null);
                return;
            }
            new OptionListWindow(player, world, npc.Name, lines,
                (index, p, w) =>
                {
                    if (index < prizes.Count)
                    {
                        var prize = prizes[index];
                        var result = w.CommunityGoals.Claim(prize.Goal, prize.Run, p, npc, w);
                        if (result.Success)
                            Handle(npc, p, w);
                        else
                            w.Send(p, P.ServerMessage(result.Refusal!));
                        return;
                    }
                    var (goal, run) = index < prizes.Count + active.Count
                        ? active[index - prizes.Count]
                        : finished[index - prizes.Count - active.Count];
                    OpenDescription(goal, run, npc, p, w, (p, w) => Handle(npc, p, w));
                },
                npc,
                openingLine: "Community Goals");
        }

        internal static void OpenDescription(CommunityGoal goal, CommunityGoalRun run, NPC npc,
                                             Player player, GameWorld world, Action<Player, GameWorld>? onBack)
        {
            new CommunityGoalInfoWindow(goal, run, npc, player, world, onBack);
        }

        internal static void OpenContribute(CommunityGoal goal, CommunityGoalRun run, NPC npc,
                                            Player player, GameWorld world, Action<Player, GameWorld>? onBack)
        {
            var definition = goal.Definition!;
            run.Contributions.TryGetValue(player.PlayerID, out var mine);
            var lines = new List<string>();
            var graphics = new List<(int Sheet, int Graphic, int R, int G, int B, int A)>();
            foreach (var requirement in definition.Requirements)
            {
                (var line, var graphic) = RequirementLine(requirement, run, mine, world);
                if (definition.EntryCost is { } entryCost && entryCost.TryGetValue(requirement.Key, out var cost))
                {
                    long myEntries = mine is { } m ? Ranking.Entries(definition, m) : 0;
                    long allEntries = run.Contributions.Values.Sum(c => Ranking.Entries(definition, c));
                    line = $"Tickets: {cost:N0}g each - you hold {myEntries} of {allEntries}";
                }
                lines.Add(line);
                graphics.Add(graphic);
            }
            lines.Add("Top contributors");
            graphics.Add((0, 0, 0, 0, 0, 0));
            new OptionListWindow(player, world, goal.Name, lines,
                (index, p, w) =>
                {
                    if (index < definition.Requirements.Count)
                    {
                        var requirement = definition.Requirements[index];
                        if (requirement.Kind == RequirementKind.Kill && requirement.NpcTemplateId is int targetNpcId)
                        {
                            var targetName = w.NPCHandler.GetNPCTemplate(targetNpcId)?.Name ?? "Unknown NPC";
                            w.Send(p, P.ServerMessage($"Slay {targetName} to contribute."));
                        }
                        return;
                    }
                    OpenTopContributors(goal, run, npc, p, w, (p, w) => OpenContribute(goal, run, npc, p, w, onBack));
                },
                npc,
                lineGraphics: graphics,
                onBack: (p, w) => OpenDescription(goal, run, npc, p, w, onBack));
        }

        internal static void OpenTopContributors(CommunityGoal goal, CommunityGoalRun run, NPC npc,
                                                 Player player, GameWorld world, Action<Player, GameWorld> onBack)
        {
            var definition = goal.Definition!;
            var entries = new List<(int PlayerId, double Score)>();
            if (run.Status == RunStatus.Active)
            {
                foreach (var contribution in run.Contributions.Values
                             .Where(c => Ranking.Score(definition, c) > 0)
                             .OrderByDescending(c => Ranking.Score(definition, c))
                             .ThenBy(c => c.LastAt)
                             .ThenBy(c => c.PlayerId)
                             .Take(10))
                    entries.Add((contribution.PlayerId, Ranking.Score(definition, contribution)));
            }
            else if (run.Results is JsonObject results && results["top"] is JsonArray top)
            {
                foreach (var entry in top)
                {
                    if (entry is not JsonObject obj) continue;
                    entries.Add((obj["player"]?.GetValue<int>() ?? 0, obj["score"]?.GetValue<double>() ?? 0));
                }
            }
            var lines = entries.Count == 0
                ? new List<string> { "No contributions yet." }
                : entries.Select((e, i) => $"{i + 1}. {world.PlayerHandler.GetPlayerName(e.PlayerId) ?? "Unknown"} - {e.Score:N0}").ToList();
            new OptionListWindow(player, world, "Top contributors", lines, (_, _, _) => { }, npc, onBack: onBack);
        }

        private static string ActiveLine(CommunityGoal goal, CommunityGoalRun run, GameWorld world)
        {
            var line = $"{goal.Name} - {world.CommunityGoals.GetSummaryText(goal, run, world)}";
            var timeLeft = TimeLeft(goal, run, world);
            if (timeLeft is not null)
                line += $" - {FormatTimeLeft(timeLeft.Value)}";
            return line;
        }

        internal static TimeSpan? TimeLeft(CommunityGoal goal, CommunityGoalRun run, GameWorld world)
        {
            var definition = goal.Definition;
            if (definition is null) return null;
            var now = world.CommunityGoals.UtcNow();
            TimeSpan? left = null;
            if (definition.Duration is TimeSpan duration)
            {
                var d = run.StartedAt + duration - now;
                left = d < TimeSpan.Zero ? TimeSpan.Zero : d;
            }
            if (definition.ClosesAt is DateTime closesAt)
            {
                var c = closesAt - now;
                if (left is null || c < left)
                    left = c < TimeSpan.Zero ? TimeSpan.Zero : c;
            }
            return left;
        }

        internal static string FormatTimeLeft(TimeSpan t)
        {
            var parts = new List<string>();
            if (t.Days > 0) parts.Add($"{t.Days}d");
            if (t.Hours > 0) parts.Add($"{t.Hours}h");
            if (t.Minutes > 0) parts.Add($"{t.Minutes}m");
            if (parts.Count == 0) parts.Add("0m");
            return string.Join(" ", parts);
        }

        private static (string Line, (int Sheet, int Graphic, int R, int G, int B, int A) Graphic) RequirementLine(
            GoalRequirement requirement, CommunityGoalRun run, Contribution? mine, GameWorld world)
        {
            var total = run.Totals.GetValueOrDefault(requirement.Key);
            var my = mine?.Amounts.GetValueOrDefault(requirement.Key) ?? 0;
            (int Sheet, int Graphic, int R, int G, int B, int A) graphic = (0, 0, 0, 0, 0, 0);
            switch (requirement.Kind)
            {
                case RequirementKind.Item:
                    var template = requirement.ItemTemplateId is int itemId ? world.ItemHandler.GetTemplate(itemId) : null;
                    if (template is { } t)
                        graphic = (t.GraphicFile, t.GraphicTile, t.GraphicR, t.GraphicG, t.GraphicB, t.GraphicA);
                    return (ProgressLine(template?.Name ?? "Unknown item", total, requirement.Target, my), graphic);
                case RequirementKind.Gold:
                    var gold = world.ItemHandler.GetTemplate(world.Settings.GoldItemID);
                    if (gold is { } g)
                        graphic = (g.GraphicFile, g.GraphicTile, g.GraphicR, g.GraphicG, g.GraphicB, g.GraphicA);
                    return (ProgressLine(gold?.Name ?? "Gold", total, requirement.Target, my), graphic);
                case RequirementKind.Experience:
                    graphic = (world.Settings.ExperienceIconSheet, world.Settings.ExperienceIconId, 0, 0, 0, 0);
                    return (ProgressLine("Experience", total, requirement.Target, my), graphic);
                case RequirementKind.Kill:
                    var npcName = requirement.NpcTemplateId is int npcId
                        ? world.NPCHandler.GetNPCTemplate(npcId)?.Name ?? "Unknown NPC"
                        : "Unknown NPC";
                    return (ProgressLine(npcName, total, requirement.Target, my), graphic);
                default:
                    var label = requirement.Label ?? requirement.Key;
                    return (requirement.Target is long target ? $"{label} {total:N0} / {target:N0}" : $"{label} {total:N0}", graphic);
            }
        }

        private static string ProgressLine(string name, long total, long? target, long mine)
        {
            return target is long t
                ? $"{name} {total:N0} / {t:N0} (you: {mine:N0})"
                : $"{name} {total:N0} (you: {mine:N0})";
        }
    }
}
