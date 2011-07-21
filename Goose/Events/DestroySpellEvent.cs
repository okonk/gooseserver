using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /**
     * DestroySpellEvent, delete spell from spellbook
     * 
     */
    public class DestroySpellEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new DestroySpellEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                int id = 0;
                string data = ((string)this.Data).Substring(4);
                try
                {
                    id = Convert.ToInt32(data);
                }
                catch (Exception)
                {
                    id = 0;
                }

                if (id <= 0 || id > GameSettings.Default.SpellbookSize) return;

                this.Player.Spellbook.RemoveSpell(id, world);
            }
        }
    }
}
