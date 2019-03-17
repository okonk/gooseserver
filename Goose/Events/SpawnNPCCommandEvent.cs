using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /**
     * /spawnnpc id
     * 
     */
    public class SpawnNPCCommandEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new SpawnNPCCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State != Player.States.Ready) return;
            if (!this.Player.HasPrivilege(AccessPrivilege.SpawnNPC)) return;

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

            new NPC().LoadFromTemplate(world, this.Player.Map.ID, this.Player.MapX, this.Player.MapY, template, shouldRespawn: false);

            world.LogHandler.Log(Log.Types.SpawnedNPC,
                this.Player.PlayerID, template.Name,
                template.NPCTemplateID, this.Player.Map.ID, this.Player.MapX, this.Player.MapY);
        }
    }
}
