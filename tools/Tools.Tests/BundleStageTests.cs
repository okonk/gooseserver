using Goose.Tools.SpriteBundle;

namespace Tools.Tests;

/// <summary>The bundles are regenerated as a set. Building one can fail on ordinary client art
/// changes (Bundles.Effects throws on a multi-clip file, TresParser on a malformed one), and
/// writing each fragment as it was built left a half-new, half-stale set of committed artifacts
/// behind — a state no error message describes and git cannot distinguish from an intentional
/// partial regeneration.</summary>
public class BundleStageTests : IDisposable
{
    private readonly List<string> _tempDirs = [];

    public void Dispose()
    {
        foreach (var dir in _tempDirs)
            Directory.Delete(dir, recursive: true);
    }

    private string TempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "bundle-stage-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        _tempDirs.Add(dir);
        return dir;
    }

    private static string Of(string dir, string name) => Path.Combine(dir, $"sprites-{name}.html");

    [Fact]
    public void Commit_replaces_every_staged_bundle()
    {
        var dir = TempDir();
        File.WriteAllText(Of(dir, "icons"), "stale");

        using (var stage = new BundleStage(dir))
        {
            stage.Stage("icons", "new icons");
            stage.Stage("parts", "new parts");
            stage.Commit();
        }

        Assert.Equal("new icons", File.ReadAllText(Of(dir, "icons")));
        Assert.Equal("new parts", File.ReadAllText(Of(dir, "parts")));
    }

    [Fact]
    public void Staging_does_not_touch_the_committed_bundle()
    {
        var dir = TempDir();
        File.WriteAllText(Of(dir, "icons"), "stale");

        using var stage = new BundleStage(dir);
        stage.Stage("icons", "new icons");

        Assert.Equal("stale", File.ReadAllText(Of(dir, "icons")));
    }

    /// <summary>The failure path: icons and parts rendered, effects threw. Every original must
    /// survive, not just the one that failed.</summary>
    [Fact]
    public void Abandoning_the_stage_leaves_the_originals_and_no_debris()
    {
        var dir = TempDir();
        File.WriteAllText(Of(dir, "icons"), "stale icons");
        File.WriteAllText(Of(dir, "parts"), "stale parts");
        File.WriteAllText(Of(dir, "effects"), "stale effects");

        try
        {
            using var stage = new BundleStage(dir);
            stage.Stage("icons", "new icons");
            stage.Stage("parts", "new parts");
            throw new InvalidDataException("effects blew up");
        }
        catch (InvalidDataException)
        {
            // The build failing is the scenario; what it leaves behind is what is asserted.
        }

        Assert.Equal("stale icons", File.ReadAllText(Of(dir, "icons")));
        Assert.Equal("stale parts", File.ReadAllText(Of(dir, "parts")));
        Assert.Equal("stale effects", File.ReadAllText(Of(dir, "effects")));
        Assert.Equal(3, Directory.GetFiles(dir).Length);
    }

    /// <summary>Stage returns the byte count the console line reports, which used to come from
    /// FileInfo.Length after the final write.</summary>
    [Fact]
    public void Stage_returns_the_byte_length_of_the_written_file()
    {
        var dir = TempDir();

        using var stage = new BundleStage(dir);
        var bytes = stage.Stage("icons", "héllo");

        Assert.Equal(6, bytes);
    }
}
