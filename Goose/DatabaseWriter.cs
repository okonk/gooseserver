using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goose
{
    public class DatabaseWriter
    {
        private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
        private BlockingCollection<SqlCommand> commands = new BlockingCollection<SqlCommand>();

        public void Add(SqlCommand command)
        {
            commands.Add(command);
        }

        public void Run(GameWorld world)
        {
            while (true)
            {
                try
                {
                    var command = commands.Take();

                    command.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    log.Error(e, "SQL Query Failed");
                }
            }
        }

        public int Count { get { return commands.Count; } }
    }
}
