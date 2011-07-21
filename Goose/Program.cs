using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

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
            while (true)
            {
                try
                {
                    GameServer gs = new GameServer();
                }
                catch (Exception e)
                {
                    Console.WriteLine("\nCrashed: " + DateTime.Now.ToString());
                    Console.WriteLine(e.Message + " " + e.InnerException);
                    Console.WriteLine(e.StackTrace);

                    using (StreamWriter writer = File.AppendText("crashlog.txt"))
                    {
                        writer.WriteLine("\nCrashed: " + DateTime.Now.ToString());
                        writer.WriteLine(e.Message + " " + e.InnerException);
                        writer.WriteLine(e.StackTrace);
                    }

                    System.Threading.Thread.Sleep(10000);
                    continue;
                }

                break;
            }

            Console.ReadKey(); // so console doesn't close when server closes
        }
    }
}
