using System.Collections.Generic;

namespace CsvToSql.Core.Schema
{
    public enum CompositeKind { Graphic, Rgba, Bitmask, IdList, EquipSlots }

    /// <summary>An editor-facing control spanning one or more columns. Purely an annotation —
    /// the flat Column[] list is unchanged, because worksheet cells are read positionally.</summary>
    public sealed class Composite
    {
        public CompositeKind Kind { get; }
        public IReadOnlyList<string> Columns { get; }

        /// <summary>Double duty: for Bitmask, the sheet whose rows define the bit positions;
        /// for IdList, the sheet the ids point at. Null for the other kinds.</summary>
        public string SourceSheet { get; }

        private Composite(CompositeKind kind, string sourceSheet, params string[] columns)
        {
            Kind = kind;
            SourceSheet = sourceSheet;
            Columns = columns;
        }

        public static Composite Graphic(string tile, string file) =>
            new Composite(CompositeKind.Graphic, null, tile, file);

        public static Composite Rgba(string r, string g, string b, string a) =>
            new Composite(CompositeKind.Rgba, null, r, g, b, a);

        public static Composite Bitmask(string column, string from) =>
            new Composite(CompositeKind.Bitmask, from, column);

        public static Composite IdList(string column, string refSheet) =>
            new Composite(CompositeKind.IdList, refSheet, column);

        public static Composite EquipSlots(string column) =>
            new Composite(CompositeKind.EquipSlots, null, column);
    }
}
