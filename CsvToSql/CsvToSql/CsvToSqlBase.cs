using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CsvToSql
{
    abstract class CsvToSqlBase
    {
        public string Convert(IXLWorksheet worksheet, string template, string tableName)
        {
            string[] allColumns = GetColumns();

            var sqlBuilder = new StringBuilder();

            using (var csvReader = new CsvReader(new ExcelParser(worksheet, new CsvHelper.Configuration.CsvConfiguration() { HasHeaderRecord = true })))
            {
                while (csvReader.Read())
                {
                    List<string> columns = new List<string>();
                    List<string> values = new List<string>();

                    for (int i = 0; i < allColumns.Length; i++)
                    {
                        string value = csvReader.GetField(i);
                        if (value.Length == 0) continue;

                        columns.Add(allColumns[i]);
                        values.Add(TransformValue(allColumns[i], value));
                    }

                    sqlBuilder.AppendFormat("INSERT INTO {0} (", tableName);
                    sqlBuilder.Append(string.Join(", ", columns));
                    sqlBuilder.Append(")\nVALUES (");
                    sqlBuilder.Append(string.Join(", ", values));
                    sqlBuilder.Append(");\n");
                }
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

        protected abstract string TransformValue(string columnName, string value);
        protected abstract string[] GetColumns();
    }
}
