using System.Collections.Generic;
using Goose;
using Goose.Scripting;

public class ArenaLobby : BaseMapScript
{
    private static readonly Dictionary<(int X, int Y), string> TileNotices = new()
    {
        [(19, 7)] = "LazyGoose, World First Doomslayer",
    };

    public override void OnPlayerMove(Map map, Player player, GameWorld world) => Notice(player, world);

    public override void OnPlayerTurn(Map map, Player player, GameWorld world) => Notice(player, world);

    private static void Notice(Player player, GameWorld world)
    {
        if (TileNotices.TryGetValue((player.MapX, player.MapY), out var text))
            world.Send(player, P.Notice(NoticeType.Info, text));
    }
}

return typeof(ArenaLobby);
