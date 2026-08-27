using System.Text;
using Goose.Quests;
using Goose.Scripting;

namespace Goose
{
    /**
     * NPCHandler, loads/holds npcs
     * 
     */
    public class NPCHandler
    {
        private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        private Dictionary<int, NPCTemplate> templates = new();
        private List<NPC> npcs = new();
        private Dictionary<int, NPC> idToNPC = new();

        public IEnumerable<NPCTemplate> GetTemplates()
        {
            return templates.Values;
        }

        internal static List<Quest> ResolveQuests(int npcTemplateId, string rawQuestIds, QuestHandler handler)
        {
            var quests = new List<Quest>();
            foreach (string token in rawQuestIds.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries))
            {
                int id;
                if (!int.TryParse(token, out id)) { log.Error("NPC template {0}: bad quest id '{1}'", npcTemplateId, token); continue; }
                Quest? quest = handler.Get(id);
                if (quest is null) { log.Error("NPC template {0}: unknown quest {1}", npcTemplateId, id); continue; }
                quests.Add(quest);
            }
            return quests;
        }

        /**
         * LoadNPCTemplates, loads npc templatess from database
         * 
         */
        public void LoadNPCTemplates(GameWorld world)
        {
            world.Database.Execute(conn =>
            {
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM npc_templates";
                    using var reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        int id = reader.GetInt32("npc_id");

                        NPCTemplate? npc = null;
                        if (!templates.TryGetValue(id, out npc))
                            npc = new NPCTemplate();

                        npc.NPCTemplateID = id;
                        npc.NPCType = (NPCTemplate.Types)reader.GetInt32("npc_type");
                        npc.Name = reader.GetString("npc_name");
                        npc.Title = reader.GetString("npc_title");
                        npc.Surname = reader.GetString("npc_surname");
                        npc.RespawnTime = reader.GetInt32("respawn_time");
                        npc.Facing = reader.GetInt32("npc_facing");
                        npc.Level = reader.GetInt32("npc_level");
                        npc.Experience = reader.GetInt64("experience");
                        npc.WeaponDamage = reader.GetInt64("weapon_damage");
                        npc.AggroRange = reader.GetInt32("aggro_range");
                        npc.AttackRange = reader.GetInt32("attack_range");
                        npc.AttackSpeed = Decimal.Parse(reader.GetString("attack_speed"));
                        npc.MoveSpeed = Decimal.Parse(reader.GetString("move_speed"));
                        npc.CanMove = reader.GetString("stationary") != "1";
                        npc.CanBeStunned = reader.GetString("stunnable") != "0";
                        npc.SeeInvisible = "1".Equals(reader.GetString("see_invisible"));
                        npc.CanBeRooted = reader.GetString("rootable") != "0";
                        npc.CanBeSlowed = reader.GetString("slowable") != "0";
                        npc.CanBeKilled = reader.GetString("invincible") != "1";
                        npc.ClassID = reader.GetInt32("class_id");
                        npc.EquippedItems = reader.GetString("equipped_items");

                        npc.BodyState = reader.GetInt32("body_state");
                        npc.BodyID = reader.GetInt32("body_id");
                        npc.BodyR = reader.GetInt32("body_r");
                        npc.BodyG = reader.GetInt32("body_g");
                        npc.BodyB = reader.GetInt32("body_b");
                        npc.BodyA = reader.GetInt32("body_a");
                        npc.FaceID = reader.GetInt32("face_id");
                        npc.HairID = reader.GetInt32("hair_id");
                        npc.HairR = reader.GetInt32("hair_r");
                        npc.HairG = reader.GetInt32("hair_g");
                        npc.HairB = reader.GetInt32("hair_b");
                        npc.HairA = reader.GetInt32("hair_a");

                        npc.BaseStats = new AttributeSet();
                        npc.BaseStats.HP = reader.GetInt64("npc_hp");
                        npc.BaseStats.MP = reader.GetInt64("npc_mp");
                        npc.BaseStats.SP = reader.GetInt64("npc_sp");
                        npc.BaseStats.AC = reader.GetInt32("stat_ac");
                        npc.BaseStats.Strength = reader.GetInt32("stat_str");
                        npc.BaseStats.Stamina = reader.GetInt32("stat_sta");
                        npc.BaseStats.Intelligence = reader.GetInt32("stat_int");
                        npc.BaseStats.Dexterity = reader.GetInt32("stat_dex");
                        npc.BaseStats.FireResist = reader.GetInt32("res_fire");
                        npc.BaseStats.AirResist = reader.GetInt32("res_air");
                        npc.BaseStats.EarthResist = reader.GetInt32("res_earth");
                        npc.BaseStats.SpiritResist = reader.GetInt32("res_spirit");
                        npc.BaseStats.WaterResist = reader.GetInt32("res_water");

                        npc.BaseStats.HPPercentRegen = Decimal.Parse(reader.GetString("hp_percent_regen"));
                        npc.BaseStats.HPStaticRegen = reader.GetInt32("hp_static_regen");
                        npc.BaseStats.MPPercentRegen = Decimal.Parse(reader.GetString("mp_percent_regen"));
                        npc.BaseStats.MPStaticRegen = reader.GetInt32("mp_static_regen");

                        npc.AlliesString = reader.GetString("npc_alliance");

                        npc.Behaviour = (NPCTemplate.BehaviourTypes)reader.GetInt32("stuck_behaviour");
                        npc.BehaviourTimeout = reader.GetInt64("stuck_timeout");

                        npc.CreditDealer = reader.GetString("credit_dealer") != "0";

                        // Credit dealers are the only vendors with a non-gold currency in
                        // sheet data. Null (not "gold") so Resolve's fallback chain stays
                        // uniform: item override, then vendor, then gold.
                        npc.CurrencyId = npc.CreditDealer ? Currency.Credits : null;

                        npc.Quests = ResolveQuests(npc.NPCTemplateID, reader.GetString("quest_ids"), world.QuestHandler);

                        string scriptPath = reader.GetString("script_path");
                        if (!string.IsNullOrEmpty(scriptPath))
                        {
                            npc.Script = world.ScriptHandler.GetScript<INPCScript>(scriptPath);
                            npc.ScriptParams = reader.GetString("script_params");
                        }

                        npc.ArmorPierce = reader.GetInt32("armor_pierce");

                        this.templates[npc.NPCTemplateID] = npc;
                    }
                }

                foreach (var npc in this.templates.Values)
                {
                    var allies = new List<NPCTemplate>();

                    try
                    {
                        foreach (int ally in npc.AlliesString.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries).Select(q => Convert.ToInt32(q)))
                        {
                            NPCTemplate? a = this.GetNPCTemplate(ally);
                            if (a is null)
                            {
                                // log bad template id in allies
                            }
                            else
                            {
                                allies.Add(a);
                            }
                        }
                    }
                    catch (Exception)
                    {

                    }

                    npc.Allies = allies;
                }

