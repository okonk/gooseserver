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
            if (this.Player.State == Player.States.Ready && 
                this.Player.HasPrivilege(AccessPrivilege.ReloadSQL))
            {
                Task.Run(() =>
                {
                    try
                    {
                        var sqlData = CsvToSql.Core.CsvToSqlConverter.Convert(GameWorld.Settings.DataLink);
                        var command = world.SqlConnection.CreateCommand();
                        command.CommandText = sqlData;

                        world.DatabaseWriter.Add(command, UpdateCompletedCallback);

                        log.Info("Added sql command to queue");
                    }
                    catch (Exception e)
                    {
                        log.Error(e, "Failed updating sql data");
                        //world.Send(this.Player, "$7" + e.Message);
                    }
                });
            }
        }

        private void UpdateCompletedCallback(Exception error)
        {
            if (error != null)
                log.Error(error, "Updating sql failed");
            else
                log.Info("Updating sql success");
        }
    }
}