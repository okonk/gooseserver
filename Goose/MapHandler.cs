using System;
using System.Collections.Generic;
using System.Linq;
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
            this.maps = new Dictionary<int, Map>();
        }

        public Dictionary<int, Map> Maps { get { return this.maps; } }

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

                    map.CanAuction = ("0".Equals(Convert.ToString(reader["auction_enabled"])) ? false : true);
                    map.CanPVP = ("0".Equals(Convert.ToString(reader["pvp_enabled"])) ? false : true);
                    map.CanChat = ("0".Equals(Convert.ToString(reader["chat_enabled"])) ? false : true);
                    map.CanShout = ("0".Equals(Convert.ToString(reader["shout_enabled"])) ? false : true);
                    map.CanUseItems = ("0".Equals(Convert.ToString(reader["items_enabled"])) ? false : true);
                    map.CanCast = ("0".Equals(Convert.ToString(reader["spells_enabled"])) ? false : true);
                    map.CanBind = ("0".Equals(Convert.ToString(reader["bind_enabled"])) ? false : true);
                    map.CanSpawnPets = ("0".Equals(Convert.ToString(reader["pets_enabled"])) ? false : true);

                    string scriptPath = Convert.ToString(reader["script_path"]);
                    if (!string.IsNullOrEmpty(scriptPath))
                    {
                        map.Script = world.ScriptHandler.GetScript<IMapScript>(scriptPath);
                        map.ScriptParams = Convert.ToString(reader["script_params"]);
                    }

                    this.maps[map.ID] = map;
                }
            });

            foreach (Map map in this.maps.Values)
            {
                map.LoadData(world);

                Event ev = new ClearMapItemsEvent();
                // H6: clamp to >= 1, a 0/negative sweep time re-enqueues at now and spins EventHandler.Update
                ev.Ticks += world.TimerFrequency * Math.Max(1, world.Configuration.ItemGroundSweepTime);
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
            get { return this.maps.Count; }
        }
    }
}
