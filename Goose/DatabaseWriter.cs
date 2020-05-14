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
        private BlockingCollection<Tuple<DbCommand, Action<Exception>>> commands = new BlockingCollection<Tuple<DbCommand, Action<Exception>>>();

        public void Add(DbCommand command, Action<Exception> callback = null)
        {
            commands.Add(Tuple.Create(command, callback));
        }

        public void Run(GameWorld world)
        {
            while (true)
            {
                var tuple = commands.Take();
                var command = tuple.Item1;
                try
                {
                    command.ExecuteNonQuery();

                    tuple.Item2?.Invoke(null);
                }
                catch (Exception e)
                {
                    log.Error(e, "SQL Query Failed: {query}", command.CommandText);

                    tuple.Item2?.Invoke(e);
                }
            }
        }

        public int Count { get { return commands.Count; } }
    }
}
