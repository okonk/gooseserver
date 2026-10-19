using Goose.Testing;

namespace Goose.Tests;

public class NpcPathStepTests : IDisposable
{
    private readonly TestWorldFixture fixture;
    private readonly Map map;
    private readonly NPCTemplate template;

    public NpcPathStepTests()
    {
        this.fixture = new TestWorldFixture();
        this.map = this.fixture.AddBaseMap(1, "Test", 20, 20);
        this.template = this.fixture.AddNpcTemplate(1, "Test NPC", t => t.MoveSpeed = 1);
    }

    public void Dispose() => this.fixture.Dispose();

    private NPC SpawnNpc(int x, int y) => this.fixture.SpawnNpc(this.map, x, y, this.template);

    private void BuildHorizontalWallWithDoorway(int wallY, int doorX)
    {
        for (int x = 1; x <= this.map.Width; x++)
        {
            if (x == doorX) continue;
            this.map.SetTile(x, wallY, new BlockedTile());
        }
    }

    [Fact]
    public void StepTo_moves_when_free_and_faces_when_blocked()
    {
        var npc = this.SpawnNpc(5, 5);
        npc.MoveEvent = null;

        Assert.True(npc.StepTo(Direction.Right, this.fixture.World));
        Assert.Equal(6, npc.MapX);
        Assert.Equal(5, npc.MapY);
        Assert.Equal(Direction.Right, npc.Facing);
        Assert.Null(npc.MoveEvent);

        this.map.SetTile(6, 6, new BlockedTile());

        Assert.False(npc.StepTo(Direction.Down, this.fixture.World));
        Assert.Equal(6, npc.MapX);
        Assert.Equal(5, npc.MapY);
        Assert.Equal(Direction.Down, npc.Facing);
        Assert.Null(npc.MoveEvent);
    }

    [Fact]
    public void PathStepTo_returns_first_step_around_wall()
    {
        this.BuildHorizontalWallWithDoorway(wallY: 6, doorX: 15);
        var npc = this.SpawnNpc(5, 5);

        Assert.Equal(Direction.Right, npc.PathStepTo(5, 15, this.fixture.World));
    }

    [Fact]
    public void PathStepTo_returns_null_when_unreachable_or_at_target()
    {
        var npc = this.SpawnNpc(5, 5);

        Assert.Null(npc.PathStepTo(5, 5, this.fixture.World));

        for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0) continue;
                this.map.SetTile(15 + dx, 15 + dy, new BlockedTile());
            }

        Assert.Null(npc.PathStepTo(15, 15, this.fixture.World));
    }

    [Fact]
    public void PathStepTo_respects_occupancy_predicate()
    {
        for (int x = 1; x <= this.map.Width; x++)
        {
            this.map.SetTile(x, 4, new BlockedTile());
            this.map.SetTile(x, 6, new BlockedTile());
        }
        var npc = this.SpawnNpc(5, 5);
        this.SpawnNpc(10, 5);

        Assert.Equal(Direction.Right, npc.PathStepTo(15, 5, this.fixture.World));
        Assert.Null(npc.PathStepTo(15, 5, this.fixture.World,
            (x, y) => this.map.GetCharacterAt(x, y) is NPC));
    }
}
