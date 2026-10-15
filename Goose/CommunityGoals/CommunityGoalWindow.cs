using System.Text.Json.Nodes;

namespace Goose.CommunityGoals
{
    public static class CommunityGoalWindow
    {
        private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

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
                        else if (requirement.Kind is RequirementKind.Item or RequirementKind.Gold or RequirementKind.Experience)
                        {
                            OpenDonation(goal, requirement, npc, p, w, onBack);
                        }
                        return;
                    }
                    OpenTopContributors(goal, run, npc, p, w, (p, w) => OpenContribute(goal, run, npc, p, w, onBack));
                },
                npc,
                lineGraphics: graphics,
                onBack: (p, w) => OpenDescription(goal, run, npc, p, w, onBack));
        }

        internal static void OpenDonation(CommunityGoal goal, GoalRequirement requirement, NPC npc,
                                          Player player, GameWorld world, Action<Player, GameWorld>? onBack)
        {
            var definition = goal.Definition!;
            long? cost = definition.EntryCost is { } entryCost
                && entryCost.TryGetValue(requirement.Key, out var ticketCost) && ticketCost > 0
                ? ticketCost : null;
            var have = Have(player, requirement, world);
            var remaining = world.CommunityGoals.Remaining(goal, requirement.Key);
            long max;
            long defaultValue;
            string prompt;
            if (cost is long ticket)
            {
                max = Math.Min(have / ticket, remaining / ticket);
                defaultValue = 1;
                prompt = $"Buy how many tickets? ({ticket:N0}g each)";
            }
            else
            {
                max = Math.Min(have, remaining);
                defaultValue = max;
                prompt = remaining == long.MaxValue
                    ? $"Donate how many {DonationLabel(requirement, world)}? You have {have:N0}."
                    : $"Donate how many {DonationLabel(requirement, world)}? You have {have:N0}, {remaining:N0} still needed.";
            }
            if (max < 1)
            {
                world.Send(player, P.ServerMessage("You cannot donate to this goal right now."));
                return;
            }
            new AmountWindow(player, world, goal.Name, prompt, RequirementIcon(requirement, world),
                1, max, defaultValue,
                (units, p, w) => ConfirmDonation(goal, requirement, cost, npc, units, p, w, onBack), npc);
        }

        private static void ConfirmDonation(CommunityGoal goal, GoalRequirement requirement, long? cost, NPC npc,
                                            long units, Player player, GameWorld world, Action<Player, GameWorld>? onBack)
        {
            if (!Map.InRange(player, npc))
            {
                world.Send(player, P.ServerMessage("You are too far away."));
                return;
            }
            var run = world.CommunityGoals.CurrentRun(goal.Id);
            if (run is null || run.Status != RunStatus.Active || goal.Definition is null)
            {
                world.Send(player, P.ServerMessage("This goal has ended."));
                return;
            }
            var amount = units * (cost ?? 1);
            if (Have(player, requirement, world) < amount)
            {
                world.Send(player, P.ServerMessage("You cannot donate to this goal right now."));
                return;
            }
            var accepted = Math.Min(amount, world.CommunityGoals.Remaining(goal, requirement.Key));
            if (cost is long ticket)
                accepted -= accepted % ticket;
            if (accepted <= 0)
            {
                world.Send(player, P.ServerMessage("That's no longer needed."));
                return;
            }
            // Gate before take: a CanContribute refusal must not burn the player's resources.
            var refusal = world.CommunityGoals.CanContribute(goal, run, player, requirement.Key, accepted, world);
            if (refusal is not null)
            {
                world.Send(player, P.ServerMessage(refusal));
                return;
            }
            // Take before credit: a failed take must never be credited, and a credit is
            // never issued for more than was taken.
            if (!Take(player, requirement, accepted, world))
            {
                world.Send(player, P.ServerMessage("You cannot donate to this goal right now."));
                return;
            }
            var result = world.CommunityGoals.ContributeValidated(goal, player, requirement.Key, accepted, world);
            if (result.Accepted != accepted)
                log.Error("Community goal {goalId} run {run} credited {credited} of {taken} donated by {player}",
                    goal.Id, run.Run, result.Accepted, accepted, player.Name);
            // The credit can complete the run (or start a repeat run); reopening the
            // contribute window then would show a finished run as donatable.
            if (run.Status == RunStatus.Active && world.CommunityGoals.CurrentRun(goal.Id) == run)
                OpenContribute(goal, run, npc, player, world, onBack);
            else
                Handle(npc, player, world);
        }

        private static long Have(Player player, GoalRequirement requirement, GameWorld world)
        {
            return requirement.Kind switch
            {
                RequirementKind.Item => requirement.ItemTemplateId is int itemId
                    ? player.Inventory.GetInventoryItemFamilyCount(itemId) : 0,
                RequirementKind.Gold => player.Gold,
                RequirementKind.Experience => player.Experience,
                _ => 0,
            };
        }

        private static bool Take(Player player, GoalRequirement requirement, long amount, GameWorld world)
        {
            switch (requirement.Kind)
            {
                case RequirementKind.Item:
                    return requirement.ItemTemplateId is int itemId
                        && player.Inventory.TryRemoveInventoryItemFamily(itemId, amount, world);
                case RequirementKind.Gold:
                    if (player.Gold < amount) return false;
                    player.RemoveGold(amount, world);
                    return true;
                case RequirementKind.Experience:
                    if (player.Experience < amount) return false;
                    var before = player.Experience;
                    // applyModifiers: false grants exactly `amount` — the modifier-scaled and
                    // XP-cap early-return paths of AddExperience would otherwise break the
                    // take-equals-credit invariant.
                    player.AddExperience(-amount, world, Player.ExperienceMessage.None, applyModifiers: false);
                    return player.Experience == before - amount;
                default:
                    return false;
            }
        }

        private static string DonationLabel(GoalRequirement requirement, GameWorld world)
        {
            return requirement.Kind switch
            {
                RequirementKind.Item => requirement.ItemTemplateId is int itemId
                    ? world.ItemHandler.GetTemplate(itemId)?.Name ?? "Unknown item"
                    : "Unknown item",
                RequirementKind.Gold => world.ItemHandler.GetTemplate(world.Settings.GoldItemID)?.Name ?? "Gold",
                _ => "Experience",
            };
        }

        private static (int Sheet, int Graphic, int R, int G, int B, int A) RequirementIcon(
            GoalRequirement requirement, GameWorld world)
        {
            (int Sheet, int Graphic, int R, int G, int B, int A) icon = (0, 0, 0, 0, 0, 0);
            switch (requirement.Kind)
            {
                case RequirementKind.Item:
                    if (requirement.ItemTemplateId is int itemId && world.ItemHandler.GetTemplate(itemId) is { } t)
                        icon = (t.GraphicFile, t.GraphicTile, t.GraphicR, t.GraphicG, t.GraphicB, t.GraphicA);
                    break;
                case RequirementKind.Gold:
                    if (world.ItemHandler.GetTemplate(world.Settings.GoldItemID) is { } g)
                        icon = (g.GraphicFile, g.GraphicTile, g.GraphicR, g.GraphicG, g.GraphicB, g.GraphicA);
                    break;
                case RequirementKind.Experience:
                    icon = (world.Settings.ExperienceIconSheet, world.Settings.ExperienceIconId, 0, 0, 0, 0);
                    break;
            }
            return icon;
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
