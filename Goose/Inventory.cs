using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;

namespace Goose
{
    /**
     * Inventory, handles a players inventory
     * 
     */
    public class Inventory
    {
        public enum EquipSlots
        {
            Weapon = 1,
            Shield,
            Head,
            Chest,
            Legs,
            Feet,
            Pauldrons,
            Gloves,
            Cloak,
            Belt,
            Necklace,
            Ring1,
            Ring2,
            Mount
        }

        ItemSlot[] equipped;
        ItemSlot[] inventory;
        ItemSlot[] combine;
        /**
         * The player this inventory belongs to
         */
        Player player;

        public Inventory(Player player)
        {
            // Inventory is numbered 1 to InventorySize.
            this.inventory = new ItemSlot[GameSettings.Default.InventorySize + 1];
            // Equipped is numbered 1 to EquippedSize.
            this.equipped = new ItemSlot[GameSettings.Default.EquippedSize + 1];
            // Combine is numbered 1 to CombineBagSize.
            this.combine = new ItemSlot[GameSettings.Default.CombineBagSize + 1];
            this.player = player;
        }

        /**
         * AddItem, adds an item to inventory
         * 
         * Adds to first free slot or stack that it can fit into.
         * If it can't do any of these returns false.
         * 
         */
        public bool AddItem(Item item, long stack, GameWorld world)
        {
            for (int i = 1; i <= GameSettings.Default.InventorySize; i++)
            {
                ItemSlot slot = new ItemSlot();
                slot.Item = item;
                slot.Stack = stack;

                if (this.inventory[i] == null)
                {
                    this.inventory[i] = slot;

                    this.SendSlot(i, world);
                    return true;
                }
                else if (this.inventory[i].CanStack(slot))
                {
                    // Mark for deletion since we're adding to a stack
                    slot.Item.Delete = true;
                    this.inventory[i].Stack += slot.Stack;
                    this.SendSlot(i, world);

                    return true;
                }
            }

            return false;
        }

        /**
         * SendSlot, sends information about slot i to the player who owns the inventory
         * 
         * if they're in game
         * 
         */
        public void SendSlot(int i, GameWorld world)
        {
            if (i < 1 && i > GameSettings.Default.InventorySize) return;

            if (this.player.State >= Player.States.LoadingMap)
            {
                ItemSlot slot = this.inventory[i];
                if (slot != null)
                {
                    world.Send(this.player, "SIS" + i + "|" + slot.Item.GraphicTile + "|" +
                    ((int)slot.Item.Slot == 1 ? 2269 : (int)slot.Item.Slot < 13 ? 2278 : 2268) + "|" +
                    "title" + "|" + slot.Item.Name + "|" + "surname" + "|" + slot.Stack + "|" + slot.Item.Value + "|" +
                    "" + "|" + slot.Item.Description + "|" + slot.Item.WeaponDamage + "|" + slot.Item.WeaponDamage + "|" +
                    slot.Item.WeaponDelay + "|" + "0" + "|" + slot.Item.BaseStats.AC + "|" +
                    slot.Item.BaseStats.HP + "|" + slot.Item.BaseStats.MP + "|" + slot.Item.BaseStats.SP + "|" +
                    slot.Item.BaseStats.Strength + "|" + slot.Item.BaseStats.Stamina + "|" + slot.Item.BaseStats.Intelligence + "|" +
                    slot.Item.BaseStats.Dexterity + "|" + slot.Item.BaseStats.FireResist + "|" + slot.Item.BaseStats.WaterResist + "|" +
                    slot.Item.BaseStats.EarthResist + "|" + slot.Item.BaseStats.AirResist + "|" +
                    slot.Item.BaseStats.SpiritResist + "|" + slot.Item.MinLevel + "|" + slot.Item.MaxLevel + "|0|0|0|0|0" + "|" +
                    (slot.Item.SpellEffect == null ? "" : slot.Item.SpellEffect.Name) + "|" + (int)slot.Item.SpellEffectChance + "|" + slot.Item.Type + "|" + slot.Item.UseType + "|" + 0 + "|" + slot.Item.GraphicR + "|" + slot.Item.GraphicG + "|" + slot.Item.GraphicB + "|" + slot.Item.GraphicA);
                }
                else
                {
                    world.Send(this.player, "CIS" + i);
                }
            }
        }

        /**
         * SendAll, sends all slots to player
         * 
         */
        public void SendAll(GameWorld world)
        {
            for (int i = 1; i <= GameSettings.Default.InventorySize; i++)
            {
                this.SendSlot(i, world);
            }

            for (int i = 1; i <= GameSettings.Default.EquippedSize; i++)
            {
                this.SendEquippedSlot((EquipSlots)i, world);
            }
        }

