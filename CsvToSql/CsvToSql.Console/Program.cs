using System;
using System.IO;

namespace CsvToSql.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            File.Delete("illutiaData.sql");

            var sqlTemplate = CsvToSql.Core.CsvToSqlConverter.Convert();

            File.WriteAllText("illutiaData.sql", sqlTemplate);
        }
    }
}
