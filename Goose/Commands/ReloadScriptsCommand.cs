namespace Goose.Commands
{
    [Command("/reloadscripts", AccessPrivilege.ReloadScripts, Section = "Admin", Help = "Reload all scripts.")]
    public sealed class ReloadScriptsCommand : BaseCommand
    {
        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public void Execute(CommandContext ctx)
        {
            var world = ctx.World;
            var player = ctx.Player;

            Task.Run(() =>
            {
                try
                {
                    var commit = world.ScriptHandler.PrepareReload();
                    var globals = world.CompileNewGlobalScripts();

                    world.EnqueueCompletion(() =>
                    {
                        try
                        {
                            commit();
                            world.RunGlobalScripts(globals);
                            world.CommunityGoals.Redefine(world);

                            world.Send(player, "$7Reloaded scripts.");
                            log.Info("Reloaded scripts");
                        }
                        catch (Exception e)
                        {
                            Fail(world, player, e);
                        }
                    });
                }
                catch (Exception e)
                {
                    world.EnqueueCompletion(() => Fail(world, player, e));
                }
            });
        }

        private static void Fail(GameWorld world, Player player, Exception e)
        {
            log.Error(e, "Failed reloading scripts");
            world.Send(player, "$7" + e.Message);
        }
    }
}
