using System.Globalization;

namespace Goose.Events
{
    /**
     * TextInputSubmitEvent
     *
     * Player submitted text for a generic text input window
     *
     * Format: TXT<windowId>,<text>
     *
     */
    public class TextInputSubmitEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.Player.RefuseWhileDead(world)) return;
            if (this.Player.State != Player.States.Ready)
                return;

            string[] t = ((string)this.Data).Substring(3).Split(',', 2);
            if (t.Length != 2) return;

            if (!int.TryParse(t[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int windowId))
                return;

            var window = this.Player.Windows.OfType<TextInputWindow>().FirstOrDefault(w => w.ID == windowId);
            if (window is null) return;

            window.Submit(t[1], this.Player, world);
        }
    }
}
