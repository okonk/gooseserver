using CsvToSql.Core.Schema;

namespace CsvToSql
{
    class MapRequiredItemsCsvToSql : CsvToSqlBase
    {
        public override Column[] GetColumnDescriptors() => new[]
        {
            Col.Id("map_id", SqlType.Int).Ref("Maps"),
            Col.Id("item_template_id", SqlType.Int).Ref("Items"),
        };
    }
}
