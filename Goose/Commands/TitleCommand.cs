namespace Goose.Commands
{
    [Command("/title", Section = "General", Help = "Choose your displayed title.")]
    public sealed class TitleCommand : BaseCommand
    {
        public void Execute(CommandContext ctx)
        {
            var player = ctx.Player;
            var world = ctx.World;

            foreach (var w in player.Windows.Where(w => w.Type == Window.WindowTypes.OptionList && w.NPC is null).ToList())
                w.Close(player, world);

            var unlocked = player.UnlockedTitles()
                .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
                .ToList();
            var lines = unlocked
                .Select(t => string.Equals(t, player.Title, StringComparison.OrdinalIgnoreCase) ? t + " (current)" : t)
                .ToList();
            lines.Add("Clear");

            new OptionListWindow(player, world, "Titles", lines,
                (line, p, w) => p.SetTitle(line < unlocked.Count ? unlocked[line] : "", w), null);
        }
    }
}
