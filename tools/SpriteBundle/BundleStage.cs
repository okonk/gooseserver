namespace Goose.Tools.SpriteBundle;

/// <summary>Keeps a regeneration of the three bundles from leaving a half-updated set behind. Each
/// rendered fragment is written to a temp sibling, and nothing is moved into place until every one
/// of them has been rendered and staged successfully — so the whole build-and-render phase, which
/// is where failures actually happen and where they take seconds, cannot disturb the committed
/// files. The move phase itself is a loop of renames rather than one atomic act; see Commit.
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

        // Registered before the write, not after: a write that throws partway — disk full, with
        // three fragments now on disk at once — still leaves the partial file behind, and an
        // unregistered one is debris Dispose would never clean up. File.Delete on a path that was
        // never created is a no-op, so registering first is strictly safer.
        _staged.Add((temp, final));
        File.WriteAllText(temp, html);

        return new FileInfo(temp).Length;
    }

    /// <summary>Moves every staged fragment into place. Not atomic across the three: a failure on
    /// the second move leaves the first committed. Deliberately not designed around — these are
    /// same-directory renames, so the window is microseconds against a build phase measured in
    /// seconds, and the alternative (swapping a whole directory) buys that sliver at the cost of
    /// the output path no longer being the one the caller asked for.</summary>
    public void Commit()
    {
        // Dropped as each one lands, so a Dispose after a partial Commit deletes exactly the
        // temps that are still temps.
        while (_staged.Count > 0)
        {
            var (temp, final) = _staged[0];
            File.Move(temp, final, overwrite: true);
            _staged.RemoveAt(0);
        }
    }

    /// <summary>Removes anything not committed. Best-effort per file, catching everything rather
    /// than the usual IOException/UnauthorizedAccessException/ArgumentException set: this runs
    /// during exception unwinding, so anything escaping it would replace the real build failure
    /// with an unrelated cleanup one — the precise outcome it exists to avoid.</summary>
    public void Dispose()
    {
        foreach (var (temp, _) in _staged)
        {
            try
            {
                File.Delete(temp);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"could not remove {temp}: {ex.Message}");
            }
        }

        _staged.Clear();
    }
}
