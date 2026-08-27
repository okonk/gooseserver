using System.Reflection;
using Goose;
using Goose.Events;
using Goose.Testing;
using Goose.Tests.Fakes;

namespace Goose.Tests;

public class ClassLevelNullGuardTests
{
    [Fact]
    public void ExpBar_MissingLevelRow_ReturnsFlatBar()
    {
        using var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "m");
        var player = fixture.CommandPlayerOn(map, 1, 1);
        player.Level = 99;

        Assert.Equal("TNL0,0,0,0", P.ExpBar(player));
    }

    [Fact]
    public void SpawnNPC_UnknownClass_ReturnsNull()
    {
        using var fixture = new TestWorldFixture();
        fixture.AddBaseMap(1, "m");
        var template = new NPCTemplate { NPCTemplateID = 1, Name = "x", ClassID = 999,
            Level = 5, BaseStats = new AttributeSet() };

        Assert.Null(fixture.World.NPCHandler.SpawnNPC(fixture.World, 1, 2, 2, template, false));
    }

    [Fact]
    public void ValidateLevels_GappedOrEmpty_ReturnsFalse()
    {
        Class Build(int[] levels)
        {
            var cls = new Class { ClassID = 1, ClassName = "t" };
            foreach (int l in levels)
                cls.AddLevel(new ClassLevel { Level = l, BaseStats = new AttributeSet(), Spells = new List<Spell>() });
            return cls;
        }

        Assert.False(ClassHandler.ValidateLevels(Build(new[] { 5, 6, 7 })));
        Assert.False(ClassHandler.ValidateLevels(Build([])));
        Assert.True(ClassHandler.ValidateLevels(Build(new[] { 1, 2, 3 })));
    }

    [Fact]
    public void ResolveClassAndLevel_EmptyClassTable_ReturnsFalse()
    {
        using var fixture = new TestWorldFixture();
        var classes = (Dictionary<int, Class>)typeof(ClassHandler)
            .GetField("classes", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(fixture.World.ClassHandler)!;
        classes.Clear();
        var player = new Player(0) { Name = "t", ClassID = 0, Level = 5 };

        Assert.False(Player.ResolveClassAndLevel(player, fixture.World));
    }

    [Fact]
    public void ResolveClassAndLevel_ClassWithoutLevelOne_ClampsToLowestExisting()
    {
        using var fixture = new TestWorldFixture();
        // bypasses the load-time rejection on purpose — pins the seam's clamp contract
        fixture.SeedClassLevels(9, "Gapped", new[] { 5, 6, 7 });
        var player = new Player(0) { Name = "t", ClassID = 9, Level = 3 };

        Assert.True(Player.ResolveClassAndLevel(player, fixture.World));
        Assert.Equal(5, player.Level);
        Assert.NotNull(player.Class.GetLevel(player.Level));
    }

    [Fact]
    public void ResolveClassAndLevel_MissingClass_FallsBackAndUpdatesClassId()
    {
        using var fixture = new TestWorldFixture();
        var player = new Player(0) { Name = "t", ClassID = 999, Level = 5 };

        Assert.True(Player.ResolveClassAndLevel(player, fixture.World));
        Assert.Equal(0, player.ClassID);
        Assert.NotNull(player.Class.GetLevel(player.Level));
    }

    [Fact]
    public void PetFromReader_UnknownClass_ReturnsNull()
    {
        using var fixture = new TestWorldFixture();
        var reader = new FakeDbDataReader(new Dictionary<string, object>
        {
            ["pet_id"] = 1, ["pet_title"] = "", ["pet_name"] = "p", ["pet_surname"] = "",
            ["pet_level"] = 5, ["class_id"] = 999, ["experience"] = 0L, ["experience_sold"] = 0L,
            ["body_id"] = 1, ["body_r"] = 0, ["body_g"] = 0, ["body_b"] = 0, ["body_a"] = 0,
            ["face_id"] = 1, ["hair_id"] = 1, ["hair_r"] = 0, ["hair_g"] = 0, ["hair_b"] = 0, ["hair_a"] = 0,
            ["pet_hp"] = 100L, ["pet_mp"] = 10L, ["pet_sp"] = 10L,
            ["stat_ac"] = 0, ["stat_str"] = 0, ["stat_sta"] = 0, ["stat_int"] = 0, ["stat_dex"] = 0,
            ["res_fire"] = 0, ["res_air"] = 0, ["res_earth"] = 0, ["res_spirit"] = 0, ["res_water"] = 0,
            ["weapon_damage"] = 0L,
            // every column Pet.FromReader reads — the fake reader's name indexer throws
            // KeyNotFoundException on a missing key
            ["respawn_time"] = 0, ["next_respawn_time"] = 0L, ["equipped_items"] = "",
            ["body_state"] = 0, ["aggro_range"] = 0, ["move_speed"] = 1m,
            ["attack_range"] = 1, ["attack_speed"] = 1m,
        });

        Assert.Null(Pet.FromReader(reader, fixture.World));
    }

    [Fact]
    public void ChangeClass_MissingDestinationLevel_RejectsBeforeMutation()
    {
        using var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "m");
        var player = fixture.CommandPlayerOn(map, 1, 1);
        fixture.SeedClassLevels(7, "Short", new[] { 1, 2, 3 });
        fixture.World.ClassHandler.GetClass(0)!.GetLevel(50)!.BaseStats.HP = 200;
        player.Level = 50;   // class 0 (fixture default) has levels 1..50
        player.MaxStats.HP = 200;
        long maxBefore = player.MaxStats.HP;

        player.ChangeClass(7, 50, fixture.World, 0.07);

        Assert.Equal(0, player.ClassID);
        Assert.Equal(50, player.Level);
        Assert.Equal(maxBefore, player.MaxStats.HP);
    }

    [Fact]
    public void ChangeClass_MissingOldClassIntermediateLevel_RejectsBeforeMutation()
    {
        using var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "m");
        var player = fixture.CommandPlayerOn(map, 1, 1);
        int[] src = new int[49];
        for (int i = 0; i < 48; i++) src[i] = i + 1;
        src[48] = 50;   // level 49 missing — the old-class deref in ChangeClass
        fixture.SeedClassLevels(8, "Src", src);
        fixture.SeedClassLevels(7, "Dst", Enumerable.Range(1, 50).ToArray());
        var srcClass = fixture.World.ClassHandler.GetClass(8)!;
        srcClass.GetLevel(50)!.BaseStats.HP = 200;
        player.Class = srcClass;
        player.ClassID = 8;
        player.Level = 50;
        player.MaxStats.HP = 200;
        long maxBefore = player.MaxStats.HP;

        player.ChangeClass(7, 50, fixture.World, 0.07);

        Assert.Equal(8, player.ClassID);
        Assert.Equal(50, player.Level);
        Assert.Equal(maxBefore, player.MaxStats.HP);
    }

    [Fact]
    public void BuyVita_MissingLevelRow_RejectsCommand()
    {
        using var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "m");
        var player = fixture.CommandPlayerOn(map, 1, 1);
        player.Level = 99;
        var ev = new BuyVitaCommandEvent { Player = player };

        ev.Ready(fixture.World);
    }
}
