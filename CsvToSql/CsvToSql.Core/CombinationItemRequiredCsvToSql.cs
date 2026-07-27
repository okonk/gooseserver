using CsvToSql.Core.Schema;

namespace CsvToSql
{
    class CombinationItemRequiredCsvToSql : CsvToSqlBase
    {
        public override Column[] GetColumnDescriptors() => new[]
        {
            Col.Id("combination_id", SqlType.Int).Ref("Combinations"),
            Col.Id("item_template_id", SqlType.Int).Ref("Items"),
        };
    }
}
