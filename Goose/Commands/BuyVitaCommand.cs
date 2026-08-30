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

            decimal buyrate = 0;

            for (int i = 1; i <= buys; i++)
            {
                buyrate =
                    ((ctx.Player.BaseStats.HP / world.Settings.IncreaseVitaBuyAmount) * (decimal).2) + 1;

                if (ctx.Player.Experience >= ctx.Player.Class.VitaCost * buyrate)
                {
                    ctx.Player.Experience -= (long)(ctx.Player.Class.VitaCost * buyrate);
                    ctx.Player.ExperienceSold += (long)(ctx.Player.Class.VitaCost * buyrate);
                    ctx.Player.BaseStats.HP += world.Settings.VitaBuyAmount;
                    bought += world.Settings.VitaBuyAmount;
                    soldexp += (long)(ctx.Player.Class.VitaCost * buyrate);
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
