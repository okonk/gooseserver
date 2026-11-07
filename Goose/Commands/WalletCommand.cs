namespace Goose.Commands
{
    [Command("/wallet", Section = "General", Help = "Show your currency balances.")]
    public sealed class WalletCommand : BaseCommand
    {
        public void Execute(CommandContext ctx)
        {
            var lines = new List<string>();
            var graphics = new List<(int Sheet, int Graphic, int R, int G, int B, int A)>();
            foreach (var currency in ctx.World.CurrencyHandler.All)
            {
                lines.Add($"{Capitalise(currency.Name)}: {currency.GetBalance(ctx.Player):N0}");
                var icon = currency.Icon;
                graphics.Add(icon is { } i ? (i.Sheet, i.Graphic, 0, 0, 0, 0) : (0, 0, 0, 0, 0, 0));
            }
            new OptionListWindow(ctx.Player, ctx.World, "Wallet", lines,
                (_, _, _) => { }, null, 0, graphics);
        }

        private static string Capitalise(string name)
        {
            if (name.Length == 0) return name;
            return char.ToUpperInvariant(name[0]) + name.Substring(1);
        }
    }
}
