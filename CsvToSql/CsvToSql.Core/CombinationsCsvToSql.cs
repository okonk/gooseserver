using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CsvToSql
{
    class CombinationsCsvToSql : CsvToSqlBase
    {
        protected override string[] GetColumns()
        {
            return new[] {
               "combination_id", "combination_name", "min_level", "max_level", "min_experience", "max_experience", "class_restrictions"
            };
        }

        protected override string TransformValue(string columnName, string value)
        {
            switch (columnName)
            {
                case "combination_name":
                    return EscapeString(value);
                default:
                    return value;
            }
        }
    }
}
