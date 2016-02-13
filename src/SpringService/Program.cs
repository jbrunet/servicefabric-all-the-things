﻿using System;
using System.Collections;
using System.Diagnostics;
using System.Fabric;
using System.Linq;
using System.Threading;

namespace SpringService
{
    internal static class Program
    {
        /// <summary>
        /// This is the entry point of the service host process.
        /// </summary>
        private static void Main()
        {
            try
            {
                // Creating a FabricRuntime connects this host process to the Service Fabric runtime.
                using (FabricRuntime fabricRuntime = FabricRuntime.Create())
                {
                    foreach(DictionaryEntry env in Environment.GetEnvironmentVariables())
                    {
                        ServiceEventSource.Current.Message("{0}: {1}", env.Key, env.Value);
                    }
                    // The ServiceManifest.XML file defines one or more service type names.
                    // RegisterServiceType maps a service type name to a .NET class.
                    // When Service Fabric creates an instance of this service type,
                    // an instance of the class is created in this host process.
                    fabricRuntime.RegisterServiceType("SpringServiceType", typeof(SpringService));

                    ServiceEventSource.Current.ServiceTypeRegistered(Process.GetCurrentProcess().Id, typeof(SpringService).Name);

                    Thread.Sleep(Timeout.Infinite);  // Prevents this host process from terminating so services keeps running.
                }
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.ServiceHostInitializationFailed(e.ToString());
                throw;
            }
        }
    }
}
