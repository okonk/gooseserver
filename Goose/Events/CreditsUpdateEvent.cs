using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;

namespace Goose.Events
{
    public class CreditsUpdateEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            List<string> redeemed = new List<string>();
            Player player;
            int credits;

            SqlCommand command = new SqlCommand("SELECT txn_id, player_name, credits FROM paypal_payments WHERE redeemed='0';", world.SqlConnection);
            SqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                player = world.PlayerHandler.GetPlayerFromData(reader["player_name"].ToString());

                if (player != null)
                {
                    credits = Convert.ToInt32(reader["credits"]);
                    player.Credits += credits;

                    if (player.State == Player.States.Ready)
                    {
                        world.Send(player, "$7You have gained " + credits + " donation credits.");
                    }
                    else
                    {
                        player.SaveToDatabase(world);
                    }

                    redeemed.Add(reader["txn_id"].ToString());

                    world.LogHandler.Log(Log.Types.ReceivedCredits,
                        player.PlayerID, credits.ToString());
                }
            }
            reader.Close();

            foreach (string r in redeemed)
            {
                command.CommandText = "UPDATE paypal_payments SET redeemed='1' WHERE txn_id='" + r + "';";
                command.ExecuteNonQuery();
            }

            this.Ticks += world.TimerFrequency * GameSettings.Default.CreditUpdateInterval;
            world.EventHandler.AddEvent(this);
        }
    }
}
