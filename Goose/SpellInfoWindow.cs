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
                Utils.FormatDuration(this.spell.Aether), 
                (this.spell.SpellEffect.Duration == 0 ? "" : "Duration: " + Utils.FormatDuration(this.spell.SpellEffect.Duration * 1000))));
            world.Send(player, string.Format("WNF{0},{1},Target Type: {2}|0|0|0|0|*", this.ID, ++lineNo, Enum.GetName(typeof(Spell.SpellTargets), this.spell.Target)));
            world.Send(player, string.Format("WNF{0},{1},Effect: {2}|0|0|0|0|*", this.ID, ++lineNo, this.spell.SpellEffect.Name));

            foreach (var line in this.spell.SpellEffect.GetItemDescription(world))
            {
                if (lineNo > 10)
                    return;

                world.Send(player, string.Format("WNF{0},{1},  {2}|0|0|0|0|*", this.ID, ++lineNo, line));
            }
        }
    }
}
