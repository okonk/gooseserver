using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CsvToSql
{
    class MapsCsvToSql : CsvToSqlBase
    {
        protected override string[] GetColumns()
        {
            return new[] {
                "map_id", "map_name", "map_filename", "map_x", "map_y", "min_level", "max_level", "min_experience", "max_experience", "pvp_enabled", "chat_enabled", "auction_enabled", "shout_enabled", "spells_enabled", "bind_enabled", "items_enabled", "pets_enabled",
                "script_path", "script_params",
            };
        }

        protected override string TransformValue(string columnName, string value)
        {
            switch (columnName)
            {
                case "map_name":
                case "map_filename":
                case "script_path":
                case "script_params":
                    return EscapeString(value);
                case "pvp_enabled":
                case "chat_enabled":
                case "auction_enabled":
                case "shout_enabled":
                case "spells_enabled":
                case "bind_enabled":
                case "items_enabled":
                case "pets_enabled":
                    // booleans
                    return EscapeString(value);
                default:
                    return value;
            }
        }
    }
}
