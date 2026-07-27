using CsvToSql.Core.Schema;

namespace CsvToSql
{
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
