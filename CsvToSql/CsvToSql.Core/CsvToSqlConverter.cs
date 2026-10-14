using ClosedXML.Excel;
using CsvToSql.Core.Schema;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;

namespace CsvToSql.Core
{
    public class CsvToSqlConverter
    {
        public static string Convert(string dataLinkId)
        {
            var url = $"https://docs.google.com/spreadsheets/u/0/d/{dataLinkId}/export?format=xlsx&id={dataLinkId}";
            var spreadsheet = new MemoryStream(new HttpClient().GetByteArrayAsync(url).Result);

            return ConvertWorkbook(spreadsheet);
        }

        /// <summary>Converts an already-loaded .xlsx stream. Exists so tests can run against a
        /// committed fixture instead of the network.</summary>
        public static string ConvertWorkbook(Stream spreadsheet) =>
            ConvertWorkbook(spreadsheet, SchemaRegistry.Tables);

        internal static string ConvertWorkbook(Stream spreadsheet, IReadOnlyList<TableSchema> schemas)
        {
            var sb = new StringBuilder();
            sb.Append("BEGIN TRANSACTION;\n\n");

            using (var workbook = new XLWorkbook(spreadsheet))
            {
                foreach (var schema in schemas)
                {
                    if (!workbook.Worksheets.TryGetWorksheet(schema.Sheet, out var worksheet))
                    {
                        if (!schema.Optional)
                            throw new InvalidOperationException(
                                $"Spreadsheet is missing required worksheet '{schema.Sheet}'.");

                        sb.Append(TableDdl.Emit(schema.Table, schema.Columns, schema.Indexes));
                        sb.Append('\n');
                        continue;
                    }

                    // Emit and BuildInserts each already end in \n; the appends below are the
                    // blank line separating one block from the next.
                    sb.Append(TableDdl.Emit(schema.Table, schema.Columns, schema.Indexes));
                    sb.Append('\n');
                    sb.Append(schema.Converter.BuildInserts(worksheet, schema.Table, schema.Columns));
                    sb.Append('\n');
                }
            }

            sb.Append("COMMIT;\n");
            return sb.ToString();
        }
    }
}
