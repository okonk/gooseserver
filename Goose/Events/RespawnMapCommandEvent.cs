using System.Text;

namespace Goose.Events
{
    public class RespawnMapCommandEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                foreach (NPC npc in this.Player.Map.NPCs) {
                    if (npc.State == NPC.States.Dead)
                    {
                        npc.Spawn(world);
                    }
                }

                world.SendToMap(this.Player.Map, P.ServerMessage("Respawned all NPCs."));

                world.LogHandler.Log(Log.Types.RespawnMap,
                    this.Player.PlayerID, "",
                    this.Player.Map.ID, this.Player.MapX, this.Player.MapY);
            }
        }
    }
}