namespace CsvToSql.Core.Schema
{
    /// <summary>SQL column types used by the game data schema. The string value is emitted
    /// verbatim into CREATE TABLE.</summary>
    public sealed class SqlType
    {
        public string Sql { get; }
        private SqlType(string sql) => Sql = sql;

        public static readonly SqlType Integer = new("INTEGER");
        public static readonly SqlType SmallInt = new("SMALLINT");
        public static readonly SqlType Int = new("INT");
        public static readonly SqlType BigInt = new("BIGINT");
        public static readonly SqlType Text = new("TEXT");
        public static readonly SqlType Char1 = new("CHAR(1)");
        public static readonly SqlType Varchar64 = new("VARCHAR(64)");
        public static readonly SqlType Decimal94 = new("DECIMAL(9,4)");
        public static readonly SqlType Decimal92 = new("DECIMAL(9,2)");
        public static readonly SqlType Decimal52 = new("DECIMAL(5,2)");
        public static readonly SqlType Decimal54 = new("DECIMAL(5,4)");

        public override string ToString() => Sql;
    }
}
