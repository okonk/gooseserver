using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Goose.Events
{
    public class SetConfigCommandEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new SetConfigCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready &&
                this.Player.HasPrivilege(AccessPrivilege.SetConfig))
            {
                string data = ((string)this.Data).Substring(11);

                string[] tokens = data.Split(" ".ToCharArray(), 2);

                // Reflection.. fun
                // Get GameSettings type
                Type gs = GameSettings.Default.GetType();
                // Try to get the property specified
                PropertyInfo prop = gs.GetProperty(tokens[0]);
                // Couldn't find property.. error and return
                if (prop == null)
                {
                    world.Send(this.Player, P.ServerMessage("Couldn't find Game Setting: " + tokens[0] + "."));
                    return;
                }
                // Get Setter/Getter
                MethodInfo setter = prop.GetSetMethod();
                MethodInfo getter = prop.GetGetMethod();
                // If string we can just set directly
                if (getter.ReturnType == typeof(string))
                {
                    setter.Invoke(GameSettings.Default, new object[] { tokens[1] });
                }
                else
                {
                    // Else we have to get a parser from the return type of the getter
                    // And Set the value to the parsed 
                    try
                    {
                        MethodInfo parser = getter.ReturnType.GetMethod("Parse", new Type[] { typeof(string) });
                        setter.Invoke(GameSettings.Default,
                            new object[] { parser.Invoke(null, new object[] { tokens[1] }) });
                    }
                    catch
                    {

                    }
                }

                world.SendToAll(P.ServerMessage("[GM] Set Game Setting " + tokens[0] + " to: " + tokens[1]));
            }
        }
    }
}