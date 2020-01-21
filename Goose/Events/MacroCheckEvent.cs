using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    public class MacroCheckEvent : Event
    {
        public bool Passed { get; set; }

        public string Code { get; set; }

        public override void Ready(GameWorld world)
        {
            if (Passed)
            {
                this.Player.MacroCheckEvent = null;
                return;
            }

            if (this.Player.State == Player.States.NotLoggedIn)
            {
                // Penalty for trying to avoid check
                this.Player.Experience -= 2000000;
                this.Player.SaveToDatabase(world);
                return;
            }

            world.LogHandler.Log(Log.Types.MacroCheckFailed, this.Player.PlayerID, "", 0, this.Player.MapID, this.Player.MapX, this.Player.MapY);

            this.Player.MacroCheckFailures++;

            if (this.Player.MacroCheckFailures >= 5)
            {
                this.Player.Access = Player.AccessStatus.Banned;
                this.Player.UnbanDate = DateTime.Now.AddDays(30);

                world.Send(this.Player, P.ServerMessage("You have failed the macro check and will be banned for a month."));
            }
            else if (this.Player.MacroCheckFailures >= 2)
            {
                this.Player.Access = Player.AccessStatus.Banned;
                this.Player.UnbanDate = DateTime.Now.AddDays(7);

                world.Send(this.Player, P.ServerMessage("You have failed the macro check and will be banned for a week."));
            }
            else
            {
                world.Send(this.Player, P.ServerMessage("You have failed the macro check and have been kicked. This is your only warning."));
            }

            world.LostConnection(this.Player.Sock);
        }
    }
}
