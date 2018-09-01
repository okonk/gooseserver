using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /*
     * SIDslotNo
     * 
     */
    public class SpellInfoEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new SpellInfoEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State != Player.States.Ready) return;

            string data = ((string)this.Data).Substring(3);
            if (data.Length <= 0) return;

            int slotNo = 0;
            int.TryParse(data, out slotNo);

            if (slotNo <= 0 || slotNo > GameSettings.Default.SpellbookSize) return;

            var spell = this.Player.Spellbook.GetSlot(slotNo);
            if (spell == null) return;

            SpellInfoWindow.Open(world, this.Player, spell);
        }
    }
}
