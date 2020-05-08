using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goose
{
    public class DatabaseWriter
    {
        private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
        private BlockingCollection<DbCommand> commands = new BlockingCollection<DbCommand>();

        public void Add(DbCommand command)
        {
            commands.Add(command);
        }

        public void Run(GameWorld world)
        {
            while (true)
            {
                var command = commands.Take();
                try
                {
                    command.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    log.Error(e, "SQL Query Failed: {query}", command.CommandText);
                }
            }
        }

        public int Count { get { return commands.Count; } }
    }
}
