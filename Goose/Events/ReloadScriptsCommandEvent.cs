using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goose.Events
{
    public class ReloadScriptsCommandEvent : Event
    {
        private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                Task.Run(() =>
                {
                    try
                    {
                        world.ScriptHandler.ReloadScripts();

                         // TODO: This is bad, it executes the global script OnLoaded on the wrong thread
                        world.LoadGlobalScripts();

                        world.Send(this.Player, "$7Reloaded scripts.");
                        log.Info("Reloaded scripts");
                    }
                    catch (Exception e)
                    {
                        log.Error(e, "Failed reloading scripts");
                        world.Send(this.Player, "$7" + e.Message);
                    }
                });
            }
        }
    }
}