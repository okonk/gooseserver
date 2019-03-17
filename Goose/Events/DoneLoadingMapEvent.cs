using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /**
     * DoneLoadingMapEvent, event for DLM
     * 
     * Called in response to SCMMapId,MapVersion,MapName
     * Packet format: DLM
     * 
     * Server responds:
     * - SMNMapname - Send Map Name, I'm unsure why it sends it twice or if it's even needed
     * - DSM - Done Sending Map
     * - MKC - See Player.MKCString() for full syntax. 
     * This is sent to all players in the range of the player. Including the player himself.
     * - SUCId - Set Your Character (I think) - Tells the client which character is the player
     * 
     */
    class DoneLoadingMapEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new DoneLoadingMapEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.LoadingMap)
            {

                Map map = world.MapHandler.GetMap(this.Player.MapID);

                if (!this.Player.IsGMInvisible)
                {
                    map.PlaceCharacter(this.Player);
                    map.SetCharacter(this.Player, this.Player.MapX, this.Player.MapY);
                }

                this.Player.State = Player.States.Ready;
                world.Send(this.Player, "DSM");
                world.Send(this.Player, this.Player.MKCString());
                world.Send(this.Player, "SUC" + this.Player.LoginID.ToString());

                string gmstring = "AMA" + this.Player.LoginID + ",1";
                if (this.Player.Access == Goose.Player.AccessStatus.GameMaster)
                {
                    world.Send(this.Player, gmstring);
                }

                // Notify people in range that someone joined
                // Notify person that joined about people in range
                List<Player> range = map.GetPlayersInRange(this.Player);
                foreach (Player player in range)
                {
                    if (!this.Player.IsGMInvisible)
                    {
                        world.Send(player, this.Player.MKCString());
                        if (this.Player.HasPrivilege(AccessPrivilege.GMInvisible))
                        {
                            world.Send(player, gmstring);
                        }
                    }

                    if (!player.IsGMInvisible)
                    {
                        world.Send(this.Player, player.MKCString());
                        if (player.HasPrivilege(AccessPrivilege.GMInvisible))
                        {
                            world.Send(this.Player, "AMA" + player.LoginID + ",1");
                        }
                    }
                }

                List<NPC> npcrange = map.GetNPCsInRange(this.Player);
                foreach (NPC npc in npcrange)
                {
                    world.Send(this.Player, npc.MKCString());

                    if (!this.Player.IsGMInvisible)
                        npc.AggroIfInRange(this.Player, world);
                }

                this.Player.Map = map;
                this.Player.Map.AddPlayer(this.Player);

                this.Player.AddRegenEvent(world);

                world.Send(this.Player, this.Player.TNLString());
                world.Send(this.Player, this.Player.WPSString());

                this.Player.SendBuffBar(world);

                foreach (ItemTile tile in this.Player.Map.Items)
                {
                    world.Send(this.Player, tile.MOBString());
                }

                if (this.Player.Group != null)
                {
                    this.Player.Group.SendPartyWindow(this.Player, world);
                }
            }
        }
    }
}
