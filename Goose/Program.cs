using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
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

            // Without these, Ctrl+C or `systemctl stop` killed the process outright with
            // all authoritative state still in memory. The database is a write behind
            // mirror flushed on PlayerSavePeriod, so that discarded up to that much player
            // progress. Both handlers cancel the default terminate and ask the game loop to
            // stop, which runs the normal save and database drain before Main returns.
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                server.RequestShutdown();
            };

            using var sigterm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, context =>
            {
                context.Cancel = true;
                server.RequestShutdown();
            });

            server.Run();

            // Interactive stdin is owned by the console command reader thread (started when
            // input is not redirected). Waiting for a key here would hang after clean exit
            // because that thread permanently blocks on Console.ReadLine().
        }
    }
}
