using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /**
     * ChangeClassEvent, changes a players class if they're a commoner
     * 
     * Syntax: /changeclass [rogue|magus|warrior|priest]
     * 
     */
    class ChangeClassEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new ChangeClassEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                if (!this.Player.Class.ClassName.Equals("Commoner"))
                {
                    world.Send(this.Player, P.ServerMessage("/changeclass: You need to be a Commoner to change class."));
                    return;
                }

                string packet = (string)this.Data;
                string[] tokens = packet.Split(" ".ToCharArray());
                if (tokens.Length < 2) return;

                string cl = tokens[1];

                switch (cl.ToLower())
                {
                    case "rogue":
                        this.Player.ChangeClass(2, 1, world);
                        break;
                    case "warrior":
                        this.Player.ChangeClass(3, 1, world);
                        break;
                    case "magus":
                        this.Player.ChangeClass(4, 1, world);
                        break;
                    case "priest":
                        this.Player.ChangeClass(5, 1, world);
                        break;
                    default:
                        world.Send(this.Player, P.ServerMessage("/changeclass [rogue|magus|warrior|priest]"));
                        break;
                }
            }
        }
    }
}
