using System.Text.Json.Serialization;
using CsvToSql.Core.Schema;

namespace Goose.Tools.SchemaGen;

/// <summary>Serialisation shape for the editor's schema.js. Property names are lowercase in
/// JSON (see SchemaJs) because the Apps Script side reads them directly.</summary>
public sealed record SchemaColumn(
    string Name,
    string? Header,
    string Kind,
    string Sql,
    string? Default,
    bool Required,
    bool Pk,
    string? Ref,
    IReadOnlyList<string>? EnumNames,
    int? Scale,
    double? Max);

public sealed record SchemaComposite(
    string Kind,
    IReadOnlyList<string> Columns,
    string? Source);

public sealed record SchemaSheet(
    string Sheet,
    string Table,
    IReadOnlyList<SchemaColumn> Columns,
    IReadOnlyList<SchemaComposite> Composites,
    IReadOnlyList<string> Indexes,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    bool EditorOnly = false);

public sealed record SchemaRoot(IReadOnlyList<SchemaSheet> Sheets);

public static class SchemaModel
{
    /// <summary>Projects SchemaRegistry.Tables and EditorOnlyTables into the editor-facing
    /// shape. No schema knowledge lives here — it is a pure mapping.</summary>
    public static SchemaRoot Build() => new(
        SchemaRegistry.Tables.Select(t => (Table: t, EditorOnly: false))
            .Concat(SchemaRegistry.EditorOnlyTables.Select(t => (Table: t, EditorOnly: true)))
            .Select(e => Sheet(e.Table, e.EditorOnly)).ToList());

    private static SchemaSheet Sheet(TableSchema t, bool editorOnly) => new(
        t.Sheet,
        t.Table,
        t.Columns.Select(c => new SchemaColumn(
            c.Name,
            c.Header,
            c.Kind.ToString(),
            c.Type.Sql,
            c.Default,
            c.IsRequired,
            c.IsPrimaryKey,
            c.RefSheet,
            c.Kind == ColumnKind.Enum ? c.EnumNames : null,
            c.Scale,
            c.Max)).ToList(),
        t.Composites.Select(k => new SchemaComposite(
            k.Kind.ToString(),
            k.Columns,
            k.SourceSheet)).ToList(),
        t.Indexes,
        editorOnly);
}
