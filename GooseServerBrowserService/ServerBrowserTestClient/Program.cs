using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerBrowserTestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            new GooseServerBrowserService.Client.GooseServerBrowserClient("http://illutia.tk:3000/").Register(new GooseServerBrowserService.Contract.RegisterRequest()
            {
                ServerName = "Test 124",
                PlayerCount = 1,
                Port = 2008,
                Version = "1.0.0"
            });

            Console.ReadKey();
        }
    }
}
