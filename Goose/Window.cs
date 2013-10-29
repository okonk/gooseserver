using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose
{
    /**
     * Window
     * 
     * Holds information about a window in game
     * 
     */
    public class Window
    {
        public enum ButtonTypes
        {
            Exit = 0,
            Combine,
            Close,
            Three,
            Four,
            Five
        }

        public enum WindowFrames
        {
            Vendor = 13,
            ItemInfo = 22,
            EightSlot = 18,
            TenSlot = 19
        }
        public WindowFrames Frame { get; set; }
        public enum WindowTypes
        {
            Vendor = 1,
            ItemInfo,
            CharInfo,
            Rank,
            CombineBag,
            PetInfo
        }
        public WindowTypes Type { get; set; }

        public string Title { get; set; }
        /**
         * Buttons
         * Seems to be 5 comma separated values
         * one for each button type
         * 
         * eg vendor window is 0,1,0,0,0
         * The 1 making a close button on the window
         * 
         */
        public string Buttons { get; set; }

        public NPC NPC { get; set; }

        public int ID { get; set; }

        public Object Data { get; set; }

        /**
         * Create, creates window on player's screen
         * 
         */
        public void Create(Player player, GameWorld world)
        {
            this.ID = ++player.LastWindowID;

            switch (this.Type)
            {
                case WindowTypes.Vendor:
                    this.Frame = WindowFrames.Vendor;
                    break;
                case WindowTypes.Rank:
                case WindowTypes.CharInfo:
                case WindowTypes.ItemInfo:
                case WindowTypes.PetInfo:
                    this.Frame = WindowFrames.ItemInfo;
                    break;
                case WindowTypes.CombineBag:
                    this.Frame = WindowFrames.TenSlot;
                    this.ID = 22; // combine has to be id 22
                    break;
            }

            string create = "MKW" +
                            this.ID + "," +
                            (int)this.Frame + "," +
                            this.Title + "," +
                            this.Buttons + ",";

            switch (this.Type)
            {
                case WindowTypes.Vendor:
                    create += this.NPC.LoginID + "," +
                              "0,0";
                    break;
                case WindowTypes.Rank:
                case WindowTypes.CharInfo:
                case WindowTypes.ItemInfo:
                case WindowTypes.PetInfo:
                    create += "0,0,0";
                    break;
                case WindowTypes.CombineBag:
                    create += "0,0,0";
                    break;
            }

            world.Send(player, create);
            this.Populate(player, world);
            world.Send(player, "ENW" + this.ID);
        }

        /**
         * Populate, initially populates the window
         * 
         */
        public void Populate(Player player, GameWorld world)
        {
            switch (this.Type)
            {
                case WindowTypes.Vendor:
                    // start at 0 since first slot is always null
                    int i = 0;
                    foreach (NPCVendorSlot slot in this.NPC.VendorItems)
                    {
                        if (slot == null)
                        {
                            i++;
                            continue;
                        }

                        world.Send(player, "WNF" + this.ID + "," + i + "," + slot.ItemTemplate.Name +
                            (slot.Stack > 1 ? " (" + slot.Stack + ")" : "") + "|" + 0 + "|" +
                            slot.ItemTemplate.ID + "|" + slot.ItemTemplate.GraphicTile + "|*");
                        i++;
                    }
                    break;
                case WindowTypes.ItemInfo:
                    this.PopulateItemInfo(player, world);
                    break;
                case WindowTypes.CombineBag:
                    player.Inventory.SendCombineBag(world);
                    break;
                case WindowTypes.CharInfo:
                    this.PopulateCharInfo(player, world);
                    break;
                case WindowTypes.Rank:
                    this.PopulateRanks(player, world);
                    break;
                case WindowTypes.PetInfo:
                    this.PopulatePetInfo(player, world);
                    break;
            }
        }

        /**
         * Refresh, repopulates the window
         * 
         */
        public void Refresh(Player player, GameWorld world)
        {
            switch (this.Type)
            {
                case WindowTypes.Vendor:
                    // start at 0 since first slot is always null
                    int i = 0;
                    foreach (NPCVendorSlot slot in this.NPC.VendorItems)
                    {
                        if (slot == null)
                        {
                            world.Send(player, "WNF" + this.ID + "," + i + ", |0|0|0|*");
                        }
                        else
                        {
                            world.Send(player, "WNF" + this.ID + "," + i + "," + slot.ItemTemplate.Name +
                                (slot.Stack > 1 ? " (" + slot.Stack + ")" : "") + "|" +
                                0 + "|" +
                                slot.ItemTemplate.ID + "|" + slot.ItemTemplate.GraphicTile + "|*");
                        }
                        i++;
                    }

                    while (i <= GameSettings.Default.VendorSlotSize)
                    {
                        world.Send(player, "WNF" + this.ID + "," + i + ", |0|0|0|*");
                        i++;
                    }

                    break;
                case WindowTypes.ItemInfo:
                    this.PopulateItemInfo(player, world);
                    break;
                case WindowTypes.CombineBag:
                    player.Inventory.SendCombineBag(world);
                    break;
                case WindowTypes.CharInfo:
                    this.PopulateCharInfo(player, world);
                    break;
                case WindowTypes.Rank:
                    this.PopulateRanks(player, world);
                    break;
                case WindowTypes.PetInfo:
                    this.PopulatePetInfo(player, world);
                    break;
            }
        }

        /**
         * PopulateItemInfo, populates item info window
         * 
         */
        public void PopulateItemInfo(Player player, GameWorld world)
        {
            IItem item = null;

            if (this.Data is Item)
            {
                item = (Item)this.Data;
            }
            else
            {
                item = (ItemTemplate)this.Data;
            }

            string line = "WNF" + this.ID + "," + 1 + ",";
            if (item.IsLore) line += "LORE ";
            if (item.IsEvent) line += "EVENT ";
            if (item.IsBindOnPickup) line += "BOP ";
            if (item.IsBindOnEquip) line += "BOE ";
            if (item is Item && ((Item)item).IsBound) line += "BOUND ";
            line += "|0|0|0|*";
            world.Send(player, line);

            line = "WNF" + this.ID + "," + 2 + "," + item.Description + "|0|0|0|*";
            world.Send(player, line);

            if (item.UseType == ItemTemplate.UseTypes.Armor || item.UseType == ItemTemplate.UseTypes.Weapon)
            {
                if (item.Slot == ItemTemplate.ItemSlots.OneHanded ||
                    item.Slot == ItemTemplate.ItemSlots.TwoHanded)
                {
                    line = "WNF" + this.ID + "," + 3 + ",";
                    line += "DMG: " + item.WeaponDamage;
                    line += " DLY: " + item.WeaponDelay;
                    line += " " + item.Type.ToString();
                    line += "|0|0|0|*";

                    world.Send(player, line);
                }
                else
                {
                    line = "WNF" + this.ID + "," + 3 + ",";
                    line += "Type: " + item.Type.ToString();
                    line += "|0|0|0|*";

                    world.Send(player, line);
                }
            }
            else
            {
                world.Send(player, "WNF" + this.ID + "," + 3 + ",|0|0|0|*");
            }

            AttributeSet stats = null;
            if (item is Item) stats = ((Item)item).TotalStats;
            else stats = item.BaseStats;

            line = "WNF" + this.ID + "," + 4 + ",";
            if (stats.AC != 0) line += "AC: " + stats.AC + " ";
            if (stats.HP != 0) line += "HP: " + stats.HP + " ";
            if (stats.MP != 0) line += "MP: " + stats.MP + " ";
            if (stats.SP != 0) line += "SP: " + stats.SP + " ";
            line += "|0|0|0|*";
            world.Send(player, line);

            line = "WNF" + this.ID + "," + 5 + ",";
            if (stats.Strength != 0) line += "STR: " + stats.Strength + " ";
            if (stats.Stamina != 0) line += "STA: " + stats.Stamina + " ";
            if (stats.Intelligence != 0) line += "INT: " + stats.Intelligence + " ";
            if (stats.Dexterity != 0) line += "DEX: " + stats.Dexterity + " ";
            line += "|0|0|0|*";
            world.Send(player, line);

            line = "WNF" + this.ID + "," + 6 + ",";
            if (stats.FireResist != 0) line += "FR: " + stats.FireResist + " ";
            if (stats.AirResist != 0) line += "AR: " + stats.AirResist + " ";
            if (stats.EarthResist != 0) line += "ER: " + stats.EarthResist + " ";
            if (stats.WaterResist != 0) line += "WR: " + stats.WaterResist + " ";
            if (stats.SpiritResist != 0) line += "SR: " + stats.SpiritResist + " ";
            line += "|0|0|0|*";
            world.Send(player, line);

            line = "WNF" + this.ID + "," + 7 + ",";
            if (item.MinLevel >= 1) line += "Level " + item.MinLevel + " ";

            /* This part will probably need changing later */
            for (int i = 1; i <= world.ClassHandler.Count; i++)
            {
                Class c = world.ClassHandler.GetClass(i);

                if ((item.ClassRestrictions & (int)Math.Pow(2, c.ClassID)) == 0)
                {
                    line += c.ClassName + " ";
                }
            }

            line += "|0|0|0|*";
            world.Send(player, line);

            line = "WNF" + this.ID + "," + 8 + ",";
            if (item.MinExperience > 0)
            {
                line += "Sold: " + item.MinExperience;
            }
            line += "|0|0|0|*";
            world.Send(player, line);

            line = "WNF" + this.ID + "," + 9 + ",";
            if (item.SpellEffect != null)
            {
                line += "Effect: " + item.SpellEffect.Name;
                line += " (" + (int)Math.Round(item.SpellEffectChance, 0) + "%)";
            }
            line += "|0|0|0|*";
            world.Send(player, line);

            line = "WNF" + this.ID + "," + 10 + ",";
            if (item.UseType == ItemTemplate.UseTypes.Armor || item.UseType == ItemTemplate.UseTypes.Weapon)
            {
                line += "Slot: " + item.Slot.ToString() + " ";
            }
            line += "Value: " + item.Value;
            if (item.Credits > 0) line += "g / " + item.Credits + "cr";
            line += "|0|0|0|*";
            world.Send(player, line);
        }

        /**
         * InventoryToWindow, handles inventory to window event
         * 
         * 
         */
        public void InventoryToWindow(Player player, int invslot, int toslot, GameWorld world)
        {
            switch (this.Type)
            {
                case WindowTypes.CombineBag:
                    player.Inventory.SwapInventoryCombineSlots(invslot, toslot, world);

                    break;
            }
        }

        /**
         * WindowToInventory, handles window to inventory event
         * 
         * 
         */
        public void WindowToInventory(Player player, int fromslot, int invslot, GameWorld world)
        {
            switch (this.Type)
            {
                case WindowTypes.CombineBag:
                    player.Inventory.SwapInventoryCombineSlots(invslot, fromslot, world);

                    break;
            }
        }

        /**
         * PopulateCharInfo, populates character info window
         * 
         */
        public void PopulateCharInfo(Player player, GameWorld world)
        {
            string line;
            // Experience sold, hp, mp
            line = "WNF" + this.ID + "," + 1 + "," +
                "Experience Sold: " + player.ExperienceSold +
                "|0|0|0|*";
            world.Send(player, line);
            // Bound map, x, y
            line = "WNF" + this.ID + "," + 2 + "," +
                "Bound: " + player.BoundMap.Name + " (" + player.BoundX + "," + player.BoundY + ")" +
                "|0|0|0|*";
            world.Send(player, line);
            // HP regen
            line = "WNF" + this.ID + "," + 3 + "," +
                "HP Regeneration: " + Math.Round(player.MaxStats.HPPercentRegen * 100, 0) + "%" +
                "|0|0|0|*";
            world.Send(player, line);
            // MP Regen
            line = "WNF" + this.ID + "," + 4 + "," +
                "MP Regeneration: " + Math.Round(player.MaxStats.MPPercentRegen * 100, 0) + "%" +
                "|0|0|0|*";
            world.Send(player, line);
            // SD
            line = "WNF" + this.ID + "," + 5 + "," +
                "Spell Damage Increase: " + Math.Round(player.MaxStats.SpellDamage * 100, 0) + "%" +
                "|0|0|0|*";
            world.Send(player, line);
            // SC
            line = "WNF" + this.ID + "," + 6 + "," +
                "Spell Critical Chance: " + Math.Round(player.MaxStats.SpellCrit * 100, 0) + "%" +
                "|0|0|0|*";
            world.Send(player, line);
            // MD
            line = "WNF" + this.ID + "," + 7 + "," +
                "Melee Damage Increase: " + Math.Round(player.MaxStats.MeleeDamage * 100, 0) + "%" +
                "|0|0|0|*";
            world.Send(player, line);
            // MC
            line = "WNF" + this.ID + "," + 8 + "," +
                "Melee Critical Chance: " + Math.Round(player.MaxStats.MeleeCrit * 100, 0) + "%" +
                "|0|0|0|*";
            world.Send(player, line);
            // Haste
            line = "WNF" + this.ID + "," + 9 + "," +
                "Haste: " + Math.Round(player.MaxStats.Haste * 100, 0) + "%" +
                "|0|0|0|*";
            world.Send(player, line);
            // Damage reduce
            line = "WNF" + this.ID + "," + 10 + "," +
                "Damage Reduction: " + Math.Round(player.MaxStats.DamageReduction * 100, 0) + "%" +
                "|0|0|0|*";
            world.Send(player, line);
        }

        /**
         * Ranks window
         * 
         */
        public void PopulateRanks(Player player, GameWorld world)
        {
            Ranks rank = (Ranks)this.Data;
            int i = 1;

            foreach (string line in rank.GetRanks(world))
            {
                world.Send(player, "WNF" + this.ID + "," + i + "," + line + "|0|0|0|*");
                i++;
            }
        }

        /**
         * PopulatePetInfo, populates pet info window
         * 
         */
        public void PopulatePetInfo(Player player, GameWorld world)
        {
            Pet pet = (Pet)this.Data;

            string line;
            // Name
            line = "WNF" + this.ID + "," + 1 + "," +
                "Name: " + pet.Name +
                "|0|0|0|*";
            world.Send(player, line);
            // Experience
            line = "WNF" + this.ID + "," + 2 + "," +
                "Experience: " + pet.Experience +
                "|0|0|0|*";
            world.Send(player, line);
            // if max level show experience sold, else show tnl
            if (pet.Class.GetLevel(pet.Level).Experience == 0)
            {
                // Experience Sold
                line = "WNF" + this.ID + "," + 3 + "," +
                    "Experience Sold: " + pet.ExperienceSold +
                    "|0|0|0|*";
                world.Send(player, line);
            }
            else
            {
                // TNL
                line = "WNF" + this.ID + "," + 3 + "," +
                    "Next Level: " + (pet.Class.GetLevel(pet.Level).Experience - pet.Experience) +
                    "|0|0|0|*";
                world.Send(player, line);
            }
            // Level
            line = "WNF" + this.ID + "," + 4 + "," +
                "Level: " + pet.Level +
                "|0|0|0|*";
            world.Send(player, line);
            // Weapon Damage
            line = "WNF" + this.ID + "," + 5 + "," +
                "Weapon Damage: " + pet.WeaponDamage +
                "|0|0|0|*";
            world.Send(player, line);
            // Attack Speed
            line = "WNF" + this.ID + "," + 6 + "," +
                "Attack Speed: " + Math.Round(pet.AttackSpeed, 2) +
                "|0|0|0|*";
            world.Send(player, line);
            // Move Speed
            line = "WNF" + this.ID + "," + 7 + "," +
                "Move Speed: " + Math.Round(pet.MoveSpeed, 2) +
                "|0|0|0|*";
            world.Send(player, line);
            // Bought HP
            line = "WNF" + this.ID + "," + 8 + "," +
                "Extra HP: " + pet.BaseStats.HP +
                "|0|0|0|*";
            world.Send(player, line);

            /*
            // MD
            line = "WNF" + this.ID + "," + 7 + "," +
                "Melee Damage Increase: " + Math.Round(player.MaxStats.MeleeDamage * 100,0) + "%" +
                "|0|0|0|*";
            world.Send(player, line);
            // MC
            line = "WNF" + this.ID + "," + 8 + "," +
                "Melee Critical Chance: " + Math.Round(player.MaxStats.MeleeCrit * 100,0) + "%" +
                "|0|0|0|*";
            world.Send(player, line);
            // Haste
            line = "WNF" + this.ID + "," + 9 + "," +
                "Haste: " + Math.Round(player.MaxStats.Haste * 100,0) + "%" +
                "|0|0|0|*";
            world.Send(player, line);
            // Damage reduce
            line = "WNF" + this.ID + "," + 10 + "," +
                "Damage Reduction: " + Math.Round(player.MaxStats.DamageReduction * 100, 0) + "%" +
                "|0|0|0|*";
            world.Send(player, line);
             */
        }
    }
}
