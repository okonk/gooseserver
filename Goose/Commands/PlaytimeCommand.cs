using System.Text;

namespace Goose.Commands
{
    [Command("/playtime", Section = "General", Help = "Show your total play and AFK time.")]
    public sealed class PlaytimeCommand : BaseCommand
    {
        public void Execute(CommandContext ctx)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("You have spent");

            bool needcomma = false;
            TimeSpan afkTime = TimeSpan.FromSeconds(ctx.Player.TotalAfkTime);
            if (afkTime.Days > 0)
            {
                builder.AppendFormat(" {0} days", afkTime.Days);
                needcomma = true;
            }
            if (afkTime.Hours > 0)
            {
                if (needcomma) builder.Append(",");
                builder.AppendFormat(" {0} hours", afkTime.Hours);
                needcomma = true;
            }
            if (afkTime.Minutes > 0)
            {
                if (needcomma) builder.Append(",");
                builder.AppendFormat(" {0} minutes", afkTime.Minutes);
                needcomma = true;
            }
            if (!needcomma)
            {
                builder.Append(" no time");
            }
            builder.Append(" AFK. And");
            needcomma = false;
            TimeSpan playTime = TimeSpan.FromSeconds(ctx.Player.TotalPlayTime);
            if (playTime.Days > 0)
            {
                builder.AppendFormat(" {0} days", playTime.Days);
                needcomma = true;
            }
            if (playTime.Hours > 0)
            {
                if (needcomma) builder.Append(",");
                builder.AppendFormat(" {0} hours", playTime.Hours);
                needcomma = true;
            }
            if (playTime.Minutes > 0)
            {
                if (needcomma) builder.Append(",");
                builder.AppendFormat(" {0} minutes", playTime.Minutes);
                needcomma = true;
            }
            if (!needcomma)
            {
                builder.Append(" no time");
            }
            builder.Append(" playing.");

            ctx.World.Send(ctx.Player, P.ServerMessage(builder.ToString()));
        }
    }
}
