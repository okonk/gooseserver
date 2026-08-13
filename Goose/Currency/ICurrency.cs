namespace Goose
{
    /// <summary>A currency a vendor can transact in. Gold and credits ship as built-ins;
    /// scripts register their own (see Scripts/Global/Dimensions.csx for spirit).
    ///
    /// Buy takes an ItemTemplate and sell takes an Item because that is what each vendor
    /// path actually holds — the sell path reads slot.Item.Value, which item modifiers can
    /// move away from the template value.</summary>
    public interface ICurrency
    {
        /// <summary>Registry key, and the value ItemTemplate.CurrencyId references.</summary>
        string Id { get; }

        /// <summary>Interpolated into player-facing messages: "for 500 gold".</summary>
        string Name { get; }

        /// <summary>Interpolated into LogHandler entries: "(500 gp)".</summary>
        string ShortName { get; }

        long GetBalance(Player player);

        /// <summary>Cost to buy. Negative means this item is not purchasable in this currency.</summary>
        long GetBuyPrice(ItemTemplate template, int stack);

        /// <summary>Payout for selling. Negative means the vendor refuses to buy it.</summary>
        long GetSellPrice(Item item, int stack);

        void Add(Player player, long amount, GameWorld world);

        void Remove(Player player, long amount, GameWorld world);
    }
}
