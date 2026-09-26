using System.Collections.Immutable;
using Goose.Logs;
using Xunit;

namespace Goose.Tests;

public class LogViewerSearchSessionTests
{
    private static LogSearchQuery MakeBaseQuery(int? participantId = 42)
        => new(1000, 2000, participantId, null, ImmutableArray<int>.Empty, "", null);

    private static LogQueryRow MakeRow(long rowId)
        => new(rowId, 123, 0, true, 42, true, 0, false, 7, true, 1, true, 2, true,
            "Chat", "Communication", LogOtherIdKind.Unused, null, null, null, "s", "t");

    [Fact]
    public void Session_RetainsCursorlessBaseQueryAndResolvedParticipantId()
    {
        var baseQuery = MakeBaseQuery(42);
        var session = new LogViewerSearchSession(baseQuery, 1);

        Assert.Same(baseQuery, session.Base);
        Assert.Null(session.Base.Cursor);
        Assert.Equal(42, session.Base.ParticipantId);
        Assert.Equal(1, session.Generation);
    }

    [Fact]
    public void FirstToken_MapsToNull_AndContinuationToken_MapsToExactCursor()
    {
        var baseQuery = MakeBaseQuery();
        var session = new LogViewerSearchSession(baseQuery, 1);

        Assert.True(session.TryResolve(session.FirstPageToken, out var first, out var firstRows));
        Assert.Same(baseQuery, first);
        Assert.Null(firstRows);

        var cursor = new LogPageCursor(100, 1234, 56);
        string token = session.IssueToken(cursor);
        Assert.True(session.TryResolve(token, out var continuation, out var continuationRows));
        Assert.Same(cursor, continuation!.Cursor);
        Assert.Null(continuationRows);
        Assert.Equal(42, continuation.ParticipantId);
    }

    [Fact]
    public void SameCursor_ReusesOneToken_AndGeneratedCollision_RetriesWithoutOverwriting()
    {
        var baseQuery = MakeBaseQuery();
        string first = new('a', 22);
        string second = new('b', 22);
        var queue = new Queue<string>([first, first, second]);
        var session = new LogViewerSearchSession(baseQuery, 1, () => queue.Dequeue());

        Assert.Equal(first, session.FirstPageToken);

        var cursor = new LogPageCursor(7, null, null);
        string token = session.IssueToken(cursor);
        Assert.Equal(second, token);
        Assert.Equal(token, session.IssueToken(cursor));

        Assert.True(session.TryResolve(first, out var pageOne, out _));
        Assert.Same(baseQuery, pageOne);
        Assert.True(session.TryResolve(second, out var continuation, out _));
        Assert.Equal(cursor, continuation!.Cursor);
    }

    [Fact]
    public void TokenLookup_IsOrdinalAndScopedToSessionInstance()
    {
        var baseQuery = MakeBaseQuery();
        string first = new('a', 22);
        string second = new('b', 22);
        int issued = 0;
        var session = new LogViewerSearchSession(baseQuery, 1, () => ++issued == 1 ? first : second);
        var other = new LogViewerSearchSession(baseQuery, 2, () => new('c', 22));

        string token = session.IssueToken(new LogPageCursor(1, null, null));
        Assert.Equal(second, token);
        Assert.True(session.TryResolve(token, out _, out _));
        Assert.False(session.TryResolve(token.ToUpperInvariant(), out _, out _));
        Assert.False(other.TryResolve(token, out _, out _));
        Assert.False(session.TryResolve(other.FirstPageToken, out _, out _));
    }

    [Fact]
    public void ClearAndReplacement_MakeEveryOldTokenUnreachable()
    {
        var baseQuery = MakeBaseQuery();
        var session = new LogViewerSearchSession(baseQuery, 1);
        string token = session.IssueToken(new LogPageCursor(1, null, null));

        session.Clear();

        Assert.False(session.TryResolve(session.FirstPageToken, out _, out _));
        Assert.False(session.TryResolve(token, out _, out _));

        var replacement = new LogViewerSearchSession(baseQuery, 2);
        Assert.NotEqual(session.Generation, replacement.Generation);
        Assert.False(replacement.TryResolve(session.FirstPageToken, out _, out _));
        Assert.False(replacement.TryResolve(token, out _, out _));
        Assert.True(replacement.TryResolve(replacement.FirstPageToken, out _, out _));
    }

    [Fact]
    public void FirstPageToken_UsesStoredReplayCursorWhenContinuationExists_OtherwiseStoredRows()
    {
        var baseQuery = MakeBaseQuery();
        var session = new LogViewerSearchSession(baseQuery, 1);
        var next = new LogPageCursor(55, 999, 8);
        session.StoreFirstPage([], next);

        Assert.True(session.TryResolve(session.FirstPageToken, out var replay, out var replayRows));
        Assert.Null(replayRows);
        Assert.Equal(LogPageCursor.ForFirstPage(55), replay!.Cursor);
        Assert.Null(replay.Cursor.BeforeUtcTicks);
        Assert.Null(replay.Cursor.BeforeRowId);

        var single = new LogViewerSearchSession(baseQuery, 2);
        var stored = new[] { MakeRow(1), MakeRow(2) };
        single.StoreFirstPage(stored, null);

        Assert.True(single.TryResolve(single.FirstPageToken, out var singleQuery, out var singleRows));
        Assert.Null(singleQuery);
        Assert.Equal(stored, singleRows);
    }

    [Fact]
    public void ParticipantId_CannotChangeAfterConstruction()
    {
        var baseQuery = MakeBaseQuery(42);
        var session = new LogViewerSearchSession(baseQuery, 1);
        string token = session.IssueToken(new LogPageCursor(1, null, null));

        Assert.True(session.TryResolve(token, out var continuation, out _));
        Assert.True(session.TryResolve(session.FirstPageToken, out var first, out _));
        session.Clear();

        Assert.Same(baseQuery, session.Base);
        Assert.Equal(42, session.Base.ParticipantId);
        Assert.Equal(42, continuation!.ParticipantId);
        Assert.Equal(42, first!.ParticipantId);
    }
}
