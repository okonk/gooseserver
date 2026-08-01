using CsvToSql.Core.Schema;

namespace CsvToSql
{
    public class QuestRewardsCsvToSql : CsvToSqlBase
    {
        public override Column[] GetColumnDescriptors() => new[]
        {
            Col.Id("id", SqlType.Integer).PrimaryKey(),
            Col.Id("quest_id", SqlType.Int).Ref("Quests"),
            Col.Enum<RewardType>("reward_type", SqlType.Int),
            Col.Int("long_value", SqlType.BigInt, def: 0).Nullable(),
            Col.Int("long_value2", SqlType.BigInt, def: 0).Nullable(),
            Col.Text("string_value", def: "''").Nullable(),
        };

        public enum RewardType
        {
            Gold,
            Item,
            Title,
            Surname,
            Teleport,
            Experience,
            FaceGraphic,
            BodyGraphic,
            HairGraphic,
            HairColour,
            BodyColour,
            ClassChange,
            HP,
            MP,
            AC,
            Stamina,
            Strength,
            Dexterity,
            Intelligence,
            SpellBuff,
            LearnSpell,
        }
    }
}
