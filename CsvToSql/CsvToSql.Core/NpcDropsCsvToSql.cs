using CsvToSql.Core.Schema;

namespace CsvToSql
{
    public class NpcDropsCsvToSql : CsvToSqlBase
    {
        public override Column[] GetColumnDescriptors() => new[]
        {
            Col.Id("npc_template_id", SqlType.Int).Ref("NPCs"),
            Col.Id("item_template_id", SqlType.Int).Ref("Items"),
            Col.Int("stack"),
            Col.Decimal("droprate"),
        };
    }
}
