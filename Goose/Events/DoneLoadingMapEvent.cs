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
                world.Send(this.Player, P.DoneSendingMap());
                world.Send(this.Player, P.MakeCharacter(this.Player));
                world.Send(this.Player, P.SetYourCharacter(this.Player.LoginID));

                string gmstring = P.AdminMode(this.Player.LoginID);
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
                        world.Send(player, P.MakeCharacter(this.Player));
                        if (this.Player.HasPrivilege(AccessPrivilege.GMInvisible))
                        {
                            world.Send(player, gmstring);
                        }
                    }

                    if (!player.IsGMInvisible)
                    {
                        world.Send(this.Player, P.MakeCharacter(player));
                        if (player.HasPrivilege(AccessPrivilege.GMInvisible))
                        {
                            world.Send(this.Player, P.AdminMode(player.LoginID));
                        }
                    }
                }

                List<NPC> npcrange = map.GetNPCsInRange(this.Player);
                foreach (NPC npc in npcrange)
                {
                    world.Send(this.Player, P.MakeNPCCharacter(npc));

                    if (!this.Player.IsGMInvisible)
                        npc.AggroIfInRange(this.Player, world);
                }

                this.Player.Map = map;
                this.Player.Map.AddPlayer(this.Player, world);

                this.Player.AddRegenEvent(world);

                world.Send(this.Player, P.ExpBar(this.Player));
                world.Send(this.Player, P.WeaponSpeed(this.Player));

                this.Player.SendBuffBar(world);

                foreach (ItemTile tile in this.Player.Map.Items)
                {
                    world.Send(this.Player, P.MakeObject(tile));
                }

                if (this.Player.Group != null)
                {
                    this.Player.Group.SendPartyWindow(this.Player, world);
                }
            }
        }
    }
}
