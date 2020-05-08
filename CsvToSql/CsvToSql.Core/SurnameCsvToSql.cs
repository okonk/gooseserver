using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CsvToSql
{
    class SurnameCsvToSql : CsvToSqlBase
    {
        protected override string[] GetColumns()
        {
            return new[] {
                "id", "name", "min_level", "max_level", "min_experience", "max_experience", "item_usetype", "item_slot", "chance", "script_path", "script_params",
            };
        }

        protected override string TransformValue(string columnName, string value)
        {
            switch (columnName)
            {
                case "name":
                case "script_path":
                case "script_params":
                    return EscapeString(value);
                case "item_usetype":
                    return ConvertEnum(value, typeof(ItemsCsvToSql.UseTypes));
                case "item_slot":
                    return ConvertEnum(value, typeof(ItemsCsvToSql.ItemSlots));
                default:
                    return value;
            }
        }
    }
}
