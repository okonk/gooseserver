using CsvToSql.Core.Schema;

namespace CsvToSql
{
    public class WarpTilesCsvToSql : CsvToSqlBase
    {
        public override Column[] GetColumnDescriptors() => new[]
        {
            Col.Id("map_id", SqlType.SmallInt).Ref("Maps").HeaderText("map id"),
            Col.Int("map_x", SqlType.SmallInt).HeaderText("map x"),
            Col.Int("map_y", SqlType.SmallInt).HeaderText("map y"),
            Col.Id("warp_id", SqlType.SmallInt).Ref("Maps").HeaderText("warp to map id"),
            Col.Int("warp_x", SqlType.SmallInt).HeaderText("warp to x"),
            Col.Int("warp_y", SqlType.SmallInt).HeaderText("warp to y"),
        };
    }
}
