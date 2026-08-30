namespace Goose.Commands
{
    [Command("/playerinfo ", AccessPrivilege.PlayerInfoCheck, Section = "GM", Help = "Open a player's info window.")]
    public sealed class PlayerInfoCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, string name)
        {
            Player? player = ctx.World.PlayerHandler.GetPlayerFromData(name);
            if (player is not null)
            {
                PlayerInfoWindow.Open(ctx.World, ctx.Player, player);
            }
            else
            {
                ctx.Send("Couldn't find player.");
            }
        }
    }
}
