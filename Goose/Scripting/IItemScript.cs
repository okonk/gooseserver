using System.Text;

namespace Goose.Scripting
{
    public interface IItemScript
    {
        void OnCreateEvent(Item item, GameWorld world);

        bool OnUseConsumableEvent(Player player, Item item, GameWorld world);

        void OnMeleeEvent(Player player, Item item, GameWorld world);

        /// <summary>Return a refusal message to block picking this item up, or null to
        /// allow. Consulted by PickupItemEvent. Mirrors IMapScript.CanPlayerJoin.</summary>
        string CanPickup(Player player, Item item, GameWorld world);

        /// <summary>Return true to suppress the native title/surname rolls, having done
        /// whatever rolling this item needs. Consulted by ItemHandler.RollTitleAndSurname
        /// before its use-type filter.</summary>
        bool OnRollModifiersEvent(Item item, GameWorld world);
    }
}
