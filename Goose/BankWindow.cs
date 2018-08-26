using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goose
{
    public class BankWindow : ItemContainerWindow
    {
        public const int SlotsPerPage = 30;

        public int CurrentPage { get; set; }

        public int MaxPages { get; set; }

        public override string Title
        {
            get { return string.Format("Bank Page {0}/{1}", CurrentPage, MaxPages);  }
        }

        public override string Buttons
        {
            get { return string.Format("0,1,{0},{1},0", (CurrentPage - 1 <= 0 ? "0" : "1"), (CurrentPage == MaxPages ? "0" : "1")); }
        }

        public BankWindow(GameWorld world, Player player, NPC npc)
        {
            this.ItemContainer = player.Bank.GetOrCreateContainer(player, npc.NPCTemplateID);
            this.CurrentPage = 1;
            this.MaxPages = player.NumberOfBankPages;

            this.ID = 21;
            this.Frame = WindowFrames.Bank;
            this.Type = WindowTypes.Bank;
            this.NPC = npc;

            this.SendCreate(player, world);
        }

        public static void Open(GameWorld world, Player player, NPC npc)
        {
            if (!player.Windows.Any(w => w.Type == WindowTypes.Bank && w.NPC == npc))
            {
                player.Windows.Add(new BankWindow(world, player, npc));
            }
        }

        public override void Populate(Player player, GameWorld world)
        {
            for (int i = 1; i <= SlotsPerPage; i++)
            {
                this.SendSlot(i, player, world);
            }
        }

        public override void Refresh(Player player, GameWorld world)
        {
            this.Populate(player, world);
        }

        public bool BankerInRange(Player player)
        {
            return Map.InRange(player, this.NPC);
        }

        private int GetSlotOffset()
        {
            return (CurrentPage - 1) * SlotsPerPage;
        }

        public override bool ValidateSlotIndex(int index)
        {
            return (index > 0 && index <= SlotsPerPage);
        }

        public override ItemSlot GetSlot(int slotIndex)
        {
            return this.ItemContainer.GetSlot(slotIndex + GetSlotOffset());
        }

        public override void SetSlot(int slotIndex, ItemSlot slot)
        {
            this.ItemContainer.SetSlot(slotIndex + GetSlotOffset(), slot);
        }

        public override void InventoryToWindow(Player player, int invSlotIndex, int toSlotIndex, GameWorld world)
        {
            if (!BankerInRange(player)) return;

            base.InventoryToWindow(player, invSlotIndex, toSlotIndex, world);
        }

        public override void WindowToInventory(Player player, int fromSlotIndex, int invSlotIndex, GameWorld world)
        {
            if (!BankerInRange(player)) return;
            
            base.WindowToInventory(player, fromSlotIndex, invSlotIndex, world);
        }

        public override void SendSlot(int slotIndex, Player player, GameWorld world)
        {
            ItemSlot slot = this.GetSlot(slotIndex);
            if (slot != null)
            {
                world.Send(player, "SBS" + slot.Item.GetSlotPacket(world, slotIndex, slot.Stack));
            }
            else
            {
                world.Send(player, "CBS" + slotIndex);
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
                    if (CurrentPage == MaxPages)
                        return;

                    CurrentPage++;
                    this.SendCreate(player, world);

                    break;
                case ButtonTypes.Back:
                    if (CurrentPage - 1 <= 0)
                        return;

                    CurrentPage--;
                    this.SendCreate(player, world);

                    break;
                default:
                    player.Windows.Remove(this);
                    break;
            }
        }
    }
}
