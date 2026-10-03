namespace Goose
{
    public class ChestHandler
    {
        private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        private readonly Dictionary<int, ItemContainer> containers = new();

        public void Load(GameWorld world)
        {
            foreach (string key in world.WorldState.KeysWithPrefix("chest:"))
            {
                if (!int.TryParse(key.Substring("chest:".Length), out int npcTemplateId))
                {
                    log.Error("chest key {0} has a malformed template id; skipped", key);
                    continue;
                }

                ItemSlot[]? slots = world.WorldState.Get<ItemSlot[]>(key);

                // Grow-only and whole-page sizing keeps every page-visible index a real container slot;
                // the -1 excludes sentinel slot 0, which would otherwise ratchet capacity up a page every restart.
                int maxPages = Math.Max(
                    Math.Max(1, world.Settings.CommunityChestPages),
                    (int)Math.Ceiling(Math.Max(0, (slots?.Length ?? 1) - 1) / (double)world.Settings.BankSlotsPerPage));
                var container = new ItemContainer(maxPages * world.Settings.BankSlotsPerPage + 1);

                if (slots is not null)
                {
                    for (int i = 0; i < slots.Length && i < container.MaxSlots; i++)
                    {
                        var containerSlot = slots[i];
                        if (containerSlot is null) continue;

                        if (containerSlot.Item is null)
                        {
                            log.Error("chest {0}: slot {1} has a null item; discarded", npcTemplateId, i);
                            continue;
                        }

                        ItemTemplate? template = world.ItemHandler.GetTemplate(containerSlot.Item.TemplateID);
                        if (template is null)
                        {
                            log.Error("chest {0}: item template {1} not found; slot discarded", npcTemplateId, containerSlot.Item.TemplateID);
                            continue;
                        }

                        world.ItemHandler.AddItem(containerSlot.Item, world);
                        containerSlot.Item.Template = template;
                        containerSlot.Item.RefreshStats();

                        container.SetSlot(i, containerSlot);
                    }

                    if (slots.Length > container.MaxSlots)
                        log.Warn("chest {0}: blob has {1} slots, container holds {2}; excess discarded", npcTemplateId, slots.Length, container.MaxSlots);
                }

                world.WorldState.Set(key, container);
                containers[npcTemplateId] = container;
                container.SlotChanged += (slot, oldSlot, newSlot) => { };
            }
        }

        public ItemContainer GetOrCreateContainer(GameWorld world, int npcTemplateId)
        {
            if (containers.TryGetValue(npcTemplateId, out ItemContainer? container))
                return container;

            container = new ItemContainer(Math.Max(1, world.Settings.CommunityChestPages) * world.Settings.BankSlotsPerPage + 1);
            world.WorldState.Set(KeyFor(npcTemplateId), container);
            containers[npcTemplateId] = container;
            container.SlotChanged += (slot, oldSlot, newSlot) => { };

            return container;
        }

        internal string KeyFor(int npcTemplateId) => "chest:" + npcTemplateId;
    }
}