        /**
         * GetSlot, returns slot i
         * 
         */
        public ItemSlot GetSlot(int i)
        {
            if (i > 0 && i <= GameSettings.Default.InventorySize)
            {
                return this.inventory[i];
            }

            // log bad slot id
            return null;
        }

        /**
         * SwapSlots, swaps the 2 slots
         * 
         */
        public void SwapSlots(int id1, int id2, GameWorld world)
        {
            if (id1 <= 0 || id1 > GameSettings.Default.InventorySize ||
                id2 <= 0 || id2 > GameSettings.Default.InventorySize)
            {
                // log id out of inventory range
                return;
            }

            ItemSlot slot1 = this.GetSlot(id1);
            ItemSlot slot2 = this.GetSlot(id2);

            if (slot1 == null && slot2 == null) return;

            if (slot1 == null || slot2 == null)
            {
                this.inventory[id1] = slot2;
                this.inventory[id2] = slot1;
            }
            // Same base item and they can stack
            else if (slot1.Item.TemplateID == slot2.Item.TemplateID && slot2.CanStack(slot1))
            {
                slot2.Stack += slot1.Stack;
                slot1.Item.Delete = true;
                this.inventory[id1] = null;
            }
            else
            {
                this.inventory[id1] = slot2;
                this.inventory[id2] = slot1;
            }

            this.SendSlot(id1, world);
            this.SendSlot(id2, world);
        }

        /**
         * SplitSlots, adds one item from slot 1 to slot 2
         * 
         */
        public void SplitSlots(int id1, int id2, GameWorld world)
        {
            if (id1 <= 0 || id1 > GameSettings.Default.InventorySize ||
                id2 <= 0 || id2 > GameSettings.Default.InventorySize)
            {
                // log id out of inventory range
                return;
            }

            ItemSlot slot1 = this.GetSlot(id1);
            ItemSlot slot2 = this.GetSlot(id2);

            if (slot1 == null) return;

            ItemSlot temp = new ItemSlot();
            temp.Item = new Item();
            temp.Item.LoadFromTemplate(slot1.Item.Template);
            temp.Stack = 0;

            if (temp.Item.IsBindOnPickup) temp.Item.IsBound = true;

            if (slot2 == null)
            {
                world.ItemHandler.AddItem(temp.Item);
                slot2 = temp;
            }

            if (slot2.CanStack(slot1))
            {
                slot2.Stack += 1;
                this.inventory[id2] = slot2;

                if (slot1.Stack > 1) slot1.Stack--;
                else
                {
                    slot1.Item.Delete = true;
                    this.inventory[id1] = null;
                }
            }

            this.SendSlot(id1, world);
            this.SendSlot(id2, world);
        }

        /**
         * Use, equip/use item at slot id if possible
         * 
         * Note: Assumes slot id is valid
         * 
         */
        public void Use(int id, GameWorld world)
        {
            ItemSlot slot = this.GetSlot(id);
            if (slot == null) return;

            if (this.player.CanUse(slot.Item, world))
            {
                if (slot.Item.UseType == ItemTemplate.UseTypes.Equipment) 
                {
                    this.Equip(slot.Item, world);
                }
                else if (slot.Item.UseType == ItemTemplate.UseTypes.Consumable)
                {
                    this.UseConsumable(slot.Item, world);
                }
                else if (slot.Item.UseType == ItemTemplate.UseTypes.Scroll)
                {
                    if (this.player.LearnSpell(slot.Item.LearnSpellID, world))
                    {
                        this.RemoveItem(slot.Item, 1, world);
                    }
                }
            }
            else if (!this.player.Map.CanUseItems)
            {
                world.Send(this.player, "#You can't use items in this map.");
            }
        }

