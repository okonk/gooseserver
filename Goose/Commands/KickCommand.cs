namespace Goose.Commands
{
    [Command("/kick ", AccessPrivilege.Kick, Section = "GM", Help = "Kick a player from the server.")]
    public sealed class KickCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, Player target)
        {
            if (target.State != Player.States.NotLoggedIn)
            {
                ctx.World.LogHandler.Log(Log.Types.Kick, ctx.Player.PlayerID, "", target.PlayerID);

                ctx.World.LostConnection(target.Sock);
            }
            else
            {
                ctx.Send("Couldn't find player.");
            }
        }
    }
}
