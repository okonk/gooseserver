namespace Goose.Commands
{
    [Command("/surname", Section = "General", Help = "Choose your displayed surname.")]
    public sealed class SurnameCommand : BaseCommand
    {
        public void Execute(CommandContext ctx)
        {
            var player = ctx.Player;
            var world = ctx.World;

            foreach (var w in player.Windows.Where(w => w.Type == Window.WindowTypes.OptionList && w.NPC is null).ToList())
                w.Close(player, world);

            var unlocked = player.UnlockedSurnames()
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToList();
            var lines = unlocked
                .Select(s => string.Equals(s, player.Surname, StringComparison.OrdinalIgnoreCase) ? s + " (current)" : s)
                .ToList();
            lines.Add("Clear");

            new OptionListWindow(player, world, "Surnames", lines,
                (line, p, w) => p.SetSurname(line < unlocked.Count ? unlocked[line] : "", w), null);
        }
    }
}
