using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
	/**
     * WindowToWindowEvent
     * 
     * 
     */
	class WindowToWindowEvent : Event
	{
		public static Event Create(Player player, Object data)
		{
			Event e = new WindowToWindowEvent();
			e.Player = player;
			e.Data = data;

			return e;
		}

		public override void Ready(GameWorld world)
		{
			if (this.Player.State == Player.States.Ready)
			{
				int fromWindowId;
				int fromWindowSlot;
				int toWindowId;
				int toWindowSlot;
				string data = ((string)this.Data).Substring(3);

				try
				{
					string[] t = data.Split(",".ToCharArray());
					fromWindowId = Convert.ToInt32(t[0]);
					fromWindowSlot = Convert.ToInt32(t[1]);
					toWindowId = Convert.ToInt32(t[2]);
					toWindowSlot = Convert.ToInt32(t[3]);
				}
				catch (Exception)
				{
					fromWindowId = 0;
					fromWindowSlot = 0;
					toWindowId = 0;
					toWindowSlot = 0;
				}

				if (fromWindowId <= 0 || fromWindowSlot <= 0 || toWindowId <= 0 || toWindowSlot <= 0) return;
				if (fromWindowId == toWindowId && fromWindowSlot == toWindowSlot) return;

				ItemContainerWindow fromWindow = null;
				ItemContainerWindow toWindow = null;

				foreach (Window window in this.Player.Windows)
				{
					if (fromWindow != null && toWindow != null) break;

					if (window.ID == fromWindowId)
					{
						fromWindow = window as ItemContainerWindow;
					}

                    if (window.ID == toWindowId)
                    {
						toWindow = window as ItemContainerWindow;
					}
				}

				if (fromWindow == null || toWindow == null) return;

				ItemContainerWindow.WindowToWindow(this.Player, fromWindow, fromWindowSlot, toWindow, toWindowSlot, world);
			}
		}
	}
}
