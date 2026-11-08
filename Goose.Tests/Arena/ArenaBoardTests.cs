namespace Goose.Tests.Arena;

public sealed class ArenaBoardTests : IDisposable
{
    private readonly ArenaTestKit kit = new();
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    public void Dispose() => kit.Dispose();

    private static void StartRun(ArenaTestKit kit, DateTime now) => ArenaTestKit.Call(kit.Host, "StartRun", kit.World, now);
    private static object Run(ArenaTestKit kit) => ArenaTestKit.Call(kit.Host, "Run", kit.World)!;
    private static void EndRun(ArenaTestKit kit, string reason) => ArenaTestKit.Call(kit.Host, "EndRun", kit.World, reason);
    private static object Board(ArenaTestKit kit) => ArenaTestKit.Call(kit.Host, "BoardLoad", kit.World)!;
    private static void BoardSave(ArenaTestKit kit, object board) => ArenaTestKit.Call(kit.Host, "BoardSave", kit.World, board);
    private static void BoardRecord(ArenaTestKit kit, object run, DateTime now) => ArenaTestKit.Call(kit.Host, "BoardRecord", kit.World, run, now);
    private static List<(int Id, double Value)> BoardTop(ArenaTestKit kit, object board, int kind, bool weekly, string week, int n)
        => (List<(int Id, double Value)>)ArenaTestKit.Call(kit.Host, "BoardTop", board, kind, weekly, week, n)!;
    private static (int Rank, double Value)? BoardRankOf(ArenaTestKit kit, object board, int id, int kind, bool weekly, string week)
        => (ValueTuple<int, double>?)ArenaTestKit.Call(kit.Host, "BoardRankOf", board, id, kind, weekly, week);
    private static string WeekKey(ArenaTestKit kit, DateTime utc) => (string)ArenaTestKit.Call(kit.Host, "WeekKey", utc)!;

    private static object? Prop(object? obj, string name) => ArenaTestKit.GetProp(obj, name);
    private static void SetProp(object obj, string name, object? value) => obj.GetType().GetProperty(name)!.SetValue(obj, value);
    private static System.Collections.IDictionary Players(object board) => (System.Collections.IDictionary)Prop(board, "Players")!;
    private static System.Collections.IDictionary Participants(object run) => (System.Collections.IDictionary)Prop(run, "Participants")!;

    [Fact]
    public void Record_keeps_max_best_round_and_sums_kills()
    {
        kit.LobbyPlayer(70001, "Alice");
        kit.LobbyPlayer(70002, "Bob");
        StartRun(kit, Now);
        var run = Run(kit);
        var p1 = Participants(run)[70001];
        var p2 = Participants(run)[70002];
        SetProp(p1, "Kills", 3.0);
        SetProp(p1, "RoundsCleared", 4);
        SetProp(p2, "Kills", 2.0);
        SetProp(p2, "RoundsCleared", 2);
        BoardRecord(kit, run, Now);

        SetProp(p1, "Kills", 1.0);
        SetProp(p1, "RoundsCleared", 2);
        SetProp(p2, "Kills", 0.0);
        BoardRecord(kit, run, Now.AddHours(1));

        var board = Board(kit);
        var e1 = Players(board)[70001];
        var e2 = Players(board)[70002];
        Assert.Equal(WeekKey(kit, Now), (string)Prop(e1, "Week")!);
        Assert.Equal(4, (int)Prop(e1, "BestRound")!);
        Assert.Equal(Now, (DateTime)Prop(e1, "BestAtUtc")!);
        Assert.Equal(4.0, (double)Prop(e1, "Kills")!);
        Assert.Equal(4, (int)Prop(e1, "WeekBestRound")!);
        Assert.Equal(Now, (DateTime)Prop(e1, "WeekBestAtUtc")!);
        Assert.Equal(4.0, (double)Prop(e1, "WeekKills")!);
        Assert.Equal(2, (int)Prop(e2, "BestRound")!);
        Assert.Equal(2.0, (double)Prop(e2, "Kills")!);
    }

    [Fact]
    public void New_week_resets_weekly_but_not_all_time()
    {
        kit.LobbyPlayer(70001, "Alice");
        StartRun(kit, Now);
        var run = Run(kit);
        var p1 = Participants(run)[70001];
        SetProp(p1, "Kills", 7.0);
        SetProp(p1, "RoundsCleared", 5);
        BoardRecord(kit, run, Now);

        var board = Board(kit);
        SetProp(Players(board)[70001], "Week", "2025-W52");
        BoardSave(kit, board);

        SetProp(p1, "Kills", 1.0);
        SetProp(p1, "RoundsCleared", 3);
        BoardRecord(kit, run, Now);

        var e1 = Players(Board(kit))[70001];
        Assert.Equal(WeekKey(kit, Now), (string)Prop(e1, "Week")!);
        Assert.Equal(5, (int)Prop(e1, "BestRound")!);
        Assert.Equal(8.0, (double)Prop(e1, "Kills")!);
        Assert.Equal(3, (int)Prop(e1, "WeekBestRound")!);
        Assert.Equal(Now, (DateTime)Prop(e1, "WeekBestAtUtc")!);
        Assert.Equal(1.0, (double)Prop(e1, "WeekKills")!);
    }

