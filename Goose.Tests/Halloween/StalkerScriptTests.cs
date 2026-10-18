using System.Reflection;
using Goose;
using Goose.Events;
using Goose.Scripting;
using Goose.Testing;
using Xunit;

namespace Goose.Tests.Halloween;

[Collection("NLog")]
public class StalkerScriptTests : IDisposable
{
    private static readonly long Now = 100_000;

    private readonly HalloweenTestKit kit;
    private readonly Map house;
    private readonly Map grove;
    private readonly NPCTemplate template;
    private readonly NPCTemplate blockerTemplate;
    private readonly object stalker;

    public StalkerScriptTests()
    {
        kit = new HalloweenTestKit();
        var script = kit.Compile<INPCScript>("NPC", "Stalker.csx");
        stalker = script.Object;
        template = kit.Fixture.AddNpcTemplate(92002, "Stalker", t =>
        {
            t.Script = script;
            t.CanBeKilled = false;
            t.AttackSpeed = 0;
            t.AggroRange = 0;
            t.MoveSpeed = 5;
            t.BaseStats.HP = 10_000;
        });
        blockerTemplate = kit.Fixture.AddNpcTemplate(92003, "Blocker", t =>
        {
            t.AggroRange = 0;
        });
        house = kit.Fixture.AddBaseMap(48, "House", 60, 60);
        grove = kit.Fixture.AddBaseMap(38, "Grove", 48, 48);
    }

    public void Dispose() => kit.Dispose();

    [Fact]
    public void Waits_one_second_then_follows_target_through_a_doorway()
    {
        var target = PlayerOn(30, 30, "T");
        var npc = Spawn(10, 10, target, "chase");

        Assert.Equal(1.0, npc.MoveSpeed);

        for (int x = 1; x <= house.Width; x++)
            if (x != 10) house.SetTile(x, 20, new BlockedTile());

        var trail = new List<(int X, int Y)>();
        for (int i = 0; i < 100 && npc.State == NPC.States.Alive; i++)
        {
            Step(npc, Now + i);
            trail.Add((npc.MapX, npc.MapY));
        }

        Assert.Contains((10, 20), trail);
        Assert.Equal(NPC.States.Dead, npc.State);
    }

    [Fact]
    public void Speed_tracks_target_mount_each_step()
    {
        var target = PlayerOn(30, 30, "T");
        SeedMoveSpeed(target, 200);
        var npc = Spawn(10, 10, target, "chase");

        Step(npc, Now);
        Assert.Equal(0.22, npc.MoveSpeed, 10);

        var rider = PlayerOn(40, 30, "R");
        SeedMoveSpeed(rider, 200);
        var hunt = Spawn(10, 40, rider, "hunt");

        Step(hunt, Now);
        Assert.Equal(0.18, hunt.MoveSpeed, 10);
    }

    [Fact]
    public void Players_do_not_block_his_path_npcs_do()
    {
        Corridor();
        var target = PlayerOn(10, 10, "T");
        var npc = Spawn(10, 30, target, "chase");
        var blocker = kit.Fixture.SpawnNpc(house, 10, 20, blockerTemplate);

        Step(npc, Now);
        Assert.Equal((10, 30), (npc.MapX, npc.MapY));

        blocker.Despawn(kit.World);
        var bystander = PlayerOn(10, 20, "B");
        int guard = 0;
        while (npc.MapY > 21 && guard++ < 50)
            Step(npc, Now + guard);
        Assert.Equal((10, 21), (npc.MapX, npc.MapY));

        Step(npc, Now + guard);

        CompleteWarp(bystander);
        Assert.Equal((10, 21), (npc.MapX, npc.MapY));
        Assert.Equal(grove, bystander.Map);
        Assert.Equal((38, 37), (bystander.MapX, bystander.MapY));
        Assert.Contains(bystander.Sent, s => s.Contains("The chainsaw catches you. You land in the pumpkin patch."));
    }

