using System.Text;

namespace Goose.Events
{
    /**
     * PlayerDropGoldEvent, event for "/dropgold " packet
     * 
     * Packet syntax: /dropgold amount
     * 
     */
    public class PlayerDropGoldEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                long gold = 0;

                try
                {
                    gold = Convert.ToInt32(((string)this.Data).Split(' ')[1]);
                }
                catch (Exception)
                {
                    gold = 0;
                }

                // log bad drop gold stuff
                if (gold <= 0) return;

                if (gold > this.Player.Gold) return;

                if (this.Player.Level < 10)
                {
                    world.Send(this.Player, P.ServerMessage("You need to be level 10 or higher to drop gold."));
                    return;
                }

                this.Player.RemoveGold(gold, world);

                ItemTile tile = new ItemTile();
                tile.ItemSlot = new ItemSlot();
                tile.ItemSlot.Item = world.ItemHandler.GetGold(world);
                tile.ItemSlot.Stack = gold;
                tile.X = this.Player.MapX;
                tile.Y = this.Player.MapY;
                tile.Owner = this.Player;
                this.Player.Map.PlaceItem(tile);

                // tile can stack
                ItemTile? maptile = (ItemTile?)this.Player.Map.GetTile(tile.X, tile.Y);
                if (maptile is not null && maptile is ItemTile)
                {
                    maptile.ItemSlot.Stack += tile.ItemSlot.Stack;

                    world.SendToMap(this.Player.Map, P.MakeObject(maptile));
                }
                else
                {
                    this.Player.Map.AddItem(tile, world);
                }

                world.LogHandler.Log(Log.Types.PlayerDropItem,
                    this.Player.PlayerID, tile.ItemSlot.Stack + " gold",
                    0, this.Player.Map.ID, this.Player.MapX, this.Player.MapY);
            }
        }
    }
}
