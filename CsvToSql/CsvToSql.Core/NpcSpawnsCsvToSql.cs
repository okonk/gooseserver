using CsvToSql.Core.Schema;

namespace CsvToSql
{
    class NpcSpawnsCsvToSql : CsvToSqlBase
    {
        public override Column[] GetColumnDescriptors() => new[]
        {
            Col.Id("npc_id", SqlType.Int).Ref("NPCs"),
            Col.Id("map_id", SqlType.SmallInt).Ref("Maps"),
            Col.Int("map_x", SqlType.SmallInt),
            Col.Int("map_y", SqlType.SmallInt),
        };
    }
}
