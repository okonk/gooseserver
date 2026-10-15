using System.Globalization;

namespace Goose.Events
{
    /**
     * AmountWindowSubmitEvent
     *
     * Player submitted an amount for a generic amount window
     *
     * Format: AMT<windowId>,<amount>
     *
     */
    public class AmountWindowSubmitEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.Player.State != Player.States.Ready)
                return;

            string[] t = ((string)this.Data).Substring(3).Split(',', 2);
            if (t.Length != 2) return;

            if (!int.TryParse(t[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int windowId))
                return;

            if (!long.TryParse(t[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out long amount))
                return;

            var window = this.Player.Windows.OfType<AmountWindow>().FirstOrDefault(w => w.ID == windowId);
            if (window is null) return;

            window.Submit(amount, this.Player, world);
        }
    }
}
