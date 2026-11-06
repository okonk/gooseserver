namespace Goose
{
    public sealed class DataCurrency : ICurrency
    {
        private string name;
        private string shortName;
        private CurrencyIcon? icon;
        private bool vendorBuyback;

        public DataCurrency(string id, string name, string shortName, CurrencyIcon? icon, bool vendorBuyback)
        {
            this.Id = id;
            this.name = name;
            this.shortName = shortName;
            this.icon = Normalize(icon);
            this.vendorBuyback = vendorBuyback;
        }

        public string Id { get; }

        public string Name => this.name;

        public string ShortName => this.shortName;

        public CurrencyIcon? Icon => this.icon;

        public void Update(string name, string shortName, CurrencyIcon? icon, bool vendorBuyback)
        {
            this.name = name;
            this.shortName = shortName;
            this.icon = Normalize(icon);
            this.vendorBuyback = vendorBuyback;
        }

        public static string PropertyKey(string id) => "currency." + id;

        public long GetBalance(Player player)
        {
            return player.Properties.GetProperty<long>(PropertyKey(this.Id), 0);
        }

        public long GetBuyPrice(ItemTemplate template, int stack)
        {
            return template.Value * stack;
        }

        public long GetSellPrice(Item item, int stack)
        {
            if (!this.vendorBuyback || item.Value == 0) return -1;
            return stack * item.Value / 2;
        }

        public void Add(Player player, long amount, GameWorld world)
        {
            player.Properties[PropertyKey(this.Id)] = Apply(this.GetBalance(player), amount);
        }

        public void Remove(Player player, long amount, GameWorld world)
        {
            player.Properties[PropertyKey(this.Id)] = Apply(this.GetBalance(player), -amount);
        }

        /// <summary>Saturate instead of wrapping - a wrapped balance is a negative wallet.
        /// The overflow check must precede the add: in unchecked mode the sum wraps before
        /// any clamp could see it.</summary>
        private static long Apply(long current, long amount)
        {
            if (amount > 0)
                return current > long.MaxValue - amount ? long.MaxValue : current + amount;
            if (amount == long.MinValue) return 0;
            long magnitude = -amount;
            return current < magnitude ? 0 : current - magnitude;
        }

        private static CurrencyIcon? Normalize(CurrencyIcon? icon)
        {
            if (icon is null) return null;
            if (icon.Value.Sheet == 0 && icon.Value.Graphic == 0) return null;
            return icon;
        }
    }
}
