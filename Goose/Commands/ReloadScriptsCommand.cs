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
                    world.ScriptHandler.ReloadScripts();

                     // TODO: This is bad, it executes the global script OnLoaded on the wrong thread
                    world.LoadGlobalScripts();

                    world.Send(player, "$7Reloaded scripts.");
                    log.Info("Reloaded scripts");
                }
                catch (Exception e)
                {
                    log.Error(e, "Failed reloading scripts");
                    world.Send(player, "$7" + e.Message);
                }
            });
        }
    }
}
