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

            // Disposed before the move on any failure, so a build that dies partway leaves all
            // three committed fragments exactly as they were.
            using var stage = new BundleStage(outDir);

            total += Emit("icons", stage,
                () => AtlasBuilder.Build(manifest, Bundles.Icons(manifest, config),
                                         config.AtlasWidth));

            total += Emit("parts", stage,
                () => AtlasBuilder.BuildFromFrames(manifest, Bundles.Parts(assetRoot, config),
                                                   config.AtlasWidth));

            total += Emit("effects", stage,
                () => AtlasBuilder.BuildFromFrames(manifest, Bundles.Effects(assetRoot, config),
                                                   config.AtlasWidth));

            stage.Commit();

            Console.WriteLine(
                $"Total {total / 1024.0 / 1024.0:F2} MB of HTML in {sw.Elapsed.TotalSeconds:F1}s");
            return 0;
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException
                                      or UnauthorizedAccessException or ArgumentException)
        {
            // ex.Message rather than GetBaseException().Message, unlike SchemaGen: this catch
            // spans reading the assets and writing the output, so it cannot name the offending
            // path in its own prefix the way SchemaGen does. Every exception reachable here does
            // name its file — but on Linux EACCES, Directory.CreateDirectory's "Access to the
            // path '...' is denied" wraps an errno-level inner exception that carries no path,
            // so unwrapping to the base exception throws that path away.
            Console.Error.WriteLine($"sprite bundle failed: {ex.Message}");
            return 1;
        }
    }

    /// <summary>Builds, renders and stages one bundle, then reports it. Everything stays inside the
    /// `using`, and the rendered html is handed straight to Stage rather than held: only one
    /// atlas and one ~1.5 MB fragment are alive at a time, even though all three are now staged
    /// before any is moved into place. Stage returns the size on disk, which the console line
    /// used to read back with FileInfo after the final write.</summary>
    private static long Emit(string name, BundleStage stage, Func<BuiltAtlas> build)
    {
        using var built = build();
        var bytes = stage.Stage(name, BundleWriter.Render(name, built.Image, built.Rects));

        var used = built.Rects.Values.Sum(r => (long)r.W * r.H);
        var area = (long)built.Image.Width * built.Image.Height;

        Console.WriteLine(
            $"{name,-8} {built.Rects.Count,6} sprites  " +
            $"{built.Image.Width}x{built.Image.Height}  " +
            // F1, not F0: the build gate is "above 95%", and rounded to whole percent 94.6 and
            // 95.4 both print as 95 — the reported number could not decide the thing it is read
            // for.
            $"{used * 100.0 / area:F1}% efficient  " +
            $"{bytes / 1024.0 / 1024.0:F2} MB html");

        // Grouped by sheet, not counted: skips cluster by sheet (a whole sheet's rects overrun it,
        // or its PNG is missing and every sprite on it vanishes), and a bare total would let a
        // whole effect disappear from the bundle without anything saying which one.
        foreach (var group in built.Skipped.GroupBy(s => s.Sheet).OrderBy(g => g.Key))
        {
            // One representative reason, flagged as such when the group is not uniform, so the
            // line does not read as if all N sprites failed identically. Phrased as varying
            // messages rather than "N other reasons": a bad-rect reason embeds the rect, so one
            // cause across nineteen sprites yields nineteen distinct strings, and calling those
            // nineteen reasons would overclaim in the opposite direction.
            var reasons = group.Select(s => s.Reason).Distinct().Count();
            Console.WriteLine(
                $"         skipped sheet {group.Key}: {group.Count()} sprites — " +
                group.First().Reason +
                (reasons > 1 ? $" (messages vary; showing 1 of {reasons})" : ""));
        }

        return bytes;
    }

    /// <summary>Prints the union of icon sheets referenced by the given databases, indented and
    /// wrapped so the body pastes straight into the "iconSheets" array. Pass every dataset at
    /// once — `derive-sheets &lt;db1&gt; &lt;db2&gt;` — rather than running per dataset and merging
    /// by hand.</summary>
    private static int DeriveSheets(string[] dbPaths)
    {
        if (dbPaths.Length == 0)
        {
            Usage();
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
