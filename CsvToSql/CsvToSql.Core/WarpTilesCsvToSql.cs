using CsvToSql.Core.Schema;

namespace CsvToSql
{
    class WarpTilesCsvToSql : CsvToSqlBase
    {
        public override Column[] GetColumnDescriptors() => new[]
        {
            Col.Id("map_id", SqlType.SmallInt).Ref("Maps"),
            Col.Int("map_x", SqlType.SmallInt),
            Col.Int("map_y", SqlType.SmallInt),
            Col.Id("warp_id", SqlType.SmallInt).Ref("Maps"),
            Col.Int("warp_x", SqlType.SmallInt),
            Col.Int("warp_y", SqlType.SmallInt),
        };
    }
}
