using Goose.Scripting;

namespace Goose.Tests;

public class MapCanPlayerJoinTests
{
    private sealed class RefusingMapScript : BaseMapScript
    {
        public override string CanPlayerJoin(Map map, Player player, GameWorld world) => "denied";
    }

    [Fact]
    public void Base_script_allows_by_default()
    {
        Assert.Null(new BaseMapScript().CanPlayerJoin(null!, null!, null!));
    }

    [Fact]
    public void A_refusing_script_blocks_entry()
    {
        Assert.Equal("denied", new RefusingMapScript().CanPlayerJoin(null!, null!, null!));
    }
}
