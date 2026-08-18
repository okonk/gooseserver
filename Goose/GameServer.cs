using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using Goose.ConsoleCommands;

namespace Goose
{
    /**
     * The GameServer class handles all of the basic Socket handling to do with a server
     * It contains the GameWorld class where all of the game specific stuff happens
     * 
     */
    public class GameServer
    {
        private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        /**
         * How many ticks in a row GameWorld.Update may throw before we stop containing
         * the error and let the world restart.
         */
        private const int MaxConsecutiveUpdateFailures = 10;

        /**
         * How often we sweep for connections that were accepted but never logged in.
         */
        private static readonly TimeSpan PreLoginSweepInterval = TimeSpan.FromSeconds(5);

        private Socket listen;
        private List<Socket> sockets;

        /**
         * Tracks the source address and accept time of every open connection.
         *
         * The address is captured at accept because RemoteEndPoint throws once a socket
         * has been torn down, and we still need it to decrement the per-IP count.
         */
        private Dictionary<Socket, ConnectionInfo> connections;
        private Dictionary<string, int> connectionsPerIP;

        private DateTime lastPreLoginSweep = DateTime.UtcNow;

        /**
         * Reusable buffers for Socket.Select to avoid allocating new lists every tick.
         */
        private readonly List<Socket> readList = new();
        private readonly List<Socket> writeList = new();
        private readonly byte[] receiveBuffer = new byte[8192];

        private GameWorld gameworld;

        /**
         * Console commands. Created once and started before the restart loop below,
         * since a second reader thread would compete for stdin.
         */
        private readonly ConsoleCommandHandler consoleCommands = new();

        private bool stopping = false;

        private class ConnectionInfo
        {
            public string IP;
            public DateTime AcceptedAt;
        }

        /**
         * Constructor, constructs the GameWorld
         * 
         * Then calls Start to set up everything
         * Then calls GameLoop, the main program loop
         * 
         */
        public GameServer()
        {
            
        }

        public void Run()
        {
            this.consoleCommands.Start();

            while (true)
            {
                try
                {
                    this.sockets = new();
                    this.connections = new();
                    this.connectionsPerIP = new();
                    this.gameworld = new GameWorld(this);
                    this.Start();
                    this.GameLoop();
                }
                catch (FatalStartupException e)
                {
                    // Persistent config/bind failure (bad IP, port in use): restarting
                    // can only spin, so exit instead.
                    Console.WriteLine("\n" + e.Message);

                    try
                    {
                        this.StopWorld();
                    }
                    catch { }

                    Environment.ExitCode = 1;
                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine("\nCrashed: " + DateTime.Now.ToString());
                    Console.WriteLine(e.Message + " " + e.InnerException);
                    Console.WriteLine(e.StackTrace);

                    try
                    {
                        using (System.IO.StreamWriter writer = System.IO.File.AppendText(Paths.ResolveData("crashlog.txt")))
                        {
                            writer.WriteLine("\nCrashed: " + DateTime.Now.ToString());
                            writer.WriteLine(e.Message + " " + e.InnerException);
                            writer.WriteLine(e.StackTrace);
                        }
                    }
                    catch (Exception err)
                    {
                        Console.WriteLine("Could not write crashlog: " + err.Message);
                    }

                    // H7: Stop() would set stopping=true and shut down NLog, so the
                    // next clean shutdown would skip player saves and lose all logging.
                    try
                    {
                        this.StopWorld();
                    }
                    catch { }

                    System.Threading.Thread.Sleep(10000);
                    continue;
                }

                break;
            }
        }

        /**
         * Start, server setup
         * 
         * Calls along to the GameWorld.Start()
         * Sets up a listen socket and adds it to the socket list
         * 
         */
        public void Start()
        {
            this.gameworld.Start();

            this.listen = this.CreateListenSocket();

            this.sockets.Add(this.listen);
        }

        internal Socket CreateListenSocket()
        {
            string ip = GameWorld.Settings.GameServerIP;
            int port = GameWorld.Settings.GameServerPort;

            Socket socket = null;

            try
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Bind(new IPEndPoint(IPAddress.Parse(ip), port));
                socket.Listen(10);
                return socket;
            }
            catch (Exception e)
            {
                socket?.Dispose();
                throw new FatalStartupException(
                    "Fatal startup error: cannot listen on " + ip + ":" + port + ": " + e.Message, e);
            }
        }

        /**
         * GameLoop, the main game loop
         * 
         * Handles all of the low level socket details
         * Eg, on a new connection it calls GameWorld.NewConnection(Socket)
         * on a closed connection calls GameWorld.LostConnection(Socket)
         * on receiving data calls GameWorld.Received(Socket, String)
         * 
         * At the end of the loop it calls GameWorld.Update(),
         * Update returns a bool to specify to keep the server going or not
         * 
         * Once the loop is stopped this.Stop() is called to tidy up
         * 
         */
        public void GameLoop()
        {
            int updateFailures = 0;

            while (this.gameworld.Running)
            {
                System.Threading.Thread.Sleep(1);

                this.readList.Clear();
                this.readList.AddRange(this.sockets);

                this.writeList.Clear();
                foreach (var s in this.sockets)
                {
                    if (gameworld.PlayerHandler.GetPlayer(s)?.SendBuffer?.Count > 0)
                        this.writeList.Add(s);
                }

                Socket.Select(this.readList, this.writeList, null, 2000);

                foreach (var writeSocket in this.writeList)
                {
                    try
                    {
                        var player = gameworld.PlayerHandler.GetPlayer(writeSocket);

                        player?.Send();
                    }
                    catch (Exception e)
                    {
                        log.Error(e, "Error sending to socket, dropping connection.");
                        this.DropSocket(writeSocket);
                    }
                }

                foreach (Socket sock in this.readList)
                {
                    if (sock == this.listen)
                    {
                        Socket newSocket = null;
                        try
                        {
                            newSocket = this.listen.Accept();
                            newSocket.Blocking = false;

                            if (this.TryRegisterConnection(newSocket))
                            {
                                this.sockets.Add(newSocket);
                                this.gameworld.NewConnection(newSocket);
                            }
                            else
                            {
                                // Over a limit. Close immediately without adding it to the
                                // select list so it costs us nothing further.
                                try { newSocket.Close(); } catch (Exception) { }
                                newSocket = null;
                            }
                        }
                        catch (Exception e)
                        {
                            // A peer that resets between select and accept must not take
                            // the server down.
                            log.Error(e, "Error accepting connection.");
                            if (newSocket != null) this.DropSocket(newSocket);
                        }
                    }
                    else
                    {
                        try
                        {
                            int bytesRead = 0;
                            try
                            {
                                bytesRead = sock.Receive(this.receiveBuffer);
                            }
                            catch (SocketException)
                            {
                            }

                            if (bytesRead <= 0)
                            {
                                this.gameworld.LostConnection(sock);
                            }
                            else
                            {
                                string strBuffer = Encoding.ASCII.GetString(this.receiveBuffer, 0, bytesRead);
                                this.gameworld.Received(sock, strBuffer);
                            }
                        }
                        catch (Exception e)
                        {
                            log.Error(e, "Error receiving from socket, dropping connection.");
                            this.DropSocket(sock);
                        }
                    }
                }

                this.SweepPreLoginConnections();

                this.consoleCommands.Update(this.gameworld);

                try
                {
                    this.gameworld.Update();
                    updateFailures = 0;
                }
                catch (Exception e)
                {
                    // EventHandler.Update contains per-event exceptions itself; this is the
                    // backstop for everything else in the tick so one bad tick does not
                    // rebuild the world. Persistent failure means the world really is
                    // broken, so fall through to the restart path rather than spinning.
                    updateFailures++;
                    log.Error(e, "Unhandled exception in GameWorld.Update ({0} consecutive).", updateFailures);

                    if (updateFailures >= MaxConsecutiveUpdateFailures) throw;
                }
            }

            if (!stopping)
                this.Stop();
        }

        /**
         * Stop, server shutdown tidyup
         * 
         * Calls along to GameWorld.Stop()
         * 
         * Closes all sockets
         * 
         */
        public void Stop()
        {
            this.stopping = true;
            this.StopWorld();

            NLog.LogManager.Shutdown();
        }

        private void StopWorld()
        {
            this.gameworld.Stop();

            foreach (Socket sock in this.sockets)
            {
                try
                {
                    sock.Close();
                }
                catch { }
            }
        }

