using System.Collections;
using System.Text;

namespace Goose
{
    /**
     * ClassHandler, handles Class objects
     *
     */
    public class ClassHandler
    {
        private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        Dictionary<int, Class> classes;

        public ClassHandler()
        {
            this.classes = [];
        }

        /**
         * GetClass, returns class object from id
         *
         */
        public Class? GetClass(int id)
        {
            Class? classs;

            if (this.classes.TryGetValue(id, out classs))
            {
                return classs;
            }

            return null;
        }

        /**
         * LoadClasses, loads classes from database
         *
         */
        public void LoadClasses(GameWorld world)
        {
            world.Database.Execute(conn =>
            {
            using var command = conn.CreateCommand();
            command.CommandText = "SELECT * FROM classes";
            using (var reader = command.ExecuteReader())
            {
            while (reader.Read())
            {
                Class c = new Class();
                c.ClassID = reader.GetInt32("class_id");
                c.ClassName = reader.GetString("class_name");
                c.ACMultiplier = Decimal.Parse(reader.GetString("ac_multiplier"));

                c.VitaCost = reader.GetInt64("vita_cost");
                c.ManaCost = reader.GetInt64("mana_cost");

                this.classes[c.ClassID] = c;
            }
            }

            command.CommandText = "SELECT * FROM class_info";
            using (var reader = command.ExecuteReader())
            {
            while (reader.Read())
            {
                ClassLevel c = new ClassLevel();
                c.ClassID = reader.GetInt32("class_id");

                Class? cl = this.GetClass(c.ClassID);
                if (cl is null)
                {
                    log.Error("class_info row for unknown class {0} skipped", c.ClassID);
                    continue;
                }

                c.Level = reader.GetInt32("level");
                c.Experience = reader.GetInt64("level_up_exp");

                c.BaseStats = new AttributeSet();
                c.BaseStats.HP = reader.GetInt64("player_hp");
                c.BaseStats.MP = reader.GetInt64("player_mp");
                c.BaseStats.SP = reader.GetInt64("player_sp");
                c.BaseStats.AC = reader.GetInt32("stat_ac");
                c.BaseStats.Strength = reader.GetInt32("stat_str");
                c.BaseStats.Stamina = reader.GetInt32("stat_sta");
                c.BaseStats.Intelligence = reader.GetInt32("stat_int");
                c.BaseStats.Dexterity = reader.GetInt32("stat_dex");
                c.BaseStats.FireResist = reader.GetInt32("res_fire");
                c.BaseStats.AirResist = reader.GetInt32("res_air");
                c.BaseStats.EarthResist = reader.GetInt32("res_earth");
                c.BaseStats.SpiritResist = reader.GetInt32("res_spirit");
                c.BaseStats.WaterResist = reader.GetInt32("res_water");

                c.BaseStats.HPPercentRegen = Decimal.Parse(reader.GetString("hp_percent_regen"));
                c.BaseStats.HPStaticRegen = reader.GetInt32("hp_static_regen");
                c.BaseStats.MPPercentRegen = Decimal.Parse(reader.GetString("mp_percent_regen"));
                c.BaseStats.MPStaticRegen = reader.GetInt32("mp_static_regen");

                c.BaseStats.Haste = Decimal.Parse(reader.GetString("haste"));
                c.BaseStats.SpellDamage = Decimal.Parse(reader.GetString("spell_damage"));
                c.BaseStats.SpellCrit = Decimal.Parse(reader.GetString("spell_crit"));
                c.BaseStats.MeleeDamage = Decimal.Parse(reader.GetString("melee_damage"));
                c.BaseStats.MeleeCrit = Decimal.Parse(reader.GetString("melee_crit"));
                c.BaseStats.DamageReduction = Decimal.Parse(reader.GetString("damage_reduce"));

                c.Spells = [];

                cl.AddLevel(c);
            }
            }

            var rejected = this.classes.Values.Where(c => !ValidateLevels(c)).Select(c => c.ClassID).ToList();
            foreach (int id in rejected)
            {
                log.Error("class {0} ({1}): level rows must be contiguous 1..N; class rejected",
                    id, this.classes[id].ClassName);
                this.classes.Remove(id);
            }

            foreach (Class c in this.classes.Values)
                world.RankHandler.AddClass(c);

            command.CommandText = "SELECT * FROM classes_levelup_spells";
            using (var reader = command.ExecuteReader())
            {
            Class? clas;
            ClassLevel? level;
            Spell? spell;

            while (reader.Read())
            {
                clas = this.GetClass(reader.GetInt32("class_id"));
                if (clas is null)
                {
                    // log bad class id
                    continue;
                }

                level = clas.GetLevel(reader.GetInt32("level"));
                if (level is null)
                {
                    // log bad level
                    continue;
                }

                spell = world.SpellHandler.GetSpell(reader.GetInt32("spell_id"));
                if (spell is null)
                {
                    // log bad spell
                    continue;
                }

                level.Spells.Add(spell);
            }
            }
            });
        }

        internal static bool ValidateLevels(Class c)
        {
            var ids = c.LevelIds.OrderBy(i => i).ToList();
            return ids.Count > 0 && ids[0] == 1 && ids[ids.Count - 1] == ids.Count;
        }

        public Class? GetFallbackClass() => this.classes.Values.OrderBy(c => c.ClassID).FirstOrDefault();

        /**
         * Count, returns class count
         */
        public int Count
        {
            get => this.classes.Count;
        }

        public ICollection<Class> Classes { get => this.classes.Values; }
    }
}
