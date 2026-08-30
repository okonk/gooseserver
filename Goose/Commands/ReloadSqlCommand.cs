namespace Goose.Commands
{
    [Command("/reloadsql", AccessPrivilege.ReloadSQL, Section = "Admin", Help = "Reload SQL data.")]
    public sealed class ReloadSqlCommand : BaseCommand
    {
        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public void Execute(CommandContext ctx)
        {
            var world = ctx.World;
            var player = ctx.Player;

            world.Send(player, "$7Reloading sql...");

            Task.Run(() =>
            {
                try
                {
                    world.SpellHandler.LoadSpellEffects(world);
                    world.SpellHandler.LoadSpells(world);
                    world.ItemHandler.LoadTemplates(world);
                    world.ItemHandler.RefreshItemStats(world);
                    world.QuestHandler.LoadQuests(world);
                    //world.MapHandler.LoadMaps(world);
                    //world.ClassHandler.LoadClasses(world);
                    world.NPCHandler.LoadNPCTemplates(world);
                    //world.NPCHandler.LoadNPCs(world);
                    //world.CombinationHandler.LoadCombinations(world);

                    world.Send(player, "$7Reloaded sql data.");
                    log.Info("Reloaded sql data");
                }
                catch (Exception e)
                {
                    log.Error(e, "Failed reloading sql data");
                    world.Send(player, "$7Failed reloading sql: " + e.Message);
                }
            });
        }
    }
}
