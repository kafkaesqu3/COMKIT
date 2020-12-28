using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace COMKIT
{
    //class for interacting with services using WMI
    public static class WMIServiceHelper
    {
        public static void ChangeStartModeWMI(string serviceName, string startMode, string RemoteHost = "localhost")
        {
            try
            {
                ManagementObject classInstance =
                    new ManagementObject(@"\\" + RemoteHost + @"\root\cimv2",
                                         "Win32_Service.Name='" + serviceName + "'",
                                         null);

                // Obtain in-parameters for the method
                ManagementBaseObject inParams = classInstance.GetMethodParameters("ChangeStartMode");

                // Add the input parameters.
                inParams["StartMode"] = startMode;

                // Execute the method and obtain the return values.
                ManagementBaseObject outParams = classInstance.InvokeMethod("ChangeStartMode", inParams, null);

                // List outParams
                //Console.WriteLine("Out parameters:");
                //richTextBox1.AppendText(DateTime.Now.ToString() + ": ReturnValue: " + outParams["ReturnValue"]);
            }
            catch (ManagementException err)
            {
                //richTextBox1.AppendText(DateTime.Now.ToString() + ": An error occurred while trying to execute the WMI method: " + err.Message);
            }
        }
    }

    /// <summary>
    /// Interacts with services using the ServiceController
    /// </summary>
    public static class SCServiceHelper
    {

        /// <summary>
        /// Uses ServiceManager to start a service
        /// </summary>
        /// <param name="ServiceName">Name of service to start</param>
        /// <param name="enabled">Set service start mode to manual if true by invoking </param>
        /// <param name="RemoteHost">Host to start the service on (defaults to localhost if parameter is not present)</param>
        public static bool StartServiceSC(string ServiceName, bool enabled = true, string RemoteHost = "localhost")
        {
            ServiceController service;
            if (RemoteHost == "localhost")
            {
                service = new ServiceController(ServiceName);
            }
            else
            {
                service = new ServiceController(RemoteHost, ServiceName);
            }
            try
            {
                if (enabled)
                {
                    ChangeStartModeSC(service, ServiceStartMode.Automatic);
                }

                TimeSpan timeout = TimeSpan.FromMilliseconds(2000);

                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running, timeout);
                return true;
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("Error starting {0} service on {1}: {2}", ServiceName, RemoteHost, e));
            }
            return false;
        }

        public static bool StopServiceSC(string ServiceName, string RemoteHost = "localhost", bool disabled = true)
        {
            ServiceController service;
            if (RemoteHost == "localhost")
            {
                service = new ServiceController(ServiceName);
            }
            else
            {
                service = new ServiceController(RemoteHost, ServiceName);
            }
            try
            {
                if (disabled)
                {
                    ChangeStartModeSC(service, ServiceStartMode.Disabled);
                }
                TimeSpan timeout = TimeSpan.FromMilliseconds(2000);

                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Running, timeout);
                return true;
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("Error stopping {0} service on {1}: {2}", ServiceName, RemoteHost, e));
            }
        }

        public static string CheckServiceStatusSC(string ServiceName, string RemoteHost = "localhost")
        {
            ServiceController service;
            if (RemoteHost == "localhost")
            {
                service = new ServiceController(ServiceName);
            }
            else
            {
                service = new ServiceController(RemoteHost, ServiceName);
            }
            try
            {
                switch (service.Status)
                {
                    case ServiceControllerStatus.Running:
                        Console.WriteLine("Running");
                        return "Running";
                    case ServiceControllerStatus.Stopped:
                        Console.WriteLine("Stopped");
                        return "Stopped";
                    case ServiceControllerStatus.Paused:
                        Console.WriteLine("Paused");
                        return "Paused";
                    case ServiceControllerStatus.StopPending:
                        Console.WriteLine("Stopping");
                        return "Stopping";
                    case ServiceControllerStatus.StartPending:
                        Console.WriteLine("Starting");
                        return "Starting";
                    default:
                        Console.WriteLine("Status Changing");
                        return "Status Changing";
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("Error querying {0} service on {1}: {2}", ServiceName, RemoteHost, e));
            }
        }


        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern Boolean ChangeServiceConfig(
            IntPtr hService,
            UInt32 nServiceType,
            UInt32 nStartType,
            UInt32 nErrorControl,
            String lpBinaryPathName,
            String lpLoadOrderGroup,
            IntPtr lpdwTagId,
            [In] char[] lpDependencies,
            String lpServiceStartName,
            String lpPassword,
            String lpDisplayName);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern IntPtr OpenService(
            IntPtr hSCManager, string lpServiceName, uint dwDesiredAccess);

        [DllImport("advapi32.dll", EntryPoint = "OpenSCManagerW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr OpenSCManager(
            string machineName, string databaseName, uint dwAccess);

        [DllImport("advapi32.dll", EntryPoint = "CloseServiceHandle")]
        public static extern int CloseServiceHandle(IntPtr hSCObject);

        private const uint SERVICE_NO_CHANGE = 0xFFFFFFFF;
        private const uint SERVICE_QUERY_CONFIG = 0x00000001;
        private const uint SERVICE_CHANGE_CONFIG = 0x00000002;
        private const uint SC_MANAGER_ALL_ACCESS = 0x000F003F; //TODO is this the best permission i need

        public static void ChangeStartModeSC(ServiceController svc, ServiceStartMode mode)
        {
            var scManagerHandle = OpenSCManager(null, null, SC_MANAGER_ALL_ACCESS);
            if (scManagerHandle == IntPtr.Zero)
            {
                throw new ExternalException("Open Service Manager Error");
            }

            var serviceHandle = OpenService(
                scManagerHandle,
                svc.ServiceName,
                SERVICE_QUERY_CONFIG | SERVICE_CHANGE_CONFIG);

            if (serviceHandle == IntPtr.Zero)
            {
                throw new ExternalException("Open Service Error");
            }

            var result = ChangeServiceConfig(
                serviceHandle,
                SERVICE_NO_CHANGE,
                (uint)mode,
                SERVICE_NO_CHANGE,
                null,
                null,
                IntPtr.Zero,
                null,
                null,
                null,
                null);

            if (result == false)
            {
                int nError = Marshal.GetLastWin32Error();
                var win32Exception = new Win32Exception(nError);
                throw new ExternalException("Could not change service start type: "
                    + win32Exception.Message);
            }

            CloseServiceHandle(serviceHandle);
            CloseServiceHandle(scManagerHandle);
        }
    }

    public static class RemoteRegServiceHelper
    {
        enum StartType
        {
            Low,
            Medium,
            High
        }

        public static void SetStartMode(string serviceName, ServiceStartMode StartType, string RemoteHost = "localhost")
        {
            var reg = new RemoteRegistry();
            int startTypeInt = 2;
            switch (StartType) {
                case ServiceStartMode.Automatic:
                    startTypeInt = 2;
                    break;
                case ServiceStartMode.Disabled:
                    startTypeInt = 4;
                    break;
                case ServiceStartMode.Manual:
                    startTypeInt = 3;
                    break;
            }

            reg.WriteRegistryKey(@"HKLM\System\CurrentControlSet\Services\" + serviceName, "Start", "DWORD", startTypeInt.ToString(), RemoteHost);
        }
    }

}