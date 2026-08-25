using System.Text;

namespace Goose.Events
{
    public class PetDamageCommandEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                string[] data = ((string)this.Data).Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);

                long bought = 0;
                long soldexp = 0;

                int buys = 1;
                int petid = 0;

                try
                {
                    petid = Convert.ToInt32(data[1]);
                }
                catch (Exception)
                {
                    petid = 0;
                }
                try
                {
                    buys = Convert.ToInt32(data[2]);
                }
                catch (Exception)
                {
                    buys = 1;
                }

                if (buys <= 0)
                {
                    world.Send(this.Player, P.ServerMessage("Invalid buy amount."));
                    return;
                }
                if (petid <= 0)
                {
                    world.Send(this.Player, P.ServerMessage("Invalid pet id."));
                    return;
                }

                Pet match = null;
                foreach (Pet pet in this.Player.Pets)
                {
                    if (pet.PetID == petid)
                    {
                        match = pet;
                        break;
                    }
                }

                if (match == null)
                {
                    world.Send(this.Player, P.ServerMessage("Couldn't find pet matching ID."));
                    return;
                }

                if (match.Class.GetLevel(match.Level).Experience != 0) return;

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

                world.Send(this.Player, P.ServerMessage("Bought " + bought + " damage for " + soldexp + " experience."));
            }
        }
    }
}