    [Fact]
    public void Body_blocker_is_thrown_to_grove_and_target_is_not()
    {
        Corridor();
        var target = PlayerOn(10, 10, "T");
        var npc = Spawn(10, 30, target, "chase");
        var bystander = PlayerOn(10, 20, "B");

        int guard = 0;
        while (npc.MapY > 21 && guard++ < 50)
            Step(npc, Now + guard);
        Step(npc, Now + guard);

        Assert.Equal((10, 21), (npc.MapX, npc.MapY));
        CompleteWarp(bystander);
        Assert.Equal(grove, bystander.Map);
        Assert.Equal((38, 37), (bystander.MapX, bystander.MapY));
        Assert.Contains(bystander.Sent, s => s.Contains("The chainsaw catches you. You land in the pumpkin patch."));
        Assert.Equal(house, target.Map);
        Assert.Equal((10, 10), (target.MapX, target.MapY));
        Assert.DoesNotContain(target.Sent, s => s.Contains("pumpkin patch"));
    }

    [Fact]
    public void Catching_in_chase_sends_target_to_foyer_and_sets_grace()
    {
        var target = PlayerOn(30, 30, "T");
        var npc = Spawn(29, 30, target, "chase");
        target.ScriptState["hw.stalkerNpc"] = npc.LoginID;

        Assert.True(Step(npc, Now));

        Assert.Equal(house, target.Map);
        Assert.Equal((43, 53), (target.MapX, target.MapY));
        Assert.Contains(target.Sent, s => s.Contains("You wake up in the foyer, shaking."));
        Assert.Equal(Now + kit.World.TimerFrequency * 300, target.ScriptState["hw.stalkerGraceUntil"]);
        Assert.Equal(0, target.ScriptState["hw.stalkerNpc"]);
        Assert.Equal(NPC.States.Dead, npc.State);
        Assert.DoesNotContain(target.Sent, s => s.Contains("Candy"));
    }

    [Fact]
    public void Catching_in_hunt_throws_target_out()
    {
        var target = PlayerOn(30, 30, "T");
        var npc = Spawn(31, 30, target, "hunt");
        target.ScriptState["hw.stalkerNpc"] = npc.LoginID;

        Assert.True(Step(npc, Now));

        CompleteWarp(target);
        Assert.Equal(grove, target.Map);
        Assert.Equal((38, 37), (target.MapX, target.MapY));
        Assert.Contains(target.Sent, s => s.Contains("He throws you out of the front door."));
        Assert.Equal(0, target.ScriptState.GetValueOrDefault("hw.stalkerGraceUntil"));
        Assert.Equal(0, target.ScriptState["hw.stalkerNpc"]);
        Assert.Equal(NPC.States.Dead, npc.State);
        Assert.DoesNotContain(target.Sent, s => s.Contains("Candy"));
    }

    [Fact]
    public void Surviving_25s_gives_10_candy_and_despawns()
    {
        var target = PlayerOn(30, 30, "T");
        var npc = Spawn(10, 10, target, "chase");
        target.ScriptState["hw.stalkerNpc"] = npc.LoginID;

        Assert.True(Step(npc, Now + kit.World.TimerFrequency * 25));

        Assert.Contains(target.Sent, s => s.Contains("The revving fades into the walls."));
        Assert.Contains(target.Sent, s => s.Contains("+10 Candy"));
        Assert.Equal(10, CandyCount(target));
        Assert.Equal(Now + kit.World.TimerFrequency * 25 + kit.World.TimerFrequency * 300,
            target.ScriptState["hw.stalkerGraceUntil"]);
        Assert.Equal(0, target.ScriptState["hw.stalkerNpc"]);
        Assert.Equal(NPC.States.Dead, npc.State);
        Assert.DoesNotContain(house.NPCs, n => n.LoginID == npc.LoginID);
    }

