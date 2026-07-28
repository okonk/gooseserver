using ClosedXML.Excel;
using CsvToSql.Core.Schema;
using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
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
        public static string ConvertWorkbook(Stream spreadsheet)
        {
            var assembly = typeof(CsvToSqlConverter).GetTypeInfo().Assembly;
            var resource = assembly.GetManifestResourceStream($"CsvToSql.Core.sqlTemplate.sql");
            using var streamReader = new StreamReader(resource, Encoding.UTF8);

            string sqlTemplate = streamReader.ReadToEnd();

            using (var workbook = new XLWorkbook(spreadsheet))
            {
                foreach (var schema in SchemaRegistry.Tables)
                {
                    if (!workbook.Worksheets.TryGetWorksheet(schema.Sheet, out var worksheet))
                        throw new InvalidOperationException(
                            $"Spreadsheet is missing required worksheet '{schema.Sheet}'.");

                    sqlTemplate = schema.Converter.Convert(worksheet, sqlTemplate, schema.Table);
                }
            }

            return sqlTemplate;
        }
    }
}
