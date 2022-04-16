using System;
using System.IO;

namespace CsvToSql.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            File.Delete("illutiaData.sql");

            var sqlTemplate = CsvToSql.Core.CsvToSqlConverter.Convert("https://docs.google.com/spreadsheets/u/0/d/1AE7SKm46KuAdr5mJk_u-Qn20C1Myg4Q6eyzxTb3Wqpo/export?format=xlsx&id=1AE7SKm46KuAdr5mJk_u-Qn20C1Myg4Q6eyzxTb3Wqpo");

            File.WriteAllText("illutiaData.sql", sqlTemplate);
        }
    }
}
