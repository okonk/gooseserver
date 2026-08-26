using System.Text;

namespace Goose.Events
{
    public class BuyVitaCommandEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                // can't sell exp when not max level
                // this enables commoners to sell exp but uh so what
                if (this.Player.Class.GetLevel(this.Player.Level)!.Experience != 0) return;

                long bought = 0;
                long soldexp = 0;

                int buys = 1;

                try
                {
                    buys = Convert.ToInt32(((string)this.Data).Split(' ')[1]);
                }
                catch (Exception)
                {
                    buys = 1;
                }

                if (buys <= 0) return;

                this.Player.RemoveStats(this.Player.BaseStats, world, false);

                decimal buyrate = 0;

                for (int i = 1; i <= buys; i++)
                {
                    buyrate =
                        ((this.Player.BaseStats.HP / world.Settings.IncreaseVitaBuyAmount) * (decimal).2) + 1;

                    if (this.Player.Experience >= this.Player.Class.VitaCost * buyrate)
                    {
                        this.Player.Experience -= (long)(this.Player.Class.VitaCost * buyrate);
                        this.Player.ExperienceSold += (long)(this.Player.Class.VitaCost * buyrate);
                        this.Player.BaseStats.HP += world.Settings.VitaBuyAmount;
                        bought += world.Settings.VitaBuyAmount;
                        soldexp += (long)(this.Player.Class.VitaCost * buyrate);
                    }
                    else
                    {
                        break;
                    }
                }

                this.Player.AddStats(this.Player.BaseStats, world);

                if (bought == 0) return;

                world.Send(this.Player, P.ServerMessage("Bought " + bought + " hp for " + soldexp + " experience."));
                world.Send(this.Player, P.ExpBar(this.Player));
            }
        }
    }
}