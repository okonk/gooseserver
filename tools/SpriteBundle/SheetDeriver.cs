using System.Data.SQLite;

namespace Goose.Tools.SpriteBundle;

/// <summary>Derives the icon sheet list from a built game database. Run once per dataset and
/// union the results into sheets.json.</summary>
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
}
