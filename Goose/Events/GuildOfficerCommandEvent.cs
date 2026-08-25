using System.Text;

namespace Goose.Events
{
    class GuildOfficerCommandEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                if (this.Player.Guild is null) return;
                if (this.Player.Guild.GetRank(this.Player) < Guild.GuildRanks.Leader) return;

                string name = ((string)this.Data).Substring(14);

                Player player = world.PlayerHandler.GetPlayer(name);
                if (player is not null && player.State == Player.States.Ready)
                {
                    if (player.Guild == this.Player.Guild && player != this.Player)
                    {
                        switch (player.Guild.GetRank(player))
                        {
                            case Guild.GuildRanks.Officer:
                                player.Guild.ChangeRank(player, Guild.GuildRanks.Member, world);
                                break;
                            case Guild.GuildRanks.Member:
                                player.Guild.ChangeRank(player, Guild.GuildRanks.Officer, world);
                                break;
                        }
                    }
                }
                else
                {
                    world.Send(this.Player, P.ServerMessage("Couldn't find player."));
                }
            }
        }
    }
}
