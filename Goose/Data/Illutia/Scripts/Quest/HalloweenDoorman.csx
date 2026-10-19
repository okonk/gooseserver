#load "../Global/Halloween/HalloweenConfig.csx"
#load "../Global/Halloween/HalloweenCommon.csx"
using Goose;
using Goose.Quests;
using Goose.Scripting;

public class HalloweenDoorman : BaseQuestScript
{
    public override string? CanComplete(QuestReward reward, Player player, GameWorld world)
        => Halloween.Active(world) ? null : "The door is locked tonight.";

    public override void GiveReward(QuestReward reward, NPC npc, Player player, GameWorld world)
    {
        var cfg = HalloweenConfig.Get(world);
        if (cfg.Maps.Entrance.Length < 2) return;
        if (!world.MapHandler.Maps.TryGetValue(cfg.Maps.House, out var house) || house is null) return;

        player.WarpTo(world, house, cfg.Maps.Entrance[0], cfg.Maps.Entrance[1]);
        Halloween.Feed(world, player, "The door locks behind you.");
    }
}

return typeof(HalloweenDoorman);
