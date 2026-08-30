namespace Goose.Commands
{
    [Command("/setsurname ", AccessPrivilege.SetSurname, Section = "GM", Help = "Set a player's surname.")]
    public sealed class SetSurnameCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, string name, string[] surname)
        {
            var world = ctx.World;

            string surnameText = string.Join(" ", surname);

            Player? player = world.PlayerHandler.GetPlayerFromData(name);
            if (player is not null)
            {
                player.Surname = surnameText;
                ctx.Send("Changed surname successfully.");

                if (player.State != Player.States.NotLoggedIn)
                {
                    world.Send(player, P.StatusInfo(player));

                    if (player.Map is not null)
                    {
                        List<Player> range = player.Map.GetPlayersInRange(player);

                        string packet = P.EraseCharacter(player.LoginID);
                        string packet2 = P.MakeCharacter(player);

                        foreach (var p in range)
                        {
                            world.Send(p, packet);
                            world.Send(p, packet2);
                        }
                    }
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
