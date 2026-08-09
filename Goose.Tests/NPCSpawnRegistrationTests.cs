using System.Reflection;
using Goose.Tests.Collections;

namespace Goose.Tests;

[Collection(GameWorldSettingsCollection.Name)]
public class NPCSpawnRegistrationTests : IDisposable
{
    private readonly GooseSettings previousSettings = GameWorld.Settings;
    private readonly string dataDirectory;
    private readonly GameWorld world;

    private const int MapId = 1;
    private const int ClassId = 1;

    public NPCSpawnRegistrationTests()
    {
        // Same shape as QuestScriptFixture: isolated settings + a bare GameWorld.
        dataDirectory = Path.Combine(Path.GetTempPath(), "npc-spawn-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(dataDirectory, "Scripts", "Quest"));
        GameWorld.Settings = new GooseSettings
        {
            DataPath = dataDirectory, ExperienceModifier = 1,
            InventorySize = 30, EquippedSize = 20, CombineBagSize = 10, SpellbookSize = 30,
            MaxAC = 3500, MaxPlayers = 200, MaxNPCs = 15000,
        };
        world = new GameWorld(null);

        // One map at id 1. Map.tiles/characters are public and must be sized before
        // Spawn -> PlaceCharacter/SetCharacter can run.
        var map = new Map { ID = MapId, Name = "Test", Width = 20, Height = 20 };
        map.characters = new ICharacter[(map.Width + 1) * (map.Height + 1)];
        map.tiles = new ITile[(map.Width + 1) * (map.Height + 1)];
        world.MapHandler.Maps[MapId] = map;

        // One class with a level-50 row. NPC.LoadFromTemplate dereferences
        // Class.GetLevel(Level) unconditionally (NPC.cs:635-636) and ClassHandler has no
        // public registration path (classes come from the database via LoadClasses), so
        // inject straight into the private dictionary for these tests.
        RegisterClass(ClassId, "Test", level: 50);
    }

    private void RegisterClass(int id, string name, int level)
    {
        var cls = new Class { ClassID = id, ClassName = name, ACMultiplier = 1m };
        cls.AddLevel(new ClassLevel { Level = level, BaseStats = new AttributeSet() });

        var classes = (Dictionary<int, Class>)typeof(ClassHandler)
            .GetField("classes", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(world.ClassHandler)!;
        classes[id] = cls;
    }

    private NPCTemplate Template() => new()
    {
        NPCTemplateID = 1,
        Name = "Test NPC",
        Level = 50,
        ClassID = ClassId,
        BaseStats = new AttributeSet(),
    };

    public void Dispose()
    {
        GameWorld.Settings = previousSettings;
        if (Directory.Exists(dataDirectory)) Directory.Delete(dataDirectory, recursive: true);
    }

    [Fact]
    public void A_spawned_npc_is_counted_by_the_handler()
    {
        // world with one map at id 1 and one class carrying a level-50 row
        var before = world.NPCHandler.NPCCount;

        var npc = world.NPCHandler.SpawnNPC(world, MapId, 5, 5, Template(), shouldRespawn: true);

        Assert.NotNull(npc);
        Assert.Equal(before + 1, world.NPCHandler.NPCCount);
        Assert.Contains(npc, world.MapHandler.GetMap(MapId).NPCs);
        Assert.NotEqual(0, npc.LoginID);
    }

    [Fact]
    public void An_npc_on_a_map_that_does_not_exist_is_not_registered()
    {
        var before = world.NPCHandler.NPCCount;

        // LoadFromTemplate returns false when GetMap is null (NPC.cs:589).
        Assert.Null(world.NPCHandler.SpawnNPC(world, 999999, 5, 5, Template(), shouldRespawn: true));
        Assert.Equal(before, world.NPCHandler.NPCCount);
    }

    /// <summary>The template-level tests above only prove the field holds the value. This one
    /// runs a high-damage template through NPC.LoadFromTemplate and its damage path, which is
    /// where an int on NPC.WeaponDamage or ICharacter.WeaponDamage would surface.</summary>
    [Fact]
    public void A_high_damage_template_survives_the_NPC_damage_path()
    {
        var template = new NPCTemplate
        {
            NPCTemplateID = 1,
            Name = "Overflow",
            Level = 50,
            ClassID = ClassId,
            WeaponDamage = 6_000_000_000L,
            BaseStats = new AttributeSet { HP = 7_000_000_000L },
        };

        var npc = world.NPCHandler.SpawnNPC(world, MapId, 5, 5, template, shouldRespawn: false);

        Assert.Equal(6_000_000_000L, npc.WeaponDamage);
        Assert.Equal(7_000_000_000L, npc.MaxHP);

        // Target with no stats: MaxHP is 0, so a successful one-shot lands CurrentHP at
        // (long)(MaxHP * 0.5) == 0. A wrapped int WeaponDamage would come out as a small
        // positive or negative number and never one-shot; either way <= 0 pins the path.
        var target = new Player(0)
        {
            Name = "Target",
            Level = 1,
            ClassID = ClassId,
            Class = world.ClassHandler.GetClass(ClassId),
            Map = world.MapHandler.GetMap(MapId),
            MapID = MapId,
            MapX = 5,
            MapY = 5,
            BoundMap = world.MapHandler.GetMap(MapId),
            BoundX = 5,
            BoundY = 5,
            State = Player.States.Ready,
            MaxStats = new AttributeSet(),
        };
        target.Inventory = new Inventory(target);

        npc.Attack(target, world);

        // Damage is a double accumulator (NPC.cs:1370); the assertion is that a 6e9 weapon
        // one-shots a target rather than healing it, which is what a wrapped int would do.
        Assert.True(target.CurrentHP <= 0);
    }
}
