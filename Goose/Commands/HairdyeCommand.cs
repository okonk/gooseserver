namespace Goose.Commands
{
    [Command("/hairdye", Section = "General", Help = "Preview or apply a new hair color.",
        Usage = "/hairdye [preview|kill|accept] <r> <g> <b> <a>")]
    public sealed class HairdyeCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, string[] args)
        {
            var world = ctx.World;

            if (args.Length == 0 || args[0].Equals("help", StringComparison.OrdinalIgnoreCase))
            {
                ctx.Send(ctx.Usage);
                return;
            }

            int r, g, b, a;
            string? error;

            switch (args[0].ToLower())
            {
                case "accept":
                case "gogodyeme":
                    if (ctx.Player.Gold < world.Settings.HairdyeCommandCost)
                    {
                        world.Send(ctx.Player, P.ServerMessage($"/hairdye accept requires {world.Settings.HairdyeCommandCost} gold."));
                        return;
                    }

                    error = ParseRGBA(args, out r, out g, out b, out a);
                    if (error is not null)
                    {
                        world.Send(ctx.Player, error);
                        return;
                    }

                    ctx.Player.Gold -= world.Settings.HairdyeCommandCost;
                    ctx.Player.HairR = r;
                    ctx.Player.HairG = g;
                    ctx.Player.HairB = b;
                    ctx.Player.HairA = a;

                    string chpstring = P.UpdateCharacter(ctx.Player);
                    world.Send(ctx.Player, chpstring);
                    foreach (var player in ctx.Player.Map.GetPlayersInRange(ctx.Player))
                    {
                        world.Send(player, chpstring);
                    }

                    break;
                case "preview":
                    error = ParseRGBA(args, out r, out g, out b, out a);
                    if (error is not null)
                    {
                        world.Send(ctx.Player, error);
                        return;
                    }

                    int prevx = ctx.Player.MapX;
                    int prevy = ctx.Player.MapY;

                    if (prevx == 1) prevx += 1;
                    else prevx -= 1;

                    int pose = ctx.Player.BodyState;
                    ItemSlot? weapon = ctx.Player.Inventory.GetEquippedSlot(Inventory.EquipSlots.Weapon);
                    if (weapon is not null)
                    {
                        pose = weapon.Item.BodyState;
                    }

                    if (world.Settings.ServerType == "Illutia")
                    {
                        world.Send(ctx.Player,
                            "MKC" + 9000 + "," +
                            "1," +
                            "Hairdye Preview," +
                            "," +
                            "," +
                            "" + "," + // Guild name
                            prevx + "," +
                            prevy + "," +
                            ctx.Player.Facing + "," +
                            100 + "," + // HP %
                            ctx.Player.BodyID + "," +
                            ctx.Player.BodyR + "," + // Body Color R
                            ctx.Player.BodyG + "," + // Body Color G
                            ctx.Player.BodyB + "," + // Body Color B
                            ctx.Player.BodyA + "," + // Body Color A
                            pose + "," +
                            ctx.Player.HairID + "," +
                            ctx.Player.Inventory.EquippedDisplay() + // Note: EquippedDisplay() adds it's own , on end
                            r + "," +
                            g + "," +
                            b + "," +
                            a + "," +
                            "0" + "," + // Invis thing
                            ctx.Player.FaceID + "," +
                            ctx.Player.CalculateMoveSpeed() + "," + // Move Speed
                            "0" + "," + // Player Name Color
                            ctx.Player.Inventory.MountDisplay()); // Mount
                    }
                    else
                    {
                        world.Send(ctx.Player,
                            "MKC" + 9000 + "," +
                            "1," +
                            "Hairdye Preview," +
                            "," +
                            "," +
                            "" + "," + // Guild name
                            prevx + "," +
                            prevy + "," +
                            ctx.Player.Facing + "," +
                            100 + "," + // HP %
                            ctx.Player.BodyID + "," +
                            ctx.Player.BodyR + "," + // Body Color R
                            ctx.Player.BodyG + "," + // Body Color G
                            ctx.Player.BodyB + "," + // Body Color B
                            ctx.Player.BodyA + "," + // Body Color A
                            pose + "," +
                            ctx.Player.HairID + "," +
                            ctx.Player.Inventory.EquippedDisplay() + // Note: EquippedDisplay() adds it's own , on end
                            r + "," +
                            g + "," +
                            b + "," +
                            a + "," +
                            "0" + "," + // Invis thing
                            ctx.Player.FaceID);
                    }
                    break;
                case "kill":
                    world.Send(ctx.Player, P.EraseCharacter(9000));
                    break;
            }
        }

        private static string? ParseRGBA(string[] tokens, out int r, out int g, out int b, out int a)
        {
            if (tokens.Length < 5)
            {
                r = 0;
                g = 0;
                b = 0;
                a = 0;
                return P.ServerMessage("/hairdye [preview|kill|accept] <r> <g> <b> <a>");
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
