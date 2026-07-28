using System.Diagnostics;

namespace Goose.Tools.SpriteBundle;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            Usage();
            return 1;
        }

        switch (args[0])
        {
            case "derive-sheets":
                return DeriveSheets(args[1..]);
            default:
                return Bundle(args);
        }
    }

    private static void Usage()
    {
        Console.Error.WriteLine("usage: SpriteBundle <client-assets-dir> <output-dir>");
        Console.Error.WriteLine("       SpriteBundle derive-sheets <db> [<db>...]");
    }

    /// <summary>Builds the three bundles and writes them as HTML fragments.</summary>
    private static int Bundle(string[] args)
    {
        if (args.Length != 2)
        {
            Usage();
            return 1;
        }

        var assetRoot = args[0];
        var outDir = args[1];

        try
        {
            Directory.CreateDirectory(outDir);

            var config = BundleConfig.Load(Path.Combine(AppContext.BaseDirectory, "sheets.json"));
            var manifest = Manifest.Load(assetRoot);

            var sw = Stopwatch.StartNew();
            long total = 0;

            total += Emit("icons", outDir,
                () => AtlasBuilder.Build(manifest, Bundles.Icons(manifest, config),
                                         config.AtlasWidth));

            total += Emit("parts", outDir,
                () => AtlasBuilder.BuildFromFrames(manifest, Bundles.Parts(assetRoot, config),
                                                   config.AtlasWidth));

            total += Emit("effects", outDir,
                () => AtlasBuilder.BuildFromFrames(manifest, Bundles.Effects(assetRoot, config),
                                                   config.AtlasWidth));

            Console.WriteLine(
                $"Total {total / 1024.0 / 1024.0:F2} MB of HTML in {sw.Elapsed.TotalSeconds:F1}s");
            return 0;
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException
                                      or UnauthorizedAccessException or ArgumentException)
        {
            Console.Error.WriteLine($"sprite bundle failed: {ex.GetBaseException().Message}");
            return 1;
        }
    }

    private static long Emit(string name, string outDir, Func<BuiltAtlas> build)
    {
        using var built = build();
        var html = BundleWriter.Render(name, built.Image, built.Rects);
        var path = Path.Combine(outDir, $"sprites-{name}.html");
        File.WriteAllText(path, html);

        var used = built.Rects.Values.Sum(r => (long)r.W * r.H);
        var area = (long)built.Image.Width * built.Image.Height;
        var bytes = new FileInfo(path).Length;

        Console.WriteLine(
            $"{name,-8} {built.Rects.Count,6} sprites  " +
            $"{built.Image.Width}x{built.Image.Height}  " +
            $"{used * 100.0 / area:F0}% efficient  " +
            $"{bytes / 1024.0 / 1024.0:F2} MB html");

        // Grouped by sheet, not counted: skips cluster by sheet (a whole sheet's rects overrun it,
        // or its PNG is missing and every sprite on it vanishes), and a bare total would let a
        // whole effect disappear from the bundle without anything saying which one.
        foreach (var group in built.Skipped.GroupBy(s => s.Sheet).OrderBy(g => g.Key))
            Console.WriteLine(
                $"         skipped sheet {group.Key}: {group.Count()} sprites — " +
                group.First().Reason);

        return bytes;
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
