using Goose;
using Goose.Quests;
using Goose.Scripting;
using System.Text.Json;

/// <summary>RewardType.Script row handing each class its own item. script_params is a map of
/// class id to item id, with an optional quantity:
///
///   {"qty": 1, "1": 10, "2": 11, "3": {"item": 25, "qty": 5}, "4": 11}
///
/// A bare value is an item id taking the top-level qty (default 1); the object form overrides
/// qty for that class alone. A class that is not a key gets nothing, so one row serves every
/// class. A class needing a second item gets a second Script reward row on the same quest.
///
/// ScriptHandler caches one instance per file path, shared by every row pointing here
/// (ScriptHandler.cs:20-30), while ScriptParams is per-ROW — so the map is parsed from the
/// reward handed to the call and never cached in a field.</summary>
public class ClassRewardScript : BaseQuestScript
{
    private const string MisconfiguredMessage =
        "This quest's reward is misconfigured.\\nPlease report this to a GM.";

    private sealed class ClassItem
    {
        public int ItemId { get; set; }
        public int Quantity { get; set; }
    }

    public override string CanComplete(QuestReward reward, Player player, GameWorld world)
    {
        var rewards = Parse(reward.ScriptParams);
        if (rewards is null) return MisconfiguredMessage;

        // A class owed nothing is fine - GiveReward hands over nothing for it. A class owed an
        // item that has no template is not: completing would mark the quest done for nothing.
        if (!rewards.TryGetValue(player.ClassID, out ClassItem classItem)) return null;

        return world.ItemHandler.GetTemplate(classItem.ItemId) is null ? MisconfiguredMessage : null;
    }

    public override int GetRequiredInventorySpace(QuestReward reward, Player player, GameWorld world)
    {
        return Resolve(reward, player, world) is null ? 0 : 1;
    }

    public override void GiveReward(QuestReward reward, NPC npc, Player player, GameWorld world)
    {
        var classItem = Resolve(reward, player, world);
        if (classItem is null) return;

        var item = new Item();
        if (!item.LoadFromTemplate(world.ItemHandler.GetTemplate(classItem.ItemId))) return;

        world.ItemHandler.RollTitleAndSurname(item, world);
        world.ItemHandler.AddAndAssignId(item, world);
        player.Inventory.AddItem(item, classItem.Quantity, world);

        world.Send(player, P.ServerMessage($"[Quest Reward]: Item: {item.Name} ({classItem.Quantity})"));
    }

    /// <summary>The item this player is owed, or null when they are owed nothing: no entry for
    /// their class, params that do not parse, or an item id with no template. One slot covers it
    /// however large the quantity, because AddItem never splits a stack (Inventory.cs:78).</summary>
    private static ClassItem Resolve(QuestReward reward, Player player, GameWorld world)
    {
        var rewards = Parse(reward.ScriptParams);
        if (rewards is null || !rewards.TryGetValue(player.ClassID, out ClassItem classItem)) return null;

        return world.ItemHandler.GetTemplate(classItem.ItemId) is null ? null : classItem;
    }

    /// <summary>Null when the params are not a valid map, which blocks completion: a reward row
    /// nobody can parse must not mark the quest done and hand over nothing.</summary>
    private static Dictionary<int, ClassItem> Parse(string scriptParams)
    {
        if (string.IsNullOrWhiteSpace(scriptParams)) return null;

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(scriptParams);
        }
        catch (JsonException)
        {
            return null;
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return null;

            var defaultQuantity = 1;
            foreach (var property in root.EnumerateObject())
            {
                if (property.Name == "qty" && !TryReadPositive(property.Value, out defaultQuantity))
                    return null;
            }

            var rewards = new Dictionary<int, ClassItem>();
            foreach (var property in root.EnumerateObject())
            {
                if (property.Name == "qty") continue;

                if (!int.TryParse(property.Name, out int classId) || classId <= 0) return null;

                var classItem = ReadClassItem(property.Value, defaultQuantity);
                if (classItem is null) return null;

                rewards[classId] = classItem;
            }

            return rewards;
        }
    }

    private static ClassItem ReadClassItem(JsonElement value, int defaultQuantity)
    {
        if (value.ValueKind == JsonValueKind.Number)
        {
            return TryReadPositive(value, out int itemId)
                ? new ClassItem { ItemId = itemId, Quantity = defaultQuantity }
                : null;
        }

        if (value.ValueKind != JsonValueKind.Object) return null;

        var item = 0;
        var quantity = defaultQuantity;
        var hasItem = false;

        foreach (var property in value.EnumerateObject())
        {
            if (property.Name == "item")
            {
                if (!TryReadPositive(property.Value, out item)) return null;
                hasItem = true;
            }
            else if (property.Name == "qty")
            {
                if (!TryReadPositive(property.Value, out quantity)) return null;
            }
            else
            {
                return null;
            }
        }

        return hasItem ? new ClassItem { ItemId = item, Quantity = quantity } : null;
    }

    private static bool TryReadPositive(JsonElement value, out int result)
    {
        result = 0;
        return value.ValueKind == JsonValueKind.Number
            && value.TryGetInt32(out result)
            && result > 0;
    }
}

return typeof(ClassRewardScript);
