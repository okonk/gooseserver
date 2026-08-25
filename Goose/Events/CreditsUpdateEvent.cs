using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    public class CreditsUpdateEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            List<string> redeemed = new List<string>();
            Player player;
            int credits;

            var pendingSaves = new List<(Player Player, int Credits, string TxnId)>();

            world.Database.Execute(conn =>
            {
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = "SELECT txn_id, player_name, credits FROM paypal_payments WHERE redeemed='0';";
                    using var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        player = world.PlayerHandler.GetPlayerFromData(reader["player_name"].ToString());

                        if (player != null)
                        {
                            credits = Convert.ToInt32(reader["credits"]);
                            player.Credits += credits;

                            if (player.State == Player.States.Ready)
                            {
                                world.Send(player, P.ServerMessage("You have gained " + credits + " donation credits."));
                            }
                            else
                            {
                                pendingSaves.Add((player, credits, reader["txn_id"].ToString()));
                            }

                            redeemed.Add(reader["txn_id"].ToString());

                            world.LogHandler.Log(Log.Types.ReceivedCredits,
                                player.PlayerID, credits.ToString());
                        }
                    }
                }

                foreach (string r in redeemed)
                {
                    using var command = conn.CreateCommand();
                    command.CommandText = "UPDATE paypal_payments SET redeemed='1' WHERE txn_id='" + r + "';";
                    command.ExecuteNonQuery();
                }
            });

            foreach (var (p, _, _) in pendingSaves)
            {
                p.SaveToDatabase(world);
            }

            this.Ticks += world.TimerFrequency * world.Settings.CreditUpdateInterval;
            world.EventHandler.AddEvent(this);
        }
    }
}
