namespace Goose
{
    /// <summary>Donation credits. Selected by the vendor, never by the item: credits_value
    /// defaults to 0 (items.sql:46), so an item-level test would match every row.</summary>
    public class CreditsCurrency : ICurrency
    {
        public string Id { get { return Currency.Credits; } }
        public string Name { get { return "credits"; } }
        public string ShortName { get { return "cr"; } }

        public long GetBalance(Player player) { return player.Credits; }

        public long GetBuyPrice(ItemTemplate template, int stack) { return (long)template.Credits * stack; }

        /// <summary>Credit dealers buy nothing at all - the historic behaviour of
        /// VendorSellInventoryEvent.cs:75.</summary>
        public long GetSellPrice(Item item, int stack) { return -1; }

        public void Add(Player player, long amount, GameWorld world)
        {
            player.Credits = Clamp((long)player.Credits + amount);
            if (world != null) world.Send(player, P.StatusInfo(player));
        }

        public void Remove(Player player, long amount, GameWorld world)
        {
            player.Credits = Clamp((long)player.Credits - amount);
            if (world != null) world.Send(player, P.StatusInfo(player));
        }

        /// <summary>Player.Credits is an int while the interface is long. Saturate instead of
        /// wrapping - a wrapped balance is a negative wallet.</summary>
        private static int Clamp(long value)
        {
            if (value > int.MaxValue) return int.MaxValue;
            if (value < int.MinValue) return int.MinValue;
            return (int)value;
        }
    }
}
