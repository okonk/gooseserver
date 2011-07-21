using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    class HairdyeCommandEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new HairdyeCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                string data = ((string)this.Data).Substring(9);

                if (data.Length == 0)
                {
                    world.Send(this.Player, "$7/hairdye [preview|kill|gogodyeme] <r> <g> <b> <a>");
                    return;
                }

                string[] tokens = data.Split(' ');
                int r,g,b,a;
                string error;

                switch (tokens[0].ToLower())
                {
                    case "gogodyeme":
                        if (this.Player.Credits < 1)
                        {
                            world.Send(this.Player, "$7/hairdye gogodyeme requires 1 credit, and you have 0.");
                            return;
                        }

                        error = ParseRGBA(tokens, out r, out g, out b, out a);
                        if (error != null)
                        {
                            world.Send(this.Player, error);
                            return;
                        }

                        this.Player.Credits -= 1;
                        this.Player.HairR = r;
                        this.Player.HairG = g;
                        this.Player.HairB = b;
                        this.Player.HairA = a;

                        string chpstring = this.Player.CHPString();
                        world.Send(this.Player, chpstring);
                        foreach (Player player in this.Player.Map.GetPlayersInRange(this.Player))
                        {
                            world.Send(player, chpstring);
                        }

                        break;
                    case "preview":
                        error = ParseRGBA(tokens, out r, out g, out b, out a);
                        if (error != null)
                        {
                            world.Send(this.Player, error);
                            return;
                        }

                        int prevx = this.Player.MapX;
                        int prevy = this.Player.MapY;

                        if (prevx == 1) prevx += 1;
                        else prevx -= 1;

                        world.Send(this.Player, 
                           "MKC" + 9000 + "," +
                           "1," +
                           "Hairdye Preview" + "," +
                           "," +
                           "," +
                           "" + "," + // Guild name
                           prevx + "," +
                           prevy + "," +
                           this.Player.Facing + "," +
                           100 + "," + // HP %
                           11 + "," +
                           1 + "," +
                           this.Player.HairID + "," +
                           "0,*,0,*,0,*,0,*,0,*,0,*," + // Note: EquippedDisplay() adds it's own , on end
                           r + "," +
                           g + "," +
                           b + "," +
                           a + "," +
                           "0" + ",0"); // Invis thing
                        break;
                    case "kill":
                        world.Send(this.Player, "ERC9000");
                        break;
                }
            }
        }

        /// <summary>
        /// Skips first token
        /// </summary>
        /// <param name="tokens"></param>
        /// <param name="r"></param>
        /// <param name="g"></param>
        /// <param name="b"></param>
        /// <param name="a"></param>
        /// <returns></returns>
        public static string ParseRGBA(string[] tokens, out int r, out int g, out int b, out int a)
        {
            if (tokens.Length < 5)
            {
                r = 0;
                g = 0;
                b = 0;
                a = 0;
                return "$7/hairdye [preview|kill|gogodyeme] <r> <g> <b> <a>";
            }

            try
            {
                r = Convert.ToInt32(tokens[1]);
                g = Convert.ToInt32(tokens[2]);
                b = Convert.ToInt32(tokens[3]);
                a = Convert.ToInt32(tokens[4]);
            }
            catch (Exception)
            {
                r = -1;
                g = -1;
                b = -1;
                a = -1;
            }

            if (r < 0 || r > 255) return "$7/hairdye: invalid r value";
            if (g < 0 || g > 255) return "$7/hairdye: invalid g value";
            if (b < 0 || b > 255) return "$7/hairdye: invalid b value";
            if (a < 0 || a > 255) return "$7/hairdye: invalid a value";

            return null;
        }
    }
}
