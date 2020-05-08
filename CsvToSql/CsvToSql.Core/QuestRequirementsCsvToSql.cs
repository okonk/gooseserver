using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CsvToSql
{
    class QuestRequirementsCsvToSql : CsvToSqlBase
    {
        protected override string[] GetColumns()
        {
            return new[] {
                "id", "quest_id", "requirement_type", "requirement_value", "requirement_value2", "keep_requirement"
            };
        }

        protected override string TransformValue(string columnName, string value)
        {
            switch (columnName)
            {
                case "keep_requirement":
                    // booleans
                    return EscapeString(value);
                case "requirement_type":
                    return ConvertEnum(value, typeof(RequirementType));
                default:
                    return value;
            }
        }

        enum RequirementType
        {
            Gold,
            Item,
            Kill,
            TalkToNPC,
            ExperienceBanked,
            ExperienceSold,
            NothingEquipped,
        }
    }
}
