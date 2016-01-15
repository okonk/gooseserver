using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace Goose
{
    partial class GooseWindowsService : ServiceBase
    {
        GameServer server;

        public GooseWindowsService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Task.Factory.StartNew(() => { server = new GameServer(); server.Run(); });
        }

        protected override void OnStop()
        {
            server.Stop();
        }
    }
}