        /**
         * Equip, equips item
         * 
         * Returns true if item could be successfully equipped
         * 
         */
        public bool Equip(Item item, GameWorld world)
        {
            EquipSlots equipslot = this.ItemSlotToEquipSlot(item.Slot);
            if (equipslot == 0) return false;

            ItemSlot equipped = this.GetEquippedSlot(equipslot);
            if (equipped == null)
            {
                // Try to unequip shield if 2handed weapon is tried to be equipped
                if (equipslot == EquipSlots.Weapon &&
                    item.Slot == ItemTemplate.ItemSlots.TwoHanded &&
                    this.GetEquippedSlot(EquipSlots.Shield) != null)
                {
                    // If we can't unequip the shield then we can't equip the 2handed weapon, so return false
                    if (!this.Unequip(EquipSlots.Shield, world))
                    {
                        return false;
                    }
                }
                // Try to unequip 2handed weapon if shield is tried to be equipped
                else if (equipslot == EquipSlots.Shield &&
                    this.GetEquippedSlot(EquipSlots.Weapon) != null && 
                    this.GetEquippedSlot(EquipSlots.Weapon).Item.Slot == ItemTemplate.ItemSlots.TwoHanded)
                {
                    // If we can't unequip the weapon then we can't equip the shield, so return false
                    if (!this.Unequip(EquipSlots.Weapon, world))
                    {
                        return false;
                    }
                }
            }
            else
            {
                // Try to unequip shield if 2handed weapon is tried to be equipped
                if (equipslot == EquipSlots.Weapon &&
                    item.Slot == ItemTemplate.ItemSlots.TwoHanded &&
                    this.GetEquippedSlot(EquipSlots.Shield) != null)
                {
                    // If we can't unequip the shield then we can't equip the 2handed weapon, so return false
                    if (!this.Unequip(EquipSlots.Shield, world))
                    {
                        return false;
                    }
                }
                // Couldn't unequip equipped item, so therefore we can't equip new item.
                // so return false
                if (!this.Unequip(equipslot, world))
                {
                    return false;
                }
            }

            // At this point we have unequipped everything necessary to equip the item, so go and do it

            // Remove 1 of the item from inventory
            ItemSlot slot = this.RemoveItem(item, 1, world);
            // if slot is null something went wrong
            if (slot == null)
            {
                // log something here
                return false;
            }

            this.equipped[(int)equipslot] = slot;
            this.player.AddStats(slot.Item.TotalStats, world);

            if (slot.Item.SpellEffect != null)
            {
                Buff buff = new Buff();
                buff.Caster = this.player;
                buff.Target = this.player;
                buff.ItemBuff = true;
                buff.SpellEffect = slot.Item.SpellEffect;

                this.player.AddBuff(buff, world, true);
            }

            this.SendEquippedSlot(equipslot, world);
            world.Send(this.player, this.player.CHPString());
            world.Send(this.player, this.player.SNFString());
            world.Send(this.player, this.player.WPSString());

            List<Player> range = this.player.Map.GetPlayersInRange(this.player);
            foreach (Player p in range)
            {
                world.Send(p, this.player.CHPString());
            }

            if (slot.Item.IsBindOnEquip)
            {
                slot.Item.IsBound = true;
                slot.Item.Dirty = true;
            }

            return true;
        }

        /**
         * UseConsumable, use consumable item
         * 
         * Potions, teleports, kegs
         * 
         */
        public void UseConsumable(Item item, GameWorld world)
        {
            foreach (Buff b in this.player.Buffs)
            {
                if (b.SpellEffect.EffectType == SpellEffect.EffectTypes.Stun)
                {
                    // stunned battletext
                    world.Send(this.player, "BT" + this.player.LoginID + ",50");
                    return;
                }
            }

            foreach (Window window in this.player.Windows)
            {
                if (window.Type == Window.WindowTypes.Vendor)
                {
                    world.Send(this.player, "$7You can't use items while with a vendor.");
                    return;
                }
            }

            if (world.Random.Next(1, 100001) <= item.SpellEffectChance * 1000)
            {
                item.SpellEffect.Cast(this.player, this.player, world);

                this.RemoveItem(item, 1, world);
            }
        }

        /**
         * RemoveItem, removes number items from inventory
         * 
         * Returns ItemSlot with the item and stack
         * 
         */
        public ItemSlot RemoveItem(Item item, long number, GameWorld world)
        {
            ItemSlot slot;

            for (int i = 1; i <= GameSettings.Default.InventorySize; i++)
            {
                slot = this.inventory[i];

                if (slot == null) continue;
                if (slot.Item != item) continue;
                // Return null since Item objects are unique, so the item has to be this one
                // But the stack isn't big enough so something is wrong here
                if (slot.Stack < number) return null;

                if (slot.Stack == number)
                {
                    this.inventory[i] = null;
                    this.SendSlot(i, world);
                    return slot;
                }
                else
                {
                    slot.Stack -= number;
                    this.SendSlot(i, world);

                    ItemSlot removed = new ItemSlot();
                    removed.Item = new Item();
                    removed.Item.LoadFromTemplate(item.Template);
                    removed.Stack = number;

                    return removed;
                }
            }

            return null;
        }

