namespace Goose.Commands
{
    [Command("/settitle ", AccessPrivilege.SetTitle, Section = "GM", Help = "Set a player's title.")]
    public sealed class SetTitleCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, string name, string[] title)
        {
            var world = ctx.World;

            string titleText = string.Join(" ", title);

            Player? player = world.PlayerHandler.GetPlayerFromData(name);
            if (player is not null)
            {
                player.Title = titleText;
                ctx.Send("Changed title successfully.");

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
