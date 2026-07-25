using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

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

        private Socket listen;
        private List<Socket> sockets;

        private GameWorld gameworld;

        private bool stopping = false;

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
            while (true)
            {
                try
                {
                    this.sockets = new();
                    this.gameworld = new GameWorld(this);
                    this.Start();
                    this.GameLoop();
                }
                catch (Exception e)
                {
                    Console.WriteLine("\nCrashed: " + DateTime.Now.ToString());
                    Console.WriteLine(e.Message + " " + e.InnerException);
                    Console.WriteLine(e.StackTrace);

                    using (System.IO.StreamWriter writer = System.IO.File.AppendText("crashlog.txt"))
                    {
                        writer.WriteLine("\nCrashed: " + DateTime.Now.ToString());
                        writer.WriteLine(e.Message + " " + e.InnerException);
                        writer.WriteLine(e.StackTrace);
                    }

                    try
                    {
                        this.Stop();
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

            this.listen = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.listen.Bind(new IPEndPoint(IPAddress.Parse(GameWorld.Settings.GameServerIP), 
                GameWorld.Settings.GameServerPort));
            this.listen.Listen(10);

            this.sockets.Add(this.listen);
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

                var readList = this.sockets.ToList();
                var writeList = this.sockets.Where(s => gameworld.PlayerHandler.GetPlayer(s)?.SendBuffer?.Count > 0).ToList();

                Socket.Select(readList, writeList, null, 2000);

                foreach (var writeSocket in writeList)
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

                foreach (Socket sock in readList)
                {
                    if (sock == this.listen)
                    {
                        Socket newSocket = null;
                        try
                        {
                            newSocket = this.listen.Accept();
                            newSocket.Blocking = false;
                            this.sockets.Add(newSocket);

                            this.gameworld.NewConnection(newSocket);
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
                            var buffer = new byte[8192];
                            int bytesRead = 0;
                            try
                            {
                                bytesRead = sock.Receive(buffer);
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
                                string strBuffer = Encoding.ASCII.GetString(buffer, 0, bytesRead);
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
            this.gameworld.Stop();

            foreach (Socket sock in this.sockets)
            {
                sock.Close();
            }

            NLog.LogManager.Shutdown();
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
        }
    }
}
