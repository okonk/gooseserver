using System;

namespace CsvToSql.Core.Schema
{
    /// <summary>Derives SQL literal escaping from a column's kind. Replaces each converter's
    /// hand-written TransformValue switch, which could drift from its column list.</summary>
    public static class DescriptorTransform
    {
        public static string Apply(Column column, string value)
        {
            switch (column.Kind)
            {
                case ColumnKind.Text:
                case ColumnKind.Bool:
                    return Escape(value);
                case ColumnKind.Enum:
                    return ((int)System.Enum.Parse(column.EnumType, value)).ToString();
                default:
                    return value;
            }
        }

        private static string Escape(string value) =>
            string.Format("'{0}'", value.Replace("'", "''"));
    }
}
