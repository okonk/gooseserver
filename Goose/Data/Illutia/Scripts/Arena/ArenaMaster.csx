#load "ArenaConfig.csx"
#load "ArenaMath.csx"
#load "ArenaState.csx"
#load "ArenaBoard.csx"
using System;
using System.Collections.Generic;
using Goose;
using Goose.Scripting;

public class ArenaMaster : BaseNPCScript
{
    public override bool OnPlayerClicked(NPC npc, Player player, GameWorld world)
    {
        Hub(npc, player, world);
        return true;
    }

    private static void Hub(NPC npc, Player player, GameWorld world)
    {
        var cfg = ArenaConfig.Get(world);
        var lines = new List<string>();
        lines.Add("Arena Coins: " + (world.CurrencyHandler.Get(cfg.CurrencyId)?.GetBalance(player) ?? 0).ToString("N0"));
        var run = ArenaState.Load(world);
        if (run is not null)
        {
            int alive = 0;
            foreach (var p in run.Participants.Values) if (p.Alive) alive++;
            lines.Add($"Run in progress: round {run.Round}, {alive} alive");
        }
        else if (!cfg.Enabled(world))
        {
            lines.Add("The arena is closed.");
        }
        else
        {
            var now = DateTime.UtcNow;
            var ts = ArenaMath.NextStartUtc(now, cfg) - now;
            lines.Add(ts.TotalSeconds < 60
                ? "Next run in under a minute"
                : $"Next run in {(int)ts.TotalHours}h {ts.Minutes}m");
        }
        var last = ArenaState.LoadLastRun(world);
        if (last is not null)
            lines.Add($"Last run: round {last.BestRound} ({last.Results.Count} players)");
        lines.Add("Leaderboards");
        new OptionListWindow(player, world, "Arena", lines, (i, p, w) =>
        {
            if (i == lines.Count - 1) BoardMenu(npc, p, w);
            else Hub(npc, p, w);
        }, npc);
    }

    private static void BoardMenu(NPC npc, Player player, GameWorld world)
    {
        var lines = new List<string>
        {
            "Best round - this week",
            "Best round - all time",
            "Total kills - this week",
            "Total kills - all time",
        };
        new OptionListWindow(player, world, "Leaderboards", lines, (i, p, w) =>
        {
            var kind = i < 2 ? ArenaBoardKind.BestRound : ArenaBoardKind.Kills;
            BoardList(npc, p, w, kind, i % 2 == 0);
        }, npc, onBack: (p, w) => Hub(npc, p, w));
    }

    private static void BoardList(NPC npc, Player player, GameWorld world, ArenaBoardKind kind, bool weekly)
    {
        var board = ArenaBoard.Load(world);
        var week = ArenaBoard.WeekKey(DateTime.UtcNow);
        var top = ArenaBoard.Top(board, kind, weekly, week, 10);
        var lines = new List<string>();
        if (top.Count == 0)
        {
            lines.Add("No entries yet.");
        }
        else
        {
            for (var i = 0; i < top.Count; i++)
                lines.Add($"{i + 1}. {world.PlayerHandler.GetPlayerName(top[i].Id) ?? "Unknown"} - {top[i].Value:N0}");
            lines.Add("");
            var rank = ArenaBoard.RankOf(board, player.PlayerID, kind, weekly, week);
            lines.Add(rank is not null ? $"You: #{rank.Value.Rank} - {rank.Value.Value:N0}" : "You: unranked");
        }
        new OptionListWindow(player, world, "Leaderboards", lines, (_, _, _) => { }, npc, onBack: (p, w) => BoardMenu(npc, p, w));
    }
}

return typeof(ArenaMaster);
