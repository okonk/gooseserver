using System;
using System.Collections.Generic;
using System.Linq;
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
        public static Event Create(Player player, Object data)
        {
            Event e = new PlayerDropItemEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                int id = 0;
                int stack = 0;
                string[] t = ((string)this.Data).Substring(3).Split(",".ToCharArray());

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

                ItemSlot slot = this.Player.Inventory.GetSlot(id);
                if (slot == null) return; // log bad slot
                if (stack > slot.Stack) return; // log bad stack

                // Can't drop bound item unless gm
                if (slot.Item.IsBound && !this.Player.HasPrivilege(AccessPrivilege.DropBoundItem)) return;

                ItemSlot drop = this.Player.Inventory.RemoveItem(slot.Item, slot.Stack, world);
                if (drop == null) return;

                ItemTile tile = new ItemTile();
                tile.ItemSlot = drop;
                tile.X = this.Player.MapX;
                tile.Y = this.Player.MapY;
                tile.Owner = this.Player;
                this.Player.Map.PlaceItem(tile);

                // tile can stack
                ItemTile maptile = (ItemTile)this.Player.Map.GetTile(tile.X, tile.Y);
                if (maptile != null && maptile is ItemTile)
                {
                    maptile.ItemSlot.Stack += drop.Stack;

                    world.SendToMap(this.Player.Map, P.MakeObject(maptile));
                }
                else
                {
                    this.Player.Map.AddItem(tile, world);
                }

                world.LogHandler.Log(Log.Types.PlayerDropItem,
                    this.Player.PlayerID, tile.ItemSlot.Item.ItemID + " " + tile.ItemSlot.Item.Template.ID + " " + tile.ItemSlot.Item.Name + " " + tile.ItemSlot.Stack,
                    0, this.Player.Map.ID, this.Player.MapX, this.Player.MapY);
            }
        }
    }
}