        /**
         * Unequip, unequips equipped item at equip slot
         * 
         * Returns true if item could successfully be unequipped
         * 
         */
        public bool Unequip(EquipSlots equipslot, GameWorld world)
        {
            ItemSlot slot = this.GetEquippedSlot(equipslot);
            // maybe log something bad, i don't think this should happen
            if (slot == null) return true;

            if (!this.AddItem(slot.Item, slot.Stack, world)) return false;

            this.equipped[(int)equipslot] = null;
            this.player.RemoveStats(slot.Item.TotalStats, world);

            if (slot.Item.SpellEffect != null)
            {
                Buff remove = null;
                foreach (Buff buff in this.player.Buffs)
                {
                    if (buff.ItemBuff && buff.SpellEffect == slot.Item.SpellEffect)
                    {
                        remove = buff;
                        break;
                    }
                }

                if (remove != null)
                {
                    this.player.RemoveBuff(remove, world);
                }
                else
                {
                    // log bad buff
                }
            }

            this.SendEquippedSlot(equipslot, world);
            world.Send(this.player, this.player.CHPString());
            world.Send(this.player, this.player.SNFString());

            List<Player> range = this.player.Map.GetPlayersInRange(this.player);
            foreach (Player p in range)
            {
                world.Send(p, this.player.CHPString());
            }

            return true;
        }

        /**
         * Unequip, unequips equipped item at slot id
         * 
         * Note: Assumes slot id is valid
         * 
         * Returns true if item could successfully be unequipped
         * 
         */
        public bool Unequip(int id, GameWorld world)
        {
            // id is inv size + id + 1, so get rid of inv + 1
            id -= GameSettings.Default.InventorySize;
            id -= 1;

            return this.Unequip((EquipSlots)id, world);
        }

        /**
         * ItemSlotToEquipSlot, returns the slot id for equipment
         * 
         * Note: ItemSlot refers to the ItemTemplate.ItemSlots enum, not the ItemSlot class,
         * should probably name it better but it'll do
         * 
         */
        public EquipSlots ItemSlotToEquipSlot(ItemTemplate.ItemSlots slot)
        {
            switch (slot)
            {
                case ItemTemplate.ItemSlots.Belt:
                    return EquipSlots.Belt;

                case ItemTemplate.ItemSlots.Chest:
                    return EquipSlots.Chest;

                case ItemTemplate.ItemSlots.Cloak:
                    return EquipSlots.Cloak;
 
                case ItemTemplate.ItemSlots.Gloves:
                    return EquipSlots.Gloves;

                case ItemTemplate.ItemSlots.Helmet:
                    return EquipSlots.Head;

                case ItemTemplate.ItemSlots.Necklace:
                    return EquipSlots.Necklace;

                case ItemTemplate.ItemSlots.OneHanded:
                    return EquipSlots.Weapon;

                case ItemTemplate.ItemSlots.Pants:
                    return EquipSlots.Legs;

                case ItemTemplate.ItemSlots.Pauldrons:
                    return EquipSlots.Pauldrons;

                case ItemTemplate.ItemSlots.Ring:
                    if (this.GetEquippedSlot(EquipSlots.Ring2) == null)
                        return EquipSlots.Ring2;
                    else
                        return EquipSlots.Ring1;

                case ItemTemplate.ItemSlots.Shield:
                    return EquipSlots.Shield;

                case ItemTemplate.ItemSlots.Shoes:
                    return EquipSlots.Feet;

                case ItemTemplate.ItemSlots.TwoHanded:
                    return EquipSlots.Weapon;

                case ItemTemplate.ItemSlots.Mount:
                    return EquipSlots.Mount;

                default:
                    return 0;

            }
        }

        /**
         * GetEquippedSlot, returns the equipped item at the specified slot
         * 
         */
        public ItemSlot GetEquippedSlot(EquipSlots slot)
        {
            return this.equipped[(int)slot];
        }

        /**
         * GetEquippedSlot, returns equipped slot i
         * 
         * Note: inventory size is subtracted from i to get it into equipped array range
         * since i as sent from the client is inventorysize + i
         * 
         */
        public ItemSlot GetEquippedSlot(int i)
        {
            if (i > GameSettings.Default.InventorySize &&
                i <= GameSettings.Default.InventorySize + GameSettings.Default.EquippedSize + 1)
            {
                return this.equipped[i - GameSettings.Default.InventorySize - 1];
            }

            // log bad slot id
            return null;
        }

