using System.Text;

namespace Goose.Events
{
    /**
     * /getitem templateid stack
     * 
     */
    public class GetItemCommandEvent : Event
    {
        private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                int id = 0;
                int stack = 1;
                bool powerful = false;

                string[] t = ((string)this.Data).Split(' ', 5);

                if (t.Length >= 2)
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
                        stack = Convert.ToInt32(t[2]);
                    }
                    catch (Exception)
                    {
                        stack = 1;

                        powerful = t[2].Equals("powerful", StringComparison.OrdinalIgnoreCase);
                    }
                }
                if (t.Length >= 4)
                {
                    powerful = t[3].Equals("powerful", StringComparison.OrdinalIgnoreCase);
                }

                if (id <= 0 || stack <= 0) return;

                ItemTemplate? template = world.ItemHandler.GetTemplate(id);
                if (template is null) return;

                Item item = new Item();
                if (!item.LoadFromTemplate(template))
                {
                    log.Error("item template {0}: invalid template; item not given", id);
                    return;
                }

                world.ItemHandler.AddAndAssignId(item, world);

                this.Player.Inventory.AddItem(item, stack, world);

                world.LogHandler.Log(Log.Types.GetItem,
                    this.Player.PlayerID, item.Name + " " + item.ItemID + " " + stack,
                    0, this.Player.Map.ID, this.Player.MapX, this.Player.MapY);
            }
        }
    }
}
