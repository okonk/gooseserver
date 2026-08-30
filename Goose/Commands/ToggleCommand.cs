namespace Goose.Commands
{
    [Command("/toggle ", Section = "General", Help = "Toggle a personal display or visibility setting.")]
    public sealed class ToggleCommand : BaseCommand
    {
        protected override AccessPrivilege? CheckAccess(CommandContext ctx, string[] args)
        {
            if (args.Length == 0) return null;
            return args[0].ToLower() switch
            {
                "gm-invisible" or "invisible" => AccessPrivilege.GMInvisible,
                "who-invisible" or "whoinvisible" => AccessPrivilege.WhoInvisible,
                _ => null
            };
        }

        public void Execute(CommandContext ctx, string setting)
        {
            var world = ctx.World;

            switch (setting.ToLower())
            {
                case "exp":
                case "experience":
                    ctx.Player.ToggleSettings ^= Player.ToggleSetting.Experience;
                    if ((ctx.Player.ToggleSettings & Player.ToggleSetting.Experience) == 0)
                    {
                        world.Send(ctx.Player, P.ServerMessage("Experience display is enabled."));
                    }
                    else
                    {
                        world.Send(ctx.Player, P.ServerMessage("Experience display is disabled."));
                    }
                    break;
                case "tell":
                    ctx.Player.ToggleSettings ^= Player.ToggleSetting.Tell;
                    if ((ctx.Player.ToggleSettings & Player.ToggleSetting.Tell) == 0)
                    {
                        world.Send(ctx.Player, P.ServerMessage("Tells are enabled."));
                    }
                    else
                    {
                        world.Send(ctx.Player, P.ServerMessage("Tells are disabled."));
                    }
                    break;
                case "swear":
                case "word":
                case "curse":
                    ctx.Player.ToggleSettings ^= Player.ToggleSetting.WordFilter;
                    if ((ctx.Player.ToggleSettings & Player.ToggleSetting.WordFilter) == 0)
                    {
                        world.Send(ctx.Player, P.ServerMessage("Word filter is enabled."));
                    }
                    else
                    {
                        world.Send(ctx.Player, P.ServerMessage("Word filter is disabled."));
                    }
                    break;
                case "quest":
                    ctx.Player.ToggleSettings ^= Player.ToggleSetting.QuestCredit;
                    if (ctx.Player.QuestCreditFilterEnabled)
                    {
                        world.Send(ctx.Player, P.ServerMessage("Quest credit filter is enabled."));
                    }
                    else
                    {
                        world.Send(ctx.Player, P.ServerMessage("Quest credit filter is disabled."));
                    }
                    break;
                case "gm-invisible":
                case "invisible":
                    ctx.Player.ToggleSettings ^= Player.ToggleSetting.GMInvisible;
                    if ((ctx.Player.ToggleSettings & Player.ToggleSetting.GMInvisible) == 0)
                    {
                        world.Send(ctx.Player, P.ServerMessage("You are now invisible."));

                        ctx.Player.Map.SetCharacter(null, ctx.Player.MapX, ctx.Player.MapY);
                        string erc = P.EraseCharacter(ctx.Player.LoginID);
                        foreach (var player in ctx.Player.Map.GetPlayersInRange(ctx.Player))
                        {
                            world.Send(player, erc);
                        }
                    }
                    else
                    {
                        world.Send(ctx.Player, P.ServerMessage("You are now visible."));
                        ctx.Player.WarpTo(world, ctx.Player.Map, ctx.Player.MapX, ctx.Player.MapY);
                    }
                    break;
                case "who-invisible":
                case "whoinvisible":
                    ctx.Player.ToggleSettings ^= Player.ToggleSetting.WhoInvisible;
                    if ((ctx.Player.ToggleSettings & Player.ToggleSetting.WhoInvisible) == 0)
                    {
                        world.Send(ctx.Player, P.ServerMessage("You are now who-invisible."));
                    }
                    else
                    {
                        world.Send(ctx.Player, P.ServerMessage("You are now who-visible."));
                    }
                    break;
                case "itembuffs":
                    ctx.Player.ToggleSettings ^= Player.ToggleSetting.ItemBuffs;
                    if ((ctx.Player.ToggleSettings & Player.ToggleSetting.ItemBuffs) == 0)
                    {
                        world.Send(ctx.Player, P.ServerMessage("Item buffs are now visible."));
                    }
                    else
                    {
                        world.Send(ctx.Player, P.ServerMessage("Item buffs are now hidden."));
                    }
                    ctx.Player.SendBuffBar(world);
                    break;
                default:
                    world.Send(ctx.Player, P.ServerMessage("/toggle [experience|tell|curse|quest|itembuffs]"));
                    break;
            }
        }
    }
}
