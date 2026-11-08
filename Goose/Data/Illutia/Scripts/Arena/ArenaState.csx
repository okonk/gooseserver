using System;
using System.Collections.Generic;
using System.Text.Json;
using Goose;

public enum ArenaPhase { Countdown, Fighting, Break, Ended }

public class ArenaParticipant
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool Alive { get; set; }
    public int RoundsCleared { get; set; }
    public long Coins { get; set; }
    public double Kills { get; set; }
}

public class ArenaRun
{
    public string RunId { get; set; } = "";
    public DateTime StartedUtc { get; set; }
    public ArenaPhase Phase { get; set; }
    public DateTime PhaseUntilUtc { get; set; }
    public int Round { get; set; }
    public bool Warned { get; set; }
    public int LastCountdown { get; set; }
    public Dictionary<int, ArenaParticipant> Participants { get; set; } = new();
}

public class ArenaResult
{
    public string Name { get; set; } = "";
    public int RoundsCleared { get; set; }
    public long Coins { get; set; }
    public double Kills { get; set; }
}

public class ArenaLastRun
{
    public string RunId { get; set; } = "";
    public DateTime EndedUtc { get; set; }
    public int BestRound { get; set; }
    public List<ArenaResult> Results { get; set; } = new();
}

public static class ArenaState
{
    public const string RunKey = "arena.run";
    public const string LastRunKey = "arena.lastRun";

    private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

    public static ArenaRun? Load(GameWorld world) => Load<ArenaRun>(world, RunKey);

    public static void Save(GameWorld world, ArenaRun run) => world.WorldState.Set(RunKey, JsonSerializer.Serialize(run));

    public static void Clear(GameWorld world) => world.WorldState.Remove(RunKey);

    public static ArenaLastRun? LoadLastRun(GameWorld world) => Load<ArenaLastRun>(world, LastRunKey);

    public static void SaveLastRun(GameWorld world, ArenaLastRun last) => world.WorldState.Set(LastRunKey, JsonSerializer.Serialize(last));

    private static T? Load<T>(GameWorld world, string key) where T : class
    {
        var json = world.WorldState.Get<string>(key);
        if (json is null) return null;
        try
        {
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (JsonException e)
        {
            log.Warn(e, "Failed to parse arena state {0}", key);
            return null;
        }
    }
}
