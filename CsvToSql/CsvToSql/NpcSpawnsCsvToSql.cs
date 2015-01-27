using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsvHelper;
using System.IO;

namespace CsvToSql
{
    class NpcSpawnsCsvToSql : CsvToSqlBase
    {
        protected override string[] GetColumns()
        {
            return new[] { "npc_id", "map_id", "map_x", "map_y" };
        }

        protected override string TransformValue(string columnName, string value)
        {
            switch (columnName)
            {
                default:
                    return value;
            }
        }
    }
}
