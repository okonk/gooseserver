using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;

using Goose.Events;

namespace Goose
{
    /**
     * GuildHandler, handles loading/saving of guilds
     * 
     */
    public class GuildHandler
    {
        Hashtable guilds;
        List<Guild> newguilds;

        /**
         * Constructor
         */
        public GuildHandler()
        {
            guilds = new Hashtable();
            newguilds = new List<Guild>();
        }

        /**
         * LoadGuilds, loads all guild data
         * 
         */
        public void LoadGuilds(GameWorld world)
        {
            SqlCommand command = new SqlCommand("SELECT * FROM guilds", world.SqlConnection);
            SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                Guild guild = new Guild();
                guild.ID = Convert.ToInt32(reader["guild_id"]);
                guild.Name = Convert.ToString(reader["guild_name"]);
                guild.MOTD = Convert.ToString(reader["guild_motd"]);

                guilds[guild.ID] = guild;
            }

            reader.Close();

            int playerid;
            Guild.GuildRanks rank;
            foreach (Guild guild in this.guilds.Values)
            {
                command = new SqlCommand("SELECT * FROM guild_members WHERE guild_id=" + guild.ID, 
                    world.SqlConnection);
                reader = command.ExecuteReader();

                while (reader.Read())
                {
                    playerid = Convert.ToInt32(reader["player_id"]);
                    rank = (Guild.GuildRanks) Convert.ToInt32(reader["guild_rank"]);
                    guild.AddMember(playerid, rank);
                }

                reader.Close();
            }
        }

        /**
         * Count, returns the number of guilds
         * 
         */
        public int Count { get { return this.guilds.Count; } }

        /**
         * GetGuild, returns guild for id if it exists, else null
         * 
         */
        public Guild GetGuild(int id)
        {
            return (Guild)this.guilds[id];
        }

        /**
         * AddGuild, adds a guild to the temporary new guilds buffer until saved
         * 
         */
        public void AddGuild(Guild guild)
        {
            this.newguilds.Add(guild);
        }

        /**
         * Save, saves all guilds that are marked as dirty
         * 
         */
        public void Save(GameWorld world)
        {
            foreach (Guild guild in this.newguilds)
            {
                guild.Save(world);

                this.guilds[guild.ID] = guild;
            }

            foreach (Guild guild in this.guilds.Values)
            {
                if (guild.Dirty) guild.Save(world);
            }

            this.newguilds.Clear();

            this.AddSaveEvent(world);
        }

        /**
         * AddSaveEvent, adds save event to the event handler
         * 
         */
        public void AddSaveEvent(GameWorld world)
        {
            Event ev = new GuildSaveEvent();
            ev.Ticks += (long)(GameSettings.Default.GuildSavePeriod * world.TimerFrequency);

            world.EventHandler.AddEvent(ev);
        }
    }
}
