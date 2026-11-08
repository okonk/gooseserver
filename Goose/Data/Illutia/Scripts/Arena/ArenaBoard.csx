using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Goose;

public enum ArenaBoardKind { BestRound, Kills }

public class ArenaBoardEntry
{
    public int Id { get; set; }
    public int BestRound { get; set; }
    public DateTime BestAtUtc { get; set; }
    public double Kills { get; set; }
    public string Week { get; set; } = "";
    public int WeekBestRound { get; set; }
    public DateTime WeekBestAtUtc { get; set; }
    public double WeekKills { get; set; }
}

public class ArenaBoard
{
    public const string BoardKey = "arena.board";

    public Dictionary<int, ArenaBoardEntry> Players { get; set; } = new();

    private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
    private static string lastBadJson = "";

    public static string WeekKey(DateTime utc)
        => ISOWeek.GetYear(utc) + "-W" + ISOWeek.GetWeekOfYear(utc).ToString("D2", CultureInfo.InvariantCulture);

    public static ArenaBoard Load(GameWorld world)
    {
        var json = world.WorldState.Get<string>(BoardKey);
        if (json is null) return new ArenaBoard();
        try
        {
            return JsonSerializer.Deserialize<ArenaBoard>(json) ?? new ArenaBoard();
        }
        catch (JsonException e)
        {
            if (json != lastBadJson)
            {
                lastBadJson = json;
                log.Warn(e, "Failed to parse arena board {0}", BoardKey);
            }
            return new ArenaBoard();
        }
    }

    public static void Save(GameWorld world, ArenaBoard board)
        => world.WorldState.Set(BoardKey, JsonSerializer.Serialize(board));

    public static void Record(GameWorld world, ArenaRun run, DateTime nowUtc)
    {
        var board = Load(world);
        var week = WeekKey(nowUtc);
        foreach (var p in run.Participants.Values)
        {
            if (!board.Players.TryGetValue(p.Id, out var e))
            {
                e = new ArenaBoardEntry { Id = p.Id, Week = week };
                board.Players[p.Id] = e;
            }
            if (e.Week != week)
            {
                e.Week = week;
                e.WeekBestRound = 0;
                e.WeekBestAtUtc = default;
                e.WeekKills = 0;
            }
            e.Kills += p.Kills;
            e.WeekKills += p.Kills;
            if (p.RoundsCleared > e.BestRound)
            {
                e.BestRound = p.RoundsCleared;
                e.BestAtUtc = nowUtc;
            }
            if (p.RoundsCleared > e.WeekBestRound)
            {
                e.WeekBestRound = p.RoundsCleared;
                e.WeekBestAtUtc = nowUtc;
            }
        }
        Save(world, board);
    }

    public static List<(int Id, double Value)> Top(ArenaBoard b, ArenaBoardKind kind, bool weekly, string week, int n)
        => b.Players.Values
            .Select(e => View(e, kind, weekly, week))
            .Where(v => v.Value != 0)
            .OrderByDescending(v => v.Value)
            .ThenBy(v => v.At)
            .ThenBy(v => v.Id)
            .Take(n)
            .Select(v => (v.Id, v.Value))
            .ToList();

    public static (int Rank, double Value)? RankOf(ArenaBoard b, int id, ArenaBoardKind kind, bool weekly, string week)
    {
        if (!b.Players.TryGetValue(id, out var e)) return null;
        var v = View(e, kind, weekly, week);
        if (v.Value == 0) return null;
        int rank = 1 + b.Players.Values.Count(o => Ahead(View(o, kind, weekly, week), v));
        return (rank, v.Value);
    }

    private static (double Value, DateTime At, int Id) View(ArenaBoardEntry e, ArenaBoardKind kind, bool weekly, string week)
    {
        if (weekly && e.Week != week) return (0, default, e.Id);
        if (kind == ArenaBoardKind.BestRound)
            return weekly ? (e.WeekBestRound, e.WeekBestAtUtc, e.Id) : (e.BestRound, e.BestAtUtc, e.Id);
        return (weekly ? e.WeekKills : e.Kills, default, e.Id);
    }

    private static bool Ahead((double Value, DateTime At, int Id) a, (double Value, DateTime At, int Id) b)
    {
        if (a.Value != b.Value) return a.Value > b.Value;
        if (a.At != b.At) return a.At < b.At;
        return a.Id < b.Id;
    }
}
