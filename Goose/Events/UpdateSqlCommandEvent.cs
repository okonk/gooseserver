using System.Text;

namespace Goose.Events
{
    public class UpdateSqlCommandEvent : Event
    {
        private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                world.Send(this.Player, "$7Updating sql...");

                Task.Run(() =>
                {
                    try
                    {
                        var sqlData = CsvToSql.Core.CsvToSqlConverter.Convert(world.Settings.DataLinkId);

                        // sqlData already contains its own BEGIN TRANSACTION;/COMMIT;, so it
                        // must not be wrapped in another transaction.
                        world.Database.Enqueue(conn =>
                        {
                            using var command = conn.CreateCommand();
                            command.CommandText = sqlData;
                            try
                            {
                                command.ExecuteNonQuery();
                            }
                            catch
                            {
                                // A mid-script failure leaves the script's own transaction
                                // open on the shared connection, breaking subsequent writes.
                                try
                                {
                                    using var rollback = conn.CreateCommand();
                                    rollback.CommandText = "ROLLBACK;";
                                    rollback.ExecuteNonQuery();
                                }
                                catch
                                {
                                }

                                throw;
                            }
                        }, (e) => UpdateCompletedCallback(e, world));

                        log.Info("Added sql command to queue");
                    }
                    catch (Exception e)
                    {
                        log.Error(e, "Failed updating sql data");
                        world.Send(this.Player, "$7Failed updating sql: " + e.Message);
                    }
                });
            }
        }

        private void UpdateCompletedCallback(Exception error, GameWorld world)
        {
            // Transaction is committed/rolled back inside the Enqueue work item.
            // Do not call Database.Execute from this completion callback (deadlock risk).
            if (error != null)
            {
                log.Error(error, "Updating sql failed");
                world.Send(this.Player, "$7Failed updating sql: " + error.Message);
            }
            else
            {
                log.Info("Updating sql success");
                world.Send(this.Player, "$7Updating sql success");
            }
        }
    }
}