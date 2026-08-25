using System.Text;

namespace Goose.Events
{
    public class SaveConfigCommandEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready &&
                this.Player.Access == Player.AccessStatus.GameMaster)
            {
                // Commented out because this is bad, it saves the settings to some random path in appdata
                //world.Settings.Save();
                //world.Send(this.Player, "$7Game Settings Saved.");
            }
        }
    }
}