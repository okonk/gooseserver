namespace Goose.Commands
{
    [Command("/getitem ", AccessPrivilege.SpawnItem, Section = "GM", Help = "Give yourself an item.")]
    public sealed class GetItemCommand : BaseCommand
    {
        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public void Execute(CommandContext ctx, int id, string? stack = null)
        {
            var world = ctx.World;

            int count = 1;

            if (stack is not null)
            {
                try
                {
                    count = Convert.ToInt32(stack);
                }
                catch (Exception)
                {
                    count = 1;
                }
            }

            if (id <= 0 || count <= 0) return;

            ItemTemplate? template = world.ItemHandler.GetTemplate(id);
            if (template is null) return;

            Item item = new Item();
            if (!item.LoadFromTemplate(template))
            {
                log.Error("item template {0}: invalid template; item not given", id);
                return;
            }

            world.ItemHandler.AddAndAssignId(item, world);

            ctx.Player.Inventory.AddItem(item, count, world);

            world.LogHandler.Log(Log.Types.GetItem,
                ctx.Player.PlayerID, item.Name + " " + item.ItemID + " " + count,
                0, ctx.Player.Map.ID, ctx.Player.MapX, ctx.Player.MapY);
        }
    }
}
