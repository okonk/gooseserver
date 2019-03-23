using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goose.Events
{
    public class MacroConfirmCommandEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new MacroConfirmCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State != Player.States.Ready) return;

            string code = ((string)this.Data).Substring("/mc ".Length);

            if (this.Player.MacroCheckEvent == null)
            {
                world.Send(this.Player, "$7You don't have a current macrocheck to do.");
                return;
            }

            if (this.Player.MacroCheckEvent.Code != code)
            {
                world.Send(this.Player, "$7Macrocheck code doesn't match.. try again.");
                return;
            }

            world.LogHandler.Log(Log.Types.MacroCheckConfirm, this.Player.PlayerID, "", 0, this.Player.MapID, this.Player.MapX, this.Player.MapY);

            this.Player.MacroCheckEvent.Passed = true;
            this.Player.MacroCheckEvent = null;

            this.Player.Experience += 1000000;
            world.Send(this.Player, "$7Macrocheck passed. You earned 1mil experience.");
            world.Send(this.Player, this.Player.SNFString());
            world.Send(this.Player, this.Player.TNLString());
        }
    }
}
