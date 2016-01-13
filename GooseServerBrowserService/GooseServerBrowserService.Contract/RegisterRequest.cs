using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GooseServerBrowserService.Contract
{
    public class RegisterRequest
    {
        public string ServerName { get; set; }
        public int Port { get; set; }
        public int PlayerCount { get; set; }
        public string Version { get; set; }
    }
}
