using System.Globalization;

namespace Goose
{
    public class EnchantWindow : Window
    {
        // Last confirmed slot assignment from HandleSlot; Task 7's Confirm re-resolves
        // by reference and refuses requests that don't match it.
        public int ItemSlotId { get; private set; }
        public int EssenceSlotId { get; private set; }
        public int ProtectionSlotId { get; private set; }
        public Item? TargetItem { get; private set; }
        public Item? EssenceItem { get; private set; }
        public Item? ProtectionItem { get; private set; }

        public EnchantWindow(GameWorld world, Player player, NPC npc)
        {
            this.ID = ++player.LastWindowID;
            this.Frame = WindowFrames.Enchant;
            this.Type = WindowTypes.Enchant;
            this.Title = "Enchanting Table";
            this.Buttons = "0,1,0,0,1";
            this.NPC = npc;

            player.Windows.Add(this);
            this.SendCreate(player, world);
        }

        public static void Open(GameWorld world, Player player, NPC npc)
        {
            FindOpen(player)?.Close(player, world);
            _ = new EnchantWindow(world, player, npc);
        }

        public static EnchantWindow? FindOpen(Player player)
        {
            return player.Windows.OfType<EnchantWindow>().FirstOrDefault();
        }

        public override void Clicked(ButtonTypes buttonid, int npcid, int id2, int id3, Player player, GameWorld world)
        {
            switch (buttonid)
            {
                case ButtonTypes.Exit:
                case ButtonTypes.Close:
                    player.Windows.Remove(this);
                    break;
            }
        }

        public override void Populate(Player player, GameWorld world)
        {
            this.SendPreview(world, player, null);
        }

        public static void HandleSlot(GameWorld world, Player player, int itemSlotId, int essenceSlotId, int protectionSlotId)
        {
            EnchantWindow? window = FindOpen(player);
            if (window is null) return;

            if (!Map.InRange(player, window.NPC!))
            {
                world.Send(player, P.Notice(NoticeType.Info, "You are too far from the enchanting table."));
                return;
            }

            EnchantSettings settings = world.Settings.Enchanting;

            Item? target = null;
            Item? essence = null;
            Item? protection = null;
            int acceptedItem = 0;
            int acceptedEssence = 0;
            int acceptedProtection = 0;
            string? itemReason = null;

            if (settings.EssenceTemplates.Count > 0)
            {
                if (itemSlotId != 0)
                {
                    Item? item = player.Inventory.GetSlot(itemSlotId)?.Item;
                    string reason = "";
                    if (item is not null && Enchanting.CanEnchant(item, settings, out reason))
                    {
                        acceptedItem = itemSlotId;
                        target = item;
                    }
                    else
                        itemReason = item is null ? "Item not found." : reason;
                }

                if (essenceSlotId != 0 && essenceSlotId != itemSlotId)
                {
                    Item? item = player.Inventory.GetSlot(essenceSlotId)?.Item;
                    if (item is not null && settings.EssenceTemplates.ContainsKey(item.TemplateID))
                    {
                        acceptedEssence = essenceSlotId;
                        essence = item;
                    }
                }

                if (protectionSlotId != 0
                    && protectionSlotId != itemSlotId && protectionSlotId != essenceSlotId)
                {
                    Item? item = player.Inventory.GetSlot(protectionSlotId)?.Item;
                    if (item is not null && item.TemplateID == settings.ProtectionTemplateId)
                    {
                        acceptedProtection = protectionSlotId;
                        protection = item;
                    }
                }
            }

            window.ItemSlotId = acceptedItem;
            window.EssenceSlotId = acceptedEssence;
            window.ProtectionSlotId = acceptedProtection;
            window.TargetItem = target;
            window.EssenceItem = essence;
            window.ProtectionItem = protection;
            window.SendPreview(world, player, itemReason);
        }

        private void SendPreview(GameWorld world, Player player, string? itemReason)
        {
            EnchantSettings settings = world.Settings.Enchanting;
            var lines = new List<string>();

            if (settings.EssenceTemplates.Count == 0)
            {
                lines.Add("Enchanting is not available.");
            }
            else if (this.TargetItem is not null)
            {
                lines.Add(StripPipes(this.TargetItem.Name) + " (max " + Enchanting.Max(this.TargetItem, settings) + ")");
                foreach (EnchantElement element in Enum.GetValues<EnchantElement>())
                {
                    int count = Enchanting.Count(this.TargetItem, element);
                    if (count == 0) continue;
                    lines.Add(element + " " + count + ": +"
                        + FormatPercent(count * ElementPercent(element, settings)) + "% " + StatDescription(element));
                }
                if (this.EssenceItem is not null
                    && settings.EssenceTemplates.TryGetValue(this.EssenceItem.TemplateID, out EnchantElement next))
                {
                    lines.Add("Next: " + next + " +"
                        + FormatPercent(ElementPercent(next, settings)) + "% " + StatDescription(next));
                }
                int fail = Enchanting.FailPercent(this.TargetItem, settings);
                lines.Add(fail > 0 ? "Failure chance: " + fail + "%" : "Failure chance: none");
                if (fail == 0) lines.Add("Protected: not needed");
                else if (this.ProtectionItem is not null) lines.Add("Protected: yes");
                else lines.Add("Protected: no");
            }
            else if (itemReason is not null)
            {
                lines.Add(itemReason);
            }

            bool canEnchant = settings.EssenceTemplates.Count > 0
                && this.TargetItem is not null && this.EssenceItem is not null;
            world.Send(player, P.EnchantPreview(this.ID, this.ItemSlotId, this.EssenceSlotId,
                this.ProtectionSlotId, canEnchant ? 1 : 0, lines));
        }

        // '|' separates EWP lines, so item names are rendered without it.
        private static string StripPipes(string name) => name.Replace("|", "");

        private static string FormatPercent(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

        private static string StatDescription(EnchantElement element) => element switch
        {
            EnchantElement.Earth => "spell damage",
            EnchantElement.Water => "spell crit",
            EnchantElement.Fire => "damage reduction",
            EnchantElement.Air => "MP regen",
            EnchantElement.Spirit => "HP regen",
            _ => "",
        };

        private static double ElementPercent(EnchantElement element, EnchantSettings settings) => element switch
        {
            EnchantElement.Earth => settings.EarthSpellDamagePercent,
            EnchantElement.Water => settings.WaterSpellCritPercent,
            EnchantElement.Fire => settings.FireDamageReductionPercent,
            EnchantElement.Air => settings.AirMPPercentRegenPercent,
            EnchantElement.Spirit => settings.SpiritHPPercentRegenPercent,
            _ => 0,
        };
    }
}
