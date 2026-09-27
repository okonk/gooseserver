using System.Text.Json;

namespace Goose.Tools.SpriteBundle;

/// <summary>The sheets the client's graphic viewer files under ItemTiles in
/// animation-manifest.json, which the data editor uses to keep non-item art out of item
/// pickers.</summary>
public sealed record SheetTags(IReadOnlyList<int> ItemSheets)
{
    private const string ItemTilesCategory = "ItemTiles";

    public static SheetTags Load(string assetRoot)
    {
        var path = Path.Combine(assetRoot, "animation-manifest.json");

        using var animation = Parse(path);
        if (!animation.RootElement.TryGetProperty("sheets", out var sheets)
            || sheets.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException($"{path} has no 'sheets' object");

        var items = new SortedSet<int>();
        foreach (var sheet in sheets.EnumerateObject())
        {
            if (!int.TryParse(sheet.Name, out var number))
                throw new InvalidDataException($"{path}: sheet key '{sheet.Name}' is not a number");
            if (!sheet.Value.TryGetProperty("categories", out var categories)
                || categories.ValueKind != JsonValueKind.Array) continue;
            if (categories.EnumerateArray().Any(c =>
                    c.ValueKind == JsonValueKind.Object
                    && c.TryGetProperty("name", out var name)
                    && name.ValueKind == JsonValueKind.String
                    && name.GetString() == ItemTilesCategory))
                items.Add(number);
        }

        return new SheetTags(items.ToList());
    }

    public SheetTags Within(IEnumerable<int> sheets)
    {
        var keep = sheets.ToHashSet();
        return new SheetTags(ItemSheets.Where(keep.Contains).ToList());
    }

    private static JsonDocument Parse(string path)
    {
        string text;
        try
        {
            text = File.ReadAllText(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new InvalidDataException($"{path} could not be read: {ex.Message}", ex);
        }

        try
        {
            return JsonDocument.Parse(text);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"{path} is not valid JSON: {ex.Message}", ex);
        }
    }
}
