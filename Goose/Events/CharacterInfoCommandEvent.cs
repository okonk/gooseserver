using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    class CharacterInfoCommandEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                foreach (Window window in this.Player.Windows)
                {
                    if (window.Type == Window.WindowTypes.CharInfo)
                    {
                        window.Refresh(this.Player, world);
                        return;
                    }
                }

                Window w = new Window();
                w.Title = "Character Info:";
                w.Type = Window.WindowTypes.CharInfo;
                w.Buttons = "0,0,0,0,0";

                this.Player.Windows.Add(w);
                w.Create(this.Player, world);
            }
        }
    }
}
