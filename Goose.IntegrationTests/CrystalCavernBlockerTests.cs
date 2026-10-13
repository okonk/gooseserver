using System.Diagnostics;
using Goose;
using Goose.Scripting;
using Goose.Testing;
using Xunit;

namespace Goose.IntegrationTests;

public class CrystalCavernBlockerTests
{
    private const int BlockerTemplateId = 239;
    private static readonly (int X, int Y) Lever = (47, 25);
    private static readonly (int X, int Y) BlockerHome = (45, 24);

    private static (TestWorldFixture Fixture, Map Map) Cavern()
    {
        var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(54, "Crystal Cavern", width: 80, height: 80);
        Directory.CreateDirectory(Path.Combine(fixture.DataDirectory, "Scripts", "Map"));
        File.Copy(Path.Combine(AppContext.BaseDirectory, "DimensionScripts", "CrystalCavern.csx"),
            Path.Combine(fixture.DataDirectory, "Scripts", "Map", "CrystalCavern.csx"));
        map.Script = fixture.World.ScriptHandler.GetScript<IMapScript>("Scripts/Map/CrystalCavern.csx");
        return (fixture, map);
    }

    private static NPC Spawn(TestWorldFixture fixture, Map map, int templateId, (int X, int Y) at) =>
        fixture.World.NPCHandler.SpawnNPC(fixture.World, map.ID, at.X, at.Y, new NPCTemplate
        {
            NPCTemplateID = templateId, Name = "N" + templateId, Level = 50, ClassID = 3,
            BaseStats = new AttributeSet { HP = 100 }, HairA = 255, FaceID = 70,
        }, shouldRespawn: false)!;

    private static Player PlayerOnLever(TestWorldFixture fixture, Map map)
    {
        var player = fixture.CommandPlayerOn(map, Lever.X, Lever.Y);
        player.LoginID = 1;
        return player;
    }

    [Fact]
    public void Lever_clears_a_blocker_that_spawned_away_from_its_tile()
    {
        var (fixture, map) = Cavern();
        using var _ = fixture;
        Spawn(fixture, map, 240, BlockerHome);
        var blocker = Spawn(fixture, map, BlockerTemplateId, BlockerHome);
        Assert.NotEqual(BlockerHome, (blocker.MapX, blocker.MapY));

        map.Script!.Object.OnPlayerMove(map, PlayerOnLever(fixture, map), fixture.World);

        Assert.Equal(NPC.States.Dead, blocker.State);
    }

    [Fact]
    public void Displaced_blocker_walks_home_once_its_tile_frees_up()
    {
        var (fixture, map) = Cavern();
        using var _ = fixture;
        var wolf = Spawn(fixture, map, 240, BlockerHome);
        var blocker = Spawn(fixture, map, BlockerTemplateId, BlockerHome);
        map.SetCharacter(null, wolf.MapX, wolf.MapY);
        wolf.MapX = 10;
        wolf.MapY = 10;
        map.SetCharacter(wolf, 10, 10);

        var deadline = Stopwatch.StartNew();
        while ((blocker.MapX, blocker.MapY) != BlockerHome && deadline.Elapsed < TimeSpan.FromSeconds(6))
        {
            fixture.World.EventHandler.Update(fixture.World);
            Thread.Sleep(50);
        }

        Assert.Equal(BlockerHome, (blocker.MapX, blocker.MapY));
        Assert.Same(blocker, map.GetCharacterAt(BlockerHome.X, BlockerHome.Y));
    }
}