        /**
         * EquippedDisplay, returns equipped items display for use in MKC and CHP
         * 
         * Note: keeps extra , on end
         * 
         */
        public string EquippedDisplay()
        {
            string e = "";
            EquipSlots[] slots = new EquipSlots[]{EquipSlots.Chest, EquipSlots.Head, 
                EquipSlots.Legs, EquipSlots.Feet, EquipSlots.Shield, EquipSlots.Weapon};
            ItemSlot item;
            foreach (EquipSlots eq in slots)
            {
                item = this.GetEquippedSlot(eq);
                if (item != null)
                {
                    if (item.Item.GraphicA == 0)
                    {
                        e += item.Item.GraphicEquipped + ",*,";
                    }
                    else
                    {
                        e += item.Item.GraphicEquipped + "," +
                             item.Item.GraphicR + "," +
                             item.Item.GraphicG + "," +
                             item.Item.GraphicB + "," +
                             item.Item.GraphicA + ",";
                    }
                }
                else
                {
                    e += "0,*,";
                }
            }

            return e;
        }

        /**
         * SendEquippedSlot, sends info about equipped slot to player
         * 
         */
        public void SendEquippedSlot(EquipSlots equipslot, GameWorld world)
        {
            if (this.player.State >= Player.States.LoadingMap)
            {
                ItemSlot slot = this.equipped[(int)equipslot];
                if (slot != null)
                {
                    world.Send(this.player, "SIS" + ((int)equipslot + 31) + "|" + slot.Item.GraphicTile + "|" +
                    ((int)equipslot == 1 ? 2269 : (int)equipslot < 13 ? 2278 : 2268) + "|" +
                    "title" + "|" + slot.Item.Name + "|" + "surname" + "|" + slot.Stack + "|" + slot.Item.Value + "|" +
                    "" + "|" + slot.Item.Description + "|" + slot.Item.WeaponDamage + "|" + slot.Item.WeaponDamage + "|" +
                    slot.Item.WeaponDelay + "|" + "0" + "|" + slot.Item.BaseStats.AC + "|" +
                    slot.Item.BaseStats.HP + "|" + slot.Item.BaseStats.MP + "|" + slot.Item.BaseStats.SP + "|" +
                    slot.Item.BaseStats.Strength + "|" + slot.Item.BaseStats.Stamina + "|" + slot.Item.BaseStats.Intelligence + "|" +
                    slot.Item.BaseStats.Dexterity + "|" + slot.Item.BaseStats.FireResist + "|" + slot.Item.BaseStats.WaterResist + "|" +
                    slot.Item.BaseStats.EarthResist + "|" + slot.Item.BaseStats.AirResist + "|" +
                    slot.Item.BaseStats.SpiritResist + "|" + slot.Item.MinLevel + "|" + slot.Item.MaxLevel + "|0|0|0|0|0" + "|" +
                    (slot.Item.SpellEffect == null ? "" : slot.Item.SpellEffect.Name) + "|" + (int)slot.Item.SpellEffectChance + "|" + slot.Item.Type + "|" + slot.Item.UseType + "|" + 0 + "|" + slot.Item.GraphicR + "|" + slot.Item.GraphicG + "|" + slot.Item.GraphicB + "|" + slot.Item.GraphicA);
                }
                else
                {
                    world.Send(this.player, "CIS" + ((int)equipslot + 31));
                }
            }
        }

        /**
         * HasItem, returns true if inventory has templateid somewhere
         * 
         */
        public bool HasItem(int templateid)
        {
            foreach (ItemSlot slot in this.inventory)
            {
                if (slot != null && slot.Item.Template.ID == templateid) return true;
            }
            foreach (ItemSlot slot in this.equipped)
            {
                if (slot != null && slot.Item.Template.ID == templateid) return true;
            }
            foreach (ItemSlot slot in this.combine)
            {
                if (slot != null && slot.Item.Template.ID == templateid) return true;
            }

            return false;
        }

        /**
         * GetWeaponDamage, returns currently equipped weapons damage
         * 
         * Or 1 if no weapon
         * 
         */
        public int GetWeaponDamage()
        {
            ItemSlot weapon = this.GetEquippedSlot(EquipSlots.Weapon);
            if (weapon == null) return 1;
            return weapon.Item.WeaponDamage;
        }

        /**
         * GetWeaponDelay, returns currently equipped weapons delay
         * 
         * Or 10 if no weapon
         * 
         */
        public int GetWeaponDelay()
        {
            ItemSlot weapon = this.GetEquippedSlot(EquipSlots.Weapon);
            if (weapon == null) return 10;
            return weapon.Item.WeaponDelay;
        }