    [Fact]
    public void Foyer_ends_chase_without_candy()
    {
        var target = PlayerOn(2, 2, "T");
        var npc = Spawn(10, 10, target, "chase");
        target.ScriptState["hw.stalkerNpc"] = npc.LoginID;

        Assert.True(Step(npc, Now));

        Assert.Contains(target.Sent, s => s.Contains("The revving stops at the doorway."));
        Assert.DoesNotContain(target.Sent, s => s.Contains("Candy"));
        Assert.Equal(0, CandyCount(target));
        Assert.Equal(Now + kit.World.TimerFrequency * 300, target.ScriptState["hw.stalkerGraceUntil"]);
        Assert.Equal(0, target.ScriptState["hw.stalkerNpc"]);
        Assert.Equal(NPC.States.Dead, npc.State);
    }

    [Fact]
    public void Costume_back_on_ends_hunt_without_candy()
    {
        var target = PlayerOn(30, 30, "T");
        Equip(target, kit.PartyCostumeId);
        var npc = Spawn(10, 10, target, "hunt");
        target.ScriptState["hw.stalkerNpc"] = npc.LoginID;

        Assert.True(Step(npc, Now));

        Assert.DoesNotContain(target.Sent, s => s.Contains("revving"));
        Assert.DoesNotContain(target.Sent, s => s.Contains("Candy"));
        Assert.Equal(0, CandyCount(target));
        Assert.Equal(0, target.ScriptState.GetValueOrDefault("hw.stalkerGraceUntil"));
        Assert.Equal(0, target.ScriptState["hw.stalkerNpc"]);
        Assert.Equal(NPC.States.Dead, npc.State);
    }

    [Fact]
    public void Target_logout_ends_chase()
    {
        var target = PlayerOn(30, 30, "T");
        var npc = Spawn(10, 10, target, "chase");

        house.RemovePlayer(target, kit.World);

        Assert.True(Step(npc, Now));
        Assert.Equal(NPC.States.Dead, npc.State);
        Assert.DoesNotContain(target.Sent, s => s.Contains("Candy"));
    }

    [Fact]
    public void Hits_on_him_miss()
    {
        var target = PlayerOn(30, 30, "T");
        var attacker = PlayerOn(10, 10, "A");
        var npc = Spawn(11, 11, target, "chase");

        npc.Attacked(attacker, 500, kit.World);

        Assert.Equal(npc.MaxHP, npc.CurrentHP);
        Assert.Contains(attacker.Sent, s => s.StartsWith("BT" + npc.LoginID + ",21"));
    }

    [Fact]
    public void Warning_feeds_target_once_per_cooldown()
    {
        var target = PlayerOn(10, 10, "T");
        var npc = Spawn(13, 13, target, "chase");

        Step(npc, Now);

        Assert.Single(target.Sent, s => s.Contains("He's right behind you…"));
        Assert.Equal(Now + kit.World.TimerFrequency * 5, target.ScriptState["hw.stalkerWarnAt"]);

        Step(npc, Now + kit.World.TimerFrequency * 4);
        Assert.Single(target.Sent, s => s.Contains("He's right behind you…"));

        Step(npc, Now + kit.World.TimerFrequency * 8);
        Assert.Equal(2, target.Sent.Count(s => s.Contains("He's right behind you…")));
    }

    [Fact]
    public void OnMoveEvent_reschedules_unless_the_chase_ended()
    {
        var target = PlayerOn(30, 30, "T");
        var npc = Spawn(10, 10, target, "chase");
        npc.Properties["spawnedAt"] = kit.World.TimeNow;
        npc.MoveEvent = null;

        OnMoveEvent(npc);

        Assert.Equal(NPC.States.Alive, npc.State);
        Assert.NotNull(npc.MoveEvent);
        Assert.True(npc.MoveEvent.Ticks > kit.World.TimeNow);

        house.RemovePlayer(target, kit.World);
        npc.MoveEvent = null;
        OnMoveEvent(npc);

        Assert.Equal(NPC.States.Dead, npc.State);
        Assert.Null(npc.MoveEvent);
    }

