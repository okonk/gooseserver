using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace GooseServerBrowserService
{
    public partial class WindowsService : ServiceBase
    {
        IDisposable server;

        public WindowsService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            server = ApiConfiguration.Create();
        }

        protected override void OnStop()
        {
            if (server != null)
            {
                server.Dispose();
            }
            base.OnStop();
        }
    }
}
