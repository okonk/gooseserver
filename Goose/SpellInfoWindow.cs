using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goose
{
    class SpellInfoWindow : Window
    {
        private Spell spell;

        public SpellInfoWindow(GameWorld world, Player player, Spell spell)
        {
            this.ID = ++player.LastWindowID;
            this.Title = spell.Name;
            this.Buttons = "0,0,0,0,0";
            this.Frame = WindowFrames.GenericInfo;
            this.Type = Window.WindowTypes.SpellInfo;
            this.spell = spell;

            this.SendCreate(player, world);
        }

        public static void Open(GameWorld world, Player player, Spell spell)
        {
            new SpellInfoWindow(world, player, spell);
        }

        public override void Refresh(Player player, GameWorld world)
        {
            this.Populate(player, world);
        }

        public override void Populate(Player player, GameWorld world)
        {
            int lineNo = 0;
            if (this.spell.HPStaticCost != 0 || this.spell.HPPercentCost != 0)
                world.Send(player, string.Format("WNF{0},{1},HP Cost: {2:N0} / {3:N0}%|0|0|0|0|*", this.ID, ++lineNo, this.spell.HPStaticCost, this.spell.HPPercentCost));

            if (this.spell.MPStaticCost != 0 || this.spell.MPPercentCost != 0)
                world.Send(player, string.Format("WNF{0},{1},MP Cost: {2:N0} / {3:N0}%|0|0|0|0|*", this.ID, ++lineNo, this.spell.MPStaticCost, this.spell.MPPercentCost));

            world.Send(player, string.Format("WNF{0},{1},Cooldown: {2}  {3}|0|0|0|0|*", this.ID, ++lineNo, 
                FormatDuration(this.spell.Aether), 
                (this.spell.SpellEffect.Duration == 0 ? "" : "Duration: " + FormatDuration(this.spell.SpellEffect.Duration * 1000))));
            world.Send(player, string.Format("WNF{0},{1},Target Type: {2}|0|0|0|0|*", this.ID, ++lineNo, Enum.GetName(typeof(Spell.SpellTargets), this.spell.Target)));
            world.Send(player, string.Format("WNF{0},{1},Effect: {2}|0|0|0|0|*", this.ID, ++lineNo, this.spell.SpellEffect.Name));

            foreach (var line in this.spell.SpellEffect.GetItemDescription(world))
            {
                if (lineNo > 10)
                    return;

                world.Send(player, string.Format("WNF{0},{1},  {2}|0|0|0|0|*", this.ID, ++lineNo, line));
            }
        }

        private string FormatDuration(long cooldown)
        {
            TimeSpan t = TimeSpan.FromMilliseconds(cooldown);

            string cd = "";
            if (t.Hours != 0)
                cd += t.Hours + "h ";

            if (t.Minutes != 0)
                cd += t.Minutes + "m ";

            if (t.Seconds != 0)
            {
                double seconds = t.Seconds;
                if (t.Milliseconds != 0)
                {
                    seconds += t.Milliseconds / 1000.0d;
                    cd += string.Format("{0:N1}s ", seconds);
                }
                else
                {
                    cd += t.Seconds + "s ";
                }
            }
            else if (cd == "" || t.Milliseconds != 0)
            {
                cd += t.Milliseconds + "ms";
            }

            return cd;
        }
    }
}
