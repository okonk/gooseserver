using CsvToSql.Core.Schema;

namespace CsvToSql
{
    /// <summary>item_surnames and item_titles have identical column sets. Kept as separate
    /// literal lists rather than a shared array, because Column's fluent methods mutate —
    /// check TitleCsvToSql before changing anything here.</summary>
    public class SurnameCsvToSql : CsvToSqlBase
    {
        public override Column[] GetColumnDescriptors() => new[]
        {
            Col.Id("id", SqlType.Integer).PrimaryKey(),
            Col.Text("name"),
            Col.Int("min_level", SqlType.Int, def: 1).Nullable(),
            Col.Int("max_level", SqlType.Int, def: 50).Nullable(),
            Col.Int("min_experience", SqlType.BigInt, def: 0).Nullable(),
            Col.Int("max_experience", SqlType.BigInt, def: 0).Nullable(),
            Col.Enum<ItemsCsvToSql.UseTypes>("item_usetype", SqlType.SmallInt, def: 0).Nullable(),
            Col.Enum<ItemsCsvToSql.ItemSlots>("item_slot", SqlType.SmallInt, def: 20).Nullable(),
            Col.Decimal("chance", SqlType.Decimal54),
            Col.Text("script_path", def: "''"),
            Col.Text("script_params", def: "''"),
        };
    }
}
