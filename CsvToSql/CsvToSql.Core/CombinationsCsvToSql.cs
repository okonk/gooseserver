using CsvToSql.Core.Schema;

namespace CsvToSql
{
    public class CombinationsCsvToSql : CsvToSqlBase
    {
        public override Column[] GetColumnDescriptors() => new[]
        {
            Col.Id("combination_id", SqlType.Integer).PrimaryKey(),
            Col.Text("combination_name", SqlType.Varchar64),
            Col.Int("min_level", SqlType.Int, def: 1),
            Col.Int("max_level", SqlType.Int, def: 50),
            Col.Int("min_experience", SqlType.BigInt, def: 0),
            Col.Int("max_experience", SqlType.BigInt, def: 0),
            Col.Int("class_restrictions", SqlType.BigInt, def: 0),
        };

        public override Composite[] GetComposites() => new[]
        {
            Composite.Bitmask("class_restrictions", from: "Classes"),
        };
    }
}
