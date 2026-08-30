namespace Goose.Commands
{
    [Command("/petvita ", Section = "Pets", Help = "Buy hit points for one of your pets with experience.")]
    public sealed class PetVitaCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, int petid, int buys = 1)
        {
            var world = ctx.World;

            long bought = 0;
            long soldexp = 0;

            if (buys <= 0)
            {
                world.Send(ctx.Player, P.ServerMessage("Invalid buy amount."));
                return;
            }
            if (petid <= 0)
            {
                world.Send(ctx.Player, P.ServerMessage("Invalid pet id."));
                return;
            }

            Pet? match = null;
            foreach (var pet in ctx.Player.Pets)
            {
                if (pet.PetID == petid)
                {
                    match = pet;
                    break;
                }
            }

            if (match is null)
            {
                world.Send(ctx.Player, P.ServerMessage("Couldn't find pet matching ID."));
                return;
            }

            if (match.Class.GetLevel(match.Level)?.Experience != 0) return;

            match.RemoveStats(match.BaseStats, world);

            decimal buyrate = 0;
            long expcost;

            for (int i = 1; i <= buys; i++)
            {
                buyrate =
                    ((match.BaseStats.HP / world.Settings.IncreasePetVitaBuyCost) * (decimal).2) + 1;
                expcost = (long)(world.Settings.PetVitaCost * buyrate);

                if (match.Experience >= expcost)
                {
                    match.Experience -= expcost;
                    match.ExperienceSold += expcost;
                    match.BaseStats.HP += world.Settings.PetVitaBuyAmount;
                    bought += world.Settings.PetVitaBuyAmount;
                    soldexp += expcost;
                }
                else
                {
                    break;
                }
            }

            match.AddStats(match.BaseStats, world);

            if (bought == 0) return;

            world.Send(ctx.Player, P.ServerMessage("Bought " + bought + " hp for " + soldexp + " experience."));
        }
    }
}
