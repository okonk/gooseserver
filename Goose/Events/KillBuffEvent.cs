using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /**
     * KillBuffEvent, KBUFid event
     * 
     * When a player clicks a buff in buff bar to remove it.
     * 
     */
    public class KillBuffEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new KillBuffEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                string idstr = ((string)this.Data).Substring(4);

                if (idstr.Length <= 0) return; //log bad packet

                int id = 0;
                try
                {
                    id = Convert.ToInt32(idstr);
                }
                catch (InvalidCastException)
                {
                    id = 0;
                }

                var buffs = this.Player.Buffs.Where(b => !b.ItemBuff || this.Player.ShowItemBuffs).ToArray();

                if (id <= 0 || id > buffs.Length) return; // log bad packet

                Buff buff = buffs[id - 1];
                // I dunno if I should make item buffs able to be removed or not..
                if (!buff.SpellEffect.BuffCanBeRemoved) return;

                this.Player.RemoveBuff(buff, world, true);
            }
        }
    }
}

