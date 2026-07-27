using System;
using System.Collections.Generic;

namespace CsvToSql.Core.Schema
{
    /// <summary>How a cell value is escaped on the way into SQL, and how the editor renders it.</summary>
    public enum ColumnKind { Id, Int, Decimal, Text, Bool, Enum }

    /// <summary>One spreadsheet column. Descriptors are a flat ordered list: index i maps to
    /// worksheet cell i + 1 (see CsvToSqlBase.Convert), so never merge or reorder them.</summary>
    public sealed class Column
    {
        public string Name { get; }
        public ColumnKind Kind { get; }
        public SqlType Type { get; }
        public string Default { get; private set; }

        /// <summary>The spreadsheet cell must be non-empty. Not the same as NOT NULL — every
        /// column is NOT NULL. This means there is no SQL DEFAULT to fall back on, and
        /// CsvToSqlBase omits empty cells from the INSERT, so an empty cell would be rejected.
        /// Always the exact complement of <see cref="Default"/> being set.</summary>
        public bool IsRequired { get; private set; }
        public bool IsPrimaryKey { get; private set; }
        public string RefSheet { get; private set; }
        public Type EnumType { get; private set; }
        public IReadOnlyList<string> EnumNames =>
            EnumType == null ? Array.Empty<string>() : System.Enum.GetNames(EnumType);

        internal Column(string name, ColumnKind kind, SqlType type, string def = null)
        {
            Name = name;
            Kind = kind;
            Type = type;
            Default = def;
        }

        /// <summary>Drops any DEFAULT — a column cannot both have one and require a cell value.
        /// Only needed for a column that would otherwise get a default; Col.* already marks a
        /// column required when no `def` is passed.</summary>
        public Column Required() { IsRequired = true; Default = null; return this; }
        public Column Ref(string sheet) { RefSheet = sheet; return this; }

        public Column PrimaryKey()
        {
            IsPrimaryKey = true;
            IsRequired = true;
            Default = null;
            return this;
        }

        internal Column WithEnum(Type enumType) { EnumType = enumType; return this; }
    }
}
