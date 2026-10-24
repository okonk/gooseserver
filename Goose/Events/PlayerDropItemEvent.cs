using System.Text;

namespace Goose.Events
{
    /**
     * PlayerDropItemEvent, event for "DRP" packet
     *
     * Packet syntax: DRPinvid,stacksize
     *
     */
    class PlayerDropItemEvent : Event
    {
        private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                int id = 0;
                int stack = 0;
                string[] t = ((string)this.Data).Substring(3).Split(',');

                try
                {
                    id = Convert.ToInt32(t[0]);
                    stack = Convert.ToInt32(t[1]);
                }
                catch (Exception)
                {
                    id = 0;
                    stack = 0;
                }

                // log bad drop item stuff
                if (id <= 0 || stack <= 0) return;

                ItemSlot? slot = this.Player.Inventory.GetSlot(id);
                if (slot is null) return; // log bad slot
                if (stack > slot.Stack) return; // log bad stack

                // Can't drop bound item unless gm
                if (slot.Item.IsBound && !this.Player.HasPrivilege(AccessPrivilege.DropBoundItem)) return;

                string? refusal = null;
                try
                {
                    refusal = this.Player.Map.Script?.Object.CanPlayerDropItem(this.Player.Map, this.Player, slot.Item, stack, world);
                }
                catch (Exception e)
                {
                    // Fail CLOSED, mirroring the CanPlayerJoin and item CanPickup gates: a
                    // broken gate script must refuse the drop rather than admit it.
                    log.Error(e, "Map CanPlayerDropItem {0} Exception", this.Player.Map.Name);
                    refusal = "You cannot drop that here.";
                }
                if (refusal is not null)
                {
                    world.Send(this.Player, P.Notice(NoticeType.Info, refusal));
                    return;
                }

                ItemSlot? drop = this.Player.Inventory.RemoveItem(slot.Item, stack, world);
                if (drop is null) return;

                ItemTile tile = new ItemTile();
                tile.ItemSlot = drop;
                tile.X = this.Player.MapX;
                tile.Y = this.Player.MapY;
                tile.Owner = this.Player;
                this.Player.Map.PlaceItem(tile);

                // tile can stack
                ItemTile? maptile = (ItemTile?)this.Player.Map.GetTile(tile.X, tile.Y);
                ItemTile finalTile;
                if (maptile is not null && maptile is ItemTile)
                {
                    maptile.ItemSlot.Stack += drop.Stack;

                    world.SendToMap(this.Player.Map, P.MakeObject(maptile));
                    finalTile = maptile;
                }
                else
                {
                    this.Player.Map.AddItem(tile, world);
                    finalTile = tile;
                }

                try
                {
                    this.Player.Map.Script?.Object.OnPlayerDroppedItem(this.Player.Map, this.Player, finalTile, world);
                }
                catch (Exception e)
                {
                    log.Error(e, "Map OnPlayerDroppedItem {0} Exception", this.Player.Map.Name);
                }

                world.LogHandler.Log(Log.Types.PlayerDropItem,
                    this.Player.PlayerID, tile.ItemSlot.Item.ItemID + " " + tile.ItemSlot.Item.Template.ID + " " + tile.ItemSlot.Item.Name + " " + tile.ItemSlot.Stack,
                    0, this.Player.Map.ID, this.Player.MapX, this.Player.MapY);
            }
        }
    }
}
