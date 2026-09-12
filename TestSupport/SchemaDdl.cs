using CsvToSql.Core.Schema;
using System.Text;

namespace Goose.Testing;

/// <summary>The game data tables, whose DDL the spreadsheet pipeline owns. Everything else —
/// players, banks, pets, guilds, logs and wordfilter — is still hand-written in Goose/sql and is
/// read from the shipped scripts instead.</summary>
public static class SchemaDdl
{
    public static string For(string name)
    {
        var table = SchemaRegistry.Tables.SingleOrDefault(t => t.Table == name)
            ?? throw new ArgumentException("No generated table named '" + name + "'.", nameof(name));
        return TableDdl.Emit(table.Table, table.Columns, table.Indexes);
    }

    /// <summary>DDL for several tables, in the order given. Fails loudly on a name that is not
    /// generated rather than silently emitting nothing.</summary>
    public static string ForAll(params string[] names)
    {
        var sb = new StringBuilder();
        foreach (var name in names)
            sb.Append(For(name));
        return sb.ToString();
    }
}
