using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;

namespace Goose
{
    /**
     * Guild, holds information regarding a guilds members, guild name and message of the day
     * 
     */
    public class Guild
    {
        /**
         * GuildRanks, the different guild ranks
         * 
         */
        public enum GuildRanks 
        {
            Deleted = 0,
            Member = 1,
            Officer = 5,
            Leader = 9
        }

        /**
         * PlayerGuildStatus, holds information about a guild member
         * 
         */
        class PlayerGuildStatus
        {
            public int PlayerID { get; set; }
            public GuildRanks Rank { get; set; }
            public bool Dirty { get; set; }
            public bool JustAdded { get; set; }
        }

        public Hashtable Members { get; set; }

        public int ID { get; set; }
        public string MOTD { get; set; }
        public string Name { get; set; }
        // Does data need updating
        public bool Dirty { get; set; }

        public List<Player> OnlineMembers { get; set; }

        /**
         * Constructor
         */
        public Guild()
        {
            this.Members = new Hashtable();
            this.OnlineMembers = new List<Player>();
            this.Dirty = false;
        }

        /**
         * AddMember, calls along to next addmember function supplying dirty as false.
         * 
         * Used when loading guilds
         * 
         */
        public void AddMember(int playerid, GuildRanks rank)
        {
            this.AddMember(playerid, rank, false, false);
        }

        /**
         * AddMember, adds a member to the guild
         * 
         */
        public void AddMember(int playerid, GuildRanks rank, bool dirty, bool justadded)
        {
            PlayerGuildStatus status = new PlayerGuildStatus();
            status.Dirty = dirty;
            status.PlayerID = playerid;
            status.Rank = rank;
            status.JustAdded = justadded;

            this.Members[playerid] = status;
            this.Dirty = dirty;
        }

        /**
         * SendToGuild, sends message to everyone online in the guild
         * 
         */
        public void SendToGuild(string message, GameWorld world)
        {
            foreach (Player player in this.OnlineMembers)
            {
                world.Send(player, message);
            }
        }

        /**
         * GetRank, returns the rank of the specified player
         * 
         */
        public GuildRanks GetRank(Player player)
        {
            if (!this.Members.ContainsKey(player.PlayerID)) return GuildRanks.Deleted;

            return ((PlayerGuildStatus)this.Members[player.PlayerID]).Rank;
        }

        /**
         * JoinGuild, adds the player to the guild
         * 
         */
        public void JoinGuild(Player player, GameWorld world)
        {
            this.SendToGuild("$2[guild-notice] " + player.Name + " has joined the guild.", world);
            this.AddMember(player.PlayerID, GuildRanks.Member, true, true);
            this.OnlineMembers.Add(player);
            player.Guild = this;
            if (this.ID != 0) player.GuildID = this.ID;
            world.Send(player, "$2[guild-notice] You have joined " + this.Name + ".");
            world.Send(player, player.SNFString());
        }

        /**
         * LeaveGuild, removes player from guild
         * 
         * Calls to next LeaveGuild method specifying kicked as false
         * 
         */
        public void LeaveGuild(Player player, GameWorld world)
        {
            this.LeaveGuild(player, world, false);
        }

        /**
         * LeaveGuild, removes player from guild
         * 
         */
        public void LeaveGuild(Player player, GameWorld world, bool kicked)
        {
            this.RemoveMember(player.PlayerID);
            this.OnlineMembers.Remove(player);
            player.GuildID = 0;
            player.Guild = null;
            if (kicked)
            {
                this.SendToGuild("$2[guild-notice] " + player.Name + " was kicked from the guild.", world);
                world.Send(player, "$2[guild-notice] You were kicked from the guild.");
            }
            else
            {
                this.SendToGuild("$2[guild-notice] " + player.Name + " left the guild.", world);
                world.Send(player, "$2[guild-notice] You left the guild.");
            }
            world.Send(player, player.SNFString());
        }

        /**
         * RemoveMember, removes player from the guild
         * 
         */
        public void RemoveMember(int playerid)
        {
            if (!this.Members.ContainsKey(playerid)) return;

            ((PlayerGuildStatus)this.Members[playerid]).Dirty = true;
            ((PlayerGuildStatus)this.Members[playerid]).Rank = GuildRanks.Deleted;
            this.Dirty = true;
        }

