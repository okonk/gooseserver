namespace Goose.Commands
{
    [Command("/buymana", Section = "General", Help = "Spend experience to buy more maximum MP.")]
    public sealed class BuyManaCommand : BaseCommand
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
                    ((ctx.Player.BaseStats.MP / world.Settings.IncreaseManaBuyAmount) * (decimal).2) + 1;

                if (ctx.Player.Experience >= (long)(ctx.Player.Class.ManaCost * buyrate))
                {
                    ctx.Player.Experience -= (long)(ctx.Player.Class.ManaCost * buyrate);
                    ctx.Player.ExperienceSold += (long)(ctx.Player.Class.ManaCost * buyrate);
                    ctx.Player.BaseStats.MP += world.Settings.ManaBuyAmount;
                    bought += world.Settings.ManaBuyAmount;
                    soldexp += (long)(ctx.Player.Class.ManaCost * buyrate);
                }
                else
                {
                    break;
                }
            }

            ctx.Player.AddStats(ctx.Player.BaseStats, world);

            if (bought == 0) return;

            world.Send(ctx.Player, P.ServerMessage("Bought " + bought + " mp for " + soldexp + " experience."));
            world.Send(ctx.Player, P.ExpBar(ctx.Player));
        }
    }
}
