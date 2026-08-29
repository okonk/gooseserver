using System.Collections;
using System.Text;
using System.Text.Json;
using System.Data;
using System.Data.SQLite;

namespace Goose
{
    /**
     * Inventory, handles a players inventory
     *
     */
    public class Inventory
    {
        private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

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

        ItemSlot?[] equipped;
        ItemSlot?[] inventory;
        ItemContainer combineContainer;
        /**
         * The player this inventory belongs to
         */
        Player player;
        private GooseSettings settings;

        public Inventory(Player player, GooseSettings settings)
        {
            // Inventory is numbered 1 to InventorySize.
            this.inventory = new ItemSlot[settings.InventorySize + 1];
            // Equipped is numbered 1 to EquippedSize.
            this.equipped = new ItemSlot[settings.EquippedSize + 1];
            // Combine is numbered 1 to CombineBagSize.
            this.combineContainer = new ItemContainer(settings.CombineBagSize + 1);
            this.player = player;
            this.settings = settings;
        }

        public ItemContainer GetCombineBagContainer()
        {
            return this.combineContainer;
        }

        public ItemSlot?[] GetEquippedSlots()
        {
            return this.equipped;
        }

        public ItemSlot?[] GetInventorySlots()
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
            for (int i = 1; i <= this.settings.InventorySize; i++)
            {
                ItemSlot slot = new ItemSlot();
                slot.Item = item;
                slot.Stack = stack;

                if (this.inventory[i] is null)
                {
                    this.inventory[i] = slot;

                    this.SendSlot(i, world);
                    return true;
                }
                else if (this.inventory[i]!.CanStack(slot))
                {
                    this.inventory[i]!.Stack += slot.Stack;
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
            if (i < 1 || i > this.settings.InventorySize) return;

            if (this.player.State >= Player.States.LoadingMap)
            {
                ItemSlot? slot = this.inventory[i];
                if (slot is not null)
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
            for (int i = 1; i <= this.settings.InventorySize; i++)
            {
                if (this.inventory[i] is null)
                    free++;
            }

            return free;
        }

        public int GetNextFreeSlot()
        {
            for (int i = 1; i <= this.settings.InventorySize; i++)
            {
                if (this.inventory[i] is null)
                    return i;
            }

            return -1;
        }

        /**
         * SendAll, sends all slots to player
         *
         */
        public void SendAll(GameWorld world)
        {
            for (int i = 1; i <= this.settings.InventorySize; i++)
            {
                this.SendSlot(i, world);
            }

            for (int i = 1; i <= this.settings.EquippedSize; i++)
            {
                this.SendEquippedSlot((EquipSlots)i, world);
            }
        }

        /**
         * GetSlot, returns slot i
         *
         */
        public ItemSlot? GetSlot(int i)
        {
            if (i > 0 && i <= this.settings.InventorySize)
            {
                return this.inventory[i];
            }

            // log bad slot id
            return null;
        }

        public void SetSlot(int i, ItemSlot? slot)
        {
            this.inventory[i] = slot;
        }

        /**
         * SwapSlots, swaps the 2 slots
         *
         */
        public void SwapSlots(int fromSlotId, int toSlotId, GameWorld world)
        {
            if (fromSlotId == toSlotId) return;

            ItemSlot? fromSlot = this.GetSlot(fromSlotId);
            ItemSlot? toSlot = this.GetSlot(toSlotId);

            ItemSlot.SwapSlots(ref fromSlot, ref toSlot);

            this.SetSlot(fromSlotId, fromSlot);
            this.SetSlot(toSlotId, toSlot);

            this.SendSlot(fromSlotId, world);
            this.SendSlot(toSlotId, world);
        }

        /**
         * SplitSlots, adds items from slot 1 to slot 2
         *
         */
        public void SplitSlots(int id1, int id2, int stackSize, GameWorld world)
        {
            if (id1 <= 0 || id1 > this.settings.InventorySize ||
                id2 <= 0 || id2 > this.settings.InventorySize)
            {
                // log id out of inventory range
                return;
            }

            ItemSlot? slot1 = this.GetSlot(id1);
            ItemSlot? slot2 = this.GetSlot(id2);

            if (slot1 is null) return;
            if (stackSize <= 0) return;
            if (stackSize > slot1.Stack) return;

            if (slot2 is null)
            {
                var newStack = new ItemSlot();
                newStack.Item = slot1.Item.CloneWithoutId();
                newStack.Stack = stackSize;

                if (newStack.Item.IsBindOnPickup) newStack.Item.IsBound = true;

                world.ItemHandler.AddAndAssignId(newStack.Item, world);
                this.inventory[id2] = newStack;
            }
            else if (slot2.CanStack(slot1, stackSize))
            {
                slot2.Stack += stackSize;
            }
            else
            {
                return;
            }

            if (stackSize == slot1.Stack)
                this.inventory[id1] = null;
            else
                slot1.Stack -= stackSize;

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
            ItemSlot? slot = this.GetSlot(id);
            if (slot is null) return;

            if (!this.player.Map.CanUseItems)
            {
                world.Send(this.player, P.HashMessage("You can't use items in this map."));
                return;
            }

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

            // Unequip any conflicting cross-slot items (2H weapon ↔ shield) before
            // touching the target slot. This logic was duplicated in the null/else
            // branches below; it runs once here regardless.
            if (!this.UnequipConflictingSlots(item, world)) return false;

            // Unequip whatever is currently in the target slot, if anything.
            if (this.GetEquippedSlot(equipslot) is not null)
            {
                if (!this.Unequip(equipslot, world)) return false;
            }

            // At this point we have unequipped everything necessary to equip the item, so go and do it

            // Remove 1 of the item from inventory
            ItemSlot? slot = this.RemoveItem(item, 1, world);
            // if slot is null something went wrong
            if (slot is null)
            {
                // log something here
                return false;
            }

            this.equipped[(int)equipslot] = slot;
            this.player.AddStats(slot.Item.TotalStats, world, updateCharacter: false);

            if (slot.Item.SpellEffect is not null)
            {
                Buff buff = new Buff();
                buff.Caster = this.player;
                buff.Target = this.player;
                buff.ItemBuff = true;
                buff.SpellEffect = slot.Item.SpellEffect;

                this.player.AddBuff(buff, world, true, updateCharacter: false);
            }

            this.SendEquippedSlot(equipslot, world);
            string updateCharacter = P.UpdateCharacter(this.player);
            world.Send(this.player, updateCharacter);
            world.Send(this.player, P.StatusInfo(this.player));
            world.Send(this.player, P.WeaponSpeed(this.player));

            List<Player> range = this.player.Map.GetPlayersInRange(this.player);
            foreach (var p in range)
            {
                world.Send(p, updateCharacter);
            }

            if (slot.Item.IsBindOnEquip)
            {
                slot.Item.IsBound = true;
            }

            return true;
        }

        /// <summary>
        /// Unequip any items that conflict with the given item before it can be equipped.
        /// E.g. a 2H weapon requires the shield slot to be free, and a shield requires
        /// the weapon slot to not hold a 2H weapon.
        /// Returns false if a conflicting item could not be unequipped.
        /// </summary>
        private bool UnequipConflictingSlots(Item item, GameWorld world)
        {
            // Equipping a 2H weapon requires the shield slot to be empty.
            if (item.Slot == ItemTemplate.ItemSlots.TwoHanded)
            {
                if (this.GetEquippedSlot(EquipSlots.Shield) is not null)
                {
                    if (!this.Unequip(EquipSlots.Shield, world))
                        return false;
                }
            }
            // Equipping a shield requires the weapon slot to not hold a 2H weapon.
            else if (item.Slot == ItemTemplate.ItemSlots.Shield)
            {
                ItemSlot? weapon = this.GetEquippedSlot(EquipSlots.Weapon);
                if (weapon is not null && weapon.Item.Slot == ItemTemplate.ItemSlots.TwoHanded)
                {
                    if (!this.Unequip(EquipSlots.Weapon, world))
                        return false;
                }
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
            foreach (var b in this.player.Buffs)
            {
                if (b.SpellEffect.EffectType == SpellEffect.EffectTypes.Stun)
                {
                    // stunned battletext
                    world.Send(this.player, P.BattleTextStunned(this.player));
                    return;
                }
            }

            foreach (var window in this.player.Windows)
            {
                if (window.Type == Window.WindowTypes.Vendor)
                {
                    world.Send(this.player, P.ServerMessage("You can't use items while with a vendor."));
                    return;
                }
            }

            bool remove = true;

            if (item.SpellEffect is not null && world.Random.Next(1, 100001) <= item.SpellEffectChance * 1000)
            {
                item.SpellEffect.Cast(this.player, this.player, world);
            }

            if (item.Script is not null)
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
        public ItemSlot? RemoveItem(Item item, long number, GameWorld world)
        {
            ItemSlot? slot;

            for (int i = 1; i <= this.settings.InventorySize; i++)
            {
                slot = this.inventory[i];

                if (slot is null) continue;
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
                    removed.Item = item.CloneWithoutId();
                    world.ItemHandler.AddAndAssignId(removed.Item, world);
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
            ItemSlot? slot;

            for (int i = 1; i <= this.settings.InventorySize; i++)
            {
                slot = this.inventory[i];

                if (slot is null) continue;
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
            ItemSlot? slot = this.GetEquippedSlot(equipslot);
            // maybe log something bad, i don't think this should happen
            if (slot is null) return true;

            if (!this.AddItem(slot.Item, slot.Stack, world)) return false;

            this.equipped[(int)equipslot] = null;
            this.player.RemoveStats(slot.Item.TotalStats, world);

            if (slot.Item.SpellEffect is not null)
            {
                Buff? remove = null;
                foreach (var buff in this.player.Buffs)
                {
                    if (buff.ItemBuff && buff.SpellEffect == slot.Item.SpellEffect)
                    {
                        remove = buff;
                        break;
                    }
                }

                if (remove is not null)
                {
                    this.player.RemoveBuff(remove, world, refreshbar: true);
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
            foreach (var p in range)
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
            id -= this.settings.InventorySize;
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
                    if (this.GetEquippedSlot(EquipSlots.Ring2) is null)
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
        public ItemSlot? GetEquippedSlot(EquipSlots slot)
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
        public ItemSlot? GetEquippedSlot(int i)
        {
            if (i > this.settings.InventorySize &&
                i <= this.settings.InventorySize + this.settings.EquippedSize + 1)
            {
                return this.equipped[i - this.settings.InventorySize - 1];
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
            ItemSlot? item;
            foreach (var eq in slots)
            {
                item = this.GetEquippedSlot(eq);
                if (item is not null)
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
            ItemSlot? item = this.GetEquippedSlot(EquipSlots.Mount);
            if (item is not null)
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
                ItemSlot? slot = this.equipped[(int)equipslot];
                if (slot is not null)
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
            foreach (var slot in this.inventory)
            {
                if (slot is not null && slot.Item.Template.ID == templateid) return true;
            }
            foreach (var slot in this.equipped)
            {
                if (slot is not null && slot.Item.Template.ID == templateid) return true;
            }
            foreach (var slot in this.combineContainer)
            {
                if (slot is not null && slot.Item.Template.ID == templateid) return true;
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
            foreach (var slot in this.inventory)
            {
                if (slot is not null && slot.Item.Template.ID == templateid && !slot.Item.Custom)
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
        public long GetWeaponDamage()
        {
            ItemSlot? weapon = this.GetEquippedSlot(EquipSlots.Weapon);
            if (weapon is null) return 1;
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
            ItemSlot? weapon = this.GetEquippedSlot(EquipSlots.Weapon);
            if (weapon is null) return 10;
            return weapon.Item.WeaponDelay;
        }

        /**
         * Save, saves to database
         *
         */
        public void Save(GameWorld world)
        {
            world.Database.Enqueue(this.BuildSave());
        }

        /**
         * BuildSave, snapshots state and returns the work to persist it
         *
         * Returned rather than enqueued so a full player save can run every part inside
         * one transaction. Serialization happens here, on the game thread, so later
         * mutations do not race the DB thread.
         *
         */
        public Action<SQLiteConnection> BuildSave()
        {
            int playerId = this.player.PlayerID;
            string inventoryJson = JsonHelper.Serialize(inventory);
            string equippedJson = JsonHelper.Serialize(equipped);
            string combineJson = JsonHelper.Serialize(combineContainer);

            return conn =>
            {
                using (var saveInventoryCommand = conn.CreateCommand())
                {
                    saveInventoryCommand.CommandText =
                        @"INSERT INTO inventory (player_id, serialized_data) VALUES (@player_id, @serialized_data)
                          ON CONFLICT(player_id) DO UPDATE SET serialized_data=@serialized_data WHERE player_id=@player_id;";
                    saveInventoryCommand.Parameters.Add(new SQLiteParameter("@player_id", DbType.Int32) { Value = playerId });
                    saveInventoryCommand.Parameters.Add(new SQLiteParameter("@serialized_data", DbType.String) { Value = inventoryJson });
                    saveInventoryCommand.ExecuteNonQuery();
                }

                using (var saveEquippedCommand = conn.CreateCommand())
                {
                    saveEquippedCommand.CommandText =
                        @"INSERT INTO equipped (player_id, serialized_data) VALUES (@player_id, @serialized_data)
                          ON CONFLICT(player_id) DO UPDATE SET serialized_data=@serialized_data WHERE player_id=@player_id;";
                    saveEquippedCommand.Parameters.Add(new SQLiteParameter("@player_id", DbType.Int32) { Value = playerId });
                    saveEquippedCommand.Parameters.Add(new SQLiteParameter("@serialized_data", DbType.String) { Value = equippedJson });
                    saveEquippedCommand.ExecuteNonQuery();
                }

                using var saveCombineBagCommand = conn.CreateCommand();
                saveCombineBagCommand.CommandText =
                    @"INSERT INTO combinebag (player_id, serialized_data) VALUES (@player_id, @serialized_data)
                      ON CONFLICT(player_id) DO UPDATE SET serialized_data=@serialized_data WHERE player_id=@player_id;";
                saveCombineBagCommand.Parameters.Add(new SQLiteParameter("@player_id", DbType.Int32) { Value = playerId });
                saveCombineBagCommand.Parameters.Add(new SQLiteParameter("@serialized_data", DbType.String) { Value = combineJson });
                saveCombineBagCommand.ExecuteNonQuery();
            };
        }

        /**
         * Load, loads from database
         *
         */
        public void Load(GameWorld world)
        {
            int playerId = this.player.PlayerID;
            world.Database.Execute(conn =>
            {
                this.inventory = this.LoadSlots(conn, playerId, "inventory", this.settings.InventorySize + 1);
                for (int i = 0; i < this.inventory.Length; i++)
                {
                    ItemSlot? invSlot = this.inventory[i];
                    if (invSlot is null) continue;

                    if (invSlot.Item is null)
                    {
                        log.Error("player {0}: slot with null item discarded", playerId);
                        this.inventory[i] = null;
                        continue;
                    }

                    ItemTemplate? template = world.ItemHandler.GetTemplate(invSlot.Item.TemplateID);
                    if (template is null)
                    {
                        log.Error("player {0}: item template {1} not found; slot discarded", playerId, invSlot.Item.TemplateID);
                        this.inventory[i] = null;
                        continue;
                    }

                    world.ItemHandler.AddItem(invSlot.Item, world);
                    invSlot.Item.Template = template;
                    invSlot.Item.RefreshStats();
                }

                this.equipped = this.LoadSlots(conn, playerId, "equipped", this.settings.EquippedSize + 1);
                for (int i = 0; i < this.equipped.Length; i++)
                {
                    ItemSlot? equipSlot = this.equipped[i];
                    if (equipSlot is null) continue;

                    if (equipSlot.Item is null)
                    {
                        log.Error("player {0}: slot with null item discarded", playerId);
                        this.equipped[i] = null;
                        continue;
                    }

                    ItemTemplate? template = world.ItemHandler.GetTemplate(equipSlot.Item.TemplateID);
                    if (template is null)
                    {
                        log.Error("player {0}: item template {1} not found; slot discarded", playerId, equipSlot.Item.TemplateID);
                        this.equipped[i] = null;
                        continue;
                    }

                    world.ItemHandler.AddItem(equipSlot.Item, world);
                    equipSlot.Item.Template = template;
                    equipSlot.Item.RefreshStats();

                    this.player.AddStats(equipSlot.Item.TotalStats, world);
                    if (equipSlot.Item.SpellEffect is not null)
                    {
                        Buff buff = new Buff();
                        buff.Caster = this.player;
                        buff.Target = this.player;
                        buff.ItemBuff = true;
                        buff.SpellEffect = equipSlot.Item.SpellEffect;

                        this.player.AddBuff(buff, world, false);
                    }
                }

                var combineSlots = this.LoadSlots(conn, playerId, "combinebag", this.settings.CombineBagSize + 1);
                for (int i = 0; i < combineSlots.Length; i++)
                {
                    var combineSlot = combineSlots[i];
                    if (combineSlot is null) continue;

                    if (combineSlot.Item is null)
                    {
                        log.Error("player {0}: slot with null item discarded", playerId);
                        continue;
                    }

                    ItemTemplate? template = world.ItemHandler.GetTemplate(combineSlot.Item.TemplateID);
                    if (template is null)
                    {
                        log.Error("player {0}: item template {1} not found; slot discarded", playerId, combineSlot.Item.TemplateID);
                        continue;
                    }

                    world.ItemHandler.AddItem(combineSlot.Item, world);
                    combineSlot.Item.Template = template;
                    combineSlot.Item.RefreshStats();

                    this.combineContainer.SetSlot(i, combineSlot);
                }
            });
        }

        private ItemSlot?[] LoadSlots(SQLiteConnection conn, int playerId, string table, int size)
        {
            using var query = conn.CreateCommand();
            query.CommandText = "SELECT serialized_data FROM " + table + " WHERE player_id=" + playerId;
            string? raw = Convert.ToString(query.ExecuteScalar());
            ItemSlot?[]? data = null;
            if (!string.IsNullOrEmpty(raw))
            {
                try
                {
                    data = JsonHelper.Deserialize<ItemSlot?[]>(raw);
                }
                catch (JsonException e)
                {
                    log.Error("player {0}: {1} blob is corrupt; starting empty", playerId, table, e);
                }
            }

            if (data is null)
            {
                log.Warn("player {0}: no {1} row; starting empty", playerId, table);
                return new ItemSlot[size];
            }

            var normalized = new ItemSlot?[size];
            for (int i = 0; i < Math.Min(data.Length, size); i++)
                normalized[i] = data[i];
            if (data.Length != size)
                log.Warn("player {0}: {1} blob has {2} slots, expected {3}", playerId, table, data.Length, size);
            return normalized;
        }

        /**
         * Combine, combines whatever is in combine bag if possible
         *
         */
        public void Combine(Window combineBagWindow, GameWorld world)
        {
            // Count the actual quantity of each ingredient, not the number of slots it
            // occupies. The consumption loop below works in stack quantities, so matching
            // on slot counts let a single slot satisfy a requirement for several items.
            Dictionary<int, long> combineHash = [];
            foreach (var slot in this.combineContainer)
            {
                if (slot is null) continue;
                if (slot.Stack <= 0) continue;

                combineHash.TryGetValue(slot.Item.TemplateID, out long have);
                combineHash[slot.Item.TemplateID] = have + slot.Stack;
            }

            Combination? match = world.CombinationHandler.GetMatch(combineHash);
            if (match is null)
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
            else if (!this.player.Class.CanUse(match.ClassRestrictions))
            {
                world.Send(this.player, P.ServerMessage("You are the wrong class to create " + match.Name + "."));
                return;
            }


            List<int> freeslots = [];
            var newcombine = new ItemContainer(this.settings.CombineBagSize + 1);

            Dictionary<int, int> reqhash = [];
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
                if (slot is null)
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
                    // Lower the outstanding requirement by what this slot supplies. This
                    // can go negative, which just means the requirement is now satisfied;
                    // GetMatch has already guaranteed the bag holds enough in total.
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
            foreach (var template in match.ResultItems)
            {
                if (template.IsLore && this.player.HasItem(template.ID))
                {
                    world.Send(this.player, P.ServerMessage("Already have LORE item " + template.Name + "."));
                    return;
                }

                item = new Item();
                if (!item.LoadFromTemplate(template))
                {
                    log.Error("combine result item {0} failed to load; skipped", template.ID);
                    continue;
                }
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
