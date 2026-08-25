using System.Text;

namespace Goose
{
    public class PlayerInfoWindow : Window
    {
        public override string Title
        {
            get { return $"{playerForInfo.Name} Info"; }
        }

        public override string Buttons
        {
            get { return $"0,1,{(pageNumber == 0 ? 0 : 1)},{(pageNumber == 4 + (playerForInfo.Bank.NumberOfContainers * playerForInfo.NumberOfBankPages * 2) ? 0 : 1)},0"; }
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
                world.Send(player, P.WindowTextLine(this.ID, lineno++, $"Level {playerForInfo.Level} {playerForInfo.Class.ClassName}"));
                world.Send(player, P.WindowTextLine(this.ID, lineno++, $"Gold: {playerForInfo.Gold:N0}"));
                world.Send(player, P.WindowTextLine(this.ID, lineno++, $"Base HP: {playerForInfo.BaseStats.HP:N0}"));
                world.Send(player, P.WindowTextLine(this.ID, lineno++, $"Base MP: {playerForInfo.BaseStats.MP:N0}"));
                world.Send(player, P.WindowTextLine(this.ID, lineno++, $"HP: {playerForInfo.CurrentHP:N0} / {playerForInfo.MaxHP:N0}"));
                world.Send(player, P.WindowTextLine(this.ID, lineno++, $"MP: {playerForInfo.CurrentMP:N0} / {playerForInfo.MaxMP:N0}"));
                world.Send(player, P.WindowTextLine(this.ID, lineno++, $"AC: {playerForInfo.MaxStats.AC:N0}"));
                world.Send(player, P.WindowTextLine(this.ID, lineno++, $"Experience: {playerForInfo.Experience:N0}"));
                world.Send(player, P.WindowTextLine(this.ID, lineno++, $"Exp Sold: {playerForInfo.ExperienceSold:N0}"));
                world.Send(player, P.WindowTextLine(this.ID, lineno++, $"HP Regen: {playerForInfo.MaxStats.HPPercentRegen * 100:F0}% +{playerForInfo.MaxStats.HPStaticRegen:N0}"));
                world.Send(player, P.WindowTextLine(this.ID, lineno++, $"MP Regen: {playerForInfo.MaxStats.MPPercentRegen * 100:F0}% +{playerForInfo.MaxStats.MPStaticRegen:N0}"));
                world.Send(player, P.WindowTextLine(this.ID, lineno++, $"Spell Damage Increase: {playerForInfo.MaxStats.SpellDamage * 100:F0}%"));
                world.Send(player, P.WindowTextLine(this.ID, lineno++, $"Spell Critical Chance: {playerForInfo.MaxStats.SpellCrit * 100:F0}%"));
                world.Send(player, P.WindowTextLine(this.ID, lineno++, $"Melee Damage Increase: {playerForInfo.MaxStats.MeleeDamage * 100:F0}%"));
                world.Send(player, P.WindowTextLine(this.ID, lineno++, $"Melee Critical Chance: {playerForInfo.MaxStats.MeleeCrit * 100:F0}%"));
                world.Send(player, P.WindowTextLine(this.ID, lineno++, $"Haste: {playerForInfo.MaxStats.Haste * 100:F0}%"));
                world.Send(player, P.WindowTextLine(this.ID, lineno++, $"Damage Reduction: {playerForInfo.MaxStats.DamageReduction * 100:F0}%"));
                world.Send(player, P.WindowTextLine(this.ID, lineno++, $"Move Speed: {playerForInfo.CalculateMoveSpeed()}"));
                world.Send(player, P.WindowTextLine(this.ID, lineno++, $"Bank Pages: {playerForInfo.NumberOfBankPages}"));
            }
            else if (pageNumber == 1)
            {
                world.Send(player, P.WindowTextLine(this.ID, lineno++, "Equipped Items"));
                lineno++;

                foreach (var equipSlot in Enum.GetValues(typeof(Inventory.EquipSlots)).Cast<Inventory.EquipSlots>())
                {
                    var slot = playerForInfo.Inventory.GetEquippedSlot(equipSlot);
                    world.Send(player, P.WindowTextLine(this.ID, lineno++, $"{equipSlot.ToString()}: {slot?.Item?.Name} ({slot?.Item?.TemplateID})"));
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
                        world.Send(player, P.WindowTextLine(this.ID, lineno++, $"{i}."));
                    else
                        world.Send(player, P.WindowTextLine(this.ID, lineno++, $"{i}. {slot.Item.Name} ({slot.Stack})"));
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
                        world.Send(player, P.WindowTextLine(this.ID, lineno++, $"{i}."));
                    else
                        world.Send(player, P.WindowTextLine(this.ID, lineno++, $"{i}. {slot.Item.Name} ({slot.Stack})"));
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
                        world.Send(player, P.WindowTextLine(this.ID, lineno++, $"{i}."));
                    else
                        world.Send(player, P.WindowTextLine(this.ID, lineno++, $"{i}. {slot.Item.Name} ({slot.Stack})"));
                }
            }
            else
            {
                int bankNumber = (pageNumber - 5) / 2 / playerForInfo.NumberOfBankPages;
                int bankStart = ((pageNumber - 5) - (bankNumber * 2 * playerForInfo.NumberOfBankPages)) * 15 + 1;

                var container = playerForInfo.Bank.Containers.OrderBy(k => k.Key).ElementAt(bankNumber);

                world.Send(player, P.WindowTextLine(this.ID, lineno++, $"Bank {bankStart}-{bankStart + 14} / {playerForInfo.NumberOfBankPages * 2 * 15}"));
                world.Send(player, P.WindowTextLine(this.ID, lineno++, $"{world.NPCHandler.GetNPCTemplate(container.Key)?.Name} ({container.Key})"));
                lineno++;

                for (int i = bankStart; i <= bankStart + 14; i++)
                {
                    var slot = container.Value.GetSlot(i);
                    if (slot == null)
                        world.Send(player, P.WindowTextLine(this.ID, lineno++, $"{i}."));
                    else
                        world.Send(player, P.WindowTextLine(this.ID, lineno++, $"{i}. {slot.Item.Name} ({slot.Stack})"));
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
