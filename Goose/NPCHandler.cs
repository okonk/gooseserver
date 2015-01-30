using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;

namespace Goose
{
    /**
     * NPCHandler, loads/holds npcs
     * 
     */
    public class NPCHandler
    {
        List<NPCTemplate> templates;
        List<NPC> npcs;
        Hashtable idToNPC;

        /**
         * Constructor
         * 
         */
        public NPCHandler()
        {
            this.templates = new List<NPCTemplate>();
            this.npcs = new List<NPC>();
            this.idToNPC = new Hashtable();
        }

        /**
         * LoadNPCTemplates, loads npc templatess from database
         * 
         */
        public void LoadNPCTemplates(GameWorld world)
        {
            SqlCommand command = new SqlCommand("SELECT * FROM npc_templates", world.SqlConnection);
            SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                NPCTemplate npc = new NPCTemplate();
                npc.NPCTemplateID = Convert.ToInt32(reader["npc_id"]);
                npc.NPCType = (NPCTemplate.Types)Convert.ToInt32(reader["npc_type"]);
                npc.Name = Convert.ToString(reader["npc_name"]);
                npc.Title = Convert.ToString(reader["npc_title"]);
                npc.Surname = Convert.ToString(reader["npc_surname"]);
                npc.RespawnTime = Convert.ToInt32(reader["respawn_time"]);
                npc.Facing = Convert.ToInt32(reader["npc_facing"]);
                npc.Level = Convert.ToInt32(reader["npc_level"]);
                npc.Experience = Convert.ToInt64(reader["experience"]);
                npc.WeaponDamage = Convert.ToInt32(reader["weapon_damage"]);
                npc.AggroRange = Convert.ToInt32(reader["aggro_range"]);
                npc.AttackRange = Convert.ToInt32(reader["attack_range"]);
                npc.AttackSpeed = Decimal.Parse(Convert.ToString(reader["attack_speed"]));
                npc.MoveSpeed = Decimal.Parse(Convert.ToString(reader["move_speed"]));
                npc.CanMove = ("1".Equals(Convert.ToString(reader["stationary"])) ? false : true);
                npc.CanBeStunned = ("0".Equals(Convert.ToString(reader["stunnable"])) ? false : true);
                npc.CanBeRooted = ("0".Equals(Convert.ToString(reader["rootable"])) ? false : true);
                npc.CanBeSlowed = ("0".Equals(Convert.ToString(reader["slowable"])) ? false : true);
                npc.CanBeKilled = ("1".Equals(Convert.ToString(reader["invincible"])) ? false : true);
                npc.ClassID = Convert.ToInt32(reader["class_id"]);
                npc.EquippedItems = Convert.ToString(reader["equipped_items"]);

                npc.BodyState = Convert.ToInt32(reader["body_state"]);
                npc.BodyID = Convert.ToInt32(reader["body_id"]);
                npc.BodyR = Convert.ToInt32(reader["body_r"]);
                npc.BodyG = Convert.ToInt32(reader["body_g"]);
                npc.BodyB = Convert.ToInt32(reader["body_b"]);
                npc.BodyA = Convert.ToInt32(reader["body_a"]);
                npc.FaceID = Convert.ToInt32(reader["face_id"]);
                npc.HairID = Convert.ToInt32(reader["hair_id"]);
                npc.HairR = Convert.ToInt32(reader["hair_r"]);
                npc.HairG = Convert.ToInt32(reader["hair_g"]);
                npc.HairB = Convert.ToInt32(reader["hair_b"]);
                npc.HairA = Convert.ToInt32(reader["hair_a"]);

                npc.BaseStats = new AttributeSet();
                npc.BaseStats.HP = Convert.ToInt32(reader["npc_hp"]);
                npc.BaseStats.MP = Convert.ToInt32(reader["npc_mp"]);
                npc.BaseStats.SP = Convert.ToInt32(reader["npc_sp"]);
                npc.BaseStats.AC = Convert.ToInt32(reader["stat_ac"]);
                npc.BaseStats.Strength = Convert.ToInt32(reader["stat_str"]);
                npc.BaseStats.Stamina = Convert.ToInt32(reader["stat_sta"]);
                npc.BaseStats.Intelligence = Convert.ToInt32(reader["stat_int"]);
                npc.BaseStats.Dexterity = Convert.ToInt32(reader["stat_dex"]);
                npc.BaseStats.FireResist = Convert.ToInt32(reader["res_fire"]);
                npc.BaseStats.AirResist = Convert.ToInt32(reader["res_air"]);
                npc.BaseStats.EarthResist = Convert.ToInt32(reader["res_earth"]);
                npc.BaseStats.SpiritResist = Convert.ToInt32(reader["res_spirit"]);
                npc.BaseStats.WaterResist = Convert.ToInt32(reader["res_water"]);

                npc.BaseStats.HPPercentRegen = Decimal.Parse(Convert.ToString(reader["hp_percent_regen"]));
                npc.BaseStats.HPStaticRegen = Convert.ToInt32(reader["hp_static_regen"]);
                npc.BaseStats.MPPercentRegen = Decimal.Parse(Convert.ToString(reader["mp_percent_regen"]));
                npc.BaseStats.MPStaticRegen = Convert.ToInt32(reader["mp_static_regen"]);

                npc.AlliesString = Convert.ToString(reader["npc_alliance"]);
                npc.Allies = new List<NPCTemplate>();

                npc.Behaviour = (NPCTemplate.BehaviourTypes)Convert.ToInt32(reader["stuck_behaviour"]);
                npc.BehaviourTimeout = Convert.ToInt64(reader["stuck_timeout"]);

                npc.CreditDealer = ("0".Equals(Convert.ToString(reader["credit_dealer"])) ? false : true);

                var questIds = Convert.ToString(reader["quest_ids"]).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(id => Convert.ToInt32(id));
                npc.Quests.AddRange(questIds.Select(id => world.QuestHandler.Get(id)));

                this.templates.Add(npc);
            }