        /**
         * Save, saves to database
         * 
         * Adds itself to database if it isn't already in there
         * 
         */
        public void Save(GameWorld world)
        {
            SqlParameter guildNameParam = new SqlParameter("@name", SqlDbType.VarChar, 64);
            guildNameParam.Value = this.Name;
            SqlParameter guildMOTDParam = new SqlParameter("@motd", SqlDbType.VarChar, 256);
            guildMOTDParam.Value = this.MOTD;

            bool justsaved = false;
            string query;
            SqlCommand command;
            if (this.ID == 0)
            {
                query = "INSERT INTO guilds (guild_name, guild_motd) VALUES (@name, @motd)";
                command = new SqlCommand(query, world.SqlConnection);
                command.Parameters.Add(guildNameParam);
                command.Parameters.Add(guildMOTDParam);
                command.ExecuteNonQuery();

                command.CommandText = "SELECT @@IDENTITY";
                this.ID = Convert.ToInt32(command.ExecuteScalar());

                justsaved = true;

                foreach (Player player in this.OnlineMembers)
                {
                    player.GuildID = this.ID;
                }
            }

            if (!justsaved)
            {
                query = "UPDATE guilds SET guild_name=@name, guild_motd=@motd WHERE guild_id=" + this.ID;
                command = new SqlCommand(query, world.SqlConnection);
                command.Parameters.Add(guildNameParam);
                command.Parameters.Add(guildMOTDParam);
                command.ExecuteNonQuery();
            }

            List<int> removed = new List<int>();
            PlayerGuildStatus status;
            foreach (Object obj in this.Members.Values)
            {
                status = (PlayerGuildStatus)obj;

                if (status.Dirty)
                {
                    if (status.Rank == GuildRanks.Deleted)
                    {
                        query = "DELETE FROM guild_members WHERE guild_id=" + this.ID +
                            " AND player_id=" + status.PlayerID;
                        command = new SqlCommand(query, world.SqlConnection);
                        command.ExecuteNonQuery();

                        removed.Add(status.PlayerID);
                    }
                    else
                    {
                        if (status.JustAdded)
                        {
                            query = "INSERT INTO guild_members (guild_id, player_id, guild_rank) VALUES (" +
                                this.ID + ", " + status.PlayerID + ", " + (int)status.Rank + ")";
                            command = new SqlCommand(query, world.SqlConnection);
                            command.ExecuteNonQuery();

                            status.JustAdded = false;
                        }
                        else
                        {
                            query = "UPDATE guild_members SET guild_rank=" + (int)status.Rank + 
                                " WHERE guild_id=" + this.ID + " AND player_id=" + status.PlayerID;
                            command = new SqlCommand(query, world.SqlConnection);
                            command.ExecuteNonQuery();
                        }
                    }

                    status.Dirty = false;
                }
            }
            foreach (int i in removed)
            {
                this.Members.Remove(i);
            }

            this.Dirty = false;
        }


        /**
         * ChangeOwner, swaps ownership of guild
         * 
         */
        public void ChangeOwner(Player leader, Player newleader, GameWorld world)
        {
            ((PlayerGuildStatus)this.Members[leader.PlayerID]).Rank = GuildRanks.Member;
            ((PlayerGuildStatus)this.Members[leader.PlayerID]).Dirty = true;
            ((PlayerGuildStatus)this.Members[newleader.PlayerID]).Rank = GuildRanks.Leader;
            ((PlayerGuildStatus)this.Members[newleader.PlayerID]).Dirty = true;

            this.Dirty = true;

            this.SendToGuild("$2[guild-notice] " + newleader.Name + " is now the new guild leader.", world);
        }

        /**
         * ChangeRank, changes rank of player
         * 
         */
        public void ChangeRank(Player player, GuildRanks rank, GameWorld world)
        {
            ((PlayerGuildStatus)this.Members[player.PlayerID]).Rank = rank;
            ((PlayerGuildStatus)this.Members[player.PlayerID]).Dirty = true;

            switch (rank)
            {
                case GuildRanks.Officer:
                    this.SendToGuild("$2[guild-notice] " + player.Name + " is now an officer.", world);
                    break;
                case GuildRanks.Member:
                    this.SendToGuild("$2[guild-notice] " + player.Name + " is now a member.", world);
                    break;
            }

            this.Dirty = true;
        }
    }
}
