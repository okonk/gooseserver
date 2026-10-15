using Goose.Scripting;
using Goose.Testing;

namespace Goose.Tests;

public class WanderingChestScriptTests
{
    private const int ChestTemplateId = 261;
    private const int CowTemplateId = 8;
    private const string CowRiddle = "Where cattle low beneath the little mountain, a chest chews its cud.";

    [Fact]
    public void Appears_beside_a_riddled_monster_and_announces_its_riddle()
    {
        using var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(14, "Chiisaiyama");
        SpawnMonster(fixture, map, CowTemplateId, 5, 5);
        var player = OnlinePlayer(fixture, map);
        var script = CreateScenario(fixture);

        Update(fixture, script, 0);

        var chest = Assert.Single(Chests(map));
        Assert.True(Math.Abs(chest.MapX - 5) <= 1 && Math.Abs(chest.MapY - 5) <= 1);
        Assert.Contains("$7[Event] A Wandering Chest has appeared! " + CowRiddle + "\x1", player.Sent);
    }

    [Fact]
    public void Skips_pvp_maps_excluded_maps_ambient_npcs_and_monsters_without_riddles()
    {
        using var fixture = new TestWorldFixture();
        var pvp = fixture.AddBaseMap(15, "Battle Fields");
        pvp.CanPVP = true;
        SpawnMonster(fixture, pvp, CowTemplateId, 5, 5);
        var cavern = fixture.AddBaseMap(54, "Crystal Cavern");
        SpawnMonster(fixture, cavern, CowTemplateId, 5, 5);
        var plain = fixture.AddBaseMap(2, "Plain");
        SpawnMonster(fixture, plain, 999, 5, 5);
        var sewers = fixture.AddBaseMap(46, "Minita Sewers");
        SpawnMonster(fixture, sewers, CowTemplateId, 5, 5, NPCTemplate.Types.Ambient);
        var script = CreateScenario(fixture);

        Update(fixture, script, 0);

        Assert.Empty(Chests(pvp));
        Assert.Empty(Chests(cavern));
        Assert.Empty(Chests(plain));
        Assert.Empty(Chests(sewers));
    }

    [Fact]
    public void Wanders_to_another_map_after_twenty_minutes()
    {
        using var fixture = new TestWorldFixture();
        var first = fixture.AddBaseMap(14, "Chiisaiyama");
        SpawnMonster(fixture, first, CowTemplateId, 5, 5);
        var second = fixture.AddBaseMap(16, "Chiisaiyama Two");
        SpawnMonster(fixture, second, CowTemplateId, 5, 5);
        var player = OnlinePlayer(fixture, first);
        var script = CreateScenario(fixture);

        Update(fixture, script, 0);
        var original = Assert.Single(Chests(first).Concat(Chests(second)));
        var originalMap = original.Map;

        Update(fixture, script, Minutes(fixture, 19));
        Assert.Equal(NPC.States.Alive, original.State);

        Update(fixture, script, Minutes(fixture, 21));

        Assert.Equal(NPC.States.Dead, original.State);
        Assert.Empty(Chests(originalMap));
        var otherMap = originalMap == first ? second : first;
        Assert.Single(Chests(otherMap));
        Assert.Contains("$7[Event] Bored of waiting, the Wandering Chest has crept away... " + CowRiddle + "\x1", player.Sent);
    }

    [Fact]
    public void Stays_put_while_it_has_been_hit_recently()
    {
        using var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(14, "Chiisaiyama");
        SpawnMonster(fixture, map, CowTemplateId, 5, 5);
        var script = CreateScenario(fixture);

        Update(fixture, script, 0);
        var chest = Assert.Single(Chests(map));
        chest.ScriptStore = Minutes(fixture, 20);

        Update(fixture, script, Minutes(fixture, 21));
        Assert.Same(chest, Assert.Single(Chests(map)));
        Assert.Equal(NPC.States.Alive, chest.State);

        Update(fixture, script, Minutes(fixture, 23));
        Assert.Equal(NPC.States.Dead, chest.State);
    }

    [Fact]
    public void Waits_between_one_and_two_hours_after_being_killed()
    {
        using var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(14, "Chiisaiyama");
        SpawnMonster(fixture, map, CowTemplateId, 5, 5);
        var player = OnlinePlayer(fixture, map);
        var script = CreateScenario(fixture);

        Update(fixture, script, 0);
        var chest = Assert.Single(Chests(map));
        chest.Attacked(player, chest.CurrentHP, fixture.World);
        Assert.Empty(Chests(map));

        Update(fixture, script, Minutes(fixture, 1));
        Update(fixture, script, Minutes(fixture, 60));
        Assert.Empty(Chests(map));

        Update(fixture, script, Minutes(fixture, 122));
        Assert.Single(Chests(map));
    }

