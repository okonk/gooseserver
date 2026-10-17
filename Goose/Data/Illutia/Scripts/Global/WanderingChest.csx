using Goose;
using Goose.Commands;
using Goose.Events;
using Goose.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;

public class WanderingChest : BaseGlobalScript
{
    public const bool Enabled = true;

    private const int ChestTemplateId = 261;

    private static readonly TimeSpan MinSpawnDelay = TimeSpan.FromMinutes(90);
    private static readonly TimeSpan MaxSpawnDelay = TimeSpan.FromMinutes(150);
    private static readonly TimeSpan WanderAfter = TimeSpan.FromMinutes(20);
    // WanderingChestNPC.csx stamps npc.ScriptStore with the time it was last hit.
    private static readonly TimeSpan FightGrace = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan Tick = TimeSpan.FromSeconds(30);

    // Crystal Cavern's map script buffs every NPC that spawns there.
    private static readonly HashSet<int> ExcludedMaps = [54];

    private static readonly Dictionary<int, string[]> Riddles = new()
    {
        [1] = ["Where little feet scurry through the town of beginnings, gold hides among the squeaks."],
        [2] = ["Where lambs bleat soft and woolly in the town of beginnings, a chest lies waiting."],
        [115] = ["Deep in the dark where the unloved weep, treasure sleeps."],
        [7] = ["Holy hideout! Among winged things that shun the sun, a chest awaits."],
        [117] = ["Deeper in the cave of bats, wraiths drift around something golden."],
        [10] = ["Where the dead shuffle among the headstones, something golden refuses to rot."],
        [11] = ["Down a hallway of rattling bones, a chest keeps company with the dead."],
        [14] = ["Those who serve the dead now guard a different master: gold."],
        [13] = ["Bones clatter, bones chatter, and beside them a chest grins."],
        [41] = ["Where the Slime King's subjects wobble, a chest has made itself at home."],
        [42] = ["Pink and wobbling, they bounce about. Beside them a chest has squelched in."],
        [44] = ["Blue as the sky and twice as sticky, they keep a chest company."],
        [43] = ["Among slimes that glitter like coin, one prize is the real thing."],
        [45] = ["Red and jiggling with rage, they guard a chest they cannot open."],
        [40] = ["Green and gooey, they ooze around a chest that does not belong."],
        [6] = ["Where travellers end up flattened and serpents hiss in the trees, a chest coils in wait."],
        [57] = ["Somebody's playhouse is full of ghasts tonight, and one guest brought gold."],
        [56] = ["Spooks giggle in the playhouse halls. Follow the giggles to the gold."],
        [26] = ["In a forest long dead, serpents of flame slither past a treasure."],
        [28] = ["Naga rogues creep between the dead trees. They have stolen something shiny."],
        [27] = ["Naga warriors stand guard in a lifeless wood, though they know not what they guard."],
        [35] = ["Beyond the lands you know, mouldy persecutors march past a hidden prize."],
        [34] = ["In the other lands, the weakest of the persecution stand over gold."],
        [36] = ["Privates drill in the other lands, unaware of the fortune at their feet."],
        [39] = ["Down where minds go missing, a chest has dug itself in."],
        [49] = ["Where the strong persecution toil, treasure glints in the dark."],
        [55] = ["Simple minds wander the tunnels. A simple chest waits among them."],
        [74] = ["In the stronghold of two, among those with things upon their heads, a chest stands guard."],
        [70] = ["Guardians worth nothing keep watch over something worth a lot."],
        [8] = ["Where cattle low beneath the little mountain, a chest chews its cud."],
        [9] = ["Horns clash on the mountainside. Something that is not a ram grazes among them."],
        [4] = ["Frothing bunnies hop across the frozen plain. One burrow hides gold."],
        [3] = ["So fluffy, so little, so cold. A chest sits among the bunnies in the snow."],
        [112] = ["Far to the frozen north, where snowballs roll by themselves, a chest lies half buried."],
        [113] = ["In the northern cold, madness strikes at midnight. So does fortune."],
        [114] = ["Even in the far north some things melt. The chest has not."],
        [30] = ["On a frozen lake, among the frozen and the wasted, a chest skates."],
        [32] = ["The icemen of the lake keep a cold secret: a chest of warm gold."],
        [20] = ["Wabbits lost in a frigid maze have found something. Can you?"],
        [21] = ["They look cute. They look defenceless. They are stalking a chest in the maze."],
        [24] = ["Out in the boondocks, among the pipsqueeks, a chest squeaks with gold."],
        [18] = ["Piglets root around in the boondocks. One of them has found a chest."],
        [50] = ["Where the desert drinks, naga beasts prowl around a hidden chest."],
        [51] = ["Magi of the oasis chant over a chest they cannot open."],
        [52] = ["The priests of the oasis prayed for gold, and gold has answered."],
        [62] = ["On an isle of hungry savages, something golden waits to be eaten."],
        [64] = ["Paranoid savages swear something is watching them from their isle. They are right."],
        [61] = ["Rabid savages foam at the mouth over a chest on their island."],
        [63] = ["The savages of the isle have built a shrine around a strange box."],
        [133] = ["In a grove of sorrow, among the flat and fluffy, a chest is stacked."],
        [134] = ["Deeper in sorrow, flapjacks flip around a hidden treasure."],
        [145] = ["Where all things are passing, giant wraiths drift past a chest that stays."],
        [146] = ["Those who passed and returned have brought something back with them."],
        [151] = ["High on the winter heights, Richard after Richard guards a chest."],
        [153] = ["Simon says: climb the winter heights once more and look for gold."],
        [168] = ["In a grove of gourds, the chiefs have carved out a treasure for themselves."],
        [166] = ["Among the pumpkins of the grove, one is not a pumpkin at all."],
        [130] = ["In a rugged valley where young bears play, a chest is the new toy."],
        [129] = ["Bears on patrol in their kingdom have walked straight past something golden."],
        [158] = ["In the pits, the starving bears would eat anything except this."],
        [208] = ["On a maroon shore, sunburnt persecutors bake beside a chest."],
        [210] = ["Crispy persecutors guard the bay, and something crisp and golden too."],
        [211] = ["Charred persecutors stand by the bay. The chest is the only thing not burnt."],
        [209] = ["Scorched persecutors have dragged a chest to the water's edge."],
        [230] = ["Beneath the town's streets, gutterwings flap over a sunken treasure."],
        [229] = ["Follow the vermin through the muck beneath the town."],
        [232] = ["Something purple and mouldy beneath the town guards a chest."],
    };

