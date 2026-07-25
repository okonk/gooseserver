using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goose.Events
{
    public class UpdateSqlCommandEvent : Event
    {
        private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public static Event Create(Player player, Object data)
        {
            Event e = new UpdateSqlCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                world.Send(this.Player, "$7Updating sql...");

                Task.Run(() =>
                {
                    try
                    {
                        var sqlData = CsvToSql.Core.CsvToSqlConverter.Convert(GameWorld.Settings.DataLinkId);

                        world.Database.Enqueue(conn =>
                        {
                            using var tx = conn.BeginTransaction();
                            try
                            {
                                using var command = conn.CreateCommand();
                                command.Transaction = tx;
                                command.CommandText = sqlData;
                                command.ExecuteNonQuery();
                                tx.Commit();
                            }
                            catch
                            {
                                tx.Rollback();
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