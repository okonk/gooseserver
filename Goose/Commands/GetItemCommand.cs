namespace Goose.Commands
{
    [Command("/getitem ", AccessPrivilege.SpawnItem, Section = "GM", Help = "Give yourself an item.")]
    public sealed class GetItemCommand : BaseCommand
    {
        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public void Execute(CommandContext ctx, int id, string? arg2 = null, string? arg3 = null)
        {
            var world = ctx.World;

            int stack = 1;
            bool powerful = false;

            if (arg2 is not null)
            {
                try
                {
                    stack = Convert.ToInt32(arg2);
                }
                catch (Exception)
                {
                    stack = 1;

                    powerful = arg2.Equals("powerful", StringComparison.OrdinalIgnoreCase);
                }
            }
            if (arg3 is not null)
            {
                powerful = arg3.Equals("powerful", StringComparison.OrdinalIgnoreCase);
            }

            if (id <= 0 || stack <= 0) return;

            ItemTemplate? template = world.ItemHandler.GetTemplate(id);
            if (template is null) return;

            Item item = new Item();
            if (!item.LoadFromTemplate(template))
            {
                log.Error("item template {0}: invalid template; item not given", id);
                return;
            }

            world.ItemHandler.AddAndAssignId(item, world);

            ctx.Player.Inventory.AddItem(item, stack, world);

            world.LogHandler.Log(Log.Types.GetItem,
                ctx.Player.PlayerID, item.Name + " " + item.ItemID + " " + stack,
                0, ctx.Player.Map.ID, ctx.Player.MapX, ctx.Player.MapY);
        }
    }
}
