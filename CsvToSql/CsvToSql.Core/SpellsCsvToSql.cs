using CsvToSql.Core.Schema;

namespace CsvToSql
{
    public class SpellsCsvToSql : CsvToSqlBase
    {
        public override Column[] GetColumnDescriptors() => new[]
        {
            Col.Id("spell_id", SqlType.Integer).PrimaryKey(),
            Col.Text("spell_name"),
            Col.Text("spell_description", def: "''"),
            Col.Enum<SpellTargets>("spell_target", SqlType.Int),
            Col.Int("class_restrictions", SqlType.BigInt, def: 0), // allow list: bit set = class id can cast, 0 = all
            Col.Int("spell_aether", SqlType.BigInt, def: 100), // Aether in milliseconds
            Col.Int("spellbook_graphic", SqlType.Int),
            Col.Int("spellbook_graphic_file", SqlType.Int, def: 0),

            Col.Int("hp_static_cost", SqlType.Int, def: 0),
            Col.Decimal("hp_percent_cost", SqlType.Decimal94, def: "0"),
            Col.Int("mp_static_cost", SqlType.Int, def: 0),
            Col.Decimal("mp_percent_cost", SqlType.Decimal94, def: "0"),
            Col.Int("sp_static_cost", SqlType.Int, def: 0),
            Col.Decimal("sp_percent_cost", SqlType.Decimal94, def: "0"),

            Col.Id("spell_effect_id", SqlType.Int).Ref("Spell Effects"),
        };

        public override Composite[] GetComposites() => new[]
        {
            Composite.Bitmask("class_restrictions", from: "Classes"),
            Composite.Graphic(tile: "spellbook_graphic", file: "spellbook_graphic_file"),
        };

        public enum SpellTargets
        {
            Target = 0,
            Self,
            Group
        }
    }
}
