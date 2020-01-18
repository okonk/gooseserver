using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CsvToSql
{
    class QuestsCsvToSql : CsvToSqlBase
    {
        protected override string[] GetColumns()
        {
            return new[] {
                "id", "name", "description", "pass_text", "fail_text", "min_experience", "max_experience", "min_level", "max_level", "repeatable", "show_progress", "only_one_player_can_complete", "prerequisite_quests"
            };
        }

        protected override string TransformValue(string columnName, string value)
        {
            switch (columnName)
            {
                case "name":
                case "description":
                case "pass_text":
                case "fail_text":
                case "prerequisite_quests":
                    return EscapeString(value);
                case "repeatable":
                case "show_progress":
                case "only_one_player_can_complete":
                    // booleans
                    return EscapeString(value);
                default:
                    return value;
            }
        }
    }
}
