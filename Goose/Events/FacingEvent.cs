using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /**
     * MoveEvent, event for "F" + 1-4 packet
     * 
     * Called when someone moves
     * Packet format: FDirection
     * Direction being 1-4. For some reason these differ from the moving.
     * direction for facing is as follows. 1,2,3,4 = up,left,right,down
     * 
     * Server responds: CHHLoginID,Facing
     * NOTE: Server remaps the directions 1,2,3,4 = 1,3,4,2
     * Server sends the response to everyone in the area including the player who generated it
     * 
     */
    class FacingEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new FacingEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                foreach (Buff b in this.Player.Buffs)
                {
                    // can't move when stunned
                    if (b.SpellEffect.EffectType == SpellEffect.EffectTypes.Stun)
                    {
                        // stunned battletext
                        world.Send(this.Player, P.BattleTextStunned(this.Player));
                        return;
                    }
                }

                if (((string)this.Data).Length == 1) return; // log bad facing event

                int facing = Convert.ToInt32(((string)this.Data)[1].ToString());

                if (facing <= 0 || facing >= 5) return; // log bad facing event

                if (this.Player.Facing != facing)
                {
                    this.Player.UpdateIdleStatus(world);

                    this.Player.Facing = facing;
                    string packet = P.ChangeHeading(this.Player);
                    world.Send(this.Player, packet);
                    List<Player> range = this.Player.Map.GetPlayersInRange(this.Player);
                    foreach (Player player in range)
                    {
                        world.Send(player, packet);
                    }
                }
            }
        }
    }
}
