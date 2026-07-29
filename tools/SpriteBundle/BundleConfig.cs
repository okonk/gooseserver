using System.Text.Json;
using System.Text.Json.Serialization;

namespace Goose.Tools.SpriteBundle;

/// <summary>Which sheets and clips go into each bundle. Seeded by `SpriteBundle derive-sheets`
/// from the live datasets, then hand-editable: add sheet numbers here to make graphics
/// selectable in the editor before any data references them.</summary>
public sealed class BundleConfig
{
    [JsonPropertyName("atlasWidth")]
    public int AtlasWidth { get; init; } = 2048;

    /// <summary>Sheets whose every graphic goes into the icons bundle.</summary>
    [JsonPropertyName("iconSheets")]
    public IReadOnlyList<int> IconSheets { get; init; } = [];

    [JsonPropertyName("partCategories")]
    public IReadOnlyList<string> PartCategories { get; init; } = [];

    /// <summary>Resting-pose clips. body_state only selects equip vs no-equip for idle; its
    /// 4/5/6/7 weapon variants affect attack clips only (see AnimationNames.Candidates in
    /// the client), so no attack poses are needed for a static preview.</summary>
    [JsonPropertyName("partClips")]
    public IReadOnlyList<string> PartClips { get; init; } = [];

    [JsonPropertyName("effectsCategory")]
    public string EffectsCategory { get; init; } = "Effects";

    /// <summary>Anything unreadable fails loudly naming the file, matching Manifest.Load and
    /// TresParser.Parse: a silently defaulted config would produce an empty bundle rather
    /// than a build error.</summary>
    public static BundleConfig Load(string path)
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

        BundleConfig? config;
        try
        {
            config = JsonSerializer.Deserialize<BundleConfig>(text);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"{path} is not valid JSON: {ex.Message}", ex);
        }

        if (config is null)
            throw new InvalidDataException($"{path} did not deserialise");

        if (config.AtlasWidth <= 0)
            throw new InvalidDataException(
                $"{path} has no positive 'atlasWidth' (got {config.AtlasWidth})");

        // Absent, explicitly null and empty are all rejected together: each means the bundle would
        // be built from nothing, which used to overwrite the committed artifact with an empty one
        // and exit 0. Null is not hypothetical despite the non-nullable annotations above:
        // System.Text.Json does not enforce NRTs, so a JSON null calls the setter and blows past
        // the `= []` initialiser, exactly as it did to Manifest's nested collections.
        Require(config.IconSheets, "iconSheets");
        RequireNames(config.PartCategories, "partCategories");
        RequireNames(config.PartClips, "partClips");

        // A blank category would reach Path.Combine and silently resolve to the asset root itself.
        if (string.IsNullOrWhiteSpace(config.EffectsCategory))
            throw new InvalidDataException($"{path} has no 'effectsCategory'");

        return config;

        // Nullable despite the properties' annotations, for the reason above: this is the one
        // place in the file that knows those annotations are not enforced.
        void Require<T>(IReadOnlyList<T>? values, string property)
        {
            if (values is not { Count: > 0 })
                throw new InvalidDataException($"{path} has no '{property}' entries");
        }

        // Element nulls survive the count check and then reach Path.Combine, which rejects them
        // with "Value cannot be null. (Parameter 'path2')" — naming neither this file nor the
        // property it came from.
        void RequireNames(IReadOnlyList<string>? values, string property)
        {
            Require(values, property);

            if (values!.Any(string.IsNullOrWhiteSpace))
                throw new InvalidDataException($"{path} has a blank entry in '{property}'");
        }
    }
}
