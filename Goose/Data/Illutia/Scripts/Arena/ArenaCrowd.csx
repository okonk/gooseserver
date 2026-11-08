using System;
using Goose;
using Goose.Events;
using Goose.Scripting;

public class ArenaCrowd : BaseNPCScript
{
    private const string GenerationKey = "crowd.gen";
    private const string BurstKey = "crowd.burst";

    private static readonly (int Animation, int Sheet)[] Emotes =
    {
        (1080, 8), (1081, 8), (1082, 8), (1083, 8),
        (1084, 9), (1085, 9), (1086, 9), (1087, 9),
        (1088, 10), (1089, 10), (1090, 10), (1091, 10),
    };

    private static readonly (int Animation, int Sheet) Sweat = (1088, 10);

    private static readonly (double Min, double Max) EmoteSeconds = (20, 60);
    private static readonly (double Min, double Max) AttackSeconds = (20, 60);
    private static readonly (double Min, double Max) FastAttackSeconds = (3, 8);
    private const int SweatSetHits = 4;
    private const int SweatSets = 2;
    private static readonly TimeSpan SweatBurstGap = TimeSpan.FromSeconds(0.6);
    private static readonly (double Min, double Max) SweatBurstPause = (10, 20);

    public override void OnSpawnEvent(NPC npc, GameWorld world)
    {
        long generation = npc.Properties.GetProperty<long>(GenerationKey, 0) + 1;
        npc.Properties[GenerationKey] = generation;

        npc.Properties[BurstKey] = 0;
        if (!IsSweaty(npc))
            Schedule(npc, generation, nameof(Emote), FirstDelay(EmoteSeconds, world.Random), world);
        Schedule(npc, generation, nameof(Attack), FirstDelay(AttackRange(npc), world.Random), world);
    }

    public TimeSpan Emote(NPC npc, GameWorld world)
    {
        var (animation, sheet) = IsSweaty(npc) ? Sweat : Emotes[world.Random.Next(Emotes.Length)];
        SendToRange(npc, $"EMOT{npc.LoginID},{animation},{sheet}", world);
        return Delay(EmoteSeconds, world.Random);
    }

    public TimeSpan Attack(NPC npc, GameWorld world)
    {
        SendToRange(npc, P.Attack(npc), world);
        if (!IsSweaty(npc)) return Delay(AttackRange(npc), world.Random);

        int remaining = npc.Properties.GetProperty<int>(BurstKey, 0);
        if (remaining == 0) remaining = SweatSetHits * SweatSets;
        if (remaining % SweatSetHits == 0)
            SendToRange(npc, $"EMOT{npc.LoginID},{Sweat.Animation},{Sweat.Sheet}", world);
        npc.Properties[BurstKey] = --remaining;
        return remaining > 0 ? SweatBurstGap : Delay(SweatBurstPause, world.Random);
    }

    public static bool IsFast(NPC npc) => HasToken(npc, "fast");

    public static bool IsSweaty(NPC npc) => HasToken(npc, "sweat");

    private static bool HasToken(NPC npc, string wanted)
    {
        string parameters = npc.ScriptParams ?? "";
        foreach (var token in parameters.Split(new[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries))
            if (token.Equals(wanted, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private static (double Min, double Max) AttackRange(NPC npc)
        => IsSweaty(npc) ? SweatBurstPause : IsFast(npc) ? FastAttackSeconds : AttackSeconds;

    private static void SendToRange(NPC npc, string packet, GameWorld world)
    {
        if (npc.Hidden || npc.Map is null) return;
        foreach (var player in npc.Map.GetPlayersInRange(npc))
            world.Send(player, packet);
    }

    // Spread the first action over the whole window so a freshly loaded crowd doesn't act in unison.
    private static TimeSpan FirstDelay((double Min, double Max) range, Random random)
        => TimeSpan.FromSeconds(random.NextDouble() * range.Max);

    private static TimeSpan Delay((double Min, double Max) range, Random random)
        => TimeSpan.FromSeconds(range.Min + random.NextDouble() * (range.Max - range.Min));

    // The generation check retires timers from a previous life once the NPC respawns. The method is
    // re-resolved on every fire so a /reloadscripts swap of Script.Object takes effect on live timers.
    private static void Schedule(NPC npc, long generation, string action, TimeSpan delay, GameWorld world)
    {
        ScriptTimerEvent.Create(() =>
        {
            if (npc.State != NPC.States.Alive || npc.Properties.GetProperty<long>(GenerationKey, 0) != generation) return;
            TimeSpan next = delay;
            try
            {
                var current = npc.Script?.Object;
                if (current?.GetType().GetMethod(action)?.Invoke(current, new object[] { npc, world }) is TimeSpan chosen)
                    next = chosen;
            }
            finally
            {
                Schedule(npc, generation, action, next, world);
            }
        }, delay, world);
    }
}

return typeof(ArenaCrowd);
