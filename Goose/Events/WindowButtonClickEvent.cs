using System.Text;

namespace Goose.Events
{
    /**
     * WindowButtonClickEvent
     *
     * Player clicked a button in a window
     *
     * Format: WBCbuttonid,windowid,npcid,0,0
     *
     * 0s are currently unknown
     *
     */
    public class WindowButtonClickEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                int buttonid = 0;
                int windowid = 0;
                int npcid = 0;
                int id2 = 0;
                int id3 = 0;

                string[] t = ((string)this.Data).Substring(3).Split(',');

                // log bad packet
                if (t.Length != 5) return;

                try
                {
                    buttonid = Convert.ToInt32(t[0]);
                    windowid = Convert.ToInt32(t[1]);
                    npcid = Convert.ToInt32(t[2]);
                    id2 = Convert.ToInt32(t[3]);
                    id3 = Convert.ToInt32(t[4]);
                }
                catch (Exception)
                {
                    buttonid = -1;
                }

                // WBC button ids in [Window.LineClickOffset, Window.LineClickOffset + Window.LineClickCount)
                // are option-list line clicks.
                bool isLine = buttonid >= Window.LineClickOffset
                    && buttonid < Window.LineClickOffset + Window.LineClickCount;
                if (buttonid <= -1 || (!isLine && buttonid >= Enum.GetValues(typeof(Window.ButtonTypes)).Length)) return;

                foreach (var window in this.Player.Windows)
                {
                    if (window.ID == windowid)
                    {
                        if (isLine)
                            window.LineClicked(buttonid - Window.LineClickOffset, npcid, this.Player, world);
                        else
                            window.Clicked((Window.ButtonTypes)buttonid, npcid, id2, id3, this.Player, world);

                        return;
                    }
                }
            }
        }
    }
}