        /**
         * Save, saves to database
         * 
         * Uses delete/insert so if the row doesn't exist it doesn't die.. maybe research into this more
         * 
         */
        public void Save(GameWorld world)
        {
            SqlCommand command = new SqlCommand("", world.SqlConnection);
            string query;
            ItemSlot slot;

            // Save inventory
            for (int i = 1; i <= GameSettings.Default.InventorySize; i++)
            {
                slot = this.GetSlot(i);
                if (slot == null)
                {
                    query = "DELETE FROM inventory WHERE player_id=" + this.player.PlayerID + " AND slot=" + i;
                }
                else
                {
                    if (slot.Item.Unsaved) slot.Item.AddItem(world); // have to add item

                    query = "DELETE FROM inventory WHERE player_id=" + 
                        this.player.PlayerID + " AND slot=" + i + ";";

                    query += "INSERT INTO inventory (player_id, slot, item_id, stack) VALUES (" +
                        this.player.PlayerID + ", " +
                        i + ", " +
                        slot.Item.ItemID + ", " +
                        slot.Stack + ");";
                }

                command = new SqlCommand(query, world.SqlConnection);
                command.BeginExecuteNonQuery(new AsyncCallback(GameWorld.DefaultEndExecuteNonQueryAsyncCallback), command);
            }

            // Save equipped
            for (int i = 1; i <= GameSettings.Default.EquippedSize; i++)
            {
                slot = this.equipped[i];
                if (slot == null)
                {
                    query = "DELETE FROM equipped WHERE player_id=" + this.player.PlayerID + " AND slot=" + i;
                }
                else
                {
                    if (slot.Item.Unsaved) slot.Item.AddItem(world); // have to add item

                    query = "DELETE FROM equipped WHERE player_id=" +
                        this.player.PlayerID + " AND slot=" + i + ";";

                    query += "INSERT INTO equipped (player_id, slot, item_id) VALUES (" +
                        this.player.PlayerID + ", " +
                        i + ", " +
                        slot.Item.ItemID + ");";
                }

                command = new SqlCommand(query, world.SqlConnection);
                command.BeginExecuteNonQuery(new AsyncCallback(GameWorld.DefaultEndExecuteNonQueryAsyncCallback), command);
            }

            // Save combine bag
            for (int i = 1; i <= GameSettings.Default.CombineBagSize; i++)
            {
                slot = this.combine[i];
                if (slot == null)
                {
                    query = "DELETE FROM combinebag WHERE player_id=" + this.player.PlayerID + " AND slot=" + i;
                }
                else
                {
                    if (slot.Item.Unsaved) slot.Item.AddItem(world); // have to add item

                    query = "DELETE FROM combinebag WHERE player_id=" +
                        this.player.PlayerID + " AND slot=" + i + ";";

                    query += "INSERT INTO combinebag (player_id, slot, item_id, stack) VALUES (" +
                        this.player.PlayerID + ", " +
                        i + ", " +
                        slot.Item.ItemID + ", " +
                        slot.Stack + ");";
                }

                command = new SqlCommand(query, world.SqlConnection);
                command.BeginExecuteNonQuery(new AsyncCallback(GameWorld.DefaultEndExecuteNonQueryAsyncCallback), command);
            }
        }

