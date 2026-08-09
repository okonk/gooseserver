using CsvToSql.Core.Schema;

namespace CsvToSql
{
    public class QuestRequirementsCsvToSql : CsvToSqlBase
    {
        public override Column[] GetColumnDescriptors() => new[]
        {
            Col.Id("id", SqlType.Integer).PrimaryKey(),
            Col.Id("quest_id", SqlType.Int).Ref("Quests"),
            Col.Enum<RequirementType>("requirement_type", SqlType.Int),
            Col.Int("requirement_value", SqlType.BigInt),
            Col.Int("requirement_value2", SqlType.BigInt, def: 0).Nullable(),
            Col.Bool("keep_requirement", def: false).Nullable(),
            Col.Text("script_path", def: "''"),
            Col.Text("script_params", def: "''"),
        };

        public enum RequirementType
        {
            Gold,
            Item,
            Kill,
            TalkToNPC,
            ExperienceBanked,
            ExperienceSold,
            NothingEquipped,
            Script,          // 7 — logic lives in script_path
        }
    }
}
