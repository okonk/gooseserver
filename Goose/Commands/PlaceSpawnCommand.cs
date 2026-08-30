namespace Goose.Commands
{
    [Command("/placespawn", AccessPrivilege.PlaceSpawn, Section = "GM", Help = "Place a spawnable item at your location.")]
    public sealed class PlaceSpawnCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, int npcId)
        {
            var world = ctx.World;

            if (npcId <= 0)
            {
                ctx.Send(ctx.Usage);
                return;
            }

            var npc = world.NPCHandler.GetNPCTemplate(npcId);
            if (npc is null)
            {
                ctx.Send(ctx.Usage);
                return;
            }

            ItemTemplate? goldTemplate = world.ItemHandler.GetTemplate(world.Settings.GoldItemID);
            if (goldTemplate is null)
            {
                ctx.Send("Gold item template is missing.");
                return;
            }

            var rng = new Random(npcId);

            var item = new Item();
            if (!item.LoadFromTemplate(goldTemplate))
            {
                ctx.Send("Gold item template is missing.");
                return;
            }
            item.ItemID = world.Settings.ItemIDStartpoint + world.Settings.GoldItemID;
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
            tile.X = ctx.Player.MapX;
            tile.Y = ctx.Player.MapY;
            tile.Owner = ctx.Player;
            ctx.Player.Map.PlaceItem(tile);

            // tile can stack
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
        }
    }
}
