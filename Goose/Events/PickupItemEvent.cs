using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /**
     * PickupItemEvent, event for "GET" packet
     * 
     * Called when someone presses comma
     * 
     */
    class PickupItemEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new PickupItemEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                int x = this.Player.MapX;
                int y = this.Player.MapY;
                // check tile at player x,y
                ITile itile = this.Player.Map.GetTile(x, y);
                ItemTile tile;
                if (itile == null)
                {
                    // check tile 1 square in front of player
                    switch (this.Player.Facing)
                    {
                        case 1: y--; break;
                        case 2: x++; break;
                        case 3: y++; break;
                        case 4: x--; break;
                    }
                    itile = this.Player.Map.GetTile(x, y);
                    if (itile == null)
                    {
                        // no items
                        // log no items, i don't think the client sends get unless there's an item
                        return;
                    }
                    else
                    {
                        if (itile is ItemTile)
                        {
                            tile = (ItemTile)itile;
                        }
                        else
                        {
                            return;
                        }
                        // So players can only pick up 1 tile in front if it's their item
                        // and within time limit
                        if (!(tile.Owner == this.Player ||
                            (tile.Owner.Group != null && tile.Owner.Group.Players.Contains(this.Player))))
                        {
                            return;
                        }
                    }
                }

                if (itile is ItemTile)
                {
                    tile = (ItemTile)itile;
                }
                else
                {
                    return;
                }

                // Can't pick up cause not owner and it's not past the time limit
                if (!(tile.PickupTime < world.TimeNow || 
                        (tile.Owner == this.Player || 
                            (tile.Owner.Group != null && tile.Owner.Group.Players.Contains(this.Player)))))
                {
                    return;
                }

                // picked up gold
                if (tile.ItemSlot.Item.ItemID == 
                    GameWorld.Settings.ItemIDStartpoint + GameWorld.Settings.GoldItemID)
                {
                    this.Player.AddGold(tile.ItemSlot.Stack, world);
                    this.Player.Map.RemoveItem(tile, world);
                    world.LogHandler.Log(Log.Types.PickupItem, this.Player.PlayerID, tile.ItemSlot.Stack + " gold", 0, this.Player.Map.ID, this.Player.MapX, this.Player.MapY);
                }
                else
                {
                    if (tile.ItemSlot.Item.IsLore && this.Player.HasItem(tile.ItemSlot.Item.Template.ID)) return;

                    if (this.Player.Inventory.AddItem(tile.ItemSlot.Item, tile.ItemSlot.Stack, world))
                    {
                        this.Player.Map.RemoveItem(tile, world);
                        if (tile.ItemSlot.Item.IsBindOnPickup)
                        {
                            tile.ItemSlot.Item.IsBound = true;
                        }

                        world.LogHandler.Log(Log.Types.PickupItem,
                            this.Player.PlayerID, tile.ItemSlot.Item.ItemID + " " + tile.ItemSlot.Item.Template.ID + " " + tile.ItemSlot.Item.Name + " " + tile.ItemSlot.Stack, 
                            0, this.Player.Map.ID, this.Player.MapX, this.Player.MapY);
                    }
                    else
                    {
                        return;
                    }
                }

                if (this.Player.Group != null)
                {
                    this.Player.Group.ItemPickup(this.Player, tile.ItemSlot, world);
                }
            }
        }
    }
}
