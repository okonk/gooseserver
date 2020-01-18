using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;

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
        List<Map> maps;

        /**
         * Constructor, constructs map list
         * 
         */
        public MapHandler()
        {
            this.maps = new List<Map>();
        }

        public List<Map> Maps { get { return this.maps; } }

        /**
         * LoadMaps, loads all maps
         * 
         */
        public void LoadMaps(GameWorld world)
        {
            var command = world.SqlConnection.CreateCommand();
            command.CommandText = "SELECT * FROM maps";
            var reader = command.ExecuteReader();

            while (reader.Read())
            {
                int width = Convert.ToInt32(reader["map_x"]);
                int height = Convert.ToInt32(reader["map_y"]);

                Map map = new Map(width, height);
                map.ID = Convert.ToInt32(reader["map_id"]);
                map.Name = Convert.ToString(reader["map_name"]);
                map.FileName = Convert.ToString(reader["map_filename"]);
                map.Width = width;
                map.Height = height;

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

                this.maps.Add(map);
            }

            reader.Close();

            foreach (Map map in this.maps)
            {
                map.LoadData(world);

                Event ev = new ClearMapItemsEvent();
                ev.Ticks += world.TimerFrequency * GameSettings.Default.ItemGroundSweepTime;
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
            foreach (Map map in this.maps)
            {
                if (map.ID == id) return map;
            }
            return null;
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
