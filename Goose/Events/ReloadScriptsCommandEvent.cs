using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    public class ReloadScriptsCommandEvent : Event
    {
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
                try
                {
                    world.ScriptHandler.ReloadScripts();

                    world.Send(this.Player, "$7Reloaded scripts.");
                }
                catch (Exception e)
                {
                    world.Send(this.Player, "$7" + e.Message);
                }
            }
        }
    }
}