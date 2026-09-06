using System.Text.Json;
using System.Text.Json.Serialization;

namespace Goose.Tools.SchemaGen;

public static class SchemaJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        NewLine = "\n",
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    // The map editor's four consumed sheets. The other registry sheets are not consumed by it
    // and must not leak into the client artifact.
    private static readonly string[] ConsumedSheets = { "NPCs", "NPC Spawns", "Warptiles", "Maps" };

    public static string Render(SchemaRoot model)
    {
        var root = new SchemaRoot(
            model.Sheets.Where(s => ConsumedSheets.Contains(s.Sheet)).ToList());
        return JsonSerializer.Serialize(root, Options);
    }
}
