
namespace Goose
{
    /// <summary>Registry of every currency the server knows about. Built-ins are registered
    /// during GameWorld construction; scripts register theirs from OnLoaded, which runs much
    /// later (GameWorld.cs:355), so the ordering is safe by a wide margin.</summary>
    public class CurrencyHandler
    {
        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        private readonly Dictionary<string, ICurrency> currencies =
            new Dictionary<string, ICurrency>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Insertion-ordered view beside the dictionary, so /wallet and other
        /// enumerators see registration order, not hash order.</summary>
        private readonly List<ICurrency> all = new List<ICurrency>();

        public IReadOnlyList<ICurrency> All { get => this.all; }

        /// <summary>Throws on a duplicate id. Overwriting would silently repoint every item
        /// priced in that currency at a different wallet.</summary>
        public void Register(ICurrency currency)
        {
            if (currency is null) throw new ArgumentNullException(nameof(currency));
            if (string.IsNullOrWhiteSpace(currency.Id))
                throw new ArgumentException("Currency id must not be empty.", nameof(currency));

            if (this.currencies.ContainsKey(currency.Id))
                throw new InvalidOperationException($"Currency '{currency.Id}' is already registered.");

            this.currencies[currency.Id] = currency;
            this.all.Add(currency);
        }

        /// <summary>The currency with this id, or null if nothing registered it.</summary>
        public ICurrency? Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return this.currencies.TryGetValue(id, out var currency) ? currency : null;
        }

        /// <summary>The currency a transaction settles in: the item's override if it has
        /// one, else the vendor's, else gold.
        ///
        /// An item override wins over the vendor deliberately, so a currency-marked item
        /// settles in its own currency even at a dealer that trades in another.</summary>
        public ICurrency Resolve(ItemTemplate? template, NPC? vendor)
        {
            string? id = template?.CurrencyId;
            if (string.IsNullOrEmpty(id)) id = vendor?.CurrencyId;
            if (string.IsNullOrEmpty(id)) id = Currency.Gold;

            var currency = this.Get(id);
            if (currency is null)
                throw new InvalidOperationException(
                    $"Currency '{id}' is not registered. Scripts must register a currency before stamping it onto templates.");

            return currency;
        }

        public void LoadDataCurrencies(GameWorld world)
        {
            world.Database.Execute(conn =>
            {
                using var probe = conn.CreateCommand();
                probe.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name='currencies'";
                if (probe.ExecuteScalar() is null)
                {
                    log.Warn("currencies table not found; no data currencies loaded");
                    return;
                }

                using var command = conn.CreateCommand();
                command.CommandText = "SELECT * FROM currencies";
                using var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    string id = reader.GetString("currency_id");
                    string name = reader.GetString("name");
                    string shortName = reader.GetString("short_name");
                    var icon = new CurrencyIcon(reader.GetInt32("icon_sheet"), reader.GetInt32("icon_graphic"));
                    bool vendorBuyback = reader.GetString("vendor_buyback") != "0";

                    var existing = this.Get(id);
                    if (existing is DataCurrency data)
                    {
                        data.Update(name, shortName, icon, vendorBuyback);
                    }
                    else if (existing is not null)
                    {
                        log.Error("currencies: row {0} would redefine built-in currency {1}; skipped", id, existing.GetType().Name);
                    }
                    else
                    {
                        this.Register(new DataCurrency(id, name, shortName, icon, vendorBuyback));
                    }
                }
            });
        }

        public int ClearUnknownTemplateCurrencies(GameWorld world)
        {
            int cleared = 0;

            foreach (var template in world.ItemHandler.GetTemplates())
            {
                if (!string.IsNullOrEmpty(template.CurrencyId) && this.Get(template.CurrencyId) is null)
                {
                    log.Error("item template {0} ({1}) references unregistered currency {2}; cleared", template.Name, template.ID, template.CurrencyId);
                    template.CurrencyId = null;
                    cleared++;
                }
            }

            foreach (var template in world.NPCHandler.GetTemplates())
            {
                if (!string.IsNullOrEmpty(template.CurrencyId) && this.Get(template.CurrencyId) is null)
                {
                    log.Error("npc template {0} ({1}) references unregistered currency {2}; cleared", template.Name, template.NPCTemplateID, template.CurrencyId);
                    template.CurrencyId = null;
                    cleared++;
                }
            }

            return cleared;
        }

        // Null (not "gold") keeps Resolve's fallback chain uniform: item, vendor, gold.
        // "0" covers live sheets still holding the old boolean credit_dealer values.
        internal static string? ParseTemplateCurrencyId(string? raw)
        {
            if (raw is null) return null;
            string id = raw.Trim();
            return id is "" or "0" ? null : id;
        }
    }
}
