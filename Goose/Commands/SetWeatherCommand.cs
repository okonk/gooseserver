namespace Goose.Commands
{
    [Command("/setweather ", AccessPrivilege.SetMapWeather, Section = "Admin", Help = "Set the weather on your map.", Usage = "/setweather <Clear|Rain|Fog|Storm|Snow>")]
    public sealed class SetWeatherCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, string weather)
        {
            if (!Enum.TryParse<WeatherType>(weather, ignoreCase: true, out var type) || !Enum.IsDefined(type))
            {
                ctx.Send("Unknown weather. Use: Clear, Rain, Fog, Storm, Snow.");
                return;
            }

            var map = ctx.Player.Map;
            map.Ambience.SetWeather(type);
            map.Ambience.ScheduleWeatherRoll(map, ctx.World);
            map.Ambience.Broadcast(map, ctx.World);
            ctx.Send($"Set weather to {type}.");
        }
    }
}
