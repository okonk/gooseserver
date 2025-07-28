using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using System.Data.SQLite;

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
        public class PlayerGuildStatus
        {
            public int PlayerID { get; set; }
            public GuildRanks Rank { get; set; }
            public bool Dirty { get; set; }
            public bool JustAdded { get; set; }
        }

        public Dictionary<int, PlayerGuildStatus> Members { get; set; }

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
            this.Members = new Dictionary<int, PlayerGuildStatus>();
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
            if (!this.Members.TryGetValue(player.PlayerID, out var status)) return GuildRanks.Deleted;

            return status.Rank;
        }

        /**
         * JoinGuild, adds the player to the guild
         *
         */
        public void JoinGuild(Player player, GameWorld world)
        {
            this.SendToGuild(P.GuildMessage("[guild-notice] " + player.Name + " has joined the guild."), world);
            this.AddMember(player.PlayerID, GuildRanks.Member, true, true);
            this.OnlineMembers.Add(player);
            player.Guild = this;
            if (this.ID != 0) player.GuildID = this.ID;
            world.Send(player, P.GuildMessage("[guild-notice] You have joined " + this.Name + "."));
            world.Send(player, P.StatusInfo(player));
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
                this.SendToGuild(P.GuildMessage("[guild-notice] " + player.Name + " was kicked from the guild."), world);
                world.Send(player, P.GuildMessage("[guild-notice] You were kicked from the guild."));
            }
            else
            {
                this.SendToGuild(P.GuildMessage("[guild-notice] " + player.Name + " left the guild."), world);
                world.Send(player, P.GuildMessage("[guild-notice] You left the guild."));
            }
            world.Send(player, P.StatusInfo(player));
        }

        /**
         * RemoveMember, removes player from the guild
         *
         */
        public void RemoveMember(int playerid)
        {
            if (!this.Members.TryGetValue(playerid, out var status)) return;

            status.Dirty = true;
            status.Rank = GuildRanks.Deleted;
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
            var guildNameParam = new SQLiteParameter("@name", DbType.String) { Value = this.Name };
            var guildMOTDParam = new SQLiteParameter("@motd", DbType.String) { Value = this.MOTD };

            bool justsaved = false;
            string query;
            var command = world.SqlConnection.CreateCommand();
            if (this.ID == 0)
            {
                query = "INSERT INTO guilds (guild_name, guild_motd) VALUES (@name, @motd)";
                command.CommandText = query;
                command.Parameters.Add(guildNameParam);
                command.Parameters.Add(guildMOTDParam);
                command.ExecuteNonQuery();

                command.CommandText = "SELECT last_insert_rowid()";
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
                command = world.SqlConnection.CreateCommand();
                command.CommandText = query;
                command.Parameters.Add(guildNameParam);
                command.Parameters.Add(guildMOTDParam);
                command.ExecuteNonQuery();
            }

            List<int> removed = new List<int>();
            foreach (var status in this.Members.Values)
            {
                if (status.Dirty)
                {
                    if (status.Rank == GuildRanks.Deleted)
                    {
                        query = "DELETE FROM guild_members WHERE guild_id=" + this.ID +
                            " AND player_id=" + status.PlayerID;
                        command = world.SqlConnection.CreateCommand();
                        command.CommandText = query;
                        command.ExecuteNonQuery();

                        removed.Add(status.PlayerID);
                    }
                    else
                    {
                        if (status.JustAdded)
                        {
                            query = "INSERT INTO guild_members (guild_id, player_id, guild_rank) VALUES (" +
                                this.ID + ", " + status.PlayerID + ", " + (int)status.Rank + ")";
                            command = world.SqlConnection.CreateCommand();
                            command.CommandText = query;
                            command.ExecuteNonQuery();

                            status.JustAdded = false;
                        }
                        else
                        {
                            query = "UPDATE guild_members SET guild_rank=" + (int)status.Rank +
                                " WHERE guild_id=" + this.ID + " AND player_id=" + status.PlayerID;
                            command = world.SqlConnection.CreateCommand();
                            command.CommandText = query;
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
            if (this.Members.TryGetValue(leader.PlayerID, out var leaderStatus))
            {
                leaderStatus.Rank = GuildRanks.Member;
                leaderStatus.Dirty = true;
            }
            if (this.Members.TryGetValue(newleader.PlayerID, out var newLeaderStatus))
            {
                newLeaderStatus.Rank = GuildRanks.Leader;
                newLeaderStatus.Dirty = true;
            }

            this.Dirty = true;

            this.SendToGuild(P.GuildMessage("[guild-notice] " + newleader.Name + " is now the new guild leader."), world);
        }

        /**
         * ChangeRank, changes rank of player
         *
         */
        public void ChangeRank(Player player, GuildRanks rank, GameWorld world)
        {
            if (this.Members.TryGetValue(player.PlayerID, out var status))
            {
                status.Rank = rank;
                status.Dirty = true;
            }

            switch (rank)
            {
                case GuildRanks.Officer:
                    this.SendToGuild(P.GuildMessage("[guild-notice] " + player.Name + " is now an officer."), world);
                    break;
                case GuildRanks.Member:
                    this.SendToGuild(P.GuildMessage("[guild-notice] " + player.Name + " is now a member."), world);
                    break;
            }

            this.Dirty = true;
        }
    }
}
