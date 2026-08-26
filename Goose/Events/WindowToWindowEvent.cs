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
					string[] t = data.Split(',');
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

				ItemContainerWindow? fromWindow = null;
				ItemContainerWindow? toWindow = null;

				foreach (var window in this.Player.Windows)
				{
					if (fromWindow is not null && toWindow is not null) break;

					if (window.ID == fromWindowId)
					{
						fromWindow = window as ItemContainerWindow;
					}

                    if (window.ID == toWindowId)
                    {
						toWindow = window as ItemContainerWindow;
					}
				}

				if (fromWindow is null || toWindow is null) return;

				ItemContainerWindow.WindowToWindow(this.Player, fromWindow, fromWindowSlot, toWindow, toWindowSlot, world);
			}
		}
	}
}
