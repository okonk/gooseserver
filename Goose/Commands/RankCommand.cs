namespace Goose.Commands
{
    [Command("/rank", Section = "General", Help = "Show rank lists for all, gold, or a specific class.")]
    public sealed class RankCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, string? arg = null)
        {
            var world = ctx.World;

            if (arg is null)
            {
                world.Send(ctx.Player, P.ServerMessage("Usage: /rank [all, gold, <classname>]"));
                return;
            }

            Window window;
            var argumentLower = arg.ToLowerInvariant();
            switch (argumentLower)
            {
                case "all":
                    window = new Window();
                    window.Type = Window.WindowTypes.Rank;
                    window.Title = "All Ranks";
                    window.Buttons = "0,0,0,0,0";
                    window.Data = world.RankHandler.All;
                    break;
                case "gold":
                    window = new Window();
                    window.Type = Window.WindowTypes.Rank;
                    window.Title = "Gold Ranks";
                    window.Buttons = "0,0,0,0,0";
                    window.Data = world.RankHandler.Gold;
                    break;
                default:
                    if (!world.RankHandler.ClassRanks.TryGetValue(argumentLower, out Ranks? classRank))
                    {
                        world.Send(ctx.Player, P.ServerMessage("Usage: /rank [all, gold, <classname>]"));
                        return;
                    }

                    window = new Window();
                    window.Type = Window.WindowTypes.Rank;
                    window.Title = $"{System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(argumentLower)} Ranks";
                    window.Buttons = "0,0,0,0,0";
                    window.Data = classRank;

                    break;
            }

            ctx.Player.Windows.Add(window);
            window.Create(ctx.Player, world);
        }
    }
}
