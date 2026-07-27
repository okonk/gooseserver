using System.Collections.Generic;
using System.Text;

namespace CsvToSql.Core.Schema
{
    /// <summary>Renders DROP/CREATE/CREATE INDEX for one table from its column descriptors.
    /// Replaces the hand-maintained sqlTemplate.sql.</summary>
    public static class TableDdl
    {
        public static string Emit(string table, IReadOnlyList<Column> columns,
                                  IReadOnlyList<string> indexes)
        {
            var sb = new StringBuilder();
            sb.Append($"DROP TABLE IF EXISTS {table};\n");
            sb.Append($"CREATE TABLE {table} (\n");

            for (int i = 0; i < columns.Count; i++)
            {
                var c = columns[i];
                sb.Append("  ").Append(c.Name).Append(' ').Append(c.Type.Sql);

                if (c.IsPrimaryKey)
                {
                    sb.Append(" PRIMARY KEY");
                }
                else
                {
                    if (c.Default != null) sb.Append(" DEFAULT ").Append(c.Default);
                    sb.Append(" NOT NULL");
                }

                if (i < columns.Count - 1) sb.Append(',');
                sb.Append('\n');
            }

            sb.Append(");\n");

            if (indexes != null)
                foreach (var col in indexes)
                    sb.Append($"CREATE INDEX {table}_{col}_idx ON {table}({col});\n");

            return sb.ToString();
        }
    }
}
