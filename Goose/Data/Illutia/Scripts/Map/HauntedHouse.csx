#load "../Global/Halloween/HalloweenCommon.csx"
#load "../Global/Halloween/HalloweenAmbush.csx"
#load "../Global/Halloween/HalloweenConfig.csx"
using System;
using Goose;
using Goose.Scripting;

public class HauntedHouse : BaseMapScript
{
    public override string? CanPlayerJoin(Map map, Player player, GameWorld world)
    {
        var cfg = HalloweenConfig.Get(world);
        if (map.ID != cfg.Maps.House) return "The house only exists in this world.";
        if (!Halloween.Active(world)) return "The house is dark and the door is locked.";
        return null;
    }

    public override void OnPlayerEntered(Map map, Player player, GameWorld world)
    {
        var cfg = HalloweenConfig.Get(world);
        if (map.ID != cfg.Maps.House) return;
        if (!Halloween.Active(world))
        {
            Halloween.EvictPlayer(world, player);
            return;
        }
        Halloween.CheckCostume(world, player, world.TimeNow);
    }

    public override void OnPlayerMove(Map map, Player player, GameWorld world)
    {
        Halloween.CheckTileEvents(world, map, player);
        HalloweenAmbush.OnStep(world, map, player, world.TimeNow, world.Random);
    }

    public override void OnPlayerTurn(Map map, Player player, GameWorld world)
        => Halloween.CheckTileEvents(world, map, player);

    public override void OnPlayerLeft(Map map, Player player, GameWorld world)
        => Halloween.SetState(player, "hw.costumeDeadline", 0);
}

return typeof(HauntedHouse);
