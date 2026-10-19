#load "../Global/Halloween/HalloweenConfig.csx"
#load "../Global/Halloween/HalloweenCommon.csx"
using System;
using System.Text.Json;
using Goose;
using Goose.Scripting;

public class HalloweenCostume : BaseItemScript
{
    private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

    public override void OnCreateEvent(Item item, GameWorld world)
    {
        if (string.IsNullOrWhiteSpace(item.ScriptParams)) return;

        var looks = ParseLooks(item.ScriptParams);
        if (looks is null)
        {
            log.Warn("HalloweenCostume: invalid look params on item {0} ({1}): {2}",
                item.Name, item.TemplateID, item.ScriptParams);
            return;
        }

        var look = looks[world.Random.Next(looks.Count)];
        item.GraphicEquipped = look.Graphic;
        item.GraphicR = look.R;
        item.GraphicG = look.G;
        item.GraphicB = look.B;
        item.GraphicA = look.A;
        if (look.Tile is int tile) item.GraphicTile = tile;
        if (look.File is int file) item.GraphicFile = file;
    }

    public override void OnEquipEvent(Player player, Item item, GameWorld world)
        => Halloween.CheckCostume(world, player, world.TimeNow);

    public override void OnUnequipEvent(Player player, Item item, GameWorld world)
        => Halloween.CheckCostume(world, player, world.TimeNow);

    private sealed class Look
    {
        public int Graphic { get; set; }
        public int R { get; set; }
        public int G { get; set; }
        public int B { get; set; }
        public int A { get; set; }
        public int? Tile { get; set; }
        public int? File { get; set; }
    }

    private static List<Look>? ParseLooks(string scriptParams)
    {
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
            if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0) return null;

            var looks = new List<Look>();
            foreach (var element in root.EnumerateArray())
            {
                var look = ParseLook(element);
                if (look is null) return null;
                looks.Add(look);
            }
            return looks;
        }
    }

    private static Look? ParseLook(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object) return null;

        var look = new Look();
        var seen = 0;
        foreach (var property in element.EnumerateObject())
        {
            int value;
            switch (property.Name)
            {
                case "graphic":
                    if (!TryInRange(property.Value, 0, int.MaxValue, out value)) return null;
                    look.Graphic = value;
                    break;
                case "r":
                    if (!TryInRange(property.Value, 0, 255, out value)) return null;
                    look.R = value;
                    break;
                case "g":
                    if (!TryInRange(property.Value, 0, 255, out value)) return null;
                    look.G = value;
                    break;
                case "b":
                    if (!TryInRange(property.Value, 0, 255, out value)) return null;
                    look.B = value;
                    break;
                case "a":
                    if (!TryInRange(property.Value, 0, 255, out value)) return null;
                    look.A = value;
                    break;
                case "tile":
                    if (!TryInRange(property.Value, 0, int.MaxValue, out value)) return null;
                    look.Tile = value;
                    continue;
                case "file":
                    if (!TryInRange(property.Value, 0, int.MaxValue, out value)) return null;
                    look.File = value;
                    continue;
                default:
                    return null;
            }
            seen++;
        }
        return seen == 5 ? look : null;
    }

    private static bool TryInRange(JsonElement value, int min, int max, out int result)
    {
        result = 0;
        return value.ValueKind == JsonValueKind.Number
            && value.TryGetInt32(out result)
            && result >= min
            && result <= max;
    }
}

return typeof(HalloweenCostume);
