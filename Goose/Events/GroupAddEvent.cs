using System.Text;

namespace Goose.Events
{
    /**
     * GroupAddEvent
     * 
     * /groupadd player
     * Adds player to group.
     * 
     */
    public class GroupAddEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                string name = ((string)this.Data).Split(' ', 2)[1];
                Player player = world.PlayerHandler.GetPlayer(name);
                if (player is not null && player.State == Player.States.Ready)
                {
                    if (player == this.Player)
                    {
                        world.Send(this.Player, P.GroupMessage("You can't group with yourself."));
                        return;
                    }
                    if (player.Group is not null)
                    {
                        world.Send(this.Player, P.GroupMessage("Player is already in a group."));
                        return;
                    }
                    if (!player.GroupInvitesEnabled)
                    {
                        world.Send(this.Player, P.GroupMessage("Player is not accepting group invitations."));
                        return;
                    }

                    if (this.Player.Group is null)
                    {
                        this.Player.Group = new Group();
                        this.Player.Group.Players.Add(this.Player);
                    }

                    this.Player.Group.AddPlayer(player, world, this.Player);

                    world.LogHandler.Log(Log.Types.JoinGroup, this.Player.PlayerID, "", player.PlayerID, player.Map.ID, player.MapX, player.MapY);
                }
                else
                {
                    world.Send(this.Player, P.GroupMessage("Couldn't find player."));
                }
            }
        }
    }
}
