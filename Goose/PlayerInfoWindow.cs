using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goose
{
    public class PlayerInfoWindow : Window
    {
        public override string Title
        {
            get { return string.Format("{0} Info", playerForInfo.Name); }
        }

        public override string Buttons
        {
            get { return string.Format("0,1,{0},{1},0", pageNumber == 0 ? 0 : 1, pageNumber == 4 + (playerForInfo.Bank.NumberOfContainers * playerForInfo.NumberOfBankPages * 2) ? 0 : 1); }
        }

        private Player playerForInfo;
        private int pageNumber = 0;

        public PlayerInfoWindow(GameWorld world, Player player, Player playerForInfo)
        {
            this.ID = ++player.LastWindowID;
            this.Frame = WindowFrames.Quest;
            this.Type = WindowTypes.PlayerInfo;
            this.playerForInfo = playerForInfo;

            this.SendCreate(player, world);
        }

        public static void Open(GameWorld world, Player player, Player playerForInfo)
        {
            player.Windows.Add(new PlayerInfoWindow(world, player, playerForInfo));
        }

        public override void Populate(Player player, GameWorld world)
        {
            int lineno = 1;
            if (pageNumber == 0)
            {
                world.Send(player, string.Format("WNF{0},{1},Level {2} {3}|0|0|0|0|*", this.ID, lineno++, playerForInfo.Level, playerForInfo.Class.ClassName));
                world.Send(player, string.Format("WNF{0},{1},Gold: {2:N0}|0|0|0|0|*", this.ID, lineno++, playerForInfo.Gold));
                world.Send(player, string.Format("WNF{0},{1},Base HP: {2:N0}|0|0|0|0|*", this.ID, lineno++, playerForInfo.BaseStats.HP));
                world.Send(player, string.Format("WNF{0},{1},Base MP: {2:N0}|0|0|0|0|*", this.ID, lineno++, playerForInfo.BaseStats.MP));
                world.Send(player, string.Format("WNF{0},{1},HP: {2:N0} / {3:N0}|0|0|0|0|*", this.ID, lineno++, playerForInfo.CurrentHP, playerForInfo.MaxHP));
                world.Send(player, string.Format("WNF{0},{1},MP: {2:N0} / {3:N0}|0|0|0|0|*", this.ID, lineno++, playerForInfo.CurrentMP, playerForInfo.MaxMP));
                world.Send(player, string.Format("WNF{0},{1},AC: {2:N0}|0|0|0|0|*", this.ID, lineno++, playerForInfo.MaxStats.AC));
                world.Send(player, string.Format("WNF{0},{1},Experience: {2:N0}|0|0|0|0|*", this.ID, lineno++, playerForInfo.Experience));
                world.Send(player, string.Format("WNF{0},{1},Exp Sold: {2:N0}|0|0|0|0|*", this.ID, lineno++, playerForInfo.ExperienceSold));
                world.Send(player, string.Format("WNF{0},{1},HP Regen: {2:F0}% +{3:N0}|0|0|0|0|*", this.ID, lineno++, playerForInfo.MaxStats.HPPercentRegen * 100, playerForInfo.MaxStats.HPStaticRegen));
                world.Send(player, string.Format("WNF{0},{1},MP Regen: {2:F0}% +{3:N0}|0|0|0|0|*", this.ID, lineno++, playerForInfo.MaxStats.MPPercentRegen * 100, playerForInfo.MaxStats.MPStaticRegen));
                world.Send(player, string.Format("WNF{0},{1},Spell Damage Increase: {2:F0}%|0|0|0|0|*", this.ID, lineno++, playerForInfo.MaxStats.SpellDamage * 100));
                world.Send(player, string.Format("WNF{0},{1},Spell Critical Chance: {2:F0}%|0|0|0|0|*", this.ID, lineno++, playerForInfo.MaxStats.SpellCrit * 100));
                world.Send(player, string.Format("WNF{0},{1},Melee Damage Increase: {2:F0}%|0|0|0|0|*", this.ID, lineno++, playerForInfo.MaxStats.MeleeDamage * 100));
                world.Send(player, string.Format("WNF{0},{1},Melee Critical Chance: {2:F0}%|0|0|0|0|*", this.ID, lineno++, playerForInfo.MaxStats.MeleeCrit * 100));
                world.Send(player, string.Format("WNF{0},{1},Haste: {2:F0}%|0|0|0|0|*", this.ID, lineno++, playerForInfo.MaxStats.Haste * 100));
                world.Send(player, string.Format("WNF{0},{1},Damage Reduction: {2:F0}%|0|0|0|0|*", this.ID, lineno++, playerForInfo.MaxStats.DamageReduction * 100));
                world.Send(player, string.Format("WNF{0},{1},Move Speed Increase: {2:F0}%|0|0|0|0|*", this.ID, lineno++, playerForInfo.MaxStats.MoveSpeedIncrease * 100));
                world.Send(player, string.Format("WNF{0},{1},Bank Pages: {2}|0|0|0|0|*", this.ID, lineno++, playerForInfo.NumberOfBankPages));
            }
            else if (pageNumber == 1)
            {
                world.Send(player, string.Format("WNF{0},{1},Equipped Items|0|0|0|0|*", this.ID, lineno++));
                lineno++;

                foreach (var equipSlot in Enum.GetValues(typeof(Inventory.EquipSlots)).Cast<Inventory.EquipSlots>())
                {
                    var slot = playerForInfo.Inventory.GetEquippedSlot(equipSlot);
                    world.Send(player, string.Format("WNF{0},{1},{2}: {3} ({4})|0|0|0|0|*", this.ID, lineno++, equipSlot.ToString(), slot?.Item?.Name, slot?.Item?.TemplateID));
                }
            }
            else if (pageNumber == 2)
            {
                world.Send(player, string.Format("WNF{0},{1},Inventory 1-15|0|0|0|0|*", this.ID, lineno++));
                lineno++;

                for (int i = 1; i <= 15; i++)
                {
                    var slot = playerForInfo.Inventory.GetSlot(i);
                    if (slot == null)
                        world.Send(player, string.Format("WNF{0},{1},{2}.|0|0|0|0|*", this.ID, lineno++, i));
                    else
                        world.Send(player, string.Format("WNF{0},{1},{2}. {3} ({4})|0|0|0|0|*", this.ID, lineno++, i, slot.Item.Name, slot.Stack));
                }
            }
            else if (pageNumber == 3)
            {
                world.Send(player, string.Format("WNF{0},{1},Inventory 16-30|0|0|0|0|*", this.ID, lineno++));
                lineno++;

                for (int i = 16; i <= 30; i++)
                {
                    var slot = playerForInfo.Inventory.GetSlot(i);
                    if (slot == null)
                        world.Send(player, string.Format("WNF{0},{1},{2}.|0|0|0|0|*", this.ID, lineno++, i));
                    else
                        world.Send(player, string.Format("WNF{0},{1},{2}. {3} ({4})|0|0|0|0|*", this.ID, lineno++, i, slot.Item.Name, slot.Stack));
                }
            }
            else if (pageNumber == 4)
            {
                world.Send(player, string.Format("WNF{0},{1},Combine Bag|0|0|0|0|*", this.ID, lineno++));
                lineno++;

                for (int i = 1; i <= 10; i++)
                {
                    var slot = playerForInfo.Inventory.GetCombineBagContainer().GetSlot(i);
                    if (slot == null)
                        world.Send(player, string.Format("WNF{0},{1},{2}.|0|0|0|0|*", this.ID, lineno++, i));
                    else
                        world.Send(player, string.Format("WNF{0},{1},{2}. {3} ({4})|0|0|0|0|*", this.ID, lineno++, i, slot.Item.Name, slot.Stack));
                }
            }
            else
            {
                int bankNumber = (pageNumber - 5) / 2 / playerForInfo.NumberOfBankPages;
                int bankStart = ((pageNumber - 5) - (bankNumber * 2 * playerForInfo.NumberOfBankPages)) * 15 + 1;

                var container = playerForInfo.Bank.Containers.OrderBy(k => k.Key).ElementAt(bankNumber);

                world.Send(player, string.Format("WNF{0},{1},Bank {2}-{3} / {4}|0|0|0|0|*", this.ID, lineno++, bankStart, bankStart + 14, playerForInfo.NumberOfBankPages * 2 * 15));
                world.Send(player, string.Format("WNF{0},{1},{2} ({3})|0|0|0|0|*", this.ID, lineno++, world.NPCHandler.GetNPCTemplate(container.Key)?.Name, container.Key));
                lineno++;

                for (int i = bankStart; i <= bankStart + 14; i++)
                {
                    var slot = container.Value.GetSlot(i);
                    if (slot == null)
                        world.Send(player, string.Format("WNF{0},{1},{2}.|0|0|0|0|*", this.ID, lineno++, i));
                    else
                        world.Send(player, string.Format("WNF{0},{1},{2}. {3} ({4})|0|0|0|0|*", this.ID, lineno++, i, slot.Item.Name, slot.Stack));
                }
            }
        }

        public override void Clicked(ButtonTypes buttonid, int npcid, int id2, int id3, Player player, GameWorld world)
        {
            switch (buttonid)
            {
                case ButtonTypes.Exit:
                case ButtonTypes.Close:
                    player.Windows.Remove(this);
                    break;
                case ButtonTypes.Next:
                    pageNumber++;

                    this.SendCreate(player, world);
                    break;
                case ButtonTypes.Back:
                    pageNumber--;

                    this.SendCreate(player, world);
                    break;
                default:
                    player.Windows.Remove(this);
                    break;
            }
        }

        public override void Refresh(Player player, GameWorld world)
        {
            this.Populate(player, world);
        }
    }
}
