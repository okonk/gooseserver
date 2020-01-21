using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    public class PetVitaCommandEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new PetVitaCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                string[] data = ((string)this.Data).Split(" ".ToCharArray(), 3, StringSplitOptions.RemoveEmptyEntries);

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

                match.RemoveStats(match.BaseStats, world);

                decimal buyrate = 0;
                long expcost;

                for (int i = 1; i <= buys; i++)
                {
                    buyrate =
                        ((match.BaseStats.HP / GameSettings.Default.IncreasePetVitaBuyCost) * (decimal).2) + 1;
                    expcost = (long)(GameSettings.Default.PetVitaCost * buyrate);

                    if (match.Experience >= expcost)
                    {
                        match.Experience -= expcost;
                        match.ExperienceSold += expcost;
                        match.BaseStats.HP += GameSettings.Default.PetVitaBuyAmount;
                        bought += GameSettings.Default.PetVitaBuyAmount;
                        soldexp += expcost;
                    }
                    else
                    {
                        break;
                    }
                }

                match.AddStats(match.BaseStats, world);

                if (bought == 0) return;

                world.Send(this.Player, P.ServerMessage("Bought " + bought + " hp for " + soldexp + " experience."));
            }
        }
    }
}
