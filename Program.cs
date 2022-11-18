using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace XRFAgent
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
#if !DEBUG
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new XRFAgent()
            };
            ServiceBase.Run(ServicesToRun);
#else
            XRFAgent servRun = new XRFAgent();
            servRun.OnDebug(null);
#endif
        }
    }
}
