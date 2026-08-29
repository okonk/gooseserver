using System.Text;

namespace Goose.Events
{
    /**
     * MoveEvent, event for "M" + 1-4 packet
     *
     * Called when someone moves
     * Packet format: MDirection
     * Direction being a number 1-4 corresponding to 1,2,3,4 = up,right,down,left
     *
     * Server responds: MOCLoginID,X,Y
     * Server sends the response to everyone in the area excluding the player who generated it
     *
     */
    class MoveEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                this.Player.UpdateIdleStatus(world);

                foreach (var b in this.Player.Buffs)
                {
                    // can't move when stunned or rooted
                    if (b.SpellEffect.EffectType == SpellEffect.EffectTypes.Stun)
                    {
                        // stunned battletext
                        world.Send(this.Player, P.BattleTextStunned(this.Player));
                        // Fix the clients position
                        world.Send(this.Player, P.SetYourPosition(this.Player));
                        return;
                    }
                    else if (b.SpellEffect.EffectType == SpellEffect.EffectTypes.Root)
                    {
                        // rooted battletext
                        world.Send(this.Player, P.BattleTextRooted(this.Player));
                        // Fix the clients position
                        world.Send(this.Player, P.SetYourPosition(this.Player));
                        return;
                    }
                }

                foreach (var window in this.Player.Windows)
                {
                    if (window.Type == Window.WindowTypes.Vendor)
                    {
                        world.Send(this.Player, P.ServerMessage("You can't move while with a vendor."));
                        world.Send(this.Player, P.SetYourPosition(this.Player));
                        return;
                    }
                }

                if (((string)this.Data).Length == 1) return; // log bad move event

                int direction = Convert.ToInt32(((string)this.Data)[1].ToString());

                if (direction <= 0 || direction >= 5) return; // log bad move event

                /* Speedhack detection */
                if (world.Settings.SpeedhackDetectionEnabled)
                {
                    if (this.Player.MovementRecordingSteps == 0)
                    {
                        this.Player.MovementRecordingStarted = world.TimeNow;
                    }

                    this.Player.MovementRecordingSteps++;

                    if (this.Player.MovementRecordingSteps >= 15)
                    {
                        long diff = world.TimeNow - this.Player.MovementRecordingStarted;
                        double secs = diff / (double)world.TimerFrequency;
                        double rate = (double)this.Player.MovementRecordingSteps / secs;

                        if (rate > 5.0)
                        {
                            Console.WriteLine("SUSPECTED SPEEDHACK: " +
                                this.Player.Name + " 15sq/" + secs + "sec = " + rate);
                        }

                        this.Player.MovementRecordingSteps = 0;
                    }
                }

                // original x/y
                int ox = this.Player.MapX;
                int oy = this.Player.MapY;
                int x = ox;
                int y = oy;

                (x, y) = direction switch
                {
                    1 => (x, y - 1),
                    2 => (x + 1, y),
                    3 => (x, y + 1),
                    4 => (x - 1, y),
                    _ => (x, y),
                };

                this.Player.Facing = direction;

                if (this.Player.CanMoveTo(x, y))
                {
                    this.Player.MoveTo(world, x, y);

                    // Check if new tile is a warp tile
                    ITile? tile = this.Player.Map.GetTile(x, y);
                    if (tile is not null && tile is WarpTile)
                    {
                        WarpTile warp = (WarpTile)tile;
                        if (warp.WarpMap is not null && warp.WarpMap.PlayerCanJoin(this.Player, world))
                        {
                            this.Player.WarpTo(world, warp.WarpMap, warp.WarpX, warp.WarpY);
                        }
                        else
                        {
                            // Have to move player back
                            this.Player.MoveTo(world, ox, oy);
                            // Fix the clients position
                            world.Send(this.Player, P.SetYourPosition(this.Player));
                        }
                    }
                }
                else
                {
                    // Fix the clients position
                    world.Send(this.Player, P.SetYourPosition(this.Player));
                }
            }
        }
    }
}
