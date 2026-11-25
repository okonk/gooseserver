using Goose;
using Goose.Scripting;

public class PetSlotItem : BaseItemScript
{
    public override bool OnUseConsumableEvent(Player player, Item item, GameWorld world)
    {
        int slots;
        if (string.IsNullOrWhiteSpace(item.ScriptParams))
        {
            slots = 1;
        }
        else if (!int.TryParse(item.ScriptParams, out slots) || slots <= 0)
        {
            world.Send(player, P.ServerMessage("This item has no valid pet slots configured."));
            return false;
        }

        var settings = world.Settings;
        var room = settings.PetSlotsMax - player.MaxPets(settings);
        if (room <= 0)
        {
            world.Send(player, P.ServerMessage($"You already have the maximum number of pet slots ({settings.PetSlotsMax})."));
            return false;
        }

        player.PetSlotsBonus += Math.Min(slots, room);
        world.Send(player, P.ServerMessage($"You can now keep up to {player.MaxPets(settings)} pets."));
        return true;
    }
}

return typeof(PetSlotItem);
