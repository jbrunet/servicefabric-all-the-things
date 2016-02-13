using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Fabric;
using System.Diagnostics;

namespace SpringService
{
    /// <summary>
    /// The FabricRuntime creates an instance of this class for each service type instance. 
    /// </summary>
    internal sealed class SpringService : StatelessServiceBase
    {
        private int _port;
        private Process _process;

        protected override void OnInitialize(StatelessServiceInitializationParameters initializationParameters)
        {
            base.OnInitialize(initializationParameters);

            var endpoint = initializationParameters.CodePackageActivationContext.GetEndpoints().First();
            _port = endpoint.Port;
        }

        protected override Task OnOpenAsync(IStatelessServicePartition partition, CancellationToken cancellationToken)
        {
            try
            {
                var cwd = Environment.CurrentDirectory;

                var psi = new ProcessStartInfo();
                psi.FileName = @"C:\ProgramData\Oracle\Java\javapath\java.exe";
                psi.Arguments = string.Format("-jar target/demo-0.0.1-SNAPSHOT.jar -Dserver.port={0}", _port);
                psi.WorkingDirectory = cwd;
                psi.RedirectStandardOutput = true;
                psi.UseShellExecute = false;

                _process = new Process();
                _process.StartInfo = psi;
                _process.OutputDataReceived += _process_OutputDataReceived;
                _process.EnableRaisingEvents = true;
                _process.Exited += _process_Exited;
                if (!_process.Start())
                    throw new Exception("process did not start");
            }
            catch(Exception error)
            {
                ServiceEventSource.Current.Message("process failed: " + error.Message);
            }
            return base.OnOpenAsync(partition, cancellationToken);
        }

        private void _process_Exited(object sender, EventArgs e)
        {
            ServiceEventSource.Current.Message("process exited.");
        }

        private void _process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            ServiceEventSource.Current.Message(e.Data);
        }

        protected override Task OnCloseAsync(CancellationToken cancellationToken)
        {
            return base.OnCloseAsync(cancellationToken);
        }

        protected override void OnAbort()
        {
            base.OnAbort();
        }

        /// <summary>
        /// Optional override to create listeners (like tcp, http) for this service instance.
        /// </summary>
        /// <returns>The collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            // TODO: If your service needs to handle user requests, return a list of ServiceReplicaListeners here.
            return new ServiceInstanceListener[0];
        }

        ///// <summary>
        ///// This is the main entry point for your service instance.
        ///// </summary>
        ///// <param name="cancelServiceInstance">Canceled when Service Fabric terminates this instance.</param>
        //protected override async Task RunAsync(CancellationToken cancelServiceInstance)
        //{
        //    // TODO: Replace the following sample code with your own logic.

        //    //int iterations = 0;
        //    //// This service instance continues processing until the instance is terminated.
        //    //while (!cancelServiceInstance.IsCancellationRequested)
        //    //{

        //    //    // Log what the service is doing
        //    //    //ServiceEventSource.Current.ServiceMessage(this, "Working-{0}", iterations++);

        //    //    // Pause for 1 second before continue processing.
        //    //    await Task.Delay(TimeSpan.FromSeconds(1), cancelServiceInstance);
        //    //}
        //}
    }
}
