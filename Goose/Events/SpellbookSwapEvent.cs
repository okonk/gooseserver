using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /**
     * SpellbookSwapEvent, swap two spell in spellbook
     * 
     * Packet: SWAP id1, id2
     * 
     * Not sure why the spaces are there but k
     * 
     */
    public class SpellbookSwapEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new SpellbookSwapEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                int id1 = 0;
                int id2 = 0;
                string[] ids = ((string)this.Data).Substring(4).Split(",".ToCharArray());

                try
                {
                    id1 = Convert.ToInt32(ids[0]);
                    id2 = Convert.ToInt32(ids[1]);
                }
                catch (Exception)
                {
                    id1 = 0;
                    id2 = 0;
                }

                if (id1 <= 0 || id2 <= 0 ||
                    id1 > GameWorld.Settings.SpellbookSize || id2 > GameWorld.Settings.SpellbookSize)
                {
                    // log something bad about packet
                    return;
                }

                this.Player.Spellbook.SwapSlots(id1, id2, world);
            }
        }
    }
}
