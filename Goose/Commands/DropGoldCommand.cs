namespace Goose.Commands
{
    [Command("/dropgold ", Section = "General", Help = "Drop gold onto the map at your feet.")]
    public sealed class DropGoldCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, int gold)
        {
            var world = ctx.World;

            if (gold <= 0) return;

            if (gold > ctx.Player.Gold) return;

            if (ctx.Player.Level < 10)
            {
                world.Send(ctx.Player, P.ServerMessage("You need to be level 10 or higher to drop gold."));
                return;
            }

            Item? goldItem = world.ItemHandler.GetGold(world);
            if (goldItem is null)
            {
                world.Send(ctx.Player, P.ServerMessage("Gold items are disabled on this server."));
                return;
            }

            ctx.Player.RemoveGold(gold, world);

            ItemTile tile = new ItemTile();
            tile.ItemSlot = new ItemSlot();
            tile.ItemSlot.Item = goldItem;
            tile.ItemSlot.Stack = gold;
            tile.X = ctx.Player.MapX;
            tile.Y = ctx.Player.MapY;
            tile.Owner = ctx.Player;
            ctx.Player.Map.PlaceItem(tile);

            ItemTile? maptile = (ItemTile?)ctx.Player.Map.GetTile(tile.X, tile.Y);
            if (maptile is not null && maptile is ItemTile)
            {
                maptile.ItemSlot.Stack += tile.ItemSlot.Stack;

                world.SendToMap(ctx.Player.Map, P.MakeObject(maptile));
            }
            else
            {
                ctx.Player.Map.AddItem(tile, world);
            }

            world.LogHandler.Log(Log.Types.PlayerDropItem,
                ctx.Player.PlayerID, tile.ItemSlot.Stack + " gold",
                0, ctx.Player.Map.ID, ctx.Player.MapX, ctx.Player.MapY);
        }
    }
}
