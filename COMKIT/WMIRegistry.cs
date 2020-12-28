using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management;
using Microsoft.Management.Infrastructure;
using System.Collections;

namespace COMKIT
{
    class WMIRegistry
    {
        public void DoStuff()
        {
            string Namespace = @"root\cimv2";
            string OSQuery = "SELECT * FROM Win32_OperatingSystem";
            CimSession session = CimSession.Create("localhost");
            IEnumerable queryInstance = session.QueryInstances(Namespace, "WQL", OSQuery);
        }
        
    }
}
