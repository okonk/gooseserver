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

                foreach (var b in pet.Buffs)
                {
                    // can't move when stunned or rooted
                    if (b.SpellEffect.EffectType == SpellEffect.EffectTypes.Stun ||
                        b.SpellEffect.EffectType == SpellEffect.EffectTypes.Root)
                    {
                        pet.AddMoveEvent(world);
                        return;
                    }
                }

                if (pet.Target is not null && 
                    (pet.Target.Map != pet.Map || (pet.Target is NPC && ((NPC)pet.Target).State != Goose.NPC.States.Alive)))
                {
                    pet.Target = null;

                    if (pet.Mode == Pet.Modes.Attack)
                    {
                        pet.Mode = Pet.Modes.Neutral;
                    }
                }

                Direction direction = Direction.Up;
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
                        if (pet.Target is null)
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

                (x, y) = direction switch
                {
                    Direction.Up => (x, y - 1),
                    Direction.Right => (x + 1, y),
                    Direction.Down => (x, y + 1),
                    Direction.Left => (x - 1, y),
                    _ => (x, y),
                };

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
