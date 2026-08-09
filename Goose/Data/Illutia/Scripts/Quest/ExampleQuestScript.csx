using Goose;
using Goose.Quests;
using Goose.Scripting;
using System.Text.Json;

/// <summary>Both roles in one file: as a requirement, the player must hold N of an item AND be
/// above a gold floor; as a reward, hand back an item only if there is a free inventory slot.
///
/// NOTE the ScriptParams handling. ScriptHandler caches ONE instance per file path, shared by
/// every quest_requirements/quest_rewards row pointing here, but ScriptParams is per-row.
/// Deserializing into a field would make row B read row A's params. Always deserialize from the
/// row handed to the call, on every call.</summary>
private class ExampleParams
{
    public int itemId { get; set; }
    public int count { get; set; }
    public long minGold { get; set; }
}

public class ExampleQuestScript : BaseQuestScript
{
    private static ExampleParams Params(string scriptParams) =>
        JsonSerializer.Deserialize<ExampleParams>(scriptParams, JsonHelper.DatabaseOptions);

    public override bool IsMet(QuestRequirement requirement, Player player, GameWorld world)
    {
        var p = Params(requirement.ScriptParams);
        return player.Gold >= p.minGold && player.Inventory.HasItem(p.itemId, p.count);
    }

    public override string GetProgressText(QuestRequirement requirement, Player player, GameWorld world)
    {
        var p = Params(requirement.ScriptParams);
        var template = world.ItemHandler.GetTemplate(p.itemId);
        return string.Format("{0} ({1}) and {2:N0} gp", template.Name, p.count, p.minGold);
    }

    public override void OnTakeRequirement(QuestRequirement requirement, Player player, GameWorld world)
    {
        var p = Params(requirement.ScriptParams);
        player.Inventory.RemoveItem(p.itemId, p.count, world);
    }

    public override string CanComplete(QuestReward reward, Player player, GameWorld world)
    {
        return player.Inventory.GetNumberOfFreeSlots() > 0
            ? null
            : "You need a free inventory slot\\nbefore I can hand this over.";
    }

    public override void GiveReward(QuestReward reward, NPC npc, Player player, GameWorld world)
    {
        var p = Params(reward.ScriptParams);
        var template = world.ItemHandler.GetTemplate(p.itemId);
        if (template == null) return;

        var item = new Item();
        item.LoadFromTemplate(template);
        world.ItemHandler.RollTitleAndSurname(item, world);
        world.ItemHandler.AddAndAssignId(item, world);
        player.Inventory.AddItem(item, p.count, world);

        world.Send(player, P.ServerMessage("[Quest Reward]: " + template.Name));
    }
}

return typeof(ExampleQuestScript);
