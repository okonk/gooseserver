using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CsvToSql
{
    class NpcVendorsCsvToSql : CsvToSqlBase
    {
        protected override string[] GetColumns()
        {
            return new[] { "npc_template_id", "item_template_id", "stack", "stats_visible", "slot" };
        }

        protected override string TransformValue(string columnName, string value)
        {
            switch (columnName)
            {
                case "stats_visible":
                    // booleans
                    return EscapeString(value);
                default:
                    return value;
            }
        }
    }
}
