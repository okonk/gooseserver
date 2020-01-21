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
                string data = ((string)this.Data).Substring(8);

                if (string.IsNullOrWhiteSpace(data) || data.ToLower() == " help")
                {
                    world.Send(this.Player, P.ServerMessage("/hairdye [preview|kill|accept] <r> <g> <b> <a>"));
                    return;
                }

                string[] tokens = data.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                int r, g, b, a;
                string error;

                switch (tokens[0].ToLower())
                {
                    case "accept":
                    case "gogodyeme":
                        if (this.Player.Gold < GameSettings.Default.HairdyeCommandCost)
                        {
                            world.Send(this.Player, P.ServerMessage(string.Format("/hairdye accept requires {0} gold.", GameSettings.Default.HairdyeCommandCost)));
                            return;
                        }

                        error = ParseRGBA(tokens, out r, out g, out b, out a);
                        if (error != null)
                        {
                            world.Send(this.Player, error);
                            return;
                        }

                        this.Player.Gold -= GameSettings.Default.HairdyeCommandCost;
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

                        int pose = this.Player.BodyState;
                        ItemSlot weapon = this.Player.Inventory.GetEquippedSlot(Inventory.EquipSlots.Weapon);
                        if (weapon != null)
                        {
                            pose = weapon.Item.BodyState;
                        }

                        world.Send(this.Player,
                            "MKC" + 9000 + "," +
                            "1," +
                            "Hairdye Preview," +
                            "," +
                            "," +
                            "" + "," + // Guild name
                            prevx + "," +
                            prevy + "," +
                            this.Player.Facing + "," +
                            100 + "," + // HP %
                            this.Player.BodyID + "," +
                            this.Player.BodyR + "," + // Body Color R
                            this.Player.BodyG + "," + // Body Color G
                            this.Player.BodyB + "," + // Body Color B
                            this.Player.BodyA + "," + // Body Color A
                            pose + "," +
                            this.Player.HairID + "," +
                            this.Player.Inventory.EquippedDisplay() + // Note: EquippedDisplay() adds it's own , on end
                            r + "," +
                            g + "," +
                            b + "," +
                            a + "," +
                            "0" + "," + // Invis thing
                            this.Player.FaceID + "," +
                            this.Player.CalculateMoveSpeed() + "," + // Move Speed
                            "0" + "," + // Player Name Color
                            this.Player.Inventory.MountDisplay()); // Mount
                        break;
                    case "kill":
                        world.Send(this.Player, P.EraseCharacter(9000));
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
                return P.ServerMessage("/hairdye [preview|kill|gogodyeme] <r> <g> <b> <a>");
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

            if (r < 0 || r > 255) return P.ServerMessage("/hairdye: invalid r value");
            if (g < 0 || g > 255) return P.ServerMessage("/hairdye: invalid g value");
            if (b < 0 || b > 255) return P.ServerMessage("/hairdye: invalid b value");
            if (a < 0 || a > 255) return P.ServerMessage("/hairdye: invalid a value");

            return null;
        }
    }
}
