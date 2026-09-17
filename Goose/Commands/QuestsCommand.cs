using Goose.Quests;

namespace Goose.Commands
{
    [Command("/quests", Section = "General", Help = "Show your active quests.")]
    public sealed class QuestsCommand : BaseCommand
    {
        public void Execute(CommandContext ctx)
        {
            var player = ctx.Player;
            var world = ctx.World;

            var active = player.QuestsStarted
                .Where(q => !(player.QuestsCompleted.Any(c => c.Id == q.Id) && !q.Repeatable))
                .ToList();

            if (active.Count == 0)
            {
                ctx.Send("You have no active quests.");
                return;
            }

            foreach (var w in player.Windows.Where(w => w is QuestInfoWindow
                || (w.Type == Window.WindowTypes.OptionList && w.NPC is null)).ToList())
                w.Close(player, world);

            if (active.Count == 1)
            {
                new QuestInfoWindow(active[0], player, world);
                return;
            }

            var lines = active
                .Select(q =>
                {
                    var npcName = FindGrantingNpc(world, q.Id);
                    return npcName is null ? q.Name : $"{q.Name} ({npcName})";
                })
                .ToList();

            new OptionListWindow(player, world, "Active Quests", lines,
                (line, p, w) => new QuestInfoWindow(active[line], p, w), null);
        }

        private static string? FindGrantingNpc(GameWorld world, int questId)
        {
            foreach (var template in world.NPCHandler.GetTemplates())
            {
                if (template.Quests.Any(q => q.Id == questId))
                    return template.Name;
            }

            return null;
        }
    }
}
