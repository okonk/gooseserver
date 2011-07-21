using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    public class NPCAttackEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            this.NPC.AttackEvent = null;

            if (this.NPC.State == NPC.States.Dead) return;
            if (this.NPC.AggroTarget == null) return;

            bool rooted = false;

            foreach (Buff b in this.NPC.Buffs)
            {
                // can't attack when stunned
                if (b.SpellEffect.EffectType == SpellEffect.EffectTypes.Stun)
                {
                    this.NPC.LastAttackTime = world.TimeNow;
                    this.NPC.AddAttackEvent(world);
                    return;
                }

                if (b.SpellEffect.EffectType == SpellEffect.EffectTypes.Root)
                {
                    rooted = true;
                    this.NPC.LastAttackTime = world.TimeNow;
                }

            }

            // Can't tele when rooted
            if (!rooted && this.NPC.LastAttackTime + this.NPC.BehaviourTimeout * world.TimerFrequency <
                world.TimeNow)
            {
                switch (this.NPC.Behaviour)
                {
                    case NPCTemplate.BehaviourTypes.TeleportAggro:
                        bool loseaggro = true;
                        this.NPC.AggroTarget.WarpTo(world, this.NPC.Map, 
                            this.NPC.MapX, this.NPC.MapY, !loseaggro);
                        // reset attack time so doesn't keep teleporting if it can't attack
                        this.NPC.LastAttackTime = world.TimeNow;
                        break;
                    case NPCTemplate.BehaviourTypes.TeleportToAggro:
                        // move off this square so null
                        this.NPC.Map.SetCharacter(null, this.NPC.MapX, this.NPC.MapY);
                        this.NPC.MapX = this.NPC.AggroTarget.MapX;
                        this.NPC.MapY = this.NPC.AggroTarget.MapY;
                        this.NPC.Map.PlaceCharacter(this.NPC);
                        this.NPC.MoveTo(world, this.NPC.MapX, this.NPC.MapY);
                        // kinda hackish, moveto will set the aggrotarget char to null so have to reset it
                        //this.NPC.Map.SetCharacter(this.NPC.AggroTarget, 
                        //    this.NPC.AggroTarget.MapX, this.NPC.AggroTarget.MapX);
                        // reset attack time so doesn't keep teleporting if it can't attack
                        this.NPC.LastAttackTime = world.TimeNow;
                        break;
                }
            }

            Player aggro = this.NPC.AggroTarget;
            // Try to attack main aggro first
            if (this.NPC.Map == aggro.Map &&
                Math.Abs(this.NPC.MapX - aggro.MapX) <= this.NPC.AttackRange &&
                Math.Abs(this.NPC.MapY - aggro.MapY) <= this.NPC.AttackRange)
            {
                this.NPC.Attack(aggro, world);
                this.NPC.AddAttackEvent(world);

                return;
            }

            foreach (Player player in this.NPC.AggroTargetToValue.Keys)
            {
                if (player == aggro) continue;

                if (this.NPC.Map == player.Map &&
                    Math.Abs(this.NPC.MapX - player.MapX) <= this.NPC.AttackRange &&
                    Math.Abs(this.NPC.MapY - player.MapY) <= this.NPC.AttackRange)
                {
                    this.NPC.Attack(player, world);
                    this.NPC.AddAttackEvent(world);

                    return;
                }
            }

            this.NPC.AddAttackEvent(world);
        }
    }
}
