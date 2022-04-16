using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /**
     * RankCommandEvent, handles /rank
     *
     * Packet: /rank [all, gold, magus, priest, warrior, rogue]
     *
     */
    class RankCommandEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new RankCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                string data = ((string)this.Data).Substring(5);

                if (data.Length <= 0)
                {
                    world.Send(this.Player, P.ServerMessage("Usage: /rank [all, gold, <classname>]"));
                    return;
                }

                data = data.Substring(1);

                Window window;
                var argumentLower = data.ToLowerInvariant();
                switch (argumentLower)
                {
                    case "all":
                        window = new Window();
                        window.Type = Window.WindowTypes.Rank;
                        window.Title = "All Ranks";
                        window.Buttons = "0,0,0,0,0";
                        window.Data = world.RankHandler.All;
                        break;
                    case "gold":
                        window = new Window();
                        window.Type = Window.WindowTypes.Rank;
                        window.Title = "Gold Ranks";
                        window.Buttons = "0,0,0,0,0";
                        window.Data = world.RankHandler.Gold;
                        break;
                    default:
                        if (!world.RankHandler.ClassRanks.TryGetValue(argumentLower, out Ranks classRank))
                        {
                            world.Send(this.Player, P.ServerMessage("Usage: /rank [all, gold, <classname>]"));
                            return;
                        }

                        window = new Window();
                        window.Type = Window.WindowTypes.Rank;
                        window.Title = $"{System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(argumentLower)} Ranks";
                        window.Buttons = "0,0,0,0,0";
                        window.Data = classRank;

                        break;
                }

                this.Player.Windows.Add(window);
                window.Create(this.Player, world);
            }
        }
    }
}
