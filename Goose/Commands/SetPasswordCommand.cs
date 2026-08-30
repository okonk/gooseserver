namespace Goose.Commands
{
    [Command("/setpassword ", AccessPrivilege.SetPassword, Section = "GM", Help = "Set a player's password.")]
    public sealed class SetPasswordCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, string name, string[] password)
        {
            var world = ctx.World;

            Player? player = world.PlayerHandler.GetPlayerFromData(name);
            if (player is null)
            {
                ctx.Send("Couldn't find player.");
                return;
            }

            string passwordText = string.Join(" ", password);
            if (passwordText.Length < 3)
            {
                ctx.Send("Password needs to be more than 3 characters long.");
                return;
            }
            if (passwordText.Length > 16)
            {
                ctx.Send("Password needs to be 16 characters or fewer.");
                return;
            }

            player.SetPassword(passwordText);

            ctx.Send("Password has been changed.");

            world.LogHandler.Log(Log.Types.SetPassword,
                ctx.Player.PlayerID, $"Set password of {player.Name}",
                player.PlayerID, ctx.Player.Map.ID, ctx.Player.MapX, ctx.Player.MapY);
        }
    }
}
