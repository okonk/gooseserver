using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;

namespace Goose
{
    public class CombinationHandler
    {
        Hashtable combinations;

        /**
         * Constructor
         * 
         */
        public CombinationHandler()
        {
            this.combinations = new Hashtable();
        }

        /**
         * LoadCombinations, loads all combinations from the database
         * 
         */
        public void LoadCombinations(GameWorld world)
        {
            var command = world.SqlConnection.CreateCommand();
            command.CommandText = "SELECT * FROM combinations";
            var reader = command.ExecuteReader();

            while (reader.Read())
            {
                Combination comb = new Combination();
                comb.ID = Convert.ToInt32(reader["combination_id"]);
                comb.Name = Convert.ToString(reader["combination_name"]);
                comb.MinLevel = Convert.ToInt32(reader["min_level"]);
                comb.MaxLevel = Convert.ToInt32(reader["max_level"]);
                comb.MinExperience = Convert.ToInt64(reader["min_experience"]);
                comb.MaxExperience = Convert.ToInt64(reader["max_experience"]);
                comb.ClassRestrictions = Convert.ToInt64(reader["class_restrictions"]);

                this.combinations[comb.ID] = comb;
            }
            reader.Close();

            int itemid;
            ItemTemplate template;
            int count;
            foreach (Combination comb in this.combinations.Values)
            {
                // Load required items
                comb.RequiredHash = new Dictionary<int, int>();

                command.CommandText = "SELECT item_template_id FROM combination_item_required " + 
                    "WHERE combination_id=" + comb.ID;
                reader = command.ExecuteReader();
                count = 0;
                while (reader.Read())
                {
                    count++;
                    if (count > GameSettings.Default.CombineBagSize)
                    {
                        throw new Exception("number of required items exceeds combine bag size. In combination " +
                            comb.Name);
                    }

                    itemid = Convert.ToInt32(reader["item_template_id"]);

                    template = world.ItemHandler.GetTemplate(itemid);
                    if (template == null)
                    {
                        throw new Exception("required Item ID " + itemid +
                            " doesn't exist. In combination " + comb.Name);
                    }

                    if (!comb.RequiredHash.ContainsKey(itemid))
                    {
                        comb.RequiredHash[itemid] = 1;
                    }
                    else
                    {
                        comb.RequiredHash[itemid] = (int)comb.RequiredHash[itemid] + 1;
                    }
                }
                reader.Close();

                // Load resulting items
                comb.ResultItems = new List<ItemTemplate>();

                command.CommandText = "SELECT item_template_id FROM combination_item_results " +
                    "WHERE combination_id=" + comb.ID;
                reader = command.ExecuteReader();
                count = 0;
                while (reader.Read())
                {
                    count++;
                    if (count > GameSettings.Default.CombineBagSize)
                    {
                        throw new Exception("number of result items exceeds combine bag size. In combination " +
                            comb.Name);
                    }

                    itemid = Convert.ToInt32(reader["item_template_id"]);

                    template = world.ItemHandler.GetTemplate(itemid);
                    if (template == null)
                    {
                        throw new Exception("result Item ID " + itemid + 
                            " doesn't exist. In combination " + comb.Name);
                    }

                    comb.ResultItems.Add(template);
                }
                reader.Close();
            }
        }

        /**
         * Count, returns the number of combinations
         * 
         */
        public int Count { get { return this.combinations.Keys.Count; } }

        /**
         * GetMatch, takes a hashtable and tries to match the ingredients with an existing combination
         * 
         * Returns the combination found, or null if none
         * 
         */
        public Combination GetMatch(Dictionary<int, int> combine)
        {
            int c;
            bool matched;

            foreach (Combination comb in this.combinations.Values)
            {
                matched = true;

                foreach (KeyValuePair<int, int> req in comb.RequiredHash)
                {
                    if (combine.ContainsKey(req.Key)) c = combine[req.Key];
                    else c = 0;
                    if (c <= 0 || req.Value < c)
                    {
                        matched = false;
                        break;
                    }
                }

                if (matched) return comb;
            }

            return null;
        }
    }
}
