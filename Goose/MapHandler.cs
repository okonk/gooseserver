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
                    map.ID = reader.GetInt32("map_id");
                    map.Name = reader.GetString("map_name");
                    map.FileName = reader.GetString("map_filename");

                    map.MinLevel = reader.GetInt32("min_level");
                    map.MaxLevel = reader.GetInt32("max_level");
                    map.MinExperience = reader.GetInt64("min_experience");
                    map.MaxExperience = reader.GetInt64("max_experience");

                    map.CanAuction = reader.GetString("auction_enabled") != "0";
                    map.CanPVP = reader.GetString("pvp_enabled") != "0";
                    map.CanChat = reader.GetString("chat_enabled") != "0";
                    map.CanShout = reader.GetString("shout_enabled") != "0";
                    map.CanUseItems = reader.GetString("items_enabled") != "0";
                    map.CanCast = reader.GetString("spells_enabled") != "0";
                    map.CanBind = reader.GetString("bind_enabled") != "0";
                    map.CanSpawnPets = reader.GetString("pets_enabled") != "0";

                    string scriptPath = reader.GetString("script_path");
                    if (!string.IsNullOrEmpty(scriptPath))
                    {
                        map.Script = world.ScriptHandler.GetScript<IMapScript>(scriptPath);
                        map.ScriptParams = reader.GetString("script_params");
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
