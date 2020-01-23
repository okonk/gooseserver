using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /**
     * SpellbookNextEvent, put spell on previous page
     * 
     * SBBslot
     * 
     */
    public class SpellbookNextEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new SpellbookNextEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                int slot = 0;
                string id = ((string)this.Data).Substring(3);

                try
                {
                    slot = Convert.ToInt32(id);
                }
                catch (Exception)
                {
                    slot = 0;
                }

                if (slot <= 0 || slot > GameWorld.Settings.SpellbookSize)
                {
                    // log something bad about packet
                    return;
                }

                // find next empty slot
                int nextSlot = this.Player.Spellbook.NextFreeSlot((Math.Min(2, ((slot - 1) / 30) + 1) * 30) + 1);
                // no free slots
                if (nextSlot == -1) return;

                this.Player.Spellbook.SwapSlots(slot, nextSlot, world);
            }
        }
    }
}