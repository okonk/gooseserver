using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    public class BuffTickEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            Buff buff = (Buff)this.Data;

            // Basically only item buffs are allowed to not expire
            if (!buff.ItemBuff && buff.BuffExpireEvent == null) return;

            if (buff.Target is NPC && ((NPC)buff.Target).State != NPC.States.Alive) return;
            if (buff.Target is Player && ((Player)buff.Target).State == Player.States.Ready)
            {
                if (buff.SpellEffect.EffectType == SpellEffect.EffectTypes.Tick)
                {
                    buff.SpellEffect.CastFormulaSpell(buff.Caster, buff.Target, world);
                }
                else if (buff.SpellEffect.EffectType == SpellEffect.EffectTypes.TickBuff ||
                    buff.SpellEffect.EffectType == SpellEffect.EffectTypes.Stun ||
                    buff.SpellEffect.EffectType == SpellEffect.EffectTypes.Root)
                {
                    if (buff.SpellEffect.Animation != 0)
                    {
                        List<Player> range = buff.Target.Map.GetPlayersInRange(buff.Target);

                        string packet = "SPP" + buff.Target.LoginID + "," + buff.SpellEffect.Animation;

                        if (buff.Target is Player) world.Send((Player)buff.Target, packet);
                        foreach (Player player in range)
                        {
                            world.Send(player, packet);
                        }
                    }
                }
                else if (buff.SpellEffect.EffectType == SpellEffect.EffectTypes.Viral)
                {
                    buff.SpellEffect.CastFormulaSpell(buff.Caster, buff.Target, world);
                    buff.SpellEffect.Cast(buff.Target, buff.Target, world);
                }
            }

            if (!buff.ItemBuff && buff.BuffExpireEvent != null)
            {
                // buff will expire before next tick
                if (buff.BuffExpireEvent.Ticks - world.TimeNow < 
                    GameSettings.Default.SpellEffectPeriod * world.TimerFrequency)
                {
                    return;
                }
            }

            BuffTickEvent ev = new BuffTickEvent();
            ev.Data = buff;
            ev.Player = this.Player;
            ev.NPC = this.NPC;
            ev.Ticks += (long)(GameSettings.Default.SpellEffectPeriod * world.TimerFrequency);

            world.EventHandler.AddEvent(ev);
        }
    }
}
