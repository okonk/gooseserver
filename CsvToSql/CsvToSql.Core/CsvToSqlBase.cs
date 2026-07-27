using ClosedXML.Excel;
using CsvToSql.Core.Schema;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CsvToSql
{
    public abstract class CsvToSqlBase
    {
        /// <summary>Descriptors for this sheet, or null while the converter still uses the
        /// legacy string[] path. Ordered 1:1 with worksheet columns.</summary>
        public virtual Column[] GetColumnDescriptors() => null;

        /// <summary>Editor-facing composite annotations. Does not affect column order.</summary>
        public virtual Composite[] GetComposites() => null;

        public string Convert(IXLWorksheet worksheet, string template, string tableName)
        {
            var descriptors = GetColumnDescriptors();
            string[] allColumns = descriptors != null
                ? descriptors.Select(d => d.Name).ToArray()
                : GetColumns();

            var sqlBuilder = new StringBuilder();

            foreach (var row in worksheet.Rows().Skip(1).Where(r => !r.IsEmpty()))
            {
                List<string> columns = new List<string>();
                List<string> values = new List<string>();

                for (int i = 0; i < allColumns.Length; i++)
                {
                    string value = row.Cell(i + 1).GetValue<string>();
                    if (value.Length == 0) continue;

                    columns.Add(allColumns[i]);
                    values.Add(descriptors != null
                        ? DescriptorTransform.Apply(descriptors[i], value)
                        : TransformValue(allColumns[i], value));
                }

                sqlBuilder.AppendFormat("INSERT INTO {0} (", tableName);
                sqlBuilder.Append(string.Join(", ", columns));
                sqlBuilder.Append(")\nVALUES (");
                sqlBuilder.Append(string.Join(", ", values));
                sqlBuilder.Append(");\n");
            }

            return template.Replace("{{" + tableName + "}}", sqlBuilder.ToString());
        }

        protected string EscapeString(string value)
        {
            return string.Format("'{0}'", value.Replace("'", "''"));
        }

        protected string ConvertEnum(string value, Type enumType)
        {
            return ((int)Enum.Parse(enumType, value)).ToString();
        }

        protected virtual string TransformValue(string columnName, string value) => value;
        protected virtual string[] GetColumns() => null;
    }
}
