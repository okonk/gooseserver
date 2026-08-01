using System;

namespace CsvToSql.Core.Schema
{
    /// <summary>Factory methods for column descriptors. `def` is the SQL DEFAULT rendered
    /// verbatim; pass null for a column with no default.</summary>
    public static class Col
    {
        public static Column Id(string name, SqlType type = null, int? def = null) =>
            Make(name, ColumnKind.Id, type ?? SqlType.Integer, def?.ToString());

        public static Column Int(string name, SqlType type = null, int? def = null) =>
            Make(name, ColumnKind.Int, type ?? SqlType.Int, def?.ToString());

        public static Column Decimal(string name, SqlType type = null, string def = null) =>
            Make(name, ColumnKind.Decimal, type ?? SqlType.Decimal94, def);

        public static Column Text(string name, SqlType type = null, string def = null) =>
            Make(name, ColumnKind.Text, type ?? SqlType.Text, def);

        /// <summary>Stored as CHAR(1) '0'/'1'. Previously indistinguishable from Text —
        /// the converters marked these only with a `// booleans` comment.</summary>
        public static Column Bool(string name, bool? def = null) =>
            Make(name, ColumnKind.Bool, SqlType.Char1, def is null ? null : (def.Value ? "'1'" : "'0'"));

        public static Column Enum<T>(string name, SqlType type = null, int? def = null)
            where T : struct, System.Enum =>
            Make(name, ColumnKind.Enum, type ?? SqlType.SmallInt, def?.ToString())
                .WithEnum(typeof(T));

        private static Column Make(string name, ColumnKind kind, SqlType type, string def)
        {
            var c = new Column(name, kind, type, def);
            if (def == null) c.Required();
            return c;
        }
    }
}