    [Fact]
    public void Npc_script_records_hits_and_announces_the_killer()
    {
        using var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(14, "Chiisaiyama");
        SpawnMonster(fixture, map, CowTemplateId, 5, 5);
        var player = OnlinePlayer(fixture, map, "Looter");
        var script = CreateScenario(fixture);

        Update(fixture, script, 0);
        var chest = Assert.Single(Chests(map));
        long before = fixture.World.TimeNow;

        chest.Attacked(player, 1, fixture.World);
        Assert.True(chest.ScriptStore is long hit && hit >= before);

        chest.Attacked(player, chest.CurrentHP, fixture.World);
        Assert.Contains("$7[Event] Looter has cracked open the Wandering Chest!\x1", player.Sent);
    }

    [Fact]
    public void Gm_command_spawns_the_chest_then_makes_it_wander()
    {
        using var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(14, "Chiisaiyama");
        SpawnMonster(fixture, map, CowTemplateId, 5, 5);
        var gm = OnlinePlayer(fixture, map, "Gm");
        gm.Access = Player.AccessStatus.GameMaster;
        var script = CreateScenario(fixture);
        script.Object.OnLoaded(fixture.World);

        Assert.True(fixture.RunCommand(gm, "/wanderingchest"));
        var first = Assert.Single(Chests(map));
        Assert.Contains($"$7The Wandering Chest is on Chiisaiyama (14) at {first.MapX},{first.MapY}.\x1", gm.Sent);

        Assert.True(fixture.RunCommand(gm, "/wanderingchest"));
        Assert.Equal(NPC.States.Dead, first.State);
        Assert.NotSame(first, Assert.Single(Chests(map)));
        Assert.Contains("$7[Event] Bored of waiting, the Wandering Chest has crept away... " + CowRiddle + "\x1", gm.Sent);
    }

    private static Script<IGlobalScript> CreateScenario(TestWorldFixture fixture)
    {
        fixture.World.NPCHandler.AddTemplate(new NPCTemplate
        {
            NPCTemplateID = ChestTemplateId,
            Name = "Wandering Chest",
            Level = 50,
            ClassID = 3,
            CanBeKilled = true,
            BaseStats = new AttributeSet { HP = 1000 },
            Drops = [],
            Script = CompileScript<INPCScript>(fixture, "NPC", "WanderingChestNPC.csx"),
        });
        return CompileScript<IGlobalScript>(fixture, "Global", "WanderingChest.csx");
    }

    private static Script<T> CompileScript<T>(TestWorldFixture fixture, string folder, string fileName) where T : class
    {
        Directory.CreateDirectory(Path.Combine(fixture.DataDirectory, "Scripts", folder));
        var relativePath = $"Scripts/{folder}/{fileName}";
        File.Copy(Path.Combine(FindIllutiaDataDirectory(), "Scripts", folder, fileName),
            Path.Combine(fixture.DataDirectory, relativePath));
        return fixture.World.ScriptHandler.GetScript<T>(relativePath);
    }

    private static void Update(TestWorldFixture fixture, Script<IGlobalScript> script, long now)
    {
        script.Object.GetType().GetMethod("Update")!.Invoke(script.Object, [fixture.World, now]);
    }

    private static long Minutes(TestWorldFixture fixture, int minutes)
    {
        return fixture.World.TimerFrequency * 60 * minutes;
    }

    private static void SpawnMonster(TestWorldFixture fixture, Map map, int templateId, int x, int y,
        NPCTemplate.Types type = NPCTemplate.Types.Monster)
    {
        var template = new NPCTemplate
        {
            NPCTemplateID = templateId,
            Name = "Monster " + templateId,
            NPCType = type,
            Level = 5,
            ClassID = 1,
            CanBeKilled = true,
            BaseStats = new AttributeSet { HP = 100 },
            Drops = [],
        };
        fixture.World.NPCHandler.SpawnNPC(fixture.World, map.ID, x, y, template, shouldRespawn: true);
    }

    private static TestWorldFixture.CapturingPlayer OnlinePlayer(TestWorldFixture fixture, Map map, string name = "Tester")
    {
        var player = fixture.CommandPlayerOn(map, 1, 1, name);
        fixture.AddOnlinePlayer(player);
        map.Players.Add(player);
        return player;
    }

    private static IEnumerable<NPC> Chests(Map map)
    {
        return map.NPCs.Where(n => n.NPCTemplateID == ChestTemplateId).ToList();
    }

    private static string FindIllutiaDataDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "Goose", "Data", "Illutia");
            if (Directory.Exists(candidate)) return candidate;
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Goose/Data/Illutia not found");
    }
}
