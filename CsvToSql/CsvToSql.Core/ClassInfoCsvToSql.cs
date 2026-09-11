using CsvToSql.Core.Schema;

namespace CsvToSql
{
    public class ClassInfoCsvToSql : CsvToSqlBase
    {
        // No primary key — the pre-descriptor schema declared none for class_info.
        public override Column[] GetColumnDescriptors() => new[]
        {
            Col.Id("class_id", SqlType.Int).Ref("Classes"),
            Col.Int("level", SqlType.SmallInt),
            Col.Int("level_up_exp", SqlType.BigInt, def: 0),
            Col.Int("player_hp", SqlType.Int, def: 0),
            Col.Int("player_mp", SqlType.Int, def: 0),
            Col.Int("player_sp", SqlType.Int, def: 0),
            Col.Int("stat_ac", SqlType.SmallInt, def: 0),
            Col.Int("stat_str", SqlType.SmallInt, def: 0),
            Col.Int("stat_sta", SqlType.SmallInt, def: 0),
            Col.Int("stat_dex", SqlType.SmallInt, def: 0),
            Col.Int("stat_int", SqlType.SmallInt, def: 0),
            Col.Int("res_fire", SqlType.SmallInt, def: 0),
            Col.Int("res_water", SqlType.SmallInt, def: 0),
            Col.Int("res_spirit", SqlType.SmallInt, def: 0),
            Col.Int("res_air", SqlType.SmallInt, def: 0),
            Col.Int("res_earth", SqlType.SmallInt, def: 0),
            Col.Double("hp_percent_regen", scale: 4, max: 99999.9999, def: "0"),
            Col.Int("hp_static_regen", SqlType.Int, def: 0),
            Col.Double("mp_percent_regen", scale: 4, max: 99999.9999, def: "0"),
            Col.Int("mp_static_regen", SqlType.Int, def: 0),
            Col.Double("haste", scale: 4, max: 99999.9999, def: "0"),
            Col.Double("spell_damage", scale: 4, max: 99999.9999, def: "0"),
            Col.Double("spell_crit", scale: 4, max: 99999.9999, def: "0"),
            Col.Double("melee_damage", scale: 4, max: 99999.9999, def: "0"),
            Col.Double("melee_crit", scale: 4, max: 99999.9999, def: "0"),
            Col.Double("damage_reduce", scale: 4, max: 99999.9999, def: "0"),
        };
    }
}