    [Fact]
    public void Weekly_view_ignores_stale_entries_without_writes()
    {
        kit.LobbyPlayer(70001, "Alice");
        StartRun(kit, Now);
        var run = Run(kit);
        var p1 = Participants(run)[70001];
        SetProp(p1, "Kills", 3.0);
        SetProp(p1, "RoundsCleared", 5);
        BoardRecord(kit, run, Now);

        var before = kit.World.WorldState.Get<string>("arena.board")!;
        var board = Board(kit);

        Assert.Equal([], BoardTop(kit, board, 0, true, "2026-W02", 10));
        Assert.Equal([], BoardTop(kit, board, 1, true, "2026-W02", 10));
        Assert.Null(BoardRankOf(kit, board, 70001, 0, true, "2026-W02"));
        Assert.Null(BoardRankOf(kit, board, 70001, 1, true, "2026-W02"));
        Assert.Equal(before, kit.World.WorldState.Get<string>("arena.board"));
    }

    [Fact]
    public void Best_round_tie_goes_to_earlier_achiever()
    {
        kit.LobbyPlayer(70001, "Alice");
        kit.LobbyPlayer(70002, "Bob");
        StartRun(kit, Now);
        var run = Run(kit);
        var p1 = Participants(run)[70001];
        var p2 = Participants(run)[70002];
        SetProp(p1, "RoundsCleared", 3);
        BoardRecord(kit, run, Now);
        SetProp(p2, "RoundsCleared", 3);
        BoardRecord(kit, run, Now.AddHours(1));

        var board = Board(kit);
        var week = WeekKey(kit, Now);
        Assert.Equal([(70001, 3.0), (70002, 3.0)], BoardTop(kit, board, 0, false, week, 10));
        Assert.Equal([(70001, 3.0), (70002, 3.0)], BoardTop(kit, board, 0, true, week, 10));
        Assert.Equal(((int)1, 3.0), BoardRankOf(kit, board, 70001, 0, false, week));
        Assert.Equal(((int)2, 3.0), BoardRankOf(kit, board, 70002, 0, false, week));
    }

    [Fact]
    public void RankOf_outside_top_ten()
    {
        for (int i = 0; i < 15; i++)
            kit.LobbyPlayer(70001 + i, "P" + i);
        StartRun(kit, Now);
        var run = Run(kit);
        for (int i = 0; i < 15; i++)
            SetProp(Participants(run)[70001 + i], "Kills", (double)(14 - i));
        BoardRecord(kit, run, Now);

        var board = Board(kit);
        var week = WeekKey(kit, Now);
        var top = BoardTop(kit, board, 1, false, week, 10);
        Assert.Equal(10, top.Count);
        Assert.Equal(70001, top[0].Id);
        Assert.Equal(14.0, top[0].Value);
        Assert.Equal(5.0, top[9].Value);
        Assert.Equal(((int)1, 14.0), BoardRankOf(kit, board, 70001, 1, false, week));
        Assert.Equal(((int)10, 5.0), BoardRankOf(kit, board, 70010, 1, false, week));
        Assert.Equal(((int)11, 4.0), BoardRankOf(kit, board, 70011, 1, false, week));
        Assert.Equal(((int)14, 1.0), BoardRankOf(kit, board, 70014, 1, false, week));
        Assert.Null(BoardRankOf(kit, board, 70015, 1, false, week));
        Assert.Null(BoardRankOf(kit, board, 99999, 1, false, week));
    }

    [Fact]
    public void WeekKey_iso_boundary()
    {
        Assert.Equal("2026-W53", WeekKey(kit, new DateTime(2026, 12, 31, 12, 0, 0, DateTimeKind.Utc)));
        Assert.Equal("2026-W53", WeekKey(kit, new DateTime(2027, 1, 1, 12, 0, 0, DateTimeKind.Utc)));
        Assert.Equal("2026-W01", WeekKey(kit, Now));
    }

    [Fact]
    public void EndRun_records_each_participant_once()
    {
        kit.LobbyPlayer(70001, "Alice");
        kit.LobbyPlayer(70002, "Bob");
        StartRun(kit, Now);
        var run = Run(kit);
        var p1 = Participants(run)[70001];
        var p2 = Participants(run)[70002];
        SetProp(p1, "Kills", 4.0);
        SetProp(p1, "RoundsCleared", 3);
        SetProp(p2, "Kills", 1.0);
        SetProp(p2, "RoundsCleared", 3);
        ArenaTestKit.Call(kit.Host, "SaveRun", kit.World, run);

        EndRun(kit, "test");

        var board = Board(kit);
        var players = Players(board);
        Assert.Equal(2, players.Count);
        var e1 = players[70001];
        var e2 = players[70002];
        Assert.Equal(3, (int)Prop(e1, "BestRound")!);
        Assert.Equal(4.0, (double)Prop(e1, "Kills")!);
        Assert.Equal(3, (int)Prop(e2, "BestRound")!);
        Assert.Equal(1.0, (double)Prop(e2, "Kills")!);
    }
}
