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

                if (id <= 0 || id > world.Configuration.SpellbookSize) return;

                this.Player.Spellbook.RemoveSpell(id, world);
            }
        }
    }
}
