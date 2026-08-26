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

                world.RankHandler.AddClass(c);
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
                    // log something wrong
                    return;
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
