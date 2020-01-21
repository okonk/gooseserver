using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    public class PetAttackEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            Pet pet = (Pet)this.Player;
            pet.AttackEvent = null;

            if (!pet.IsAlive) return;
            if (pet.Target == null || pet.Target.Map != pet.Map)
            {
                if (pet.Mode == Pet.Modes.Attack)
                {
                    pet.Mode = Pet.Modes.Neutral;
                    return;
                }

                pet.Target = null;
                pet.AddAttackEvent(world);
                return;
            }

            if (pet.Target is NPC && ((NPC)pet.Target).State == Goose.NPC.States.Dead)
            {
                pet.Mode = Pet.Modes.Neutral;
                pet.Target = null;
                return;
            }

            foreach (Buff b in pet.Buffs)
            {
                // can't attack when stunned
                if (b.SpellEffect.EffectType == SpellEffect.EffectTypes.Stun)
                {
                    pet.LastAttack = world.TimeNow;
                    pet.AddAttackEvent(world);
                    return;
                }
            }

            if (pet.Owner.IsIdle(world))
            {
                pet.Destroy(world);
                return;
            }

            if (Math.Abs(pet.MapX - pet.Target.MapX) <= pet.AttackRange &&
                Math.Abs(pet.MapY - pet.Target.MapY) <= pet.AttackRange)
            {
                List<Player> range = pet.Map.GetPlayersInRange(pet);

                string packet = P.Attack(pet);
                foreach (Player player in range)
                {
                    world.Send(player, packet);
                }

                pet.Attack(pet.Target, world);
                pet.AddAttackEvent(world);

                return;
            }

            pet.AddAttackEvent(world);
        }
    }
}
