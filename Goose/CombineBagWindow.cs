using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goose
{
    class CombineBagWindow : ItemContainerWindow
    {
        public CombineBagWindow(GameWorld world, Player player)
        {
            this.ItemContainer = player.Inventory.GetCombineBagContainer();

            this.ID = 22;
            this.Title = "Combine Bag";
            this.Buttons = "1,1,0,0,0";
            this.Frame = WindowFrames.TenSlot;
            this.Type = Window.WindowTypes.CombineBag;

            this.SendCreate(player, world);
        }

        public static void Open(GameWorld world, Player player)
        {
            var combineBag = player.Windows.FirstOrDefault(w => w.Type == WindowTypes.CombineBag);
            if (combineBag != null)
            {
                player.Windows.Remove(combineBag);
            }

            player.Windows.Add(new CombineBagWindow(world, player));
        }

        public override void Refresh(Player player, GameWorld world)
        {
            this.Populate(player, world);
        }

        public override void SendSlot(int slotIndex, Player player, GameWorld world)
        {
            ItemSlot slot = this.ItemContainer.GetSlot(slotIndex);
            if (slot != null)
            {
                world.Send(player, "SCS" + slot.Item.GetSlotPacket(world, slotIndex, slot.Stack));
            }
            else
            {
                world.Send(player, "CCS" + slotIndex);
            }
        }
    }
}