        /**
         * Disconnect, disconnect socket
         * 
         * Closes socket then removes from our sockets list
         * 
         */
        public void Disconnect(Socket sock)
        {
            sock.Close();
            this.sockets.Remove(sock);
            this.UnregisterConnection(sock);
        }

        /**
         * RequestShutdown, asks the game loop to stop at the end of the current tick
         *
         * Safe to call from a signal handler on another thread: it only sets a flag, and
         * the loop then exits and runs the normal Stop path, which saves players and
         * drains the database queue.
         *
         */
        public void RequestShutdown()
        {
            var world = this.gameworld;

            if (world == null) return;

            log.Info("Shutdown requested.");
            world.Running = false;
        }

        /**
         * TryRegisterConnection, applies the connection limits to a freshly accepted socket
         *
         * Returns false if the server is at its overall connection ceiling or this source
         * address already holds too many connections, in which case the caller should
         * close the socket without tracking it.
         *
         * MaxPlayers is only enforced when a LoginID is assigned, long after the socket is
         * accepted, so without this a flood of sockets that never log in was unbounded and
         * every one of them was walked twice per Socket.Select.
         *
         */
        private bool TryRegisterConnection(Socket sock)
        {
            if (this.sockets.Count >= GameWorld.Settings.MaxConnections)
            {
                log.Warn("Refusing connection: at MaxConnections (" + GameWorld.Settings.MaxConnections + ").");
                return false;
            }

            string ip;
            try
            {
                ip = ((IPEndPoint)sock.RemoteEndPoint).Address.ToString();
            }
            catch (Exception)
            {
                // Already gone between select and accept.
                return false;
            }

            this.connectionsPerIP.TryGetValue(ip, out int count);
            if (count >= GameWorld.Settings.MaxConnectionsPerIP)
            {
                log.Warn("Refusing connection from " + ip + ": at MaxConnectionsPerIP (" +
                         GameWorld.Settings.MaxConnectionsPerIP + ").");
                return false;
            }

            this.connectionsPerIP[ip] = count + 1;
            this.connections[sock] = new ConnectionInfo { IP = ip, AcceptedAt = DateTime.UtcNow };

            return true;
        }

        /**
         * UnregisterConnection, drops our bookkeeping for a closed socket
         *
         */
        private void UnregisterConnection(Socket sock)
        {
            if (!this.connections.TryGetValue(sock, out ConnectionInfo info)) return;

            this.connections.Remove(sock);

            if (this.connectionsPerIP.TryGetValue(info.IP, out int count))
            {
                if (count <= 1) this.connectionsPerIP.Remove(info.IP);
                else this.connectionsPerIP[info.IP] = count - 1;
            }
        }

        /**
         * SweepPreLoginConnections, drops connections that were accepted but never logged in
         *
         * The ping timeout only covers logged in players, so a socket that connects and
         * then says nothing previously sat in the select list forever.
         *
         */
        private void SweepPreLoginConnections()
        {
            DateTime now = DateTime.UtcNow;

            if (now - this.lastPreLoginSweep < PreLoginSweepInterval) return;
            this.lastPreLoginSweep = now;

            var timeout = TimeSpan.FromSeconds(Math.Max(1, GameWorld.Settings.PreLoginTimeoutSeconds));

            List<Socket> stale = null;

            foreach (var pair in this.connections)
            {
                if (now - pair.Value.AcceptedAt < timeout) continue;
                if (this.gameworld.PlayerHandler.GetPlayer(pair.Key) != null) continue;

                (stale ??= new List<Socket>()).Add(pair.Key);
            }

            if (stale == null) return;

            foreach (Socket sock in stale)
            {
                log.Info("Dropping connection that never logged in.");
                this.DropSocket(sock);
            }
        }

        /**
         * DropSocket, best effort teardown of a single connection after an error
         *
         * Runs the normal LostConnection path so the player is logged out, then
         * guarantees the socket is closed and removed from the select list even if
         * that path threw part way through.
         *
         */
        private void DropSocket(Socket sock)
        {
            if (sock == null) return;

            try
            {
                this.gameworld.LostConnection(sock);
            }
            catch (Exception e)
            {
                log.Error(e, "Error handling lost connection.");
            }

            try
            {
                sock.Close();
            }
            catch (Exception)
            {
            }

            this.sockets.Remove(sock);
            this.UnregisterConnection(sock);
        }
    }
}
