using System;
using Goose;
using Goose.Quests;
using Goose.Scripting;

/// <summary>The RewardType.Script reward on every dimension unlock quest. Raises the
/// player's maximum dimension to the one this quest grants.
///
/// One instance is shared by all six rewards - ScriptHandler caches one object per file
/// path (ScriptHandler.cs:20-30) - so the dimension is read from reward.ScriptParams on
/// every call and never cached in a field. That is the IQuestScript contract.</summary>
public class DimensionUnlock : BaseQuestScript
{
    private const string MaxDimensionProperty = "dimension.max";

    public override void GiveReward(QuestReward reward, NPC npc, Player player, GameWorld world)
    {
        int granted;
        if (!int.TryParse(reward.ScriptParams, out granted)) return;

        // Raise, never lower. Completing an earlier quest out of order - or a repeat after
        // a data change - must not take access away.
        int current = player.Properties.GetProperty<int>(MaxDimensionProperty, 0);
        if (granted <= current) return;

        player.Properties[MaxDimensionProperty] = granted;

        world.Send(player, P.ServerMessage(
            "The void yields. You may now enter dimension " + granted + "."));
    }
}

return typeof(DimensionUnlock);
