using System;
using System.Collections.Concurrent;
using System.Data.SQLite;
using System.IO;
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
        private volatile bool _stopping;

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

        /// <summary>
        /// Number of work items waiting in the queue. Does not include the item currently
        /// executing on the DB thread (in-flight). Stop() waits for both queued and in-flight work.
        /// </summary>
        public int PendingCount => _queue.Count;

        public void Start(string databasePath)
        {
            if (_started)
                throw new InvalidOperationException("Database already started.");

            var dir = Path.GetDirectoryName(databasePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var cs = string.Format(
                "Data Source={0}; Version=3; Journal Mode=Wal; BusyTimeout=5000;",
                databasePath);

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
                    // Open/pragma failed before Loop runs; dispose connection here so it is not leaked.
                    try
                    {
                        _connection?.Close();
                        _connection?.Dispose();
                    }
                    catch (Exception disposeEx)
                    {
                        log.Error(disposeEx, "Failed to dispose SQLite connection after Start error");
                    }
                    finally
                    {
                        _connection = null;
                    }
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
            {
                _loopTask = null;
                throw startError;
            }

            _started = true;
            _stopping = false;
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

        /// <summary>
        /// Queues work to run inside a single transaction. Either every statement the
        /// action issues commits, or none of them do.
        ///
        /// Use this whenever one logical change spans more than one table. A player save
        /// touches players, inventory, equipped, bank_items, spellbook, quest_status and
        /// pets; issuing those as separate work items meant a crash between them could
        /// persist new gold against an old inventory, which is an item dupe.
        ///
        /// onCommit, if given, runs only after the COMMIT succeeds - never on the
        /// rollback path and never if COMMIT itself throws.
        /// </summary>
        public void EnqueueTransaction(Action<SQLiteConnection> action, Action onCommit = null)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            Enqueue(conn =>
            {
                // Driven as raw SQL rather than BeginTransaction because the commands the
                // action creates do not set SQLiteCommand.Transaction. SQLite transactions
                // are connection scoped, so every statement issued on this connection
                // between BEGIN and COMMIT is included regardless.
                RunSql(conn, "BEGIN;");

                try
                {
                    action(conn);
                    RunSql(conn, "COMMIT;");
                    // H8: runs only after COMMIT succeeds, so a rolled-back transaction
                    // never lets callers mark state as persisted.
                    onCommit?.Invoke();
                }
                catch (Exception)
                {
                    try
                    {
                        RunSql(conn, "ROLLBACK;");
                    }
                    catch (Exception rollbackEx)
                    {
                        log.Error(rollbackEx, "Failed to roll back transaction");
                    }

                    throw;
                }
            });
        }

        private static void RunSql(SQLiteConnection conn, string sql)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }

        public void Stop()
        {
            if (!_started && _loopTask == null)
                return;

            // Idempotent: CompleteAdding may only be called once.
            if (_stopping)
                return;
            _stopping = true;

            try
            {
                if (!_queue.IsAddingCompleted)
                    _queue.CompleteAdding();
            }
            catch (InvalidOperationException)
            {
                // Already completed (race with another stop path).
            }

            if (_loopTask != null)
            {
                // Wait drains queued work and the in-flight item currently executing.
                if (!_loopTask.Wait(TimeSpan.FromMinutes(2)))
                {
                    log.Error("Database.Stop timed out after 2 minutes waiting for DB thread to finish; queue may still have work or an in-flight item.");
                    // Do not claim a clean stop; leave _started as-is so callers can detect the failed shutdown.
                    return;
                }
            }

            _started = false;
            _loopTask = null;
        }
    }
}
