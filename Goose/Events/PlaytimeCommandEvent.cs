using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    class PlaytimeCommandEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new PlaytimeCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                StringBuilder builder = new StringBuilder();
                builder.Append("$7You have spent");

                bool needcomma = false;
                TimeSpan afkTime = TimeSpan.FromSeconds(this.Player.TotalAfkTime);
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
                TimeSpan playTime = TimeSpan.FromSeconds(this.Player.TotalPlayTime);
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

                world.Send(this.Player, builder.ToString());
            }
        }
    }
}
