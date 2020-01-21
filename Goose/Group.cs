using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose
{
    /**
     * Group, holds group related information
     * 
     */
    public class Group
    {
        public List<Player> Players { get; set; }

        /**
         * Constructor
         */
        public Group()
        {
            this.Players = new List<Player>();
        }

        /**
         * AddPlayer, adds player to group
         * 
         * Updates everyone about new member
         * 
         */
        public void AddPlayer(Player player, GameWorld world, Player adder)
        {
            if (this.Players.Contains(player)) return;

            string packet = P.GroupMessage(player.Name + " has joined your group. (" + adder.Name + ")");
            foreach (Player p in this.Players)
            {
                if (p.State != Player.States.Ready) continue;

                world.Send(p, packet);
            }
            world.Send(player, P.GroupMessage("You have joined a group."));

            player.Group = this;
            this.Players.Add(player);

            foreach (Player p in this.Players)
            {
                this.SendPartyWindow(p, world);
            }
        }

        /**
          * RemovePlayer, removes player from group
          * 
          * Updates everyone about member leaving
          * 
          */
        public void RemovePlayer(Player player, GameWorld world, bool kicked, Player kicker)
        {
            if (!this.Players.Contains(player)) return;

            player.Group = null;
            this.Players.Remove(player);
            this.SendPartyWindow(player, world);

            string packet;
            if (kicked) { packet = P.GroupMessage(player.Name + " was kicked from your group. (" + kicker.Name + ")"); }
            else { packet = P.GroupMessage(player.Name + " has left your group."); }

            foreach (Player p in this.Players)
            {
                if (p.State != Player.States.Ready) continue;

                world.Send(p, packet);
                this.SendPartyWindow(p, world);
            }

            if (kicked) { world.Send(player, P.GroupMessage(kicker.Name + " kicked you from the group.")); }
            else { world.Send(player, P.GroupMessage("You have left the group.")); }

            if (this.Players.Count == 1)
            {
                this.Players[0].Group = null;
                world.Send(this.Players[0], P.GroupMessage("Your group has been disbanded."));
            }
        }

        /**
         * SendPartyWindow, sends party window to player
         * 
         */
        public void SendPartyWindow(Player player, GameWorld world)
        {
            int i = 1;
            // send players in party window if player is in party
            if (this.Players.Contains(player))
            {
                foreach (Player p in this.Players)
                {
                    // Can only display PartyWindowMax players in list
                    if (i > GameSettings.Default.PartyWindowMax) return;
                    if (p == player) continue;

                    world.Send(player, P.GroupUpdate(p, i));

                    i++;
                }
            }
            // blank out the rest of the party window
            while (i <= GameSettings.Default.PartyWindowMax)
            {
                world.Send(player, P.GroupUpdate(null, i));
                i++;
            }
        }

        /**
         * Chat, player talked in group chat
         *
         */
        public void Chat(Player player, string message, GameWorld world)
        {
            string packet = P.GroupMessage("[group] " + player.Name + ": " + message);
            string filteredpacket = P.GroupMessage("[group] " + player.Name + ": ");
            bool filtered = false;

            foreach (Player p in this.Players)
            {
                if (p.ChatFilterEnabled)
                {
                    if (!filtered) 
                    {
                        filteredpacket += world.ChatFilter.Filter(message);
                        filtered = true;
                    }
                    world.Send(p, filteredpacket);
                }
                else
                {
                    world.Send(p, packet);
                }
            }
        }

        /**
         * ItemPickup, player picked up an item
         * 
         */
        public void ItemPickup(Player player, ItemSlot itemslot, GameWorld world)
        {
            string packet = P.GroupMessage(player.Name + " picked up " + itemslot.Item.Name + " (" + itemslot.Stack + ").");
            foreach (Player p in this.Players)
            {
                world.Send(p, packet);
            }
        }

        /**
         * GainExperience, distributes experience to the group
         * 
         */
        public void GainExperience(NPC npc, GameWorld world)
        {
            long exp = npc.Experience;
            double groupexp = exp * Math.Pow(0.91, this.Players.Count-1);

            int highest = 0;
            foreach (Player player in this.Players)
            {
                if (player.Level > highest) highest = player.Level;
            }

            foreach (Player player in this.Players)
            {
                // no exp too far away
                if (player.Map != npc.Map ||
                    Math.Abs(player.MapX - npc.MapX) > Map.RANGE_X ||
                    Math.Abs(player.MapY - npc.MapY) > Map.RANGE_Y)
                {
                    player.AddExperience(0, world, Player.ExperienceMessage.TooFarAway);
                }
                // no exp since group member is too high
                else if (player.Level + 9 < highest)
                {
                    player.AddExperience(0, world, Player.ExperienceMessage.TooLow);
                }
                else if (npc.Level + 9 < player.Level)
                {
                    player.Killed(npc, world);
                    player.AddExperience((long)groupexp / 10, world, Player.ExperienceMessage.TooHigh);
                }
                else
                {
                    player.Killed(npc, world);
                    player.AddExperience((long)groupexp, world, Player.ExperienceMessage.Normal);
                }
            }
        }
    }
}
