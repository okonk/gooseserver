using System.Text;

namespace Goose.Events
{
    /**
     * /summon playername
     * 
     */
    public class SummonEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                string name = ((string)this.Data).Substring(8);
                Player? player = world.PlayerHandler.GetPlayer(name);
                if (player is not null)
                {
                    if (player.State != Player.States.Ready)
                    {
                        world.Send(this.Player, P.ServerMessage("Player is still loading a map."));
                        return;
                    }

                    player.WarpTo(world, this.Player.Map, this.Player.MapX, this.Player.MapY);
                }
                else
                {
                    world.Send(this.Player, P.ServerMessage("Couldn't find player."));
                }
            }
        }
    }
}
