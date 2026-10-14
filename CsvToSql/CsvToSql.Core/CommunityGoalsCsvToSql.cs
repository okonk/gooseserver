using CsvToSql.Core.Schema;

namespace CsvToSql
{
    public class CommunityGoalsCsvToSql : CsvToSqlBase
    {
        public override Column[] GetColumnDescriptors() => new[]
        {
            Col.Id("id", SqlType.Integer).PrimaryKey(),
            Col.Text("name"),
            Col.Text("description", def: "''"),
            Col.Id("npc_template_id", SqlType.Int).Ref("NPCs"),
            Col.Text("prerequisite_goals", def: "''"),
            Col.Text("script_path", def: "''"),
            Col.Text("properties", SqlType.Text, "''"),
        };
    }
}
