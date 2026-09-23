namespace Goose
{
    public static class CustomItem
    {
        /// <summary>Equipment in a slot the client draws: what /custom accepts, and the test
        /// for a genuine custom item anywhere else - the "Custom created by ..." description
        /// prefix (Item.Custom) can be carried by items /custom never made, such as /hairdye
        /// potions.</summary>
        public static bool IsCustomEquipment(Item item)
        {
            return (item.UseType == ItemTemplate.UseTypes.Armor
                    || item.UseType == ItemTemplate.UseTypes.Weapon)
                && !IsInvisibleSlot(item.Slot);
        }

        public static bool ValidateSingleItem(GameWorld world, Player player, Item item)
        {
            if (!IsCustomEquipment(item))
            {
                world.Send(player, P.ServerMessage("Items to be customised must be equipment and must be visible items."));
                return false;
            }

            return true;
        }

        public static bool ValidateItems(GameWorld world, Player player, Item statsItem, Item lookItem)
        {
            if (!ValidateSingleItem(world, player, statsItem)) return false;
            if (!ValidateSingleItem(world, player, lookItem)) return false;

            if ((statsItem.Slot == ItemTemplate.ItemSlots.OneHanded || statsItem.Slot == ItemTemplate.ItemSlots.TwoHanded)
                && (lookItem.Slot == ItemTemplate.ItemSlots.OneHanded || lookItem.Slot == ItemTemplate.ItemSlots.TwoHanded))
            {
                return true;
            }

            if (statsItem.Slot != lookItem.Slot)
            {
                world.Send(player, P.ServerMessage("Items to be customised must be of the same equipment type."));
                return false;
            }

            return true;
        }

        private static bool IsInvisibleSlot(ItemTemplate.ItemSlots slot)
        {
            return slot is ItemTemplate.ItemSlots.Ring
                or ItemTemplate.ItemSlots.Necklace
                or ItemTemplate.ItemSlots.Pauldrons
                or ItemTemplate.ItemSlots.Cloak
                or ItemTemplate.ItemSlots.Belt
                or ItemTemplate.ItemSlots.Gloves;
        }

        public static string? ParseRGBA(int r, int g, int b, int a, int maxAlpha = 255)
        {
            if (r < 0 || r > 255) return "/custom: invalid r value";
            if (g < 0 || g > 255) return "/custom: invalid g value";
            if (b < 0 || b > 255) return "/custom: invalid b value";
            if (a < 0 || a > maxAlpha) return "/custom: invalid a value";

            return null;
        }

        public static string? SanitizeName(string raw)
        {
            string name = new string(raw.Where(c => c != ',' && c != '|' && !char.IsControl(c)).ToArray()).Trim();
            if (name.Length > 255) name = name.Substring(0, 255);
            return name.Length == 0 ? null : name;
        }

        public static Item? BuildCustomItem(Item statsItem, Item lookItem, int r, int g, int b, int a, string name, string playerName)
        {
            Item item = new Item();
            if (!item.LoadFromTemplate(statsItem.Template)) return null;
            item.StatMultiplier = statsItem.StatMultiplier;
            item.BaseStats = statsItem.BaseStats.Clone();
            item.TotalStats = statsItem.TotalStats.Clone();
            item.TotalWeaponDamage = statsItem.TotalWeaponDamage;
            item.BodyState = lookItem.BodyState;
            item.GraphicEquipped = lookItem.GraphicEquipped;
            item.GraphicR = r;
            item.GraphicG = g;
            item.GraphicB = b;
            item.GraphicA = a;
            item.GraphicTile = lookItem.GraphicTile;
            item.GraphicFile = lookItem.GraphicFile;

            item.Name = name;
            item.Description = "Custom created by " + playerName;
            item.IsBound = statsItem.IsBound;
            item.ScriptParams = statsItem.ScriptParams;

            if (statsItem.ItemProperties.TryGetValue(ItemProperty.TitleId, out object? titleId))
                item.ItemProperties[ItemProperty.TitleId] = titleId;

            if (statsItem.ItemProperties.TryGetValue(ItemProperty.SurnameId, out object? surnameId))
                item.ItemProperties[ItemProperty.SurnameId] = surnameId;

            return item;
        }
    }
}
