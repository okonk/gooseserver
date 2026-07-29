namespace Goose.Tools.SpriteBundle;

/// <summary>Makes a regeneration of the three bundles all-or-nothing. Each rendered fragment is
/// written to a temp sibling; only once every one of them exists are they moved into place.
///
/// A mid-run failure is ordinary, not exotic: Bundles.Effects throws on a multi-clip animation and
/// TresParser on a malformed one, both reachable from a routine client art change. Writing each
/// fragment as it was built then left the committed set half new and half stale — a state no error
/// message describes, and one git cannot tell from an intentional partial regeneration.
///
/// Temp siblings rather than a temp directory: File.Move within a directory is a rename on the same
/// filesystem, so no fragment is ever observed truncated.</summary>
public sealed class BundleStage : IDisposable
{
    private readonly string _outDir;
    private readonly List<(string Temp, string Final)> _staged = [];

    public BundleStage(string outDir) => _outDir = outDir;

    /// <summary>Writes one rendered fragment to its temp sibling and returns its size on disk —
    /// the number the console line reports, taken here rather than after the move so the caller
    /// need not keep the html string alive.</summary>
    public long Stage(string name, string html)
    {
        var final = Path.Combine(_outDir, $"sprites-{name}.html");
        var temp = final + ".tmp";

        File.WriteAllText(temp, html);
        _staged.Add((temp, final));

        return new FileInfo(temp).Length;
    }

    public void Commit()
    {
        foreach (var (temp, final) in _staged)
            File.Move(temp, final, overwrite: true);

        _staged.Clear();
    }

    /// <summary>Removes anything not committed. Best-effort per file: a temp left behind is
    /// untidy, but throwing here would replace the real failure with an unrelated one.</summary>
    public void Dispose()
    {
        foreach (var (temp, _) in _staged)
        {
            try
            {
                File.Delete(temp);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Console.Error.WriteLine($"could not remove {temp}: {ex.Message}");
            }
        }

        _staged.Clear();
    }
}
