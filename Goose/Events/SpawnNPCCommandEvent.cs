using System.Text;

namespace Goose.Events
{
    /**
     * /spawnnpc id
     * 
     */
    public class SpawnNPCCommandEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.Player.State != Player.States.Ready) return;

            int id = 0;

            string[] t = ((string)this.Data).Split(" ".ToCharArray(), 2);

            if (t.Length < 2) return;

            try
            {
                id = Convert.ToInt32(t[1]);
            }
            catch (Exception)
            {
                return;
            }

            if (id <= 0) return;

            NPCTemplate template = world.NPCHandler.GetNPCTemplate(id);
            if (template == null) return;

            world.NPCHandler.SpawnNPC(world, this.Player.Map.ID, this.Player.MapX, this.Player.MapY, template, shouldRespawn: false);

            world.LogHandler.Log(Log.Types.SpawnedNPC,
                this.Player.PlayerID, template.Name,
                template.NPCTemplateID, this.Player.Map.ID, this.Player.MapX, this.Player.MapY);
        }
    }
}
