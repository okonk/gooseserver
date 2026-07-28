using System.Data.SQLite;
using System.Text;

namespace Goose.Tools.SpriteBundle;

/// <summary>Derives the icon sheet list a dataset references, from its built game database.
/// Callers pass every dataset's database in one go and union the results; sheets.json itself
/// stays checked in, so this only ever seeds or refreshes it.</summary>
public static class SheetDeriver
{
    private static readonly string[] Queries =
    [
        "SELECT DISTINCT graphic_file FROM item_templates",
        "SELECT DISTINCT spellbook_graphic_file FROM spells",
        "SELECT DISTINCT buff_graphic_file FROM spell_effects WHERE buff_graphic > 0",
        "SELECT DISTINCT spell_animation_file FROM spell_effects WHERE spell_animation > 0",
    ];

    /// <summary>Missing files and schema drift fail loudly naming the database, matching
    /// Manifest.Load and TresParser.Parse: an empty sheet list looks like a valid answer.</summary>
    public static SortedSet<int> Derive(string dbPath)
    {
        if (!File.Exists(dbPath))
            throw new InvalidDataException($"{dbPath} does not exist");

        var sheets = new SortedSet<int>();

        using var conn = new SQLiteConnection($"Data Source={dbPath};Read Only=True");
        conn.Open();

        foreach (var sql in Queries)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;

            SQLiteDataReader r;
            try
            {
                r = cmd.ExecuteReader();
            }
            catch (SQLiteException ex)
            {
                throw new InvalidDataException($"{dbPath}: `{sql}` failed: {ex.Message}", ex);
            }

            using (r)
            {
                while (r.Read())
                {
                    if (r.IsDBNull(0)) continue;
                    var sheet = Convert.ToInt32(r.GetValue(0));
                    if (sheet > 0) sheets.Add(sheet);   // 0 is the "no graphic" sentinel
                }
            }
        }

        return sheets;
    }

    /// <summary>Renders a sheet list as the body of sheets.json's "iconSheets" array: four-space
    /// indent, wrapped at 72 columns so it pastes without a human reflowing it. Nothing depends
    /// on matching the checked-in file's width.</summary>
    public static string Format(IEnumerable<int> sheets)
    {
        const string Indent = "    ";
        const int Width = 72;

        // Commas ride along with the value they follow, so wrapping never orphans one.
        var list = sheets.ToList();
        var tokens = list.Select((s, i) => i == list.Count - 1 ? $"{s}" : $"{s},").ToList();

        var lines = new List<string>();
        var line = new StringBuilder(Indent);

        foreach (var token in tokens)
        {
            var separator = line.Length > Indent.Length ? " " : "";
            if (line.Length + separator.Length + token.Length > Width)
            {
                lines.Add(line.ToString());
                line = new StringBuilder(Indent);
                separator = "";
            }

            line.Append(separator).Append(token);
        }

        if (line.Length > Indent.Length) lines.Add(line.ToString());
        return string.Join('\n', lines);
    }
}
