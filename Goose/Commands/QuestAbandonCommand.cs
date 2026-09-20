using Goose.Quests;

namespace Goose.Commands
{
    [Command("/questabandon", Section = "General", Help = "Abandon one of your active quests.")]
    public sealed class QuestAbandonCommand : BaseCommand
    {
        public void Execute(CommandContext ctx)
        {
            var player = ctx.Player;
            var world = ctx.World;

            var active = QuestWindow.GetActiveQuests(player);

            if (active.Count == 0)
            {
                ctx.Send("You have no active quests.");
                return;
            }

            foreach (var w in player.Windows.Where(w => w is QuestInfoWindow
                || (w.Type == Window.WindowTypes.OptionList && w.NPC is null)).ToList())
                w.Close(player, world);

            new OptionListWindow(player, world, "Abandon Quest",
                active.Select(q =>
                {
                    var npcName = QuestWindow.FindGrantingNpc(world, q.Id);
                    return npcName is null ? q.Name : $"{q.Name} ({npcName})";
                }).ToList(),
                (line, p, w) => new AbandonConfirmWindow(active[line], p, w), null);
        }
    }
}
