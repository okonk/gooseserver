using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /**
     * /getitem templateid stack
     * 
     */
    public class GMGetItemCommandEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new GMGetItemCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                if (this.Player.Access != Player.AccessStatus.GameMaster) return;

                int id = 0;
                int stack = 1;

                string[] t = ((string)this.Data).Split(" ".ToCharArray());

                if (t.Length <= 2)
                {
                    try
                    {
                        id = Convert.ToInt32(t[1]);
                    }
                    catch (Exception)
                    {
                        return;
                    }
                }
                if (t.Length >= 3)
                {
                    try
                    {
                        id = Convert.ToInt32(t[1]);
                        stack = Convert.ToInt32(t[2]);
                    }
                    catch (Exception)
                    {
                        stack = 1;
                    }
                }

                if (id <= 0 || stack <= 0) return;

                ItemTemplate template = world.ItemHandler.GetTemplate(id);
                if (template == null) return;

                Item item = new Item();
                item.LoadFromTemplate(template);

                world.ItemHandler.AddItem(item);

                this.Player.Inventory.AddItem(item, stack, world);

                world.LogHandler.Log(Log.Types.GetItem,
                    this.Player.PlayerID, item.Name + " " + item.ItemID + " " + stack,
                    0, this.Player.Map.ID, this.Player.MapX, this.Player.MapY);
            }
        }
    }
}
