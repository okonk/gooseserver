using System;
using System.IO;

namespace CsvToSql.Console
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                System.Console.Error.WriteLine("usage: CsvToSql.Console <google-sheet-id>");
                return 1;
            }

            var sql = CsvToSql.Core.CsvToSqlConverter.Convert(args[0]);
            File.WriteAllText("illutiaData.sql", sql);
            System.Console.WriteLine($"Wrote illutiaData.sql ({sql.Length} bytes)");
            return 0;
        }
    }
}
