using System.Text.Json;
using System.Text.Json.Serialization;

namespace Goose.Tools.SpriteBundle;

public readonly record struct SpriteRect(int X, int Y, int W, int H);

/// <summary>Reads Assets/Sprites/manifest.json, produced by the client's
/// AssetConverter FrameManifestBuilder. Rects are pixel coordinates into
/// sheets/&lt;sheet&gt;.png and are used verbatim (see Scripts/Map/SpriteCache.cs:33).</summary>
public sealed class Manifest
{
    /// <summary>The JSON keys are lowercase and no case-insensitive option is set, so the
    /// [JsonPropertyName] attributes are load-bearing: without them these bind to null/0.
    /// Everything nested is nullable because System.Text.Json does not enforce NRTs on
    /// deserialisation — Load validates each level rather than trusting the annotations.</summary>
    private sealed record Root(
        [property: JsonPropertyName("tileSize")] int TileSize,
        [property: JsonPropertyName("sheets")] Dictionary<string, Dictionary<string, int[]?>?>? Sheets);

    public int TileSize { get; }
    public IReadOnlyDictionary<int, IReadOnlyDictionary<int, SpriteRect>> Sheets { get; }
    public string AssetRoot { get; }

    private Manifest(string assetRoot, int tileSize,
                     Dictionary<int, IReadOnlyDictionary<int, SpriteRect>> sheets)
    {
        AssetRoot = assetRoot;
        TileSize = tileSize;
        Sheets = sheets;
    }

    public static Manifest Load(string assetRoot)
    {
        var path = Path.Combine(assetRoot, "manifest.json");
        using var fs = File.OpenRead(path);

        Root? root;
        try
        {
            root = JsonSerializer.Deserialize<Root>(fs);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"{path} is not valid JSON: {ex.Message}", ex);
        }

        if (root is null)
            throw new InvalidDataException($"{path} did not deserialise");

        if (root.TileSize <= 0)
            throw new InvalidDataException(
                $"{path} has no positive 'tileSize' (got {root.TileSize})");

        if (root.Sheets is null)
            throw new InvalidDataException($"{path} has no 'sheets' object");

        var sheets = new Dictionary<int, IReadOnlyDictionary<int, SpriteRect>>(root.Sheets.Count);
        foreach (var (sheetKey, graphics) in root.Sheets)
        {
            if (!int.TryParse(sheetKey, out var sheetId))
                throw new InvalidDataException($"{path}: sheet key '{sheetKey}' is not a number");

            if (graphics is null)
                throw new InvalidDataException($"{path}: sheet {sheetKey} has no graphics object");

            var inner = new Dictionary<int, SpriteRect>(graphics.Count);
            foreach (var (graphicKey, r) in graphics)
            {
                if (!int.TryParse(graphicKey, out var graphicId))
                    throw new InvalidDataException(
                        $"{path}: graphic key '{graphicKey}' in sheet {sheetKey} is not a number");

                if (r is not { Length: >= 4 })
                    throw new InvalidDataException(
                        $"{path}: rect for sheet {sheetKey} graphic {graphicKey} needs 4 values, got {r?.Length ?? 0}");

                inner[graphicId] = new SpriteRect(r[0], r[1], r[2], r[3]);
            }

            sheets[sheetId] = inner;
        }

        return new Manifest(assetRoot, root.TileSize, sheets);
    }

    public bool TryGetRect(int sheet, int graphic, out SpriteRect rect)
    {
        rect = default;
        return Sheets.TryGetValue(sheet, out var g) && g.TryGetValue(graphic, out rect);
    }

    public string SheetPath(int sheet) =>
        Path.Combine(AssetRoot, "sheets", $"{sheet}.png");
}