    private NPC? chest;
    private int lastMapId;
    private long nextSpawnAt;
    private long wanderAt;

    public override void OnLoaded(GameWorld world)
    {
        if (!Enabled) return;

        nextSpawnAt = world.TimeNow + RandomSpawnDelay(world);

        world.Commands.Register("/wanderingchest", AccessPrivilege.SpawnNPC, "GM",
            "Spawn the Wandering Chest now, or make it wander if it is already out.",
            new Action<CommandContext>(Force));

        ScriptTimerEvent? timer = null;
        timer = ScriptTimerEvent.Create(() =>
        {
            try
            {
                Update(world, world.TimeNow);
            }
            finally
            {
                timer!.Reschedule(Tick, world);
            }
        }, Tick, world);
    }

    public void Update(GameWorld world, long now)
    {
        if (chest is null)
        {
            if (now >= nextSpawnAt && !Appear(world, now, "[Event] A Wandering Chest has appeared! "))
                nextSpawnAt = now + RandomSpawnDelay(world);
            return;
        }

        if (chest.State == NPC.States.Dead)
        {
            chest = null;
            nextSpawnAt = now + RandomSpawnDelay(world);
            return;
        }

        if (now < wanderAt) return;
        if (chest.ScriptStore is long lastHit && now - lastHit < Ticks(world, FightGrace)) return;

        Despawn(chest, world);
        chest = null;
        if (!Appear(world, now, "[Event] Bored of waiting, the Wandering Chest has crept away... "))
            nextSpawnAt = now + RandomSpawnDelay(world);
    }

    public void Force(CommandContext ctx)
    {
        long now = ctx.World.TimeNow;
        bool appeared;
        if (chest is null || chest.State == NPC.States.Dead)
        {
            appeared = Appear(ctx.World, now, "[Event] A Wandering Chest has appeared! ");
        }
        else
        {
            Despawn(chest, ctx.World);
            chest = null;
            appeared = Appear(ctx.World, now, "[Event] Bored of waiting, the Wandering Chest has crept away... ");
        }

        if (!appeared)
        {
            nextSpawnAt = now + RandomSpawnDelay(ctx.World);
            ctx.Send("The Wandering Chest found nowhere to appear.");
            return;
        }
        ctx.Send($"The Wandering Chest is on {chest!.Map.Name} ({chest.Map.ID}) at {chest.MapX},{chest.MapY}.");
    }

    private bool Appear(GameWorld world, long now, string announcement)
    {
        var template = world.NPCHandler.GetNPCTemplate(ChestTemplateId);
        if (template is null) return false;

        var candidates = world.MapHandler.Maps.Values
            .Where(m => !m.CanPVP && !ExcludedMaps.Contains(m.ID))
            .SelectMany(m => m.NPCs.Where(n => n.NPCType != NPCTemplate.Types.Ambient && Riddles.ContainsKey(n.NPCTemplateID)))
            .ToList();
        if (candidates.Count == 0) return false;

        var elsewhere = candidates.Where(n => n.Map.ID != lastMapId).ToList();
        if (elsewhere.Count > 0) candidates = elsewhere;

        var near = candidates[world.Random.Next(candidates.Count)];
        var spawned = world.NPCHandler.SpawnNPC(world, near.Map.ID, near.SpawnX, near.SpawnY, template, shouldRespawn: false);
        if (spawned is null) return false;

        chest = spawned;
        lastMapId = near.Map.ID;
        wanderAt = now + Ticks(world, WanderAfter);

        var riddles = Riddles[near.NPCTemplateID];
        world.SendToAll(P.ServerMessage(announcement + riddles[world.Random.Next(riddles.Length)]));
        return true;
    }

    private static void Despawn(NPC npc, GameWorld world)
    {
        string packet = P.EraseCharacter(npc.LoginID);
        foreach (var player in npc.Map.GetPlayersInRange(npc))
            world.Send(player, packet);

        npc.Map.SetCharacter(null, npc.MapX, npc.MapY);
        npc.State = NPC.States.Dead;
        npc.MoveEvent = null;
        npc.CurrentHP = 0;
        npc.Map.RemoveNPC(npc);
    }

    private static long RandomSpawnDelay(GameWorld world)
    {
        double seconds = MinSpawnDelay.TotalSeconds +
            world.Random.NextDouble() * (MaxSpawnDelay.TotalSeconds - MinSpawnDelay.TotalSeconds);
        return Ticks(world, TimeSpan.FromSeconds(seconds));
    }

    private static long Ticks(GameWorld world, TimeSpan span)
    {
        return (long)(world.TimerFrequency * span.TotalSeconds);
    }
}

return typeof(WanderingChest);
