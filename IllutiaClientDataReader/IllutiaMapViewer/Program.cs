using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace IllutiaMapViewer
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string[] resourceNames = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            string dllName = args.Name.Contains(',') ? args.Name.Substring(0, args.Name.IndexOf(',')) : args.Name.Replace(".dll", "");

            foreach (string resourceName in resourceNames)
            {
                if (resourceName.EndsWith(dllName + ".dll"))
                {
                    using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                    {
                        using (MemoryStream memoryStream = new MemoryStream())
                        {
                            stream.CopyTo(memoryStream);
                            return Assembly.Load(memoryStream.ToArray());
                        }
                    }
                }
            }

            return null;
        }
    }
}
