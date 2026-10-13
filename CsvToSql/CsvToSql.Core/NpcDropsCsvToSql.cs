using CsvToSql.Core.Schema;

namespace CsvToSql
{
    public class NpcDropsCsvToSql : CsvToSqlBase
    {
        public override Column[] GetColumnDescriptors() => new[]
        {
            Col.Id("npc_template_id", SqlType.Int).Ref("NPCs"),
            Col.Id("item_template_id", SqlType.Int).Ref("Items"),
            Col.Int("stack", SqlType.Int),
            Col.Int("max_stack", SqlType.Int, def: 0),
            Col.Double("droprate", scale: 4, max: 99999.9999),
        };
    }
}
