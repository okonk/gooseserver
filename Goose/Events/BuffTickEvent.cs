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
            if (buff.Target is Player && ((Player)buff.Target).State != Player.States.Ready)
            {
                CheckAndAddBuffTickEvent(world, buff);
                return;
            }

            if (buff.SpellEffect.Script != null)
            {
                try
                {
                    buff.SpellEffect?.Script?.Object.OnBuffTick(buff, world);
                }
                catch (Exception e) { }
            }
            else if (buff.SpellEffect.EffectType == SpellEffect.EffectTypes.Tick)
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

                    string packet = P.SpellPlayer(buff.Target.LoginID, buff.SpellEffect.Animation, buff.SpellEffect.AnimationFile);

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

            CheckAndAddBuffTickEvent(world, buff);
        }

        private void CheckAndAddBuffTickEvent(GameWorld world, Buff buff)
        {
            if (!buff.ItemBuff && buff.BuffExpireEvent != null)
            {
                // buff will expire before next tick
                if (world.TimeNow - buff.TimeCast >= (buff.SpellEffect.Duration - GameWorld.Settings.SpellEffectPeriod) * world.TimerFrequency)
                {
                    return;
                }
            }

            BuffTickEvent ev = new BuffTickEvent();
            ev.Data = buff;
            ev.Player = this.Player;
            ev.NPC = this.NPC;
            ev.Ticks += (long)(GameWorld.Settings.SpellEffectPeriod * world.TimerFrequency);

            world.EventHandler.AddEvent(ev);
        }
    }
}
