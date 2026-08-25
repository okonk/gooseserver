using System.Text;

using Goose.Events;
using Goose.Scripting;

namespace Goose
{
    /**
     * MapHandler
     * 
     * Handles loading and storage of Map objects
     * 
     */
    public class MapHandler
    {
        Dictionary<int, Map> maps;

        /**
         * Constructor, constructs map list
         * 
         */
        public MapHandler()
        {
            this.maps = [];
        }

        public Dictionary<int, Map> Maps { get => this.maps; }

        /**
         * LoadMaps, loads all maps
         * 
         */
        public void LoadMaps(GameWorld world)
        {
            world.Database.Execute(conn =>
            {
                using var command = conn.CreateCommand();
                command.CommandText = "SELECT * FROM maps";
                using var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    Map map = new Map();
                    map.ID = Convert.ToInt32(reader["map_id"]);
                    map.Name = Convert.ToString(reader["map_name"]);
                    map.FileName = Convert.ToString(reader["map_filename"]);

                    map.MinLevel = Convert.ToInt32(reader["min_level"]);
                    map.MaxLevel = Convert.ToInt32(reader["max_level"]);
                    map.MinExperience = Convert.ToInt64(reader["min_experience"]);
                    map.MaxExperience = Convert.ToInt64(reader["max_experience"]);

                    map.CanAuction = Convert.ToString(reader["auction_enabled"]) != "0";
                    map.CanPVP = Convert.ToString(reader["pvp_enabled"]) != "0";
                    map.CanChat = Convert.ToString(reader["chat_enabled"]) != "0";
                    map.CanShout = Convert.ToString(reader["shout_enabled"]) != "0";
                    map.CanUseItems = Convert.ToString(reader["items_enabled"]) != "0";
                    map.CanCast = Convert.ToString(reader["spells_enabled"]) != "0";
                    map.CanBind = Convert.ToString(reader["bind_enabled"]) != "0";
                    map.CanSpawnPets = Convert.ToString(reader["pets_enabled"]) != "0";

                    string scriptPath = Convert.ToString(reader["script_path"]);
                    if (!string.IsNullOrEmpty(scriptPath))
                    {
                        map.Script = world.ScriptHandler.GetScript<IMapScript>(scriptPath);
                        map.ScriptParams = Convert.ToString(reader["script_params"]);
                    }

                    this.maps[map.ID] = map;
                }
            });

            foreach (var map in this.maps.Values)
            {
                map.LoadData(world);

                Event ev = new ClearMapItemsEvent();
                // H6: clamp to >= 1, a 0/negative sweep time re-enqueues at now and spins EventHandler.Update
                ev.Ticks += world.TimerFrequency * Math.Max(1, world.Settings.ItemGroundSweepTime);
                ev.Data = map;
                world.EventHandler.AddEvent(ev);
            }
        }

        /**
         * GetMap, gets map by id
         * 
         */
        public Map GetMap(int id)
        {
            return this.maps.TryGetValue(id, out var map) ? map : null;
        }

        /**
         * Count, returns map count
         * 
         */
        public int Count
        {
            get => this.maps.Count;
        }
    }
}
