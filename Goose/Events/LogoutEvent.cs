using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace Goose.Events
{
    class LogoutEvent : Event
    {
        /*
         * If null they never made it into the system so just disconnect them
         *
         * If their state is not logged in we remove them from the player list
         * and disconnect them, since they're not actually in the game yet
         *
         * If their state is loading game we have already sent a "Player has entered world" message
         * but they're not in the game properly yet, so we just send a "Player left the world" message,
         * remove from player handler and disconnect them
         *
         * Else they're properly in the game and we have to remove them from maps, etc
         *
         */
        public override void Ready(GameWorld world)
        {
            Socket sock = (Socket)this.Data;
            Player player = world.PlayerHandler.GetPlayer(sock);

            if (player == null)
            {

            }
            else if (player.State == Player.States.NotLoggedIn)
            {
                world.PlayerHandler.RemovePlayer(player);
            }
            else if (player.State == Player.States.LoadingGame)
            {
                player.State = Player.States.NotLoggedIn;
                world.PlayerHandler.RemovePlayer(player);
                world.SendToAll(P.ServerMessage(player.Name + " has left the world."));
            }
            else
            {
                if (player.Map != null)
                {
                    List<Player> range = player.Map.GetPlayersInRange(player);
                    foreach (Player p in range)
                    {
                        world.Send(p, P.EraseCharacter(player.LoginID));
                    }
                    foreach (NPC npc in player.Map.GetNPCsInRange(player))
                    {
                        npc.RemoveAggro(player);
                    }
                    player.Map.RemovePlayer(player, world);

                    if (!player.IsGMInvisible)
                        player.Map.SetCharacter(null, player.MapX, player.MapY);
                }

                foreach (Pet pet in player.Pets)
                {
                    if (pet.IsAlive) pet.Destroy(world);
                }

                player.SaveToDatabase(world);

                if (player.Group != null)
                {
                    player.Group.RemovePlayer(player, world, false, player);
                }

                if (player.Guild != null)
                {
                    player.Guild.OnlineMembers.Remove(player);
                }
                player.State = Player.States.NotLoggedIn;

                // Remove all buffs on logout
                List<Buff> removebuff = new List<Buff>();
                foreach (Buff b in player.Buffs)
                {
                    if (!b.ItemBuff) removebuff.Add(b);
                }

                foreach (Buff b in removebuff)
                {
                    player.RemoveBuff(b, world, false, updateCharacter: false);
                }

                world.PlayerHandler.RemovePlayer(player);
                if (player.Access != Goose.Player.AccessStatus.GameMaster)
                    world.SendToAll(P.ServerMessage(player.Name + " has left the world."));

                world.LogHandler.Log(Log.Types.LeaveGame, player.PlayerID, "");

                Console.WriteLine(player.Name + " logged out.");
            }
        }
    }
}
