using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CsvToSql
{
    class QuestRewardsCsvToSql : CsvToSqlBase
    {
        protected override string[] GetColumns()
        {
            return new[] {
                "quest_id", "reward_type", "long_value", "long_value2", "string_value"
            };
        }

        protected override string TransformValue(string columnName, string value)
        {
            switch (columnName)
            {
                case "string_value":
                    return EscapeString(value);
                case "reward_type":
                    return ConvertEnum(value, typeof(RewardType));
                default:
                    return value;
            }
        }

        enum RewardType
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
