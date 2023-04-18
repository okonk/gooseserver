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
                        this.NPC.CurrentHP = Math.Max(1,
                            this.NPC.CurrentHP +
                            (long)Math.Round(this.NPC.MaxHP * this.NPC.MaxStats.HPPercentRegen, 0)
                            + this.NPC.MaxStats.HPStaticRegen);

                        this.NPC.CurrentMP = Math.Max(1,
                            this.NPC.CurrentMP +
                            (long)Math.Round(this.NPC.MaxMP * this.NPC.MaxStats.MPPercentRegen, 0)
                            + this.NPC.MaxStats.MPStaticRegen);

                        this.NPC.CurrentSP = Math.Max(1,
                            this.NPC.CurrentSP +
                            (long)Math.Round(this.NPC.MaxSP * this.NPC.MaxStats.SPPercentRegen, 0)
                            + this.NPC.MaxStats.SPStaticRegen);
                    }
                    else
                    {
                        this.NPC.CurrentHP +=
                            (long)Math.Round(this.NPC.MaxHP * 0.10, 0);
                    }

                    string packet = P.VitalsPercentage(this.NPC);
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
                    player.CurrentHP = Math.Max(1,
                        player.CurrentHP
                        + (long)(Math.Round(player.MaxHP * player.MaxStats.HPPercentRegen, 0))
                        + player.MaxStats.HPStaticRegen);

                    player.CurrentMP = Math.Max(0,
                        player.CurrentMP
                        + (long)(Math.Round(player.MaxMP * player.MaxStats.MPPercentRegen, 0))
                        + player.MaxStats.MPStaticRegen);

                    if (player.SPRegenSwitch) {
                        player.CurrentSP = Math.Max(0,
                            player.CurrentSP
                            + (long)(Math.Round(player.MaxSP * player.MaxStats.SPPercentRegen, 0))
                            + player.MaxStats.SPStaticRegen);
                    }

                    // regen SP every 2 events (so SP regens at half the rate). Hack for now
                    player.SPRegenSwitch = !player.SPRegenSwitch;

                    string packet = P.VitalsPercentage(player);

                    List<Player> range = player.Map.GetPlayersInRange(player);
                    world.Send(player, packet);
                    world.Send(player, P.StatusInfo(player));
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
