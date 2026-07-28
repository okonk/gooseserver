using System;
using System.IO;

namespace CsvToSql.Console
{
    class Program
    {
        private const string OutputFile = "illutiaData.sql";

        static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                System.Console.Error.WriteLine(
                    "usage: CsvToSql.Console <google-sheet-id>\n" +
                    $"Downloads the sheet and writes {OutputFile} to the current directory.");
                return 1;
            }

            var dataLinkId = args[0];

            // The id is the path segment between /d/ and /edit, never a whole URL — passing one
            // was the original bug here, and it fails deep inside as an opaque 404.
            if (dataLinkId.Contains('/') || dataLinkId.Contains(':'))
            {
                System.Console.Error.WriteLine(
                    "That looks like a URL, not a sheet id. Pass only the id — the part of the " +
                    "URL between '/d/' and '/edit'.");
                return 1;
            }

            string sql;
            try
            {
                sql = CsvToSql.Core.CsvToSqlConverter.Convert(dataLinkId);
            }
            catch (Exception e)
            {
                // Convert blocks on .Result, so the top-level message is a useless
                // "One or more errors occurred." — report the real cause instead.
                System.Console.Error.WriteLine($"Could not convert sheet '{dataLinkId}': " +
                                               e.GetBaseException().Message);
                System.Console.Error.WriteLine(
                    "Check the id is correct and the sheet is shared so it can be exported.");
                return 1;
            }

            File.WriteAllText(OutputFile, sql);
            System.Console.WriteLine(
                $"Wrote {OutputFile} ({new FileInfo(OutputFile).Length} bytes)");
            return 0;
        }
    }
}
