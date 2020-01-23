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

        public static Event Create(Player player, Object data)
        {
            Event e = new ReloadScriptsCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready && 
                this.Player.HasPrivilege(AccessPrivilege.ReloadScripts))
            {
                Task.Run(() =>
                {
                    try
                    {
                        world.ScriptHandler.ReloadScripts();

                         // TODO: This is bad, it executes the global script OnLoaded on the wrong thread
                        world.LoadGlobalScripts();

                        // TODO: Not safe to call Send on multiple threads
                        //world.Send(this.Player, "$7Reloaded scripts.");
                        log.Info("Reloaded scripts");
                    }
                    catch (Exception e)
                    {
                        log.Error(e, "Failed reloading scripts");
                        //world.Send(this.Player, "$7" + e.Message);
                    }
                });
            }
        }
    }
}