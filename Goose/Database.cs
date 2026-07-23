using System;
using System.Collections.Concurrent;
using System.Data.SQLite;
using System.Threading;
using System.Threading.Tasks;

namespace Goose
{
    /// <summary>
    /// Owns a single SQLite connection on one dedicated thread.
    /// All access must go through Execute (sync) or Enqueue (async).
    /// Never create commands on the game thread against this connection.
    /// Deadlock rule: Enqueue completion callbacks must not call Execute.
    /// </summary>
    public sealed class Database
    {
        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        private readonly BlockingCollection<WorkItem> _queue = new();
        private SQLiteConnection _connection;
        private Task _loopTask;
        private int _dbThreadId;
        private volatile bool _started;

        private abstract class WorkItem { }

        private sealed class SyncWork : WorkItem
        {
            public Action<SQLiteConnection> Action;
            public Func<SQLiteConnection, object> Func;
            public object Result;
            public Exception Error;
            public ManualResetEventSlim Done = new(false);
        }

        private sealed class AsyncWork : WorkItem
        {
            public Action<SQLiteConnection> Action;
            public Action<Exception> OnComplete;
        }

        public int PendingCount => _queue.Count;

        public void Start(string databaseName)
        {
            if (_started)
                throw new InvalidOperationException("Database already started.");

            var cs = string.Format(
                "Data Source={0}.db; Version=3; Journal Mode=Wal; BusyTimeout=5000;",
                databaseName);

            var ready = new ManualResetEventSlim(false);
            Exception startError = null;

            _loopTask = Task.Factory.StartNew(() =>
            {
                try
                {
                    _dbThreadId = Environment.CurrentManagedThreadId;
                    _connection = new SQLiteConnection(cs);
                    _connection.Open();

                    using (var cmd = _connection.CreateCommand())
                    {
                        cmd.CommandText = "PRAGMA foreign_keys=ON; PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000;";
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception e)
                {
                    startError = e;
                }
                finally
                {
                    ready.Set();
                }

                if (startError != null)
                    return;

                Loop();
            }, TaskCreationOptions.LongRunning);

            ready.Wait();
            ready.Dispose();

            if (startError != null)
                throw startError;

            _started = true;
        }

        private void Loop()
        {
            try
            {
                foreach (var item in _queue.GetConsumingEnumerable())
                {
                    try
                    {
                        if (item is SyncWork sync)
                        {
                            try
                            {
                                if (sync.Func != null)
                                    sync.Result = sync.Func(_connection);
                                else
                                    sync.Action(_connection);
                            }
                            catch (Exception e)
                            {
                                sync.Error = e;
                                log.Error(e, "SQL sync work failed");
                            }
                            finally
                            {
                                sync.Done.Set();
                            }
                        }
                        else if (item is AsyncWork async)
                        {
                            try
                            {
                                async.Action(_connection);
                                async.OnComplete?.Invoke(null);
                            }
                            catch (Exception e)
                            {
                                log.Error(e, "SQL async work failed");
                                async.OnComplete?.Invoke(e);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        log.Error(e, "Database loop error");
                    }
                }
            }
            finally
            {
                _connection?.Close();
                _connection?.Dispose();
                _connection = null;
            }
        }

        public void Execute(Action<SQLiteConnection> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (!_started) throw new InvalidOperationException("Database has not been started.");

            // Allow re-entrant Execute if already on the DB thread (defensive; prefer not needed).
            if (Environment.CurrentManagedThreadId == _dbThreadId)
            {
                action(_connection);
                return;
            }

            var work = new SyncWork { Action = action };
            try
            {
                _queue.Add(work);
                work.Done.Wait();
                if (work.Error != null) throw work.Error;
            }
            finally
            {
                work.Done.Dispose();
            }
        }

        public T Execute<T>(Func<SQLiteConnection, T> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            if (!_started) throw new InvalidOperationException("Database has not been started.");

            if (Environment.CurrentManagedThreadId == _dbThreadId)
                return func(_connection);

            var work = new SyncWork { Func = conn => func(conn) };
            try
            {
                _queue.Add(work);
                work.Done.Wait();
                if (work.Error != null) throw work.Error;
                return (T)work.Result;
            }
            finally
            {
                work.Done.Dispose();
            }
        }

        public void Enqueue(Action<SQLiteConnection> action, Action<Exception> onComplete = null)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (!_started) throw new InvalidOperationException("Database has not been started.");

            _queue.Add(new AsyncWork { Action = action, OnComplete = onComplete });
        }

        public void Stop()
        {
            if (!_started && _loopTask == null)
                return;

            _queue.CompleteAdding();
            _loopTask?.Wait(TimeSpan.FromMinutes(2));
            _started = false;
        }
    }
}
