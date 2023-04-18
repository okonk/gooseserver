using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    public class GMHaxCommandEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new GMHaxCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready && this.Player.Access == Player.AccessStatus.GameMaster)
            {
                var data = ((string)this.Data).Substring("/gmhax ".Length);

                var player = this.Player;
                int pose = player.BodyState;
                ItemSlot weapon = player.Inventory.GetEquippedSlot(Inventory.EquipSlots.Weapon);
                if (weapon != null)
                {
                    pose = weapon.Item.BodyState;
                }

                var chp = "CHP" +
                    player.LoginID + "," +
                    player.CurrentBodyID + "," +
                    player.BodyR + "," + // Body Color R
                    player.BodyG + "," + // Body Color G
                    player.BodyB + "," + // Body Color B
                    player.BodyA + "," + // Body Color A
                    (player.CurrentBodyID >= 100 ? 3 : pose) + "," +
                    (player.CurrentBodyID >= 100 ? "" : player.HairID + ",") +
                    (player.CurrentBodyID >= 100 ? "" : player.Inventory.EquippedDisplay()) + // Note: EquippedDisplay() adds it's own , on end
                    (player.CurrentBodyID >= 100 ? "" : player.HairR + ",") +
                    (player.CurrentBodyID >= 100 ? "" : player.HairG + ",") +
                    (player.CurrentBodyID >= 100 ? "" : player.HairB + ",") +
                    (player.CurrentBodyID >= 100 ? "" : player.HairA + ",") +
                    "0" + "," + // Invis thing
                    (player.CurrentBodyID >= 100 ? "" : player.FaceID + ",") +
                    data + "," + // Move Speed
                    (player.CurrentBodyID >= 100 ? "" : player.Inventory.MountDisplay()); // Mount

                world.Send(this.Player, chp);


                // string data = ((string)this.Data).Substring(7);
                // world.Send(this.Player, data);
                // foreach (var player in this.Player.Map.GetPlayersInRange(this.Player))
                // {
                //     world.Send(player, data);
                // }
            }
        }
    }
}
