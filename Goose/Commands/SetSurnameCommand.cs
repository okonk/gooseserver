namespace Goose.Commands
{
    [Command("/setsurname ", AccessPrivilege.SetSurname, Section = "GM", Help = "Set a player's surname without unlocking it.")]
    public sealed class SetSurnameCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, string name, string[] surname)
        {
            var world = ctx.World;

            string surnameText = string.Join(" ", surname);

            Player? player = world.PlayerHandler.GetPlayerFromData(name);
            if (player is not null)
            {
                player.SetSurname(surnameText, world);
                ctx.Send("Changed surname successfully.");

                if (player.State != Player.States.NotLoggedIn)
                {
                    world.Send(player, P.StatusInfo(player));
                }
                else
                {
                    player.SaveToDatabase(world);
                }
            }
            else
            {
                ctx.Send("Couldn't find player.");
            }
        }
    }
}
