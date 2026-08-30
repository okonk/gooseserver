using System.Reflection;

namespace Goose.Commands
{
    [Command("/setconfig ", AccessPrivilege.SetConfig, Section = "Admin", Help = "Change a game setting.", Usage = "/setconfig <setting> <value...>")]
    public sealed class SetConfigCommand : BaseCommand
    {
        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public void Execute(CommandContext ctx, string setting, string[] value)
        {
            var world = ctx.World;

            if (value.Length == 0)
            {
                ctx.Send(ctx.Usage);
                return;
            }

            string valueText = string.Join(" ", value);

            PropertyInfo? prop = world.Settings.GetType().GetProperty(setting);
            if (prop is null)
            {
                ctx.Send("Couldn't find Game Setting: " + setting + ".");
                return;
            }

            MethodInfo? setter = prop.GetSetMethod();
            MethodInfo? getter = prop.GetGetMethod();
            if (getter!.ReturnType == typeof(string))
            {
                setter!.Invoke(world.Settings, new object[] { valueText });
            }
            else
            {
                try
                {
                    MethodInfo? parser = getter.ReturnType.GetMethod("Parse", new Type[] { typeof(string) });
                    setter!.Invoke(world.Settings,
                        new object[] { parser!.Invoke(null, new object[] { valueText })! });
                }
                catch (Exception e)
                {
                    log.Error(e, "SetConfigCommand {0} {1} Exception", setting, valueText);
                    ctx.Send("Couldn't set value '" + valueText + "' for " + setting + ".");
                    return;
                }
            }

            world.SendToAll(P.ServerMessage("[GM] Set Game Setting " + setting + " to: " + valueText));
        }
    }
}
