using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using Newtonsoft.Json;
using System.Data.SQLite;

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
        ItemContainer combineContainer;
        /**
         * The player this inventory belongs to
         */
        Player player;

        public Inventory(Player player)
        {
            // Inventory is numbered 1 to InventorySize.
            this.inventory = new ItemSlot[GameWorld.Settings.InventorySize + 1];
            // Equipped is numbered 1 to EquippedSize.
            this.equipped = new ItemSlot[GameWorld.Settings.EquippedSize + 1];
            // Combine is numbered 1 to CombineBagSize.
            this.combineContainer = new ItemContainer(GameWorld.Settings.CombineBagSize + 1);
            this.player = player;
        }

        public ItemContainer GetCombineBagContainer()
        {
            return this.combineContainer;
        }

        public ItemSlot[] GetEquippedSlots()
        {
            return this.equipped;
        }

        public ItemSlot[] GetInventorySlots()
        {
            return this.inventory;
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
            for (int i = 1; i <= GameWorld.Settings.InventorySize; i++)
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
            if (i < 1 && i > GameWorld.Settings.InventorySize) return;

            if (this.player.State >= Player.States.LoadingMap)
            {
                ItemSlot slot = this.inventory[i];
                if (slot != null)
                {
                    world.Send(this.player, P.InventorySlot(slot.Item, world, i, slot.Stack));
                }
                else
                {
                    world.Send(this.player, P.ClearInventorySlot(i));
                }
            }
        }

        public int GetNumberOfFreeSlots()
        {
            int free = 0;
            for (int i = 1; i <= GameWorld.Settings.InventorySize; i++)
            {
                if (this.inventory[i] == null)
                    free++;
            }

            return free;
        }

        /**
         * SendAll, sends all slots to player
         * 
         */
        public void SendAll(GameWorld world)
        {
            for (int i = 1; i <= GameWorld.Settings.InventorySize; i++)
            {
                this.SendSlot(i, world);
            }

            for (int i = 1; i <= GameWorld.Settings.EquippedSize; i++)
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
            if (i > 0 && i <= GameWorld.Settings.InventorySize)
            {
                return this.inventory[i];
            }

            // log bad slot id
            return null;
        }

        public void SetSlot(int i, ItemSlot slot)
        {
            this.inventory[i] = slot;
        }

        /**
         * SwapSlots, swaps the 2 slots
         * 
         */
        public void SwapSlots(int fromSlotId, int toSlotId, GameWorld world)
        {
            ItemSlot fromSlot = this.GetSlot(fromSlotId);
            ItemSlot toSlot = this.GetSlot(toSlotId);

            ItemSlot.SwapSlots(ref fromSlot, ref toSlot);

            this.SetSlot(fromSlotId, fromSlot);
            this.SetSlot(toSlotId, toSlot);

            this.SendSlot(fromSlotId, world);
            this.SendSlot(toSlotId, world);
        }

        /**
         * SplitSlots, adds one item from slot 1 to slot 2
         * 
         */
        public void SplitSlots(int id1, int id2, GameWorld world)
        {
            if (id1 <= 0 || id1 > GameWorld.Settings.InventorySize ||
                id2 <= 0 || id2 > GameWorld.Settings.InventorySize)
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
                world.ItemHandler.AddAndAssignId(temp.Item, world);
                slot2 = temp;
            }

            if (slot2.CanStack(slot1))
            {
                slot2.Stack += 1;
                this.inventory[id2] = slot2;

                if (slot1.Stack > 1) slot1.Stack--;
                else
                {
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
                if (slot.Item.UseType == ItemTemplate.UseTypes.Armor || slot.Item.UseType == ItemTemplate.UseTypes.Weapon)
                {
                    this.Equip(slot.Item, world);
                }
                else if (slot.Item.UseType == ItemTemplate.UseTypes.OneTime)
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
                world.Send(this.player, P.HashMessage("You can't use items in this map."));
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
            string updateCharacter = P.UpdateCharacter(this.player);
            world.Send(this.player, updateCharacter);
            world.Send(this.player, P.StatusInfo(this.player));
            world.Send(this.player, P.WeaponSpeed(this.player));

            List<Player> range = this.player.Map.GetPlayersInRange(this.player);
            foreach (Player p in range)
            {
                world.Send(p, updateCharacter);
            }

            if (slot.Item.IsBindOnEquip)
            {
                slot.Item.IsBound = true;
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
                    world.Send(this.player, P.BattleTextStunned(this.player));
                    return;
                }
            }

            foreach (Window window in this.player.Windows)
            {
                if (window.Type == Window.WindowTypes.Vendor)
                {
                    world.Send(this.player, P.ServerMessage("You can't use items while with a vendor."));
                    return;
                }
            }

            bool remove = true;

            if (item.SpellEffect != null && world.Random.Next(1, 100001) <= item.SpellEffectChance * 1000)
            {
                item.SpellEffect.Cast(this.player, this.player, world); 
            }

            if (item.Script != null)
            {
                try
                {
                    remove = item.Script?.Object.OnUseConsumableEvent(player, item, world) ?? true;
                }
                catch (Exception e) { }
            }

            if (remove)
            {
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

            for (int i = 1; i <= GameWorld.Settings.InventorySize; i++)
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

        /// <summary>
        /// Removes number of templateId items from inventory
        /// </summary>
        /// <param name="templateId"></param>
        /// <param name="number"></param>
        /// <param name="world"></param>
        /// <returns></returns>
        public void RemoveItem(int templateId, long number, GameWorld world)
        {
            ItemSlot slot;

            for (int i = 1; i <= GameWorld.Settings.InventorySize; i++)
            {
                slot = this.inventory[i];

                if (slot == null) continue;
                if (slot.Item.TemplateID != templateId) continue;

                if (slot.Stack == number)
                {
                    this.inventory[i] = null;
                    this.SendSlot(i, world);
                    return;
                }
                else if (slot.Stack > number)
                {
                    slot.Stack -= number;
                    this.SendSlot(i, world);
                    return;
                }
                else
                {
                    this.inventory[i] = null;
                    this.SendSlot(i, world);
                    number -= slot.Stack;
                }
            }
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
            string updateCharacter = P.UpdateCharacter(this.player);
            world.Send(this.player, updateCharacter);
            world.Send(this.player, P.StatusInfo(this.player));

            List<Player> range = this.player.Map.GetPlayersInRange(this.player);
            foreach (Player p in range)
            {
                world.Send(p, updateCharacter);
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
            id -= GameWorld.Settings.InventorySize;
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
            if (i > GameWorld.Settings.InventorySize &&
                i <= GameWorld.Settings.InventorySize + GameWorld.Settings.EquippedSize + 1)
            {
                return this.equipped[i - GameWorld.Settings.InventorySize - 1];
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
         * MountDisplay, returns mount display for use in MKC and CHP
         * 
         */
        public string MountDisplay()
        {
            string e = "";
            ItemSlot item = this.GetEquippedSlot(EquipSlots.Mount);
            if (item != null)
            {
                if (item.Item.GraphicA == 0)
                {
                    e += item.Item.GraphicEquipped + ",*";
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
                e += "0,*";
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
                    world.Send(this.player, P.EquipSlot(slot.Item, world, (int)equipslot, slot.Stack));
                }
                else
                {
                    world.Send(this.player, P.ClearEquipSlot((int)equipslot));
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
            foreach (ItemSlot slot in this.combineContainer)
            {
                if (slot != null && slot.Item.Template.ID == templateid) return true;
            }

            return false;
        }

        /// <summary>
        /// Returns true if inventory (only inventory, no combine bag/equipped) has at least number of templateid items
        /// </summary>
        /// <param name="templateid"></param>
        /// <param name="numberOf"></param>
        /// <returns></returns>
        public bool HasItem(int templateid, long numberOf)
        {
            long count = 0;
            foreach (ItemSlot slot in this.inventory)
            {
                if (slot != null && slot.Item.Template.ID == templateid && !slot.Item.Custom)
                {
                    count += slot.Stack;

                    if (count >= numberOf)
                        return true;
                }
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
            return weapon.Item.TotalWeaponDamage;
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
         */
        public void Save(GameWorld world)
        {
            var saveInventoryCommand = world.SqlConnection.CreateCommand();
            saveInventoryCommand.CommandText =
                @"INSERT INTO inventory (player_id, serialized_data) VALUES (@player_id, @serialized_data)
                  ON CONFLICT(player_id) DO UPDATE SET serialized_data=@serialized_data WHERE player_id=@player_id;";
            saveInventoryCommand.Parameters.Add(new SQLiteParameter("@player_id", DbType.Int32) { Value = this.player.PlayerID });
            saveInventoryCommand.Parameters.Add(new SQLiteParameter("@serialized_data", DbType.String) { Value = JsonConvert.SerializeObject(inventory, GameWorld.JsonSerializerSettings) });
            world.DatabaseWriter.Add(saveInventoryCommand);

            var saveEquippedCommand = world.SqlConnection.CreateCommand();
            saveEquippedCommand.CommandText =
                @"INSERT INTO equipped (player_id, serialized_data) VALUES (@player_id, @serialized_data)
                  ON CONFLICT(player_id) DO UPDATE SET serialized_data=@serialized_data WHERE player_id=@player_id;";
            saveEquippedCommand.Parameters.Add(new SQLiteParameter("@player_id", DbType.Int32) { Value = this.player.PlayerID });
            saveEquippedCommand.Parameters.Add(new SQLiteParameter("@serialized_data", DbType.String) { Value = JsonConvert.SerializeObject(equipped, GameWorld.JsonSerializerSettings) });
            world.DatabaseWriter.Add(saveEquippedCommand);

            var saveCombineBagCommand = world.SqlConnection.CreateCommand();
            saveCombineBagCommand.CommandText =
                @"INSERT INTO combinebag (player_id, serialized_data) VALUES (@player_id, @serialized_data)
                  ON CONFLICT(player_id) DO UPDATE SET serialized_data=@serialized_data WHERE player_id=@player_id;";
            saveCombineBagCommand.Parameters.Add(new SQLiteParameter("@player_id", DbType.Int32) { Value = this.player.PlayerID });
            saveCombineBagCommand.Parameters.Add(new SQLiteParameter("@serialized_data", DbType.String) { Value = JsonConvert.SerializeObject(combineContainer, GameWorld.JsonSerializerSettings) });
            world.DatabaseWriter.Add(saveCombineBagCommand);
        }

        /**
         * Load, loads from database
         * 
         */
        public void Load(GameWorld world)
        {
            using (var query = world.SqlConnection.CreateCommand())
            {
                query.CommandText = "SELECT serialized_data FROM inventory WHERE player_id=" + this.player.PlayerID;
                string serialized_data = Convert.ToString(query.ExecuteScalar());
                this.inventory = JsonConvert.DeserializeObject<ItemSlot[]>(serialized_data, GameWorld.JsonSerializerSettings);

                foreach (var invSlot in this.inventory)
                {
                    if (invSlot == null) continue;

                    world.ItemHandler.AddItem(invSlot.Item, world);

                    invSlot.Item.Template = world.ItemHandler.GetTemplate(invSlot.Item.TemplateID);
                    invSlot.Item.RefreshStats();
                }
            }

            using (var query = world.SqlConnection.CreateCommand())
            {
                query.CommandText = "SELECT serialized_data FROM equipped WHERE player_id=" + this.player.PlayerID;
                string serialized_data = Convert.ToString(query.ExecuteScalar());
                this.equipped = JsonConvert.DeserializeObject<ItemSlot[]>(serialized_data, GameWorld.JsonSerializerSettings);

                foreach (var equipSlot in equipped)
                {
                    if (equipSlot == null) continue;

                    world.ItemHandler.AddItem(equipSlot.Item, world);

                    equipSlot.Item.Template = world.ItemHandler.GetTemplate(equipSlot.Item.TemplateID);
                    equipSlot.Item.RefreshStats();

                    this.player.AddStats(equipSlot.Item.TotalStats, world);
                    if (equipSlot.Item.SpellEffect != null)
                    {
                        Buff buff = new Buff();
                        buff.Caster = this.player;
                        buff.Target = this.player;
                        buff.ItemBuff = true;
                        buff.SpellEffect = equipSlot.Item.SpellEffect;

                        this.player.AddBuff(buff, world, false);
                    }
                }
            }

            using (var query = world.SqlConnection.CreateCommand())
            {
                query.CommandText = "SELECT serialized_data FROM combinebag WHERE player_id=" + this.player.PlayerID;
                string serialized_data = Convert.ToString(query.ExecuteScalar());
                var combineSlots = JsonConvert.DeserializeObject<ItemSlot[]>(serialized_data, GameWorld.JsonSerializerSettings);

                for (int i = 0; i < combineSlots.Length; i++)
                {
                    var combineSlot = combineSlots[i];
                    if (combineSlot == null) continue;

                    world.ItemHandler.AddItem(combineSlot.Item, world);

                    combineSlot.Item.Template = world.ItemHandler.GetTemplate(combineSlot.Item.TemplateID);
                    combineSlot.Item.RefreshStats();

                    this.combineContainer.SetSlot(i, combineSlot);
                }
            }
        }

        /**
         * Combine, combines whatever is in combine bag if possible
         * 
         */
        public void Combine(Window combineBagWindow, GameWorld world)
        {
            Dictionary<int, int> combineHash = new Dictionary<int, int>();
            foreach (ItemSlot slot in this.combineContainer)
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
                world.Send(this.player, P.ServerMessage("Couldn't combine items."));
                return;
            }

            if (match.MinLevel > this.player.Level)
            {
                world.Send(this.player, P.ServerMessage("You need to be level " + match.MinLevel + " to create " +
                    match.Name + "."));
                return;
            }
            else if (match.MaxLevel > 0 && match.MaxLevel < this.player.Level)
            {
                world.Send(this.player, P.ServerMessage("You need to be less than level " + match.MaxLevel + " to create " +
                    match.Name + "."));
                return;
            }
            else if (match.MinExperience > this.player.Experience + this.player.ExperienceSold)
            {
                world.Send(this.player, P.ServerMessage("You need " + match.MinExperience + " experience to create " +
                    match.Name + "."));
                return;
            }
            else if (match.MaxExperience > 0 &&
                match.MaxExperience < this.player.Experience + this.player.ExperienceSold)
            {
                world.Send(this.player, P.ServerMessage("You need less than " + match.MaxExperience + 
                    " experience to create " + match.Name + "."));
                return;
            }
            else if ((match.ClassRestrictions & Convert.ToInt64(Math.Pow(2.0, (double)this.player.Class.ClassID))) != 0)
            {
                world.Send(this.player, P.ServerMessage("You are the wrong class to create " + match.Name + "."));
                return;
            }


            List<int> freeslots = new List<int>();
            var newcombine = new ItemContainer(GameWorld.Settings.CombineBagSize + 1);

            Dictionary<int, int> reqhash = new Dictionary<int, int>();
            foreach (KeyValuePair<int, int> req in match.RequiredHash)
            {
                reqhash[req.Key] = req.Value;
            }
            Item item;
            long count;
            long slotcount;
            for (int i = 1; i < this.combineContainer.MaxSlots; i++)
            {
                var slot = this.combineContainer.GetSlot(i);
                if (slot == null)
                {
                    freeslots.Add(i);
                    continue;
                }
                item = slot.Item;
                slotcount = slot.Stack;
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
                    var newSlot = new ItemSlot { Item = item, Stack = slotcount };
                    newcombine.SetSlot(i, newSlot);
                }
                else
                {
                    freeslots.Add(i);
                }
			}

            if (freeslots.Count < match.ResultItems.Count)
            {
                world.Send(this.player, P.ServerMessage("Not enough free slots to create " + match.Name + "."));
                return;
            }

            int index;
            foreach (ItemTemplate template in match.ResultItems) 
            {
                if (template.IsLore && this.player.HasItem(template.ID))
                {
                    world.Send(this.player, P.ServerMessage("Already have LORE item " + template.Name + "."));
                    return;
                }

                item = new Item();
                item.LoadFromTemplate(template);
                world.ItemHandler.RollTitleAndSurname(item, world);
                world.ItemHandler.AddAndAssignId(item, world);

                if (item.IsBindOnPickup) item.IsBound = true;

                index = freeslots[0];
                freeslots.RemoveAt(0);

                var newSlot = new ItemSlot { Item = item, Stack = 1 };
                newcombine.SetSlot(index, newSlot);

                world.Send(this.player, P.ServerMessage("Successfully created " + item.Name + "."));
            }

            for (int i = 1; i < this.combineContainer.MaxSlots; i++)
            {
                this.combineContainer.SetSlot(i, newcombine.GetSlot(i));
            }

            combineBagWindow.Refresh(player, world);
        }
    }
}
