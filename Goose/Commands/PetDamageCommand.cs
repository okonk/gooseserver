namespace Goose.Commands
{
    [Command("/petdamage ", Section = "Pets", Help = "Buy weapon damage for one of your pets with experience.")]
    public sealed class PetDamageCommand : BaseCommand
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

            decimal buyrate = 0;
            long expcost;

            for (int i = 1; i <= buys; i++)
            {
                buyrate =
                    ((match.WeaponDamage / world.Settings.IncreasePetDamageBuyCost) * (decimal).2) + 1;
                expcost = (long)(world.Settings.PetDamageCost * buyrate);

                if (match.Experience >= expcost)
                {
                    match.Experience -= expcost;
                    match.ExperienceSold += expcost;
                    match.WeaponDamage += world.Settings.PetDamageBuyAmount;
                    bought += world.Settings.PetDamageBuyAmount;
                    soldexp += expcost;
                }
                else
                {
                    break;
                }
            }

            if (bought == 0) return;

            world.Send(ctx.Player, P.ServerMessage("Bought " + bought + " damage for " + soldexp + " experience."));
        }
    }
}
