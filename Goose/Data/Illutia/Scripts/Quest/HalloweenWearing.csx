using System.Text.Json;
using Goose;
using Goose.Quests;
using Goose.Scripting;

public class HalloweenWearing : BaseQuestScript
{
    public override bool IsMet(QuestRequirement requirement, Player player, GameWorld world)
    {
        var ids = Parse(requirement.ScriptParams);
        if (ids is null) return false;

        foreach (var slot in player.Inventory.GetEquippedSlots())
        {
            if (slot is not null && ids.Contains(slot.Item.TemplateID)) return true;
        }
        return false;
    }

    public override string GetProgressText(QuestRequirement requirement, Player player, GameWorld world)
        => IsMet(requirement, player, world) ? "Wearing your costume" : "Put on your costume";

    private static HashSet<int>? Parse(string scriptParams)
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
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("items", out var items) ||
                items.ValueKind != JsonValueKind.Array)
                return null;

            var ids = new HashSet<int>();
            foreach (var element in items.EnumerateArray())
            {
                if (!element.TryGetInt32(out var id)) return null;
                ids.Add(id);
            }
            return ids;
        }
    }
}

return typeof(HalloweenWearing);
