namespace Goose.Commands
{
    [Command("/setaccess", AccessPrivilege.SetAccess, Section = "Admin", Help = "Change a player's access level.")]
    public sealed class SetAccessCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, string name, string access)
        {
            var world = ctx.World;

            Player? player = world.PlayerHandler.GetPlayerFromData(name);
            if (player is not null)
            {
                try
                {
                    var accessStatus = Enum.GetValues<Player.AccessStatus>().Where(y => y.ToString().Equals(access, StringComparison.OrdinalIgnoreCase)).First();
                    player.Access = accessStatus;
                    ctx.Send($"Set AccessStatus for {player.Name} to {player.Access}.");

                    if (player.State == Player.States.NotLoggedIn)
                    {
                        player.SaveToDatabase(world);
                    }
                }
                catch
                {
                    ctx.Send("Couldn't parse access value.");
                }
            }
            else
            {
                ctx.Send("Couldn't find player.");
            }
        }
    }
}
