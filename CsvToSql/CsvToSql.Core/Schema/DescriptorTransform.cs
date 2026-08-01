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
                    return ParseEnum(column, value);
                default:
                    return value;
            }
        }

        /// <summary>Same as the old ConvertEnum, but names the offending column — otherwise a
        /// bad cell reports only its value, with no clue which of 21 sheets it came from.</summary>
        private static string ParseEnum(Column column, string value)
        {
            try
            {
                return ((int)System.Enum.Parse(column.EnumType, value)).ToString();
            }
            catch (System.ArgumentException e)
            {
                throw new System.ArgumentException(
                    $"Column '{column.Name}' has no {column.EnumType.Name} member named '{value}'.", e);
            }
        }

        private static string Escape(string value) =>
            string.Format("'{0}'", value.Replace("'", "''"));
    }
}
