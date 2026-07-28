using ClosedXML.Excel;
using CsvToSql.Core.Schema;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsvToSql
{
    public abstract class CsvToSqlBase
    {
        /// <summary>Descriptors for this sheet. Ordered 1:1 with worksheet columns — cells are
        /// read positionally, so the order is load-bearing.</summary>
        public abstract Column[] GetColumnDescriptors();

        /// <summary>Editor-facing composite annotations. Does not affect column order.</summary>
        public virtual Composite[] GetComposites() => null;

        /// <summary>Renders one INSERT per non-empty worksheet row. An empty cell is omitted
        /// from its statement entirely, so the column's declared default applies.</summary>
        public string BuildInserts(IXLWorksheet worksheet, string tableName)
        {
            var descriptors = GetColumnDescriptors();
            var sqlBuilder = new StringBuilder();

            foreach (var row in worksheet.Rows().Skip(1).Where(r => !r.IsEmpty()))
            {
                List<string> columns = new List<string>();
                List<string> values = new List<string>();

                for (int i = 0; i < descriptors.Length; i++)
                {
                    string value = row.Cell(i + 1).GetValue<string>();
                    if (value.Length == 0) continue;

                    columns.Add(descriptors[i].Name);
                    values.Add(DescriptorTransform.Apply(descriptors[i], value));
                }

                sqlBuilder.AppendFormat("INSERT INTO {0} (", tableName);
                sqlBuilder.Append(string.Join(", ", columns));
                sqlBuilder.Append(")\nVALUES (");
                sqlBuilder.Append(string.Join(", ", values));
                sqlBuilder.Append(");\n");
            }

            return sqlBuilder.ToString();
        }
    }
}
