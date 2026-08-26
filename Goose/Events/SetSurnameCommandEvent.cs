using System.Text;

namespace Goose.Events
{
    class SetSurnameCommandEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                string[] tokens = ((string)this.Data).Split(' ', 3);
                string name, surname;
                if (tokens.Length < 2)
                {
                    world.Send(this.Player, P.ServerMessage("/setsurname <name> <title>"));
                    return;
                }
                if (tokens.Length == 2)
                {
                    name = tokens[1];
                    surname = "";
                }
                else
                {
                    name = tokens[1];
                    surname = tokens[2];
                }

                Player? player = world.PlayerHandler.GetPlayerFromData(name);
                if (player is not null)
                {
                    player.Surname = surname;
                    world.Send(this.Player, P.ServerMessage("Changed surname successfully."));

                    if (player.State != Goose.Player.States.NotLoggedIn)
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
                    world.Send(this.Player, P.ServerMessage("Couldn't find player."));
                }
            }
        }
    }
}
