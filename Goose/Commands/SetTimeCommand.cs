using System.Globalization;

namespace Goose.Commands
{
    [Command("/settime ", AccessPrivilege.SetMapTime, Section = "Admin", Help = "Set the time of day on your map.", Usage = "/settime <0-24|dawn|noon|dusk|night>")]
    public sealed class SetTimeCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, string time)
        {
            double? hours = null;
            switch (time.ToLowerInvariant())
            {
                case "dawn": hours = 6; break;
                case "noon": hours = 12; break;
                case "dusk": hours = 18; break;
                case "night": hours = 0; break;
                default:
                    if (double.TryParse(time, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) && value is >= 0 and <= 24)
                        hours = value;
                    break;
            }

            if (hours is null)
            {
                ctx.Send("Invalid time. Use 0-24 or dawn, noon, dusk, night.");
                return;
            }

            var map = ctx.Player.Map;
            if (!map.Ambience.HasDayCycle)
            {
                ctx.Send("This map has no day cycle.");
                return;
            }

            map.Ambience.SetTimeOfDay(hours.Value, ctx.World.TimeNow);
            map.Ambience.Broadcast(map, ctx.World);
            int h = (int)hours.Value;
            int m = (int)Math.Round((hours.Value - h) * 60);
            ctx.Send($"Set time to {h}:{m:D2}.");
        }
    }
}
