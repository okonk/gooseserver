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
                world.Send(player, P.WindowTextLine(this.ID, lineno++, string.Format("Level {0} {1}", playerForInfo.Level, playerForInfo.Class.ClassName)));
                world.Send(player, P.WindowTextLine(this.ID, lineno++, string.Format("Gold: {0:N0}", playerForInfo.Gold)));
                world.Send(player, P.WindowTextLine(this.ID, lineno++, string.Format("Base HP: {0:N0}", playerForInfo.BaseStats.HP)));
                world.Send(player, P.WindowTextLine(this.ID, lineno++, string.Format("Base MP: {0:N0}", playerForInfo.BaseStats.MP)));
                world.Send(player, P.WindowTextLine(this.ID, lineno++, string.Format("HP: {0:N0} / {1:N0}", playerForInfo.CurrentHP, playerForInfo.MaxHP)));
                world.Send(player, P.WindowTextLine(this.ID, lineno++, string.Format("MP: {0:N0} / {1:N0}", playerForInfo.CurrentMP, playerForInfo.MaxMP)));
                world.Send(player, P.WindowTextLine(this.ID, lineno++, string.Format("AC: {0:N0}", playerForInfo.MaxStats.AC)));
                world.Send(player, P.WindowTextLine(this.ID, lineno++, string.Format("Experience: {0:N0}", playerForInfo.Experience)));
                world.Send(player, P.WindowTextLine(this.ID, lineno++, string.Format("Exp Sold: {0:N0}", playerForInfo.ExperienceSold)));
                world.Send(player, P.WindowTextLine(this.ID, lineno++, string.Format("HP Regen: {0:F0}% +{1:N0}", playerForInfo.MaxStats.HPPercentRegen * 100, playerForInfo.MaxStats.HPStaticRegen)));
                world.Send(player, P.WindowTextLine(this.ID, lineno++, string.Format("MP Regen: {0:F0}% +{1:N0}", playerForInfo.MaxStats.MPPercentRegen * 100, playerForInfo.MaxStats.MPStaticRegen)));
                world.Send(player, P.WindowTextLine(this.ID, lineno++, string.Format("Spell Damage Increase: {0:F0}%", playerForInfo.MaxStats.SpellDamage * 100)));
                world.Send(player, P.WindowTextLine(this.ID, lineno++, string.Format("Spell Critical Chance: {0:F0}%", playerForInfo.MaxStats.SpellCrit * 100)));
                world.Send(player, P.WindowTextLine(this.ID, lineno++, string.Format("Melee Damage Increase: {0:F0}%", playerForInfo.MaxStats.MeleeDamage * 100)));
                world.Send(player, P.WindowTextLine(this.ID, lineno++, string.Format("Melee Critical Chance: {0:F0}%", playerForInfo.MaxStats.MeleeCrit * 100)));
                world.Send(player, P.WindowTextLine(this.ID, lineno++, string.Format("Haste: {0:F0}%", playerForInfo.MaxStats.Haste * 100)));
                world.Send(player, P.WindowTextLine(this.ID, lineno++, string.Format("Damage Reduction: {0:F0}%", playerForInfo.MaxStats.DamageReduction * 100)));
                world.Send(player, P.WindowTextLine(this.ID, lineno++, string.Format("Move Speed: {0}", playerForInfo.CalculateMoveSpeed())));
                world.Send(player, P.WindowTextLine(this.ID, lineno++, string.Format("Bank Pages: {0}", playerForInfo.NumberOfBankPages)));
            }
            else if (pageNumber == 1)
            {
                world.Send(player, P.WindowTextLine(this.ID, lineno++, "Equipped Items"));
                lineno++;

                foreach (var equipSlot in Enum.GetValues(typeof(Inventory.EquipSlots)).Cast<Inventory.EquipSlots>())
                {
                    var slot = playerForInfo.Inventory.GetEquippedSlot(equipSlot);
                    world.Send(player, P.WindowTextLine(this.ID, lineno++, string.Format("{0}: {1} ({2})", equipSlot.ToString(), slot?.Item?.Name, slot?.Item?.TemplateID)));
                }
            }
            else if (pageNumber == 2)
            {
                world.Send(player, P.WindowTextLine(this.ID, lineno++, "Inventory 1-15"));
                lineno++;

                for (int i = 1; i <= 15; i++)
                {
                    var slot = playerForInfo.Inventory.GetSlot(i);
                    if (slot == null)
                        world.Send(player, P.WindowTextLine(this.ID, lineno++, string.Format("{0}.", i)));
                    else
                        world.Send(player, P.WindowTextLine(this.ID, lineno++, string.Format("{0}. {1} ({2})", i, slot.Item.Name, slot.Stack)));
                }
            }
            else if (pageNumber == 3)
            {
                world.Send(player, P.WindowTextLine(this.ID, lineno++, "Inventory 16-30"));
                lineno++;

                for (int i = 16; i <= 30; i++)
                {
                    var slot = playerForInfo.Inventory.GetSlot(i);
                    if (slot == null)
                        world.Send(player, P.WindowTextLine(this.ID, lineno++, string.Format("{0}.", i)));
                    else
                        world.Send(player, P.WindowTextLine(this.ID, lineno++, string.Format("{0}. {1} ({2})", i, slot.Item.Name, slot.Stack)));
                }
            }
            else if (pageNumber == 4)
            {
                world.Send(player, P.WindowTextLine(this.ID, lineno++, "Combine Bag"));
                lineno++;

                for (int i = 1; i <= 10; i++)
                {
                    var slot = playerForInfo.Inventory.GetCombineBagContainer().GetSlot(i);
                    if (slot == null)
                        world.Send(player, P.WindowTextLine(this.ID, lineno++, string.Format("{0}.", i)));
                    else
                        world.Send(player, P.WindowTextLine(this.ID, lineno++, string.Format("{0}. {1} ({2})", i, slot.Item.Name, slot.Stack)));
                }
            }
            else
            {
                int bankNumber = (pageNumber - 5) / 2 / playerForInfo.NumberOfBankPages;
                int bankStart = ((pageNumber - 5) - (bankNumber * 2 * playerForInfo.NumberOfBankPages)) * 15 + 1;

                var container = playerForInfo.Bank.Containers.OrderBy(k => k.Key).ElementAt(bankNumber);

                world.Send(player, P.WindowTextLine(this.ID, lineno++, string.Format("Bank {0}-{1} / {2}", bankStart, bankStart + 14, playerForInfo.NumberOfBankPages * 2 * 15)));
                world.Send(player, P.WindowTextLine(this.ID, lineno++, string.Format("{0} ({1})", world.NPCHandler.GetNPCTemplate(container.Key)?.Name, container.Key)));
                lineno++;

                for (int i = bankStart; i <= bankStart + 14; i++)
                {
                    var slot = container.Value.GetSlot(i);
                    if (slot == null)
                        world.Send(player, P.WindowTextLine(this.ID, lineno++, string.Format("{0}.", i)));
                    else
                        world.Send(player, P.WindowTextLine(this.ID, lineno++, string.Format("{0}. {1} ({2})", i, slot.Item.Name, slot.Stack)));
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
