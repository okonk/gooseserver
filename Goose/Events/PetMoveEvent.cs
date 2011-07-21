using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    public class PetMoveEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            Pet pet = (Pet)this.Player;
            pet.MoveEvent = null;

            if (pet.IsAlive)
            {
                if (pet.Owner.Map != pet.Map)
                {
                    pet.Destroy(world);
                    return;
                }

                foreach (Buff b in pet.Buffs)
                {
                    // can't move when stunned or rooted
                    if (b.SpellEffect.EffectType == SpellEffect.EffectTypes.Stun ||
                        b.SpellEffect.EffectType == SpellEffect.EffectTypes.Root)
                    {
                        pet.AddMoveEvent(world);
                        return;
                    }
                }

                if (pet.Target != null && 
                    (pet.Target.Map != pet.Map || (pet.Target is NPC && ((NPC)pet.Target).State != Goose.NPC.States.Alive)))
                {
                    pet.Target = null;

                    if (pet.Mode == Pet.Modes.Attack)
                    {
                        pet.Mode = Pet.Modes.Neutral;
                    }
                }

                int direction = 1;
                switch (pet.Mode) 
                {
                    case Pet.Modes.Neutral:
                        direction = pet.NextStepTo(pet.Owner.MapX + world.Random.Next(-2, 2), 
                                                   pet.Owner.MapY + world.Random.Next(-2, 2), 
                                                   world);
                        break;
                    case Pet.Modes.Follow:
                        direction = pet.NextStepTo(pet.Owner.MapX, pet.Owner.MapY, world);
                        break;
                    case Pet.Modes.Defend:
                    case Pet.Modes.Attack:
                        if (pet.Target == null)
                        {
                            direction = pet.NextStepTo(pet.Owner.MapX + world.Random.Next(-2, 2),
                                                   pet.Owner.MapY + world.Random.Next(-2, 2),
                                                   world);
                        }
                        else
                        {
                            direction = pet.NextStepTo(pet.Target.MapX, pet.Target.MapY, world);
                        }
                        break;
                }

                int ox = pet.MapX;
                int oy = pet.MapY;
                int x = ox;
                int y = oy;

                switch (direction)
                {
                    case 1:
                        y--;
                        break;
                    case 2:
                        x++;
                        break;
                    case 3:
                        y++;
                        break;
                    case 4:
                        x--;
                        break;
                }

                if (pet.CanMoveTo(x, y))
                {
                    pet.MoveTo(world, x, y);
                    pet.Facing = direction;
                }
                else
                {
                    pet.FaceTo(direction, world);
                }

                pet.AddMoveEvent(world);
            }
        }
    }
}
