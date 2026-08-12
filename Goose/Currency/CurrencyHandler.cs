using System;
using System.Collections.Generic;

namespace Goose
{
    /// <summary>Registry of every currency the server knows about. Built-ins are registered
    /// during GameWorld construction; scripts register theirs from OnLoaded, which runs much
    /// later (GameWorld.cs:355), so the ordering is safe by a wide margin.</summary>
    public class CurrencyHandler
    {
        private readonly Dictionary<string, ICurrency> currencies =
            new Dictionary<string, ICurrency>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Throws on a duplicate id. Overwriting would silently repoint every item
        /// priced in that currency at a different wallet.</summary>
        public void Register(ICurrency currency)
        {
            if (currency == null) throw new ArgumentNullException(nameof(currency));
            if (string.IsNullOrWhiteSpace(currency.Id))
                throw new ArgumentException("Currency id must not be empty.", nameof(currency));

            if (this.currencies.ContainsKey(currency.Id))
                throw new InvalidOperationException($"Currency '{currency.Id}' is already registered.");

            this.currencies[currency.Id] = currency;
        }

        /// <summary>The currency with this id, or null if nothing registered it.</summary>
        public ICurrency Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return this.currencies.TryGetValue(id, out var currency) ? currency : null;
        }
    }
}