            reader.Close();

            foreach (NPCTemplate npc in this.templates)
            {
                foreach (string ally in npc.AlliesString.Split(" ".ToCharArray()))
                {
                    try
                    {
                        NPCTemplate a = this.GetNPCTemplate(Convert.ToInt32(ally));
                        if (a == null)
                        {
                            // log bad template id in allies
                        }
                        else
                        {
                            npc.Allies.Add(a);
                        }
                    }
                    catch (Exception)
                    {

                    }
                }
            }

            foreach (NPCTemplate template in this.templates)
            {
                command.CommandText = "SELECT * FROM npc_drops WHERE npc_template_id=" + template.NPCTemplateID;
                reader = command.ExecuteReader();

                template.Drops = new List<NPCDropInfo>();

                while (reader.Read())
                {
                    NPCDropInfo drop = new NPCDropInfo();
                    drop.DropRate = Decimal.Parse(Convert.ToString(reader["droprate"]));
                    drop.Stack = Convert.ToInt32(reader["stack"]);
                    drop.ItemTemplate = world.ItemHandler.GetTemplate(Convert.ToInt32(reader["item_template_id"]));

                    if (drop.ItemTemplate != null) template.Drops.Add(drop);
                }

                reader.Close();

                command.CommandText = "SELECT * FROM npc_vendor_items WHERE npc_template_id=" + 
                    template.NPCTemplateID;
                reader = command.ExecuteReader();

                if (reader.HasRows)
                {
                    template.VendorItems = new NPCVendorSlot[GameSettings.Default.VendorSlotSize+1];

                    while (reader.Read())
                    {
                        NPCVendorSlot vslot = new NPCVendorSlot();
                        vslot.Slot = Convert.ToInt32(reader["slot"]);
                        vslot.Stack = Convert.ToInt32(reader["stack"]);
                        vslot.ItemTemplate = 
                            world.ItemHandler.GetTemplate(Convert.ToInt32(reader["item_template_id"]));
                        vslot.CanSeeStats = ("0".Equals(Convert.ToString(reader["stats_visible"])) ? false : true);

                        if (vslot.ItemTemplate != null &&
                            vslot.Slot > 0 && vslot.Slot < GameSettings.Default.VendorSlotSize)
                        {
                            template.VendorItems[vslot.Slot] = vslot;
                        }
                        else
                        {
                            // log bad vendor slot/item
                        }
                    }
                }

                reader.Close();
            }
        }

        /**
         * TemplateCount, returns npc template count
         * 
         */
        public int TemplateCount { get { return this.templates.Count; } }

        /**
         * NPCCount, returns npc count
         * 
         */
        public int NPCCount { get { return this.npcs.Count; } }

        /**
         * Gets NPCTemplate object from npc_id
         */
        public NPCTemplate GetNPCTemplate(int npc_id)
        {
            foreach (NPCTemplate template in this.templates)
            {
                if (template.NPCTemplateID == npc_id) return template;
            }

            return null;
        }

        /**
         * GetNewID, returns new login id for npc
         */
        public int GetNewID(GameWorld world)
        {
            int id;
            do
            {
                id = world.Random.Next(GameSettings.Default.MaxPlayers + 1, GameSettings.Default.MaxNPCs);
            } while (this.idToNPC[id] != null);

            return id;
        }

        public void AssignNewId(GameWorld world, NPC npc)
        {
            if (npc.LoginID != 0 && this.idToNPC[npc.LoginID] != null)
            {
                this.idToNPC[npc.LoginID] = null;
            }

            npc.LoginID = this.GetNewID(world);
            this.idToNPC[npc.LoginID] = npc;
        }

        /**
         * LoadNPCs, loads npc spawns from database
         * 
         */
        public void LoadNPCs(GameWorld world)
        {
            SqlCommand command = new SqlCommand("SELECT * FROM npc_spawns", world.SqlConnection);
            SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                NPC npc = new NPC();
                int map_id, map_x, map_y;
                int npc_id = Convert.ToInt32(reader["npc_id"]);
                map_id = Convert.ToInt32(reader["map_id"]);
                map_x = Convert.ToInt32(reader["map_x"]);
                map_y = Convert.ToInt32(reader["map_y"]);

                NPCTemplate template = this.GetNPCTemplate(npc_id);
                if (template != null)
                {
                    if (npc.LoadFromTemplate(world, map_id, map_x, map_y, template))
                    {
                        this.npcs.Add(npc);
                        AssignNewId(world, npc);
                    }
                    else
                    {
                        // couldn't load map
                    }
                }
                else 
                {
                    // log bad id
                }
            }

            reader.Close();
        }
    }
}
