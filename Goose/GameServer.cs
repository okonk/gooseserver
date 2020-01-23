using System;
using System.Collections;
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
        Socket listen;
        ArrayList sockets;

        GameWorld gameworld;

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
                    this.sockets = new ArrayList();
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
            ArrayList readList;
            ArrayList writeList;

            while (this.gameworld.Running) 
            {
                System.Threading.Thread.Sleep(1);

                readList = (ArrayList)this.sockets.Clone();
                writeList = (ArrayList)this.sockets.Clone();

                Socket.Select(readList, writeList, null, 2000);

                foreach (Socket sock in readList)
                {
                    if (sock == this.listen)
                    {
                        Socket newsock = this.listen.Accept();
                        newsock.Blocking = false;
                        this.sockets.Add(newsock);

                        this.gameworld.NewConnection(newsock);
                    }
                    else
                    {
                        byte[] buffer = new byte[512];
                        int res = 0;
                        try
                        {
                            res = sock.Receive(buffer);
                        }
                        catch (SocketException)
                        {
                        }

                        if (res <= 0)
                        {
                            this.gameworld.LostConnection(sock);
                        }
                        else
                        {
                            string strBuffer = Encoding.ASCII.GetString(buffer, 0, res);
                            this.gameworld.Received(sock, strBuffer);
                        }
                    }
                }

                this.gameworld.Update();
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
    }
}
