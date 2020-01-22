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
                world.Send(player, P.WindowTextLine(this.ID, ++lineNo, string.Format("HP Cost: {0:N0} / {1:N0}%", this.spell.HPStaticCost, this.spell.HPPercentCost)));

            if (this.spell.MPStaticCost != 0 || this.spell.MPPercentCost != 0)
                world.Send(player, P.WindowTextLine(this.ID, ++lineNo, string.Format("MP Cost: {0:N0} / {1:N0}%", this.spell.MPStaticCost, this.spell.MPPercentCost)));

            world.Send(player, P.WindowTextLine(this.ID, ++lineNo, string.Format("Cooldown: {0}  {1}",
                Utils.FormatDuration(this.spell.Aether), 
                (this.spell.SpellEffect.Duration == 0 ? "" : "Duration: " + Utils.FormatDuration(this.spell.SpellEffect.Duration * 1000)))));
            world.Send(player, P.WindowTextLine(this.ID, ++lineNo, string.Format("Target Type: {0}", Enum.GetName(typeof(Spell.SpellTargets), this.spell.Target))));
            world.Send(player, P.WindowTextLine(this.ID, ++lineNo, string.Format("Effect: {0}", this.spell.SpellEffect.Name)));

            foreach (var line in this.spell.SpellEffect.GetItemDescription(world))
            {
                if (lineNo > 10)
                    return;

                world.Send(player, P.WindowTextLine(this.ID, ++lineNo, string.Format("  {0}", line)));
            }
        }
    }
}
