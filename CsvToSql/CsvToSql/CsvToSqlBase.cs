using CsvHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CsvToSql
{
    abstract class CsvToSqlBase
    {
        public void Convert(string csvPath, string tableName)
        {
            string[] allColumns = GetColumns();

            using (var writer = new StreamWriter(tableName + ".sql"))
            using (var streamReader = new StreamReader(csvPath))
            using (var csvReader = new CsvReader(streamReader, new CsvHelper.Configuration.CsvConfiguration() { HasHeaderRecord = true }))
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

                    writer.Write("INSERT INTO {0} (", tableName);
                    writer.Write(string.Join(", ", columns));
                    writer.Write(")\nVALUES (");
                    writer.Write(string.Join(", ", values));
                    writer.WriteLine(");\n");
                }
            }
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
