using static Goose.Inventory;

namespace Goose.Commands
{
    internal static class EquipModifier
    {
        public static void Set(CommandContext ctx, string slot, int id, ItemProperty kind)
        {
            var world = ctx.World;
            var player = ctx.Player;
            string kindName = kind == ItemProperty.TitleId ? "title" : "surname";

            EquipSlots? equipslot = EquipGraphicCommand.ParseSlot(slot);
            if (equipslot is null)
            {
                ctx.Send($"Unknown slot {slot}. Slots: {EquipGraphicCommand.SlotNames}");
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

            ItemModifier? modifier = null;
            if (id != 0)
            {
                modifier = kind == ItemProperty.TitleId ? world.ItemHandler.GetTitle(id) : world.ItemHandler.GetSurname(id);
                if (modifier is null)
                {
                    ctx.Send($"No item {kindName} with id {id}.");
                    return;
                }
            }

            Item item = equipped.Item;
            player.RemoveStats(item.TotalStats, world, changeCurrentHPMP: false);
            world.ItemHandler.SetModifier(item, kind, modifier, world);
            player.AddStats(item.TotalStats, world, updateCharacter: false);

            player.Inventory.SendEquippedSlot(equipslot.Value, world);

            ctx.Send(modifier is null
                ? $"Cleared the {kindName} on your {slotName}: {item.Name}."
                : $"Set the {kindName} on your {slotName} to {modifier.Name}: {item.Name}.");
        }
    }

    [Command("/equiptitle", AccessPrivilege.ChangeEquipModifier, Section = "GM",
        Help = "Set the title of the item equipped in one of your slots (0 clears it).",
        Usage = "/equiptitle <slot> <title id>")]
    public sealed class EquipTitleCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, string slot, int id) =>
            EquipModifier.Set(ctx, slot, id, ItemProperty.TitleId);
    }

    [Command("/equipsurname", AccessPrivilege.ChangeEquipModifier, Section = "GM",
        Help = "Set the surname of the item equipped in one of your slots (0 clears it).",
        Usage = "/equipsurname <slot> <surname id>")]
    public sealed class EquipSurnameCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, string slot, int id) =>
            EquipModifier.Set(ctx, slot, id, ItemProperty.SurnameId);
    }
}
