using CsvToSql.Core.Schema;

namespace CsvToSql
{
    // Editor-only: registered in SchemaRegistry.EditorOnlyTables, so the importer never reads it.
    public class ItemBalanceCsvToSql : CsvToSqlBase
    {
        public override Column[] GetColumnDescriptors() => new[]
        {
            Col.Id("item_template_id", SqlType.Integer).PrimaryKey().Ref("Items"),
            Col.Int("audience", SqlType.BigInt, def: 0),
            Col.Enum<BalanceProfile>("profile", SqlType.SmallInt),
            Col.Enum<BalanceProfile>("profile2", SqlType.SmallInt, def: 0),
            Col.Enum<BalancePower>("power", SqlType.SmallInt, def: 0),
            Col.Int("power_pct", SqlType.Int, def: 0),
            Col.Text("group", def: "''"),
            Col.Enum<BalanceStep>("step", SqlType.SmallInt),
            Col.Enum<BalanceSource>("source", SqlType.SmallInt),
            Col.Bool("lock", def: false),
            Col.Text("note", def: "''"),
        };

        public override Composite[] GetComposites() => new[]
        {
            Composite.Bitmask("audience", from: "Classes"),
        };

        public enum BalanceProfile
        {
            None,
            Balanced,
            HP,
            MP,
            Tank,
            Dodge,
            Damage,
        }

        public enum BalancePower
        {
            Normal,
            Special,
            Weak,
            Exempt,
        }

        public enum BalanceStep
        {
            Levelling,
            Punchy,
            HayFray,
            Sewers,
            Nagan,
            Savage,
            Nibbles,
            XP20M,
            XP100M,
            XP200M,
            XP400M,
        }

        public enum BalanceSource
        {
            Vendor,
            Common,
            Uncommon,
            Rare,
            Crafted,
            RareBoss,
            HardCraft,
            Prestige,
            Special,
        }
    }
}
