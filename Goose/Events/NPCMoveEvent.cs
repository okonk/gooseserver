using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /**
     * NPCMoveEvent
     * 
     */
    public class NPCMoveEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.NPC.State == NPC.States.Alive)
            {
                this.NPC.MoveEvent = null;

                foreach (Buff b in this.NPC.Buffs)
                {
                    // can't move when stunned or rooted
                    if (b.SpellEffect.EffectType == SpellEffect.EffectTypes.Stun ||
                        b.SpellEffect.EffectType == SpellEffect.EffectTypes.Root)
                    {
                        this.NPC.AddMoveEvent(world);
                        return;
                    }
                }

                int direction;
                if (this.NPC.AggroTarget == null)
                {
                    if (this.NPC.MoveSpeed <= Decimal.Zero) return;
                    if (!this.NPC.CanMove)
                    {
                        // Return to spawn point if stationary npc
                        if (this.NPC.MapX != this.NPC.SpawnX || this.NPC.MapY != this.NPC.SpawnY)
                        {
                            direction = this.NPC.NextStepTo(this.NPC.SpawnX, this.NPC.SpawnY, world);
                        }
                        else
                        {
                            return;
                        }
                    }
                    else
                    {
                        // if outside of spawn area, try to move back to it
                        if (Math.Abs(this.NPC.MapX - this.NPC.SpawnX) > 10 || Math.Abs(this.NPC.MapY - this.NPC.SpawnY) > 10)
                        {
                            direction = this.NPC.NextStepTo(this.NPC.SpawnX, this.NPC.SpawnY, world);
                        }
                        else
                        {
                            direction = world.Random.Next(1, 5);
                        }
                    }
                }
                else
                {
                    // Fix being hit from across map bug.
                    // If the player isn't on same map/in screen range then lose aggro
                    if (this.NPC.AggroTarget.Map == this.NPC.Map &&
                        Math.Abs(this.NPC.AggroTarget.MapX - this.NPC.MapX) < Map.RANGE_X &&
                        Math.Abs(this.NPC.AggroTarget.MapY - this.NPC.MapY) < Map.RANGE_Y)
                    {
                        direction = 
                            this.NPC.NextStepTo(this.NPC.AggroTarget.MapX, this.NPC.AggroTarget.MapY, world);
                    }
                    else
                    {
                        this.NPC.AggroTargetToValue.Remove(this.NPC.AggroTarget);
                        this.NPC.AggroTarget = null;

                        List<Player> remove = new List<Player>();

                        Player highest = null;
                        NPC.Aggro aggro = new NPC.Aggro(0, 0);
                        foreach (KeyValuePair<Player, NPC.Aggro> p in this.NPC.AggroTargetToValue)
                        {
                            if (p.Value > aggro)
                            {
                                if (p.Key.Map == this.NPC.Map &&
                                    Math.Abs(p.Key.MapX - this.NPC.MapX) < Map.RANGE_X &&
                                    Math.Abs(p.Key.MapY - this.NPC.MapY) < Map.RANGE_Y)
                                {
                                    aggro = p.Value;
                                    highest = p.Key;
                                }
                                else
                                {
                                    remove.Add(p.Key);
                                }
                            }
                        }

                        foreach (Player p in remove)
                        {
                            this.NPC.AggroTargetToValue.Remove(p);
                        }

                        if (highest == null)
                        {
                            this.NPC.AddMoveEvent(world);
                            return;
                        }
                        else
                        {
                            this.NPC.AggroTarget = highest;
                            this.NPC.AggroValue = aggro;

                            direction = 
                                this.NPC.NextStepTo(this.NPC.AggroTarget.MapX, this.NPC.AggroTarget.MapY, world);
                        }
                    }
                }


                int ox = this.NPC.MapX;
                int oy = this.NPC.MapY;
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

                if (this.NPC.CanMoveTo(x, y))
                {
                    this.NPC.MoveTo(world, x, y);
                    this.NPC.Facing = direction;
                }
                else
                {
                    this.NPC.FaceTo(direction, world);
                }

                this.NPC.AddMoveEvent(world);
            }
        }
    }
}