    [Fact]
    public void Pet_on_next_tile_is_not_body_blocked()
    {
        Corridor();
        var target = PlayerOn(10, 10, "T");
        var npc = Spawn(10, 30, target, "chase");
        var pet = new Pet
        {
            Name = "Pet",
            Map = house, MapID = house.ID, MapX = 10, MapY = 20,
            State = Player.States.Ready,
            BaseStats = new AttributeSet(),
            MaxStats = new AttributeSet(),
            Class = kit.World.ClassHandler.GetClass(0)!,
        };
        house.AddPlayer(pet, kit.World);
        house.SetCharacter(pet, 10, 20);

        int guard = 0;
        while (npc.MapY > 21 && guard++ < 50)
            Step(npc, Now + guard);
        Assert.Equal((10, 21), (npc.MapX, npc.MapY));

        Step(npc, Now + guard);

        Assert.Equal((10, 21), (npc.MapX, npc.MapY));
        Assert.Equal(Direction.Up, npc.Facing);
        Assert.Equal(house, pet.Map);
        Assert.Equal((10, 20), (pet.MapX, pet.MapY));
    }

    private void Corridor()
    {
        for (int y = 1; y <= house.Height; y++)
        {
            house.SetTile(9, y, new BlockedTile());
            house.SetTile(11, y, new BlockedTile());
        }
    }

    private NPC Spawn(int x, int y, Player target, string mode) =>
        kit.Fixture.SpawnNpc(house, x, y, template,
            properties: new PropertiesDictionary
            {
                ["stalkerTarget"] = target.PlayerID,
                ["stalkerMode"] = mode,
                ["spawnedAt"] = Now,
            });

    private TestWorldFixture.CapturingPlayer PlayerOn(int x, int y, string name)
    {
        var player = kit.Fixture.CommandPlayerOn(house, x, y, name);
        player.PlayerID = x * 100 + y;
        player.MaxStats.HP = 10_000;
        player.CurrentHP = 10_000;
        SeedMoveSpeed(player, 320);
        house.AddPlayer(player, kit.World);
        house.SetCharacter(player, x, y);
        return player;
    }

    private bool Step(NPC npc, long now)
        => (bool)stalker.GetType().GetMethod("MoveStep")!.Invoke(stalker, new object?[] { npc, now, kit.World })!;

    private void OnMoveEvent(NPC npc)
        => stalker.GetType().GetMethod("OnMoveEvent")!.Invoke(stalker, new object?[] { npc, kit.World });

    private void CompleteWarp(Player player)
    {
        var ev = new DoneLoadingMapEvent { Player = player, Ticks = kit.World.TimeNow };
        kit.World.EventHandler.AddEvent(ev);
        kit.World.EventHandler.Update(kit.World);
        Assert.Equal(grove, player.Map);
    }

    private int CandyCount(Player player) =>
        player.Inventory.GetInventorySlots()
            .Where(s => s is not null && s.Item.TemplateID == kit.CandyId)
            .Sum(s => (int)s.Stack);

    private void Equip(Player player, int templateId)
    {
        var item = new Item();
        item.LoadFromTemplate(kit.World.ItemHandler.GetTemplate(templateId)!);
        kit.World.ItemHandler.AddAndAssignId(item, kit.World);
        Assert.True(player.Inventory.AddItem(item, 1, kit.World));
        Assert.True(player.Inventory.Equip(item, kit.World));
    }

    private static void SeedMoveSpeed(Player player, int speed)
    {
        player.BaseStats.MoveSpeed = speed;
        var queue = new PriorityQueue<int, int>();
        queue.Enqueue(speed, speed);
        queue.Enqueue(speed, speed);
        typeof(Player).GetProperty("moveSpeed", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(player, queue);
    }
}