                foreach (var template in this.templates.Values)
                {
                    using (var command = conn.CreateCommand())
                    {
                        command.CommandText = "SELECT * FROM npc_drops WHERE npc_template_id=" + template.NPCTemplateID;
                        using var reader = command.ExecuteReader();

                        template.Drops = [];

                        while (reader.Read())
                        {
                            NPCDropInfo drop = new NPCDropInfo();
                            drop.DropRate = Decimal.Parse(reader.GetString("droprate"));
                            drop.Stack = reader.GetInt32("stack");
                            drop.ItemTemplate = world.ItemHandler.GetTemplate(reader.GetInt32("item_template_id"))!;

                            if (drop.ItemTemplate is not null) template.Drops.Add(drop);
                        }
                    }

                    using (var command = conn.CreateCommand())
                    {
                        command.CommandText = "SELECT * FROM npc_vendor_items WHERE npc_template_id=" +
                            template.NPCTemplateID;
                        using var reader = command.ExecuteReader();

                        if (reader.HasRows)
                        {
                            template.VendorItems = new NPCVendorSlot[world.Settings.VendorSlotSize + 1];

                            while (reader.Read())
                            {
                                NPCVendorSlot vslot = new NPCVendorSlot();
                                vslot.Slot = reader.GetInt32("slot");
                                vslot.Stack = reader.GetInt32("stack");
                                vslot.ItemTemplate =
                                    world.ItemHandler.GetTemplate(reader.GetInt32("item_template_id"))!;
                                vslot.CanSeeStats = reader.GetString("stats_visible") != "0";

                                if (vslot.ItemTemplate is not null &&
                                    vslot.Slot > 0 && vslot.Slot <= world.Settings.VendorSlotSize)
                                {
                                    template.VendorItems[vslot.Slot] = vslot;
                                }
                                else
                                {
                                    // log bad vendor slot/item
                                }
                            }
                        }
                    }
                }
            });
        }

        /**
         * TemplateCount, returns npc template count
         * 
         */
        public int TemplateCount { get => this.templates.Count; }

        /**
         * NPCCount, returns npc count
         * 
         */
        public int NPCCount { get => this.npcs.Count; }

        /**
         * Gets NPCTemplate object from npc_id
         */
        public NPCTemplate? GetNPCTemplate(int npc_id)
        {
            NPCTemplate? npc = null;
            if (templates.TryGetValue(npc_id, out npc))
                return npc;

            return null;
        }

        internal static bool ValidateAndNormalize(NPCTemplate template)
        {
            if (template is null || template.BaseStats is null || string.IsNullOrWhiteSpace(template.Name))
                return false;

            template.Allies ??= [];
            template.Quests ??= [];
            template.Drops ??= [];
            template.EquippedItems ??= "";

            return true;
        }

        /// <summary>Registers a script-generated template. Overwrites any existing entry with the
        /// same id - callers that must not collide should check GetNPCTemplate first.</summary>
        public void AddTemplate(NPCTemplate template)
        {
            if (!ValidateAndNormalize(template))
            {
                log.Error("Refusing NPC template {0}: missing Name or BaseStats", template?.NPCTemplateID);
                return;
            }

            this.templates[template.NPCTemplateID] = template;
        }

        /**
         * GetNewID, returns new login id for npc
         */
        public int GetNewID(GameWorld world)
        {
            int id;
            do
            {
                id = world.Random.Next(world.Settings.MaxPlayers + 1, world.Settings.MaxNPCs);
            } while (this.idToNPC.ContainsKey(id));

            return id;
        }

        public void AssignNewId(GameWorld world, NPC npc)
        {
            if (npc.LoginID != 0 && this.idToNPC.ContainsKey(npc.LoginID))
            {
                this.idToNPC.Remove(npc.LoginID);
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
            world.Database.Execute(conn =>
            {
                using var command = conn.CreateCommand();
                command.CommandText = "SELECT * FROM npc_spawns";
                using var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    int npc_id = reader.GetInt32("npc_id");
                    int map_id = reader.GetInt32("map_id");
                    int map_x = reader.GetInt32("map_x");
                    int map_y = reader.GetInt32("map_y");

                    NPCTemplate? template = this.GetNPCTemplate(npc_id);
                    if (template is null) continue;               // log bad id
                    if (this.SpawnNPC(world, map_id, map_x, map_y, template, shouldRespawn: true) is null)
                    {
                        // couldn't load map
                    }
                }
            });
        }

        /// <summary>Registers an already-loaded NPC so NPCCount and anything enumerating the
        /// handler's npcs can see it. LoadFromTemplate does not do this - it only adds the NPC to
        /// its map and to the login-id lookup.</summary>
        public void AddNPC(NPC npc)
        {
            this.npcs.Add(npc);
        }

        /// <summary>The supported way to create an NPC at runtime: loads it from the template and
        /// registers it. Returns null if the map does not exist, in which case nothing is
        /// registered. Every caller - LoadNPCs included - should go through this rather than
        /// calling LoadFromTemplate directly, so there is one definition of "spawned".</summary>
        public NPC? SpawnNPC(GameWorld world, int mapId, int mapX, int mapY, NPCTemplate template, bool shouldRespawn)
        {
            var npc = new NPC();
            if (!npc.LoadFromTemplate(world, mapId, mapX, mapY, template, shouldRespawn)) return null;

            this.AddNPC(npc);
            return npc;
        }
    }
}
