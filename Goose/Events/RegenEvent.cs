using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /**
     * Character regen Event
     * 
     * Sends VPUid,hp %, mp % to everyone in range, including the player themselves
     * 
     */
    public class RegenEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.NPC != null)
            {
                if (this.NPC.State == NPC.States.Alive)
                {
                    if (this.NPC.AggroTarget != null)
                    {
                        this.NPC.CurrentHP += 
                            (int)Math.Round(this.NPC.MaxHP * this.NPC.MaxStats.HPPercentRegen, 0);
                        this.NPC.CurrentHP += 
                            this.NPC.MaxStats.HPStaticRegen;
                        this.NPC.CurrentMP += 
                            (int)Math.Round(this.NPC.MaxMP * this.NPC.MaxStats.MPPercentRegen, 0);
                        this.NPC.CurrentMP += 
                            this.NPC.MaxStats.MPStaticRegen;
                    }
                    else
                    {
                        this.NPC.CurrentHP += 
                            (int)Math.Round(this.NPC.MaxHP * 0.10, 0);
                    }

                    if (this.NPC.CurrentHP <= 0) this.NPC.CurrentHP = 1;
                    if (this.NPC.CurrentMP <= 0) this.NPC.CurrentMP = 1;

                    string packet = this.NPC.VPUString();
                    List<Player> range = this.NPC.Map.GetPlayersInRange(this.NPC);
                    foreach (Player p in range)
                    {
                        world.Send(p, packet);
                    }

                    this.NPC.RegenEventExists = false;

                    this.NPC.AddRegenEvent(world);

                    return;
                }

                this.NPC.RegenEventExists = false;
            }
            else
            {
                Player player = (Player)this.Data;

                if (player.State == Player.States.Ready)
                {
                    player.CurrentHP += (int)Math.Round(player.MaxHP * player.MaxStats.HPPercentRegen, 0);
                    player.CurrentHP += player.MaxStats.HPStaticRegen;
                    player.CurrentMP += (int)Math.Round(player.MaxMP * player.MaxStats.MPPercentRegen, 0);
                    player.CurrentMP += player.MaxStats.MPStaticRegen;

                    if (player.CurrentHP <= 0) player.CurrentHP = 1;
                    if (player.CurrentMP <= 0) player.CurrentMP = 1;

                    string packet = player.VPUString();

                    List<Player> range = player.Map.GetPlayersInRange(player);
                    world.Send(player, packet);
                    world.Send(player, player.SNFString());
                    foreach (Player p in range)
                    {
                        world.Send(p, packet);
                    }

                    player.RegenEventExists = false;

                    player.AddRegenEvent(world);

                    return;
                }

                player.RegenEventExists = false;
            }
        }
    }
}
