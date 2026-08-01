using CsvToSql.Core.Schema;

namespace CsvToSql
{
    public class ClassesCsvToSql : CsvToSqlBase
    {
        public override Column[] GetColumnDescriptors() => new[]
        {
            Col.Id("class_id", SqlType.Integer).PrimaryKey(),
            Col.Text("class_name"),
            Col.Decimal("ac_multiplier", SqlType.Decimal92, def: "1"),
            Col.Int("vita_cost", SqlType.BigInt, def: 200000),
            Col.Int("mana_cost", SqlType.BigInt, def: 200000),
        };
    }
}
