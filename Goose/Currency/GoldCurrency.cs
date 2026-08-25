namespace Goose
{
    /// <summary>Gold, the default currency. Wraps Player.Gold rather than reimplementing it,
    /// so AddGold/RemoveGold keep sending the StatusInfo packets clients expect.</summary>
    public class GoldCurrency : ICurrency
    {
        public string Id { get => Currency.Gold; }
        public string Name { get => "gold"; }
        public string ShortName { get => "gp"; }

        /// <summary>Carried gold only, ignoring the bank - this is what the purchase check
        /// has always compared against.</summary>
        public long GetBalance(Player player) { return player.Gold; }

        public long GetBuyPrice(ItemTemplate template, int stack) { return template.Value * stack; }

        /// <summary>Half value, and a refusal for worthless items - VendorSellInventoryEvent.cs:78.</summary>
        public long GetSellPrice(Item item, int stack)
        {
            if (item.Value == 0) return -1;
            return stack * item.Value / 2;
        }

        public void Add(Player player, long amount, GameWorld world) { player.AddGold(amount, world); }

        /// <summary>RemoveGold silently no-ops when the player is short (Player.cs:1482), so
        /// the caller's balance check is load-bearing. That is unchanged from today.</summary>
        public void Remove(Player player, long amount, GameWorld world) { player.RemoveGold(amount, world); }
    }
}
