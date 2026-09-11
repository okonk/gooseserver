namespace Goose.Commands
{
    [Command("/buyvita", Section = "General", Help = "Spend experience to buy more maximum HP.")]
    public sealed class BuyVitaCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, int buys = 1)
        {
            var world = ctx.World;

            // can't sell exp when not max level
            // this enables commoners to sell exp but uh so what
            if (ctx.Player.Class.GetLevel(ctx.Player.Level)?.Experience != 0) return;

            long bought = 0;
            long soldexp = 0;

            if (buys <= 0) return;

            ctx.Player.RemoveStats(ctx.Player.BaseStats, world, false);

            double buyrate = 0;

            for (int i = 1; i <= buys; i++)
            {
                buyrate =
                    ((ctx.Player.BaseStats.HP / world.Settings.IncreaseVitaBuyAmount) * 0.2) + 1;

                long cost = Utils.MultiplyAndTruncate(ctx.Player.Class.VitaCost, buyrate);
                if (ctx.Player.Experience >= cost)
                {
                    ctx.Player.Experience -= cost;
                    ctx.Player.ExperienceSold += cost;
                    ctx.Player.BaseStats.HP += world.Settings.VitaBuyAmount;
                    bought += world.Settings.VitaBuyAmount;
                    soldexp += cost;
                }
                else
                {
                    break;
                }
            }

            ctx.Player.AddStats(ctx.Player.BaseStats, world);

            if (bought == 0) return;

            world.Send(ctx.Player, P.ServerMessage("Bought " + bought + " hp for " + soldexp + " experience."));
            world.Send(ctx.Player, P.ExpBar(ctx.Player));
        }
    }
}
