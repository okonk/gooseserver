using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GooseServerBrowserService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            if (args.Contains("-service"))
            {
                ServiceBase.Run(new ServiceBase[] 
                { 
                    new WindowsService() 
                });

                return;
            }

            using (ApiConfiguration.Create())
            {
                Thread.Sleep(Timeout.Infinite);
            }
        }
    }
}
