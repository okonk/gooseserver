using System;
using System.Collections.Generic;
using System.Text.Json;
using Goose;
using Goose.Quests;
using Goose.Scripting;

public class HalloweenTurnIn : BaseQuestScript
{
    public override bool IsMet(QuestRequirement requirement, Player player, GameWorld world)
    {
        var turnIn = Parse(requirement.ScriptParams);
        return turnIn is not null && Count(player, turnIn.Items) >= turnIn.Count;
    }

    public override string GetProgressText(QuestRequirement requirement, Player player, GameWorld world)
    {
        var turnIn = Parse(requirement.ScriptParams);
        if (turnIn is null) return "";
        return $"{turnIn.Label} ({Math.Min(Count(player, turnIn.Items), turnIn.Count)}/{turnIn.Count})";
    }

    public override void OnTakeRequirement(QuestRequirement requirement, Player player, GameWorld world)
    {
        var turnIn = Parse(requirement.ScriptParams);
        if (turnIn is null) return;
        long remaining = turnIn.Count;
        foreach (var id in turnIn.Items)
        {
            if (remaining == 0) break;
            long take = Math.Min(remaining, player.Inventory.GetItemCount(id));
            if (take == 0) continue;
            player.Inventory.RemoveItem(id, take, world);
            remaining -= take;
        }
    }

    private static long Count(Player player, List<int> items)
    {
        long total = 0;
        foreach (var id in items)
            total += player.Inventory.GetItemCount(id);
        return total;
    }

    private sealed class TurnIn
    {
        public List<int> Items { get; } = [];
        public int Count { get; set; } = 1;
        public string Label { get; set; } = "Items";
    }

    private static TurnIn? Parse(string scriptParams)
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

            var turnIn = new TurnIn();
            foreach (var element in items.EnumerateArray())
            {
                if (!element.TryGetInt32(out var id)) return null;
                turnIn.Items.Add(id);
            }
            if (turnIn.Items.Count == 0) return null;

            if (root.TryGetProperty("count", out var count))
            {
                if (!count.TryGetInt32(out var n) || n <= 0) return null;
                turnIn.Count = n;
            }
            if (root.TryGetProperty("label", out var label) && label.ValueKind == JsonValueKind.String)
                turnIn.Label = label.GetString()!;
            return turnIn;
        }
    }
}

return typeof(HalloweenTurnIn);
