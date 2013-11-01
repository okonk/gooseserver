using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;

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
            this.classes = new Dictionary<int, Class>();
        }

        /**
         * GetClass, returns class object from id
         * 
         */
        public Class GetClass(int id)
        {
            Class classs;

            if (this.classes.TryGetValue(id, out classs))
            {
                return classs;
            }

            return null;;
        }

        /**
         * LoadClasses, loads classes from database
         * 
         */
        public void LoadClasses(GameWorld world)
        {
            SqlCommand command = new SqlCommand("SELECT * FROM classes", world.SqlConnection);
            SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                Class c = new Class();
                c.ClassID = Convert.ToInt32(reader["class_id"]);
                c.ClassName = Convert.ToString(reader["class_name"]);
                c.ACMultiplier = Decimal.Parse(Convert.ToString(reader["ac_multiplier"]));

                c.VitaCost = Convert.ToInt64(reader["vita_cost"]);
                c.ManaCost = Convert.ToInt64(reader["mana_cost"]);

                this.classes[c.ClassID] = c;
            }
            reader.Close();

            command = new SqlCommand("SELECT * FROM class_info", world.SqlConnection);
            reader = command.ExecuteReader();

            while (reader.Read())
            {
                ClassLevel c = new ClassLevel();
                c.ClassID = Convert.ToInt32(reader["class_id"]);

                Class cl = this.GetClass(c.ClassID);
                if (cl == null)
                {
                    // log something wrong
                    return;
                }

                c.Level = Convert.ToInt32(reader["level"]);
                c.Experience = Convert.ToInt64(reader["level_up_exp"]);

                c.BaseStats = new AttributeSet();
                c.BaseStats.HP = Convert.ToInt32(reader["player_hp"]);
                c.BaseStats.MP = Convert.ToInt32(reader["player_mp"]);
                c.BaseStats.SP = Convert.ToInt32(reader["player_sp"]);
                c.BaseStats.AC = Convert.ToInt32(reader["stat_ac"]);
                c.BaseStats.Strength = Convert.ToInt32(reader["stat_str"]);
                c.BaseStats.Stamina = Convert.ToInt32(reader["stat_sta"]);
                c.BaseStats.Intelligence = Convert.ToInt32(reader["stat_int"]);
                c.BaseStats.Dexterity = Convert.ToInt32(reader["stat_dex"]);
                c.BaseStats.FireResist = Convert.ToInt32(reader["res_fire"]);
                c.BaseStats.AirResist = Convert.ToInt32(reader["res_air"]);
                c.BaseStats.EarthResist = Convert.ToInt32(reader["res_earth"]);
                c.BaseStats.SpiritResist = Convert.ToInt32(reader["res_spirit"]);
                c.BaseStats.WaterResist = Convert.ToInt32(reader["res_water"]);

                c.BaseStats.HPPercentRegen = Decimal.Parse(Convert.ToString(reader["hp_percent_regen"]));
                c.BaseStats.HPStaticRegen = Convert.ToInt32(reader["hp_static_regen"]);
                c.BaseStats.MPPercentRegen = Decimal.Parse(Convert.ToString(reader["mp_percent_regen"]));
                c.BaseStats.MPStaticRegen = Convert.ToInt32(reader["mp_static_regen"]);

                c.BaseStats.Haste = Decimal.Parse(Convert.ToString(reader["haste"]));
                c.BaseStats.SpellDamage = Decimal.Parse(Convert.ToString(reader["spell_damage"]));
                c.BaseStats.SpellCrit = Decimal.Parse(Convert.ToString(reader["spell_crit"]));
                c.BaseStats.MeleeDamage = Decimal.Parse(Convert.ToString(reader["melee_damage"]));
                c.BaseStats.MeleeCrit = Decimal.Parse(Convert.ToString(reader["melee_crit"]));
                c.BaseStats.DamageReduction = Decimal.Parse(Convert.ToString(reader["damage_reduce"]));

                c.Spells = new List<Spell>();

                cl.AddLevel(c);
            }

            reader.Close();

            command = new SqlCommand("SELECT * FROM classes_levelup_spells", world.SqlConnection);
            reader = command.ExecuteReader();

            Class clas;
            ClassLevel level;
            Spell spell;

            while (reader.Read())
            {
                clas = this.GetClass(Convert.ToInt32(reader["class_id"]));
                if (clas == null)
                {
                    // log bad class id
                    continue;
                }

                level = clas.GetLevel(Convert.ToInt32(reader["level"]));
                if (level == null)
                {
                    // log bad level
                    continue;
                }

                spell = world.SpellHandler.GetSpell(Convert.ToInt32(reader["spell_id"]));
                if (spell == null)
                {
                    // log bad spell
                    continue;
                }

                level.Spells.Add(spell);
            }

            reader.Close();
        }

        /**
         * Count, returns class count
         */
        public int Count
        {
            get { return this.classes.Count; }
        }

        public ICollection<Class> Classes { get { return this.classes.Values; } }
    }
}
