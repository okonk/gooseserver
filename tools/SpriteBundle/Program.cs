namespace Goose.Tools.SpriteBundle;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length == 0) return 0;

        switch (args[0])
        {
            case "derive-sheets":
                return DeriveSheets(args[1..]);
            default:
                Console.Error.WriteLine($"unknown command '{args[0]}'");
                return 1;
        }
    }

    /// <summary>Prints the union of icon sheets referenced by the given databases, wrapped to
    /// match the checked-in file so the body pastes straight into the "iconSheets" array. Pass
    /// every dataset at once — `derive-sheets &lt;db1&gt; &lt;db2&gt;` — rather than running per
    /// dataset and merging by hand.</summary>
    private static int DeriveSheets(string[] dbPaths)
    {
        if (dbPaths.Length == 0)
        {
            Console.Error.WriteLine("usage: SpriteBundle derive-sheets <db> [<db>...]");
            return 1;
        }

        var sheets = new SortedSet<int>();
        try
        {
            foreach (var dbPath in dbPaths)
            {
                var derived = SheetDeriver.Derive(dbPath);
                Console.Error.WriteLine($"{dbPath}: {derived.Count} sheets");
                sheets.UnionWith(derived);
            }
        }
        catch (InvalidDataException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }

        Console.Error.WriteLine($"union: {sheets.Count} sheets");
        Console.WriteLine(SheetDeriver.Format(sheets));
        return 0;
    }
}
