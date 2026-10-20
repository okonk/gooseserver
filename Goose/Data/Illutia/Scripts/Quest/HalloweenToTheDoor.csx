#load "../Global/Halloween/HalloweenConfig.csx"
#load "../Global/Halloween/HalloweenCommon.csx"
using Goose;
using Goose.Quests;
using Goose.Scripting;

public class HalloweenToTheDoor : BaseQuestScript
{
    public override string? CanComplete(QuestReward reward, Player player, GameWorld world)
        => Halloween.Active(world) ? null : "The house is dark tonight. Nobody's going.";

    public override void GiveReward(QuestReward reward, NPC npc, Player player, GameWorld world)
    {
        var cfg = HalloweenConfig.Get(world);
        if (cfg.Maps.GroveExit.Length < 2) return;
        if (!world.MapHandler.Maps.TryGetValue(cfg.Maps.Grove, out var grove) || grove is null) return;

        player.WarpTo(world, grove, cfg.Maps.GroveExit[0], cfg.Maps.GroveExit[1]);
        Halloween.Feed(world, player, "You arrive at the front door of the house.");
    }
}

return typeof(HalloweenToTheDoor);
