using System.Text;

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
                world.Send(player, P.WindowTextLine(this.ID, ++lineNo, $"HP Cost: {this.spell.HPStaticCost:N0} / {this.spell.HPPercentCost:N0}%"));

            if (this.spell.MPStaticCost != 0 || this.spell.MPPercentCost != 0)
                world.Send(player, P.WindowTextLine(this.ID, ++lineNo, $"MP Cost: {this.spell.MPStaticCost:N0} / {this.spell.MPPercentCost:N0}%"));

            world.Send(player, P.WindowTextLine(this.ID, ++lineNo, $"Cooldown: {Utils.FormatDuration(this.spell.Aether)}  {(this.spell.SpellEffect.Duration == 0 ? "" : "Duration: " + Utils.FormatDuration(this.spell.SpellEffect.Duration * 1000))}"));
            world.Send(player, P.WindowTextLine(this.ID, ++lineNo, $"Target Type: {Enum.GetName(typeof(Spell.SpellTargets), this.spell.Target)}"));
            world.Send(player, P.WindowTextLine(this.ID, ++lineNo, $"Effect: {this.spell.SpellEffect.Name}"));

            foreach (var line in this.spell.SpellEffect.GetItemDescription(world))
            {
                if (lineNo > 10)
                    return;

                world.Send(player, P.WindowTextLine(this.ID, ++lineNo, $"  {line}"));
            }
        }
    }
}
