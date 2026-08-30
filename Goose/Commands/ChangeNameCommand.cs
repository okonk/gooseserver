namespace Goose.Commands
{
    [Command("/changename ", AccessPrivilege.ChangeName, Section = "GM", Help = "Rename a player.")]
    public sealed class ChangeNameCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, string oldname, string newname)
        {
            var world = ctx.World;

            Player? playerCheck = world.PlayerHandler.GetPlayerFromData(newname);
            if (playerCheck is not null)
            {
                ctx.Send("New name " + newname + " is already used.");
                return;
            }

            Player? player = world.PlayerHandler.GetPlayerFromData(oldname);
            if (player is null)
            {
                ctx.Send("Old name " + oldname + " doesn't exist.");
                return;
            }

            world.PlayerHandler.RemovePlayerFromData(player);

            if (player.State != Player.States.NotLoggedIn)
            {
                world.PlayerHandler.RenamePlayer(player, newname);
            }

            player.Name = newname;
            world.PlayerHandler.AddPlayerToData(player);

            ctx.Send("Changed name successfully.");

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
    }
}
