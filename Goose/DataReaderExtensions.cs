using System.Data.Common;

namespace Goose
{
    internal static class DataReaderExtensions
    {
        public static int GetInt32(this DbDataReader reader, string column)
            => Convert.ToInt32(reader[column]);

        public static long GetInt64(this DbDataReader reader, string column)
            => Convert.ToInt64(reader[column]);

        public static string GetString(this DbDataReader reader, string column)
            => Convert.ToString(reader[column]);

        public static decimal GetDecimal(this DbDataReader reader, string column)
            => Convert.ToDecimal(reader[column]);

        public static double GetDouble(this DbDataReader reader, string column)
            => Convert.ToDouble(reader[column]);
    }
}