        /**
         * Load, loads from database
         * 
         */
        public void Load(GameWorld world)
        {
            SqlCommand command = new SqlCommand("", world.SqlConnection);
            string query;
            SqlDataReader reader;

            int slot;
            int stack;
            int itemid;
            Item item;

            // Load inventory
            query = "SELECT * FROM inventory WHERE player_id=" + this.player.PlayerID;
            command.CommandText = query;
            reader = command.ExecuteReader();
            while (reader.Read())
            {
                slot = Convert.ToInt32(reader["slot"]);
                stack = Convert.ToInt32(reader["stack"]);
                itemid = Convert.ToInt32(reader["item_id"]);

                if (slot < 1 || slot > GameSettings.Default.InventorySize) continue; // log bad slot
                if (stack < 1) continue; // log bad stack

                item = world.ItemHandler.GetItem(itemid);
                if (item == null) continue; // log bad item id
                if (this.inventory[slot] != null) continue; // log 2 items trying to be in the same slot

                this.inventory[slot] = new ItemSlot();
                this.inventory[slot].Item = item;
                this.inventory[slot].Stack = stack;
            }

            reader.Close();

            // Load equipped
            query = "SELECT * FROM equipped WHERE player_id=" + this.player.PlayerID;
            command.CommandText = query;
            reader = command.ExecuteReader();
            while (reader.Read())
            {
                slot = Convert.ToInt32(reader["slot"]);
                itemid = Convert.ToInt32(reader["item_id"]);

                if (slot < 1 || slot > GameSettings.Default.EquippedSize) continue; // log bad slot

                item = world.ItemHandler.GetItem(itemid);
                if (item == null) continue; // log bad item id
                if (this.equipped[slot] != null) continue; // log 2 items trying to be in the same slot

                // if the database slot is a ring and the item's slot isn't, 
                // or the item's slot isn't the same as the database slot
                // should probably check for conflicting equipment, eg a shield + 2 handed sword won't work
                if ((EquipSlots)slot == EquipSlots.Ring1 || (EquipSlots)slot == EquipSlots.Ring2)
                {
                    if (item.Slot != ItemTemplate.ItemSlots.Ring) continue; // log bad equipment in slot
                }
                else if (this.ItemSlotToEquipSlot(item.Slot) != (EquipSlots)slot)
                {
                    continue; // log bad equipment in slot
                }

                this.equipped[slot] = new ItemSlot();
                this.equipped[slot].Item = item;
                this.equipped[slot].Stack = 1;

                this.player.AddStats(this.equipped[slot].Item.TotalStats, world);
                if (this.equipped[slot].Item.SpellEffect != null)
                {
                    Buff buff = new Buff();
                    buff.Caster = this.player;
                    buff.Target = this.player;
                    buff.ItemBuff = true;
                    buff.SpellEffect = this.equipped[slot].Item.SpellEffect;

                    this.player.AddBuff(buff, world, false);
                }
            }

            reader.Close();

            // Load combine bag
            query = "SELECT * FROM combinebag WHERE player_id=" + this.player.PlayerID;
            command.CommandText = query;
            reader = command.ExecuteReader();
            while (reader.Read())
            {
                slot = Convert.ToInt32(reader["slot"]);
                stack = Convert.ToInt32(reader["stack"]);
                itemid = Convert.ToInt32(reader["item_id"]);

                if (slot < 1 || slot > GameSettings.Default.CombineBagSize) continue; // log bad slot
                if (stack < 1) continue; // log bad stack

                item = world.ItemHandler.GetItem(itemid);
                if (item == null) continue; // log bad item id
                if (this.combine[slot] != null) continue; // log 2 items trying to be in the same slot

                this.combine[slot] = new ItemSlot();
                this.combine[slot].Item = item;
                this.combine[slot].Stack = stack;
            }

            reader.Close();
        }

        /**
         * ShowStatsWindow, shows stats window if player has the itemid
         * 
         */
        public void ShowStatsWindow(int itemid, GameWorld world)
        {
            foreach (ItemSlot slot in this.equipped)
            {
                if (slot != null && slot.Item.ItemID == itemid)
                {
                    this.player.ShowStatsWindow(itemid, world);
                    return;
                }
            }

            foreach (ItemSlot slot in this.inventory)
            {
                if (slot != null && slot.Item.ItemID == itemid)
                {
                    this.player.ShowStatsWindow(itemid, world);
                    return;
                }
            }
        }

