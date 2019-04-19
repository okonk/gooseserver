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
            Back,
            Next,
            ShowOk
        }
        

        public enum WindowFrames
        {
            Vendor = 13,
            GenericInfo = 22,
            EightSlot = 18,
            TenSlot = 19, // used for combine
            Quest = 20,// quest window can show 20 lines, of 50 characters each
            Bank = 26,
        }
        public WindowFrames Frame { get; set; }
        public enum WindowTypes
        {
            Vendor = 1,
            CharInfo,
            Rank,
            CombineBag,
            PetInfo,
            Bank,
            Quest,
            SpellInfo,
            MacroCheck,
            PlayerInfo
        }
        public WindowTypes Type { get; set; }

        public virtual string Title { get; set; }
        /**
         * Buttons
         * Seems to be 5 comma separated values
         * one for each button type
         * 
         * eg vendor window is 0,1,0,0,0
         * The 1 making a close button on the window
         *
         * showCombine,showClose,showBack,showNext,showOK 
         */
        public virtual string Buttons { get; set; }

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
                case WindowTypes.PetInfo:
                    this.Frame = WindowFrames.GenericInfo;
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
                case WindowTypes.PetInfo:
                    create += "0,0,0";
                    break;
            }

            world.Send(player, create);
            this.Populate(player, world);
            world.Send(player, "ENW" + this.ID);
        }

        public virtual void Clicked(ButtonTypes buttonid, int npcid, int id2, int id3, Player player, GameWorld world)
        {
            switch (buttonid)
            {
                case Window.ButtonTypes.Exit:
                case Window.ButtonTypes.Close:
                    switch (this.Type)
                    {
                        case Window.WindowTypes.Vendor:
                            this.NPC.CloseVendorWindow(this, player, world);
                            break;
                        case Window.WindowTypes.Bank:
                        case Window.WindowTypes.CombineBag:
                            player.Windows.Remove(this);
                            break;
                        case Window.WindowTypes.CharInfo:
                        case Window.WindowTypes.Rank:
                        case Window.WindowTypes.PetInfo:
                            player.Windows.Remove(this);
                            break;
                    }
                    break;
                case Window.ButtonTypes.Combine:
                    switch (this.Type)
                    {
                        case Window.WindowTypes.CombineBag:
                            player.Inventory.Combine(this, world);
                            break;
                    }
                    break;
            }
        }


        /**
         * Populate, initially populates the window
         * 
         */
        public virtual void Populate(Player player, GameWorld world)
        {
            switch (this.Type)
            {
                case WindowTypes.Vendor:
                    // clear vendor window
                    world.Send(player, "VCL");
                    // start at 0 since first slot is always null
                    int i = 0;
                    foreach (NPCVendorSlot slot in this.NPC.VendorItems)
                    {
                        if (slot == null)
                        {
                            i++;
                            continue;
                        }

                        world.Send(player, "SVS" + slot.ItemTemplate.GetSlotPacket(world, i, 1));
                        i++;
                    }
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
        public virtual void Refresh(Player player, GameWorld world)
        {
            switch (this.Type)
            {
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

        public virtual void InventoryToWindow(Player player, int invslot, int toslot, GameWorld world)
        {

        }

        public virtual void WindowToInventory(Player player, int fromslot, int invslot, GameWorld world)
        {

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
                "Experience Sold: " + string.Format("{0:N0}", player.ExperienceSold) +
                "|0|0|0|0|*";
            world.Send(player, line);
            // Bound map, x, y
            line = "WNF" + this.ID + "," + 2 + "," +
                "Bound: " + player.BoundMap.Name + " (" + player.BoundX + "," + player.BoundY + ")" +
                "|0|0|0|0|*";
            world.Send(player, line);
            // HP regen
            line = "WNF" + this.ID + "," + 3 + "," +
                "HP Regeneration: " + Math.Round(player.MaxStats.HPPercentRegen * 100, 0) + "%" + string.Format(" +{0:N0}", player.MaxStats.HPStaticRegen) +
                "|0|0|0|0|*";
            world.Send(player, line);
            // MP Regen
            line = "WNF" + this.ID + "," + 4 + "," +
                "MP Regeneration: " + Math.Round(player.MaxStats.MPPercentRegen * 100, 0) + "%" + string.Format(" +{0:N0}", player.MaxStats.MPStaticRegen) +
                "|0|0|0|0|*";
            world.Send(player, line);
            // SD
            line = "WNF" + this.ID + "," + 5 + "," +
                "Spell Damage Increase: " + Math.Round(player.MaxStats.SpellDamage * 100, 0) + "%" +
                "|0|0|0|0|*";
            world.Send(player, line);
            // SC
            line = "WNF" + this.ID + "," + 6 + "," +
                "Spell Critical Chance: " + Math.Round(player.MaxStats.SpellCrit * 100, 0) + "%" +
                "|0|0|0|0|*";
            world.Send(player, line);
            // MD
            line = "WNF" + this.ID + "," + 7 + "," +
                "Melee Damage Increase: " + Math.Round(player.MaxStats.MeleeDamage * 100, 0) + "%" +
                "|0|0|0|0|*";
            world.Send(player, line);
            // MC
            line = "WNF" + this.ID + "," + 8 + "," +
                "Melee Critical Chance: " + Math.Round(player.MaxStats.MeleeCrit * 100, 0) + "%" +
                "|0|0|0|0|*";
            world.Send(player, line);
            // Haste
            line = "WNF" + this.ID + "," + 9 + "," +
                "Haste: " + Math.Round(player.MaxStats.Haste * 100, 0) + "%" +
                "|0|0|0|0|*";
            world.Send(player, line);
            // Damage reduce
            line = "WNF" + this.ID + "," + 10 + "," +
                "Damage Reduction: " + Math.Round(player.MaxStats.DamageReduction * 100, 0) + "%" +
                "|0|0|0|0|*";
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
                world.Send(player, "WNF" + this.ID + "," + i + "," + line + "|0|0|0|0|*");
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
                "|0|0|0|0|*";
            world.Send(player, line);
            // Experience
            line = "WNF" + this.ID + "," + 2 + "," +
                "Experience: " + pet.Experience +
                "|0|0|0|0|*";
            world.Send(player, line);
            // if max level show experience sold, else show tnl
            if (pet.Class.GetLevel(pet.Level).Experience == 0)
            {
                // Experience Sold
                line = "WNF" + this.ID + "," + 3 + "," +
                    "Experience Sold: " + pet.ExperienceSold +
                    "|0|0|0|0|*";
                world.Send(player, line);
            }
            else
            {
                // TNL
                line = "WNF" + this.ID + "," + 3 + "," +
                    "Next Level: " + (pet.Class.GetLevel(pet.Level).Experience - pet.Experience) +
                    "|0|0|0|0|*";
                world.Send(player, line);
            }
            // Level
            line = "WNF" + this.ID + "," + 4 + "," +
                "Level: " + pet.Level +
                "|0|0|0|0|*";
            world.Send(player, line);
            // Weapon Damage
            line = "WNF" + this.ID + "," + 5 + "," +
                "Weapon Damage: " + pet.WeaponDamage +
                "|0|0|0|0|*";
            world.Send(player, line);
            // Attack Speed
            line = "WNF" + this.ID + "," + 6 + "," +
                "Attack Speed: " + Math.Round(pet.AttackSpeed, 2) +
                "|0|0|0|0|*";
            world.Send(player, line);
            // Move Speed
            line = "WNF" + this.ID + "," + 7 + "," +
                "Move Speed: " + Math.Round(pet.MoveSpeed, 2) +
                "|0|0|0|0|*";
            world.Send(player, line);
            // Bought HP
            line = "WNF" + this.ID + "," + 8 + "," +
                "Extra HP: " + pet.BaseStats.HP +
                "|0|0|0|0|*";
            world.Send(player, line);

            /*
            // MD
            line = "WNF" + this.ID + "," + 7 + "," +
                "Melee Damage Increase: " + Math.Round(player.MaxStats.MeleeDamage * 100,0) + "%" +
                "|0|0|0|0|*";
            world.Send(player, line);
            // MC
            line = "WNF" + this.ID + "," + 8 + "," +
                "Melee Critical Chance: " + Math.Round(player.MaxStats.MeleeCrit * 100,0) + "%" +
                "|0|0|0|0|*";
            world.Send(player, line);
            // Haste
            line = "WNF" + this.ID + "," + 9 + "," +
                "Haste: " + Math.Round(player.MaxStats.Haste * 100,0) + "%" +
                "|0|0|0|0|*";
            world.Send(player, line);
            // Damage reduce
            line = "WNF" + this.ID + "," + 10 + "," +
                "Damage Reduction: " + Math.Round(player.MaxStats.DamageReduction * 100, 0) + "%" +
                "|0|0|0|0|*";
            world.Send(player, line);
             */
        }

        public void SendCreate(Player player, GameWorld world)
        {
            world.Send(player, this.MKWString());
            this.Populate(player, world);
            world.Send(player, "ENW" + this.ID);
        }

        public string MKWString()
        {
            int npcLoginId = this.NPC == null ? 0 : this.NPC.LoginID;

            return string.Format("MKW{0},{1},{2},{3},{4},{5},{6}",
                this.ID, (int)this.Frame, this.Title, this.Buttons, npcLoginId, 0, 0);
        }
    }
}
