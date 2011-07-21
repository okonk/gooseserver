using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    public class BuffExpireEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            Buff buff = (Buff)this.Data;

            if (buff.BuffExpireEvent == null) return;

            if (buff.Target is NPC && ((NPC)buff.Target).State == NPC.States.Dead) return;
            else if (buff.Target is Player && ((Player)buff.Target).State == Player.States.NotLoggedIn) return;

            if (world.TimeNow - buff.TimeCast >= buff.SpellEffect.Duration * world.TimerFrequency)
            {
                buff.BuffExpireEvent = null;
                if (buff.Target is NPC) this.NPC.RemoveBuff(buff, world);
                else this.Player.RemoveBuff(buff, world);
            }
            else
            {
                BuffExpireEvent ev = new BuffExpireEvent();
                ev.Data = buff;
                ev.Player = this.Player;
                ev.NPC = this.NPC;
                ev.Ticks = buff.TimeCast + buff.SpellEffect.Duration * world.TimerFrequency;

                world.EventHandler.AddEvent(ev);
                buff.BuffExpireEvent = ev;
            }
        }
    }
}
