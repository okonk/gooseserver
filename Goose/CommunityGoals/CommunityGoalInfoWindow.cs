using System.Text.Json;
using System.Text.Json.Nodes;

namespace Goose.CommunityGoals
{
    public class CommunityGoalInfoWindow : Window
    {
        private readonly CommunityGoal goal;
        private readonly CommunityGoalRun run;
        private readonly Action<Player, GameWorld>? onBack;

        public CommunityGoalInfoWindow(CommunityGoal goal, CommunityGoalRun run, NPC npc, Player player,
                                       GameWorld world, Action<Player, GameWorld>? onBack)
        {
            this.ID = ++player.LastWindowID;
            this.Title = goal.Name;
            this.Frame = WindowFrames.Quest;
            this.Type = WindowTypes.CommunityGoal;
            this.NPC = npc;
            this.Buttons = $"0,1,{(onBack is not null ? 1 : 0)},{(run.Status == RunStatus.Active ? 1 : 0)},0";
            this.goal = goal;
            this.run = run;
            this.onBack = onBack;

            player.Windows.Add(this);
            this.SendCreate(player, world);
        }

        public override void Populate(Player player, GameWorld world)
        {
            var lines = new List<string>(TextWrap.Lines(this.goal.Description));
            lines.Add("");
            foreach (var statusLine in this.StatusLines(world))
                lines.AddRange(TextWrap.Lines(statusLine));
            var lineNo = 1;
            foreach (var line in lines)
                world.Send(player, P.WindowTextLine(this.ID, lineNo++, line));
        }

        private List<string> StatusLines(GameWorld world)
        {
            if (this.run.Status == RunStatus.Active)
            {
                var lines = new List<string>
                {
                    $"Progress: {world.CommunityGoals.GetSummaryText(this.goal, this.run, world)}",
                };
                var timeLeft = CommunityGoalWindow.TimeLeft(this.goal, this.run, world);
                if (timeLeft is not null)
                    lines.Add($"Ends in {CommunityGoalWindow.FormatTimeLeft(timeLeft.Value)}");
                return lines;
            }
            var finished = new List<string>();
            if (this.run.CompletedAt is DateTime completedAt)
                finished.Add($"{(this.run.Status == RunStatus.Failed ? "Failed" : "Completed")} {completedAt:yyyy-MM-dd}");
            if (this.run.Results is JsonObject results && results["places"] is JsonArray places)
            {
                var i = 1;
                foreach (var place in places)
                {
                    if (place is not JsonValue value || value.GetValueKind() != JsonValueKind.Number) continue;
                    var name = world.PlayerHandler.GetPlayerName(value.GetValue<int>()) ?? "Unknown";
                    finished.Add($"{i}{Ordinal(i)}: {name}");
                    i++;
                }
            }
            return finished;
        }

        private static string Ordinal(int n)
        {
            if ((n % 100) is 11 or 12 or 13) return "th";
            return (n % 10) switch
            {
                1 => "st",
                2 => "nd",
                3 => "rd",
                _ => "th",
            };
        }

        public override void Clicked(ButtonTypes buttonid, int npcid, int id2, int id3, Player player, GameWorld world)
        {
            switch (buttonid)
            {
                case ButtonTypes.Exit:
                case ButtonTypes.Close:
                    player.Windows.Remove(this);
                    break;
                case ButtonTypes.Next:
                    if (this.run.Status == RunStatus.Active)
                    {
                        this.Close(player, world);
                        CommunityGoalWindow.OpenContribute(this.goal, this.run, this.NPC!, player, world, this.onBack);
                    }
                    break;
                case ButtonTypes.Back:
                    this.Close(player, world);
                    this.onBack?.Invoke(player, world);
                    break;
                default:
                    player.Windows.Remove(this);
                    break;
            }
        }
    }
}
