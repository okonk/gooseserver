using static Goose.Inventory;

namespace Goose.Commands
{
    [Command("/equipgraphic", AccessPrivilege.ChangeEquipGraphic, Section = "GM",
        Help = "Change the graphic of the item equipped in one of your slots.",
        Usage = "/equipgraphic <slot> <id> [r g b a] [sheet tile]")]
    public sealed class EquipGraphicCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, string slot, int id,
            int? r = null, int? g = null, int? b = null, int? a = null,
            int? sheet = null, int? tile = null)
        {
            var world = ctx.World;
            var player = ctx.Player;

            EquipSlots? equipslot = ParseSlot(slot);
            if (equipslot is null)
            {
                ctx.Send($"Unknown slot {slot}. Slots: {SlotNames}");
                return;
            }

            string slotName = equipslot.Value.ToString().ToLowerInvariant();

            if ((int)equipslot.Value > world.Settings.EquippedSize)
            {
                ctx.Send($"This server has no {slotName} slot.");
                return;
            }

            ItemSlot? equipped = player.Inventory.GetEquippedSlot(equipslot.Value);
            if (equipped is null)
            {
                ctx.Send($"You have nothing equipped in your {slotName} slot.");
                return;
            }

            string? error = ValidateArgs(id, r, g, b, a, sheet, tile, ctx.Usage);
            if (error is not null)
            {
                ctx.Send(error);
                return;
            }

            Item item = equipped.Item;
            item.GraphicEquipped = id;

            bool hasColour = r is not null && g is not null && b is not null && a is not null;
            bool hasTile = sheet is not null && tile is not null;

            if (hasColour)
            {
                item.GraphicR = r!.Value;
                item.GraphicG = g!.Value;
                item.GraphicB = b!.Value;
                item.GraphicA = a!.Value;
            }

            if (hasTile)
            {
                item.GraphicFile = sheet!.Value;
                item.GraphicTile = tile!.Value;
            }

            player.Inventory.SendEquippedSlot(equipslot.Value, world);

            string updateCharacter = P.UpdateCharacter(player);
            world.Send(player, updateCharacter);

            if (player.Map is { } map)
            {
                foreach (var other in map.GetPlayersInRange(player))
                {
                    world.Send(other, updateCharacter);
                }
            }

            string message = $"Set {item.Name} in {slotName} to graphic {id}";
            if (hasColour) message += $" ({r},{g},{b},{a})";
            if (hasTile) message += $" tile {sheet}/{tile}";
            ctx.Send(message + ".");
        }

        private static EquipSlots? ParseSlot(string name)
        {
            foreach (var slot in Enum.GetValues<EquipSlots>())
            {
                if (string.Equals(slot.ToString(), name, StringComparison.OrdinalIgnoreCase))
                    return slot;
            }

            return null;
        }

        private static string SlotNames =>
            string.Join(", ", Enum.GetValues<EquipSlots>().Select(s => s.ToString().ToLowerInvariant()));

        private static string? ValidateArgs(int id, int? r, int? g, int? b, int? a,
            int? sheet, int? tile, string usage)
        {
            if (id < 0) return "/equipgraphic: graphic id must be 0 or more.";

            bool anyColour = r is not null || g is not null || b is not null || a is not null;
            bool allColour = r is not null && g is not null && b is not null && a is not null;
            if (anyColour && !allColour) return usage;

            if (allColour)
            {
                if (r < 0 || r > 255) return "/equipgraphic: invalid r value";
                if (g < 0 || g > 255) return "/equipgraphic: invalid g value";
                if (b < 0 || b > 255) return "/equipgraphic: invalid b value";
                if (a < 0 || a > 255) return "/equipgraphic: invalid a value";
            }

            if ((sheet is null) != (tile is null)) return usage;
            if (sheet is not null && !allColour) return usage;
            if (sheet is < 0 || tile is < 0) return "/equipgraphic: sheet and tile must be 0 or more.";

            return null;
        }
    }
}
