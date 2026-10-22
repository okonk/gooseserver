using Goose;
using Goose.Scripting;
using Goose.Testing;
using Xunit;

namespace Goose.Tests.Halloween;

[Collection("NLog")]
public sealed class HalloweenGuestScriptTests : IDisposable
{
    private readonly HalloweenTestKit kit;
    private readonly Map map;
    private readonly NPCTemplate template;
    private readonly INPCScript guest;

    public HalloweenGuestScriptTests()
    {
        kit = new HalloweenTestKit();
        var script = kit.Compile<INPCScript>("NPC", "HalloweenGuest.csx");
        guest = script.Object;
        template = kit.Fixture.AddNpcTemplate(92101, "Penguin Guest", t =>
        {
            t.Script = script;
            t.MoveSpeed = 1;
            t.CanMove = false;
        });
        map = kit.Fixture.AddBaseMap(1, "Minita", 40, 40);
    }

    public void Dispose() => kit.Dispose();

    [Fact]
    public void Invader_far_from_home_paths_around_walls()
    {
        for (int x = 1; x <= map.Width; x++)
            if (x != 30) map.SetTile(x, 15, new BlockedTile());
        var npc = kit.Fixture.SpawnNpc(map, 5, 5, template, properties: new PropertiesDictionary
        {
            ["invasion"] = 1,
            ["canMove"] = true,
        });
        npc.SpawnX = 5;
        npc.SpawnY = 35;

        for (int i = 0; i < 80 && npc.MapY < 25; i++)
            guest.OnMoveEvent(npc, kit.World);

        Assert.InRange(npc.MapY, 25, 35);
    }

    [Fact]
    public void Mansion_guest_keeps_engine_movement()
    {
        var npc = kit.Fixture.SpawnNpc(map, 5, 5, template);
        npc.SpawnX = 5;
        npc.SpawnY = 35;

        guest.OnMoveEvent(npc, kit.World);

        Assert.Equal((5, 6), (npc.MapX, npc.MapY));
    }
}
