using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    public class PlaceSpawnCommandEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new PlaceSpawnCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State != Player.States.Ready || !this.Player.HasPrivilege(AccessPrivilege.PlaceSpawn)) return;

            string[] tokens = ((string)this.Data).Split(" ".ToCharArray());

            if (tokens.Length == 1)
            {
                world.Send(this.Player, "$7/placespawn <npcid>");
                return;
            }

            int npcId = 0;

            try
            {
                npcId = Convert.ToInt32(tokens[1]);
            }
            catch (Exception)
            {
                npcId = 0;
            }

            if (npcId <= 0)
            {
                world.Send(this.Player, "$7/placespawn <npcid>");
                return;
            }

            var npc = world.NPCHandler.GetNPCTemplate(npcId);
            if (npc == null)
            {
                world.Send(this.Player, "$7/placespawn <npcid>");
                return;
            }

            var rng = new Random(npcId);

            var item = new Item();
            item.LoadFromTemplate(world.ItemHandler.GetTemplate(GameSettings.Default.GoldItemID));
            item.ItemID = GameSettings.Default.ItemIDStartpoint + GameSettings.Default.GoldItemID;
            item.ScriptParams = npcId.ToString();
            item.Name = npc.Name;
            item.GraphicR = rng.Next(0, 256);
            item.GraphicG = rng.Next(0, 256);
            item.GraphicB = rng.Next(0, 256);
            item.GraphicA = 180;

            ItemTile tile = new ItemTile();
            tile.ItemSlot = new ItemSlot();
            tile.ItemSlot.Item = item;
            tile.ItemSlot.Stack = 1;
            tile.X = this.Player.MapX;
            tile.Y = this.Player.MapY;
            tile.Owner = this.Player;
            this.Player.Map.PlaceItem(tile);

            // tile can stack
            ItemTile maptile = (ItemTile)this.Player.Map.GetTile(tile.X, tile.Y);
            if (maptile != null && maptile is ItemTile)
            {
                maptile.ItemSlot.Stack += tile.ItemSlot.Stack;

                world.SendToMap(this.Player.Map, maptile.MOBString());
            }
            else
            {
                this.Player.Map.AddItem(tile, world);
            }
        }
    }
}
