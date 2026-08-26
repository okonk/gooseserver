using System.Text;

using Goose.Events;

namespace Goose
{
    /**
     * GuildHandler, handles loading/saving of guilds
     * 
     */
    public class GuildHandler
    {
        private Dictionary<int, Guild> guilds = new();
        private List<Guild> newguilds = new();

        /**
         * LoadGuilds, loads all guild data
         * 
         */
        public void LoadGuilds(GameWorld world)
        {
            world.Database.Execute(conn =>
            {
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM guilds";
                    using var reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        Guild guild = new Guild();
                        guild.ID = reader.GetInt32("guild_id");
                        guild.Name = reader.GetString("guild_name");
                        guild.MOTD = reader.GetString("guild_motd");

                        guilds[guild.ID] = guild;
                    }
                }

                int playerid;
                Guild.GuildRanks rank;
                foreach (var guild in this.guilds.Values)
                {
                    using var command = conn.CreateCommand();
                    command.CommandText = "SELECT * FROM guild_members WHERE guild_id=" + guild.ID;
                    using var reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        playerid = reader.GetInt32("player_id");
                        rank = (Guild.GuildRanks) reader.GetInt32("guild_rank");
                        guild.AddMember(playerid, rank);
                    }
                }
            });
        }

        /**
         * Count, returns the number of guilds
         * 
         */
        public int Count { get => this.guilds.Count; }

        /**
         * GetGuild, returns guild for id if it exists, else null
         * 
         */
        public Guild? GetGuild(int id)
        {
            return this.guilds.TryGetValue(id, out var guild) ? guild : null;
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
            foreach (var guild in this.newguilds)
            {
                guild.Save(world);

                this.guilds[guild.ID] = guild;
            }

            foreach (var guild in this.guilds.Values)
            {
                if (guild.Dirty)
                {
                    var (save, onCommit) = guild.BuildSave();
                    world.Database.EnqueueTransaction(save, onCommit);
                }
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
            // H6: clamp to >= 1, a 0/negative period re-enqueues at now and spins EventHandler.Update
            ev.Ticks += (long)(Math.Max(1, world.Settings.GuildSavePeriod) * world.TimerFrequency);

            world.EventHandler.AddEvent(ev);
        }
    }
}
