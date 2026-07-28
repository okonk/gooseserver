using System.Globalization;
using System.Text.RegularExpressions;

namespace Goose.Tools.SpriteBundle;

/// <summary>A frame's location: which sheet PNG, and the pixel rect within it.</summary>
public readonly record struct TresFrame(int Sheet, int X, int Y, int W, int H);

public sealed class TresFile
{
    /// <summary>Clip name to its ordered frame list.</summary>
    public required IReadOnlyDictionary<string, IReadOnlyList<TresFrame>> Clips { get; init; }

    public bool TryGetFirstFrame(string clip, out TresFrame frame)
    {
        frame = default;
        if (!Clips.TryGetValue(clip, out var frames) || frames.Count == 0) return false;
        frame = frames[0];
        return true;
    }
}

/// <summary>Parses Godot .tres SpriteFrames resources produced by the client's AssetConverter.
///
/// The animations array is a single very long line and frame entries contain '}' characters,
/// so a per-clip lazy regex will span earlier clips and silently return the wrong frame.
/// Instead every clip object is matched globally, capturing frames and name together.</summary>
public static class TresParser
{
    private static readonly Regex Header = new(
        @"^\[gd_resource type=""SpriteFrames""", RegexOptions.Compiled);

    private static readonly Regex ExtResource = new(
        @"\[ext_resource type=""Texture2D"" path=""res://Assets/Sprites/sheets/(\d+)\.png"" id=""([^""]+)""\]",
        RegexOptions.Compiled);

    private static readonly Regex SubResource = new(
        @"\[sub_resource type=""AtlasTexture"" id=""([^""]+)""\]\s*\natlas = ExtResource\(""([^""]+)""\)\s*\nregion = Rect2\(([\d.]+), ([\d.]+), ([\d.]+), ([\d.]+)\)",
        RegexOptions.Compiled);

    private static readonly Regex Clip = new(
        @"\{""frames"": \[(.*?)\],""loop"": (?:true|false),""name"": &""([^""]+)"",""speed"": [\d.]+\}",
        RegexOptions.Compiled);

    private static readonly Regex FrameRef = new(
        @"SubResource\(""([^""]+)""\)", RegexOptions.Compiled);

    /// <summary>Counts declared clips independently of Clip. This marker appears nowhere else in
    /// the corpus, so it is an exact oracle for how many clips the file should yield.</summary>
    private static readonly Regex ClipName = new(@"""name"": &""", RegexOptions.Compiled);

    /// <summary>Malformed input fails loudly, naming the file: these resources are generated, so
    /// anything unparseable means the client's format moved and a silently empty (or short) clip
    /// list would surface much later as a missing sprite rather than a build error.</summary>
    public static TresFile Parse(string path)
    {
        var text = File.ReadAllText(path);

        if (!Header.IsMatch(text))
            throw new InvalidDataException($"{path} is not a SpriteFrames resource");

        var textures = new Dictionary<string, int>();
        foreach (Match m in ExtResource.Matches(text))
            textures[m.Groups[2].Value] = int.Parse(m.Groups[1].Value);

        // An AtlasTexture whose ExtResource is undeclared has no sheet number, so it is left out
        // here and only reported if a clip actually references it.
        var atlases = new Dictionary<string, TresFrame>();
        foreach (Match m in SubResource.Matches(text))
        {
            if (!textures.TryGetValue(m.Groups[2].Value, out var sheet)) continue;

            atlases[m.Groups[1].Value] = new TresFrame(
                sheet,
                Px(m.Groups[3].Value), Px(m.Groups[4].Value),
                Px(m.Groups[5].Value), Px(m.Groups[6].Value));
        }

        var clips = new Dictionary<string, IReadOnlyList<TresFrame>>();
        foreach (Match m in Clip.Matches(text))
        {
            var name = m.Groups[2].Value;
            var frames = new List<TresFrame>();
            foreach (Match f in FrameRef.Matches(m.Groups[1].Value))
            {
                var id = f.Groups[1].Value;
                if (!atlases.TryGetValue(id, out var frame))
                    throw new InvalidDataException(
                        $"{path}: clip '{name}' references sub-resource '{id}', which is not a "
                        + "resolvable AtlasTexture");

                frames.Add(frame);
            }

            clips[name] = frames;
        }

        if (clips.Count == 0)
            throw new InvalidDataException($"{path} declares no animations");

        // A clip Clip cannot match is not merely dropped: its frames get absorbed into the next
        // clip that does match, which then reports the wrong first frame. Nor does a duplicate
        // name survive the dictionary. Both show up as a count that disagrees with the file.
        var declared = ClipName.Matches(text).Count;
        if (declared != clips.Count)
            throw new InvalidDataException(
                $"{path} declares {declared} animations but {clips.Count} parsed; a clip is "
                + "malformed or its name is duplicated");

        return new TresFile { Clips = clips };
    }

    /// <summary>Rect2 components are written as floats ("0" or "0.0"); truncate to pixels.</summary>
    private static int Px(string value) =>
        (int)double.Parse(value, CultureInfo.InvariantCulture);
}
