using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CsvToSql
{
    class ClassesCsvToSql : CsvToSqlBase
    {
        protected override string[] GetColumns()
        {
            return new[] { "class_id", "class_name", "ac_multiplier", "vita_cost", "mana_cost" };
        }

        protected override string TransformValue(string columnName, string value)
        {
            switch (columnName)
            {
                case "class_name":
                    return EscapeString(value);
                default:
                    return value;
            }
        }
    }
}
