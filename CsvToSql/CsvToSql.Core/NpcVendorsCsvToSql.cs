using CsvToSql.Core.Schema;

namespace CsvToSql
{
    public class NpcVendorsCsvToSql : CsvToSqlBase
    {
        public override Column[] GetColumnDescriptors() => new[]
        {
            Col.Id("npc_template_id", SqlType.Int).Ref("NPCs"),
            Col.Id("item_template_id", SqlType.Int).Ref("Items"),
            Col.Int("stack", SqlType.Int, def: 1),
            Col.Bool("stats_visible", def: true),
            Col.Int("slot", SqlType.Int),
        };
    }
}
