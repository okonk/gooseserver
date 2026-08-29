using System.Text;
using System.Reflection;

namespace Goose.Events
{
    public class SetConfigCommandEvent : Event
    {
        private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                string data = (string)this.Data;
                if (data.Length < 11) { world.Send(this.Player, P.ServerMessage("Usage: /setconfig <setting> <value>")); return; }
                string rest = data.Substring(11).Trim();
                if (rest.Length == 0) { world.Send(this.Player, P.ServerMessage("Usage: /setconfig <setting> <value>")); return; }
                string[] tokens = rest.Split(' ', 2);
                if (tokens.Length < 2) { world.Send(this.Player, P.ServerMessage("Usage: /setconfig <setting> <value>")); return; }

                // Reflection.. fun
                // Get GameSettings type
                Type gs = world.Settings.GetType();
                // Try to get the property specified
                PropertyInfo? prop = gs.GetProperty(tokens[0]);
                // Couldn't find property.. error and return
                if (prop is null)
                {
                    world.Send(this.Player, P.ServerMessage("Couldn't find Game Setting: " + tokens[0] + "."));
                    return;
                }
                // Get Setter/Getter
                MethodInfo? setter = prop.GetSetMethod();
                MethodInfo? getter = prop.GetGetMethod();
                // If string we can just set directly
                if (getter!.ReturnType == typeof(string))
                {
                    setter!.Invoke(world.Settings, new object[] { tokens[1] });
                }
                else
                {
                    // Else we have to get a parser from the return type of the getter
                    // And Set the value to the parsed 
                    try
                    {
                        MethodInfo? parser = getter!.ReturnType.GetMethod("Parse", new Type[] { typeof(string) });
                        setter!.Invoke(world.Settings,
                            new object[] { parser!.Invoke(null, new object[] { tokens[1] })! });
                    }
                    catch (Exception e)
                    {
                        log.Error(e, "SetConfigCommand {0} {1} Exception", tokens[0], tokens[1]);
                        world.Send(this.Player, P.ServerMessage("Couldn't set value '" + tokens[1] + "' for " + tokens[0] + "."));
                        return;
                    }
                }

                world.SendToAll(P.ServerMessage("[GM] Set Game Setting " + tokens[0] + " to: " + tokens[1]));
            }
        }
    }
}