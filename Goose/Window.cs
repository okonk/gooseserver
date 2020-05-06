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
            Toolbar = 1,
            Inventory = 2,
            Spellbook = 3,
            Hotbar = 4,
            Buffbar = 5,
            FPS = 6,
            HP = 7,
            MP = 8,
            SP = 9,
            XP = 10,
            Equipped = 11,
            Chat = 12,
            Vendor = 13,
            Party = 14,
            TwoSlot = 15,
            FourSlot = 16,
            SixSlot = 17,
            EightSlot = 18,
            TenSlot = 19,
            Quest = 20,
            Quest2 = 21, // Dunno what this is for
            GenericInfo = 22,
            DiscardButton = 23,
            Paper = 24,
            Trade = 25,
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
            PlayerInfo,
            ItemInfo,
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

            world.Send(player, P.MakeWindow(this));
            this.Populate(player, world);
            world.Send(player, P.EndWindow(this));
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
                    world.Send(player, P.ClearVendor());
                    
                    for (int i = 1; i < this.NPC.VendorItems.Length; i++)
                    {
                        var slot = this.NPC.VendorItems[i];
                        if (slot == null) continue;

                        world.Send(player, P.VendorSlot(this, slot.ItemTemplate, world, i, 1));
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
            int i = 1;
            string line;

            line = P.WindowTextLine(this.ID, i++, string.Format("Experience Sold: {0:N0}", player.ExperienceSold));
            world.Send(player, line);
            line = P.WindowTextLine(this.ID, i++, "Bound: " + player.BoundMap.Name + " (" + player.BoundX + "," + player.BoundY + ")");
            world.Send(player, line);
            line = P.WindowTextLine(this.ID, i++, "HP Regeneration: " + Math.Round(player.MaxStats.HPPercentRegen * 100, 0) + "%" + string.Format(" +{0:N0}", player.MaxStats.HPStaticRegen));
            world.Send(player, line);
            line = P.WindowTextLine(this.ID, i++, "MP Regeneration: " + Math.Round(player.MaxStats.MPPercentRegen * 100, 0) + "%" + string.Format(" +{0:N0}", player.MaxStats.MPStaticRegen));
            world.Send(player, line);
            line = P.WindowTextLine(this.ID, i++, "Spell Damage Increase: " + Math.Round(player.MaxStats.SpellDamage * 100, 0) + "%");
            world.Send(player, line);
            line = P.WindowTextLine(this.ID, i++, "Spell Critical Chance: " + Math.Round(player.MaxStats.SpellCrit * 100, 0) + "%");
            world.Send(player, line);
            line = P.WindowTextLine(this.ID, i++, "Melee Damage Increase: " + Math.Round(player.MaxStats.MeleeDamage * 100, 0) + "%");
            world.Send(player, line);
            line = P.WindowTextLine(this.ID, i++, "Melee Critical Chance: " + Math.Round(player.MaxStats.MeleeCrit * 100, 0) + "%");
            world.Send(player, line);
            line = P.WindowTextLine(this.ID, i++, "Haste: " + Math.Round(player.MaxStats.Haste * 100, 0) + "%");
            world.Send(player, line);
            line = P.WindowTextLine(this.ID, i++, "Damage Reduction: " + Math.Round(player.MaxStats.DamageReduction * 100, 0) + "%");
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
                world.Send(player, P.WindowTextLine(this.ID, i++, line));
            }
        }

        /**
         * PopulatePetInfo, populates pet info window
         * 
         */
        public void PopulatePetInfo(Player player, GameWorld world)
        {
            Pet pet = (Pet)this.Data;

            int i = 1;
            string line;

            line = P.WindowTextLine(this.ID, i++, "Name: " + pet.Name);
            world.Send(player, line);
            line = P.WindowTextLine(this.ID, i++, "Experience: " + pet.Experience);
            world.Send(player, line);
            if (pet.Class.GetLevel(pet.Level).Experience == 0)
            {
                line = P.WindowTextLine(this.ID, i++, "Experience Sold: " + pet.ExperienceSold);
                world.Send(player, line);
            }
            else
            {
                line = P.WindowTextLine(this.ID, i++, "Next Level: " + (pet.Class.GetLevel(pet.Level).Experience - pet.Experience));
                world.Send(player, line);
            }
            line = P.WindowTextLine(this.ID, i++, "Level: " + pet.Level);
            world.Send(player, line);
            line = P.WindowTextLine(this.ID, i++, "Weapon Damage: " + pet.WeaponDamage);
            world.Send(player, line);
            line = P.WindowTextLine(this.ID, i++, "Attack Speed: " + Math.Round(pet.AttackSpeed, 2));
            world.Send(player, line);
            line = P.WindowTextLine(this.ID, i++, "Move Speed: " + Math.Round(pet.MoveSpeed, 2));
            world.Send(player, line);
            line = P.WindowTextLine(this.ID, i++, "Extra HP: " + pet.BaseStats.HP);
            world.Send(player, line);
        }

        public void SendCreate(Player player, GameWorld world)
        {
            world.Send(player, P.MakeWindow(this));
            this.Populate(player, world);
            world.Send(player, P.EndWindow(this));
        }
    }
}
