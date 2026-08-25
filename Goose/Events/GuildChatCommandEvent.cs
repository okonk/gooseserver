using System.Text;

namespace Goose.Events
{
    class GuildChatCommandEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                if (this.Player.Guild is null) return;

                this.Player.UpdateIdleStatus(world);

                string message = ((string)this.Data).Substring(7);
                if (message.Length <= 0) return;

                string packet = P.GuildMessage("[guild] " + this.Player.Name + ": " + message);
                string filteredpacket = P.GuildMessage("[guild] " + this.Player.Name + ": ");
                bool filtered = false;

                world.LogHandler.Log(Log.Types.GuildChat, this.Player.PlayerID, message, this.Player.Guild.ID, this.Player.Map.ID, this.Player.MapX, this.Player.MapY);

                List<Player> range = this.Player.Guild.OnlineMembers;
                foreach (var player in range)
                {
                    if (player.ChatFilterEnabled)
                    {
                        if (!filtered) 
                        {
                            filteredpacket += world.ChatFilter.Filter(message);
                            filtered = true;
                        }
                        world.Send(player, filteredpacket);
                    }
                    else
                    {
                        world.Send(player, packet);
                    }
                }
            }
        }
    }
}
