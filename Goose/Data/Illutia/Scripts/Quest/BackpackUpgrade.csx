using Goose;
using Goose.Quests;
using Goose.Scripting;
using System.Text.Json;

public class BackpackUpgradeScript : BaseQuestScript
{
    private const string MisconfiguredMessage =
        "This quest's reward is misconfigured.\\nPlease report this to a GM.";

    private sealed class Params
    {
        public int From { get; set; }
        public int To { get; set; }
        public int? Columns { get; set; }
    }

    // ScriptParams is per-ROW and this instance is shared by every row pointing at the
    // file (ScriptHandler.cs:20-30), so the params are parsed from the reward each call.
    public override string? CanComplete(QuestReward reward, Player player, GameWorld world)
    {
        var parameters = Parse(reward.ScriptParams);
        if (parameters is null) return MisconfiguredMessage;

        var backpack = player.Inventory.EquippedBackpack;
        if (backpack is null) return "You need to be wearing your backpack.";
        if (backpack.BackpackSlots != parameters.From)
            return $"This upgrade is for a {parameters.From}-slot backpack.";

        return null;
    }

    public override int GetRequiredInventorySpace(QuestReward reward, Player player, GameWorld world)
    {
        return 0;
    }

    public override void GiveReward(QuestReward reward, NPC npc, Player player, GameWorld world)
    {
        var parameters = Parse(reward.ScriptParams);
        if (parameters is null) return;

        var backpack = player.Inventory.EquippedBackpack;
        if (backpack is null || backpack.BackpackSlots != parameters.From) return;

        if (!player.Inventory.UpgradeBackpack(parameters.To, parameters.Columns ?? backpack.BackpackColumns, world))
            return;

        world.Send(player, P.ServerMessage($"[Quest Reward]: Backpack upgraded to {parameters.To} slots."));
    }

    private static Params? Parse(string scriptParams)
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

            var from = 0;
            var to = 0;
            int? columns = null;
            var hasFrom = false;
            var hasTo = false;

            foreach (var property in root.EnumerateObject())
            {
                if (property.Name == "from")
                {
                    if (!TryReadPositive(property.Value, out from)) return null;
                    hasFrom = true;
                }
                else if (property.Name == "to")
                {
                    if (!TryReadPositive(property.Value, out to)) return null;
                    hasTo = true;
                }
                else if (property.Name == "columns")
                {
                    if (!TryReadPositive(property.Value, out int c)) return null;
                    columns = c;
                }
                else
                {
                    return null;
                }
            }

            if (!hasFrom || !hasTo || to <= from || to > Inventory.MaxBackpackSlots) return null;

            return new Params { From = from, To = to, Columns = columns };
        }
    }

    private static bool TryReadPositive(JsonElement value, out int result)
    {
        result = 0;
        return value.ValueKind == JsonValueKind.Number
            && value.TryGetInt32(out result)
            && result > 0;
    }
}

return typeof(BackpackUpgradeScript);
