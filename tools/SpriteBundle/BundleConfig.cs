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

        return config;
    }
}
