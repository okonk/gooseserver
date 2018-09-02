using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.ServiceProcess;

namespace Goose
{
    class Program
    {
        /**
         * Just starts our GameServer
         *
         */
        static void Main(string[] args)
        {
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

            if (args.Contains("-service"))
            {
                ServiceBase.Run(new ServiceBase[] 
                { 
                    new GooseWindowsService() 
                });

                return;
            }

            GameServer server = new GameServer();
            server.Run();
            Console.ReadKey(); // so console doesn't close when server closes
        }
    }
}
