using CsvToSql.Core.Schema;

namespace CsvToSql
{
    public class NpcSpawnsCsvToSql : CsvToSqlBase
    {
        public override Column[] GetColumnDescriptors() => new[]
        {
            Col.Id("npc_id", SqlType.Int).Ref("NPCs").HeaderText("npc id"),
            Col.Id("map_id", SqlType.SmallInt).Ref("Maps").HeaderText("map id"),
            Col.Int("map_x", SqlType.SmallInt).HeaderText("map x"),
            Col.Int("map_y", SqlType.SmallInt).HeaderText("map y"),
        };
    }
}