        /**
         * Combine, combines whatever is in combine bag if possible
         * 
         */
        public void Combine(GameWorld world)
        {
            Dictionary<int, int> combineHash = new Dictionary<int, int>();
            foreach (ItemSlot slot in this.combine)
            {
                if (slot == null) continue;

                if (!combineHash.ContainsKey(slot.Item.TemplateID))
                {
                    combineHash[slot.Item.TemplateID] = 1;
                }
                else
                {
                    combineHash[slot.Item.TemplateID] = combineHash[slot.Item.TemplateID] + 1;
                }
            }

            Combination match = world.CombinationHandler.GetMatch(combineHash);
            if (match == null)
            {
                world.Send(this.player, "$7Couldn't combine items.");
                return;
            }

            if (match.MinLevel > this.player.Level)
            {
                world.Send(this.player, "$7You need to be level " + match.MinLevel + " to create " +
                    match.Name + ".");
                return;
            }
            else if (match.MaxLevel > 0 && match.MaxLevel < this.player.Level)
            {
                world.Send(this.player, "$7You need to be less than level " + match.MaxLevel + " to create " +
                    match.Name + ".");
                return;
            }
            else if (match.MinExperience > this.player.Experience + this.player.ExperienceSold)
            {
                world.Send(this.player, "$7You need " + match.MinExperience + " experience to create " +
                    match.Name + ".");
                return;
            }
            else if (match.MaxExperience > 0 &&
                match.MaxExperience < this.player.Experience + this.player.ExperienceSold)
            {
                world.Send(this.player, "$7You need less than " + match.MaxExperience + 
                    " experience to create " + match.Name + ".");
                return;
            }
            else if ((match.ClassRestrictions & Convert.ToInt64(Math.Pow(2.0, (double)this.player.Class.ClassID))) != 0)
            {
                world.Send(this.player, "$7You are the wrong class to create " + match.Name + ".");
                return;
            }


            List<int> freeslots = new List<int>();
			ItemSlot[] newcombine = new ItemSlot[GameSettings.Default.CombineBagSize + 1];

            Dictionary<int, int> reqhash = new Dictionary<int, int>();
            foreach (KeyValuePair<int, int> req in match.RequiredHash)
            {
                reqhash[req.Key] = req.Value;
            }
            Item item;
            long count;
            long slotcount;
			for (int i = 1; i < this.combine.Length; i++) {

                if (this.combine[i] == null)
                {
                    freeslots.Add(i);
                    continue;
                }
                item = this.combine[i].Item;
                slotcount = this.combine[i].Stack;
                // if this item is in the combination and it still requires more
                if (reqhash.ContainsKey(item.TemplateID)) count = reqhash[item.TemplateID];
                else count = 0;
                if (count > 0)
                {
                    // lower required by how many we have, don't care if it's negative
                    // since the check above catches it
                    reqhash[item.TemplateID] = (int)(count - slotcount);
                    // lower the amount in the stack/slot by how many we actually needed
                    slotcount -= count;
                }
                // if we still have some left over, add it back to combine bag
                if (slotcount > 0)
                {
                    newcombine[i] = new ItemSlot();
                    newcombine[i].Item = item;
                    newcombine[i].Stack = slotcount;
                }
                else
                {
                    freeslots.Add(i);
                    // else mark the item for deletion
                    item.Delete = true;
                }
			}

            if (freeslots.Count < match.ResultItems.Count)
            {
                world.Send(this.player, "$7Not enough free slots to create " + match.Name + ".");
                return;
            }

            int index;
            foreach (ItemTemplate template in match.ResultItems) 
            {
                if (template.IsLore && this.HasItem(template.ID))
                {
                    world.Send(this.player, "$7Already have LORE item " + template.Name + ".");
                    return;
                }

                item = new Item();
                item.LoadFromTemplate(template);
                world.ItemHandler.AddItem(item);

                if (item.IsBindOnPickup) item.IsBound = true;

                index = freeslots[0];
                freeslots.RemoveAt(0);

                newcombine[index] = new ItemSlot();
                newcombine[index].Item = item;
                newcombine[index].Stack = 1;

                world.Send(this.player, "$7Successfully created " + template.Name + ".");
            }

            this.combine = newcombine;
            this.SendCombineBag(world);
        }

        /**
         * SendCombineSlot, sends combine bag slot i to player
         * 
         * kinda inefficient since it searches for combine bag window id every time..
         * 
         */
        public void SendCombineSlot(int i, GameWorld world)
        {
            if (this.player.State >= Player.States.LoadingMap)
            {
                foreach (Window window in this.player.Windows)
                {
                    if (window.Type != Window.WindowTypes.CombineBag) continue;

                    ItemSlot slot = this.combine[i];
                    if (slot != null)
                    {
                        world.Send(this.player, "WNF" + window.ID + "," + i + "," + slot.Item.Name + "|" +
                                                slot.Stack + "|" + slot.Item.ItemID + "|" +
                                                slot.Item.GraphicTile + "|" +
                                                slot.Item.GraphicR + "|" + slot.Item.GraphicG + "|" +
                                                slot.Item.GraphicB + "|" + slot.Item.GraphicA);
                    }
                    else
                    {
                        world.Send(this.player, "WNF" + window.ID + "," + i + ", |0|0|0|*");
                    }
                }
            }
        }

        /**
         * SendCombineBag, sends combine bag items to player
         * 
         */
        public void SendCombineBag(GameWorld world)
        {
            for (int i = 1; i <= GameSettings.Default.CombineBagSize; i++)
            {
                this.SendCombineSlot(i, world);
            }
        }

        /***
         * SwapInventoryCombineSlots, swaps inventory slot with combine slot
         * 
         * Assumes values are valid
         * 
         */
        public void SwapInventoryCombineSlots(int invslot, int combslot, GameWorld world)
        {
            if (combslot <= 0 || combslot > GameSettings.Default.CombineBagSize) return; // log bad attempt at crash

            ItemSlot inventoryslot = this.inventory[invslot];
            ItemSlot combineslot = this.combine[combslot];

            this.combine[combslot] = inventoryslot;
            this.inventory[invslot] = combineslot;

            this.SendSlot(invslot, world);
            this.SendCombineSlot(combslot, world);
        }
    }
}
