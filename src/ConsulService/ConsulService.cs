using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Newtonsoft.Json.Linq;
using ServiceFabric.OutOfProcess;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Fabric;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsulService
{
    /// <summary>
    /// The FabricRuntime creates an instance of this class for each service type instance. 
    /// </summary>
    internal class ConsulAgentService : ProcessService
    {
        const string HttpEndpoint = "Http";

        public ConsulAgentService()
            : base(ServiceEventSource.Current)
        {
        }

        //protected override bool StartProcessOnOpenAsync { get { return false; } }

        protected override async Task ConfigureProcessAsync(ProcessStartInfo psi, CancellationToken cancellationToken)
        {
            var nodeContext = await FabricRuntime.GetNodeContextAsync(Timeout.InfiniteTimeSpan, cancellationToken);
            var activationContext = await FabricRuntime.GetActivationContextAsync(Timeout.InfiniteTimeSpan, cancellationToken);
            var httpEndpoint = activationContext.GetEndpoint(HttpEndpoint);

            var ipHostEntry = await Dns.GetHostEntryAsync(nodeContext.IPAddressOrFQDN);
            var ipAddress = ipHostEntry.AddressList.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);

            var configDir = activationContext.GetConfigurationPackageObject("Config").Path;
            psi.WorkingDirectory = activationContext.GetCodePackageObject("Code").Path;
            psi.FileName = "consul.exe";
            psi.Arguments = BuildArgs(new StringBuilder($"-node {nodeContext.NodeName} -bind {ipAddress} -http-port {httpEndpoint.Port} -config-dir \"{configDir}\" -data-dir \"{activationContext.WorkDirectory}\"")).ToString();
        }

        protected virtual StringBuilder BuildArgs(StringBuilder args)
        {
            return args;
        }

        /// <summary>
        /// Optional override to create listeners (like tcp, http) for this service instance.
        /// </summary>
        /// <returns>The collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new[] { Listen(HttpEndpoint) };
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            var activationContext = await FabricRuntime.GetActivationContextAsync(Timeout.InfiniteTimeSpan, cancellationToken);

            var consulServerServiceUri = new Uri($"{activationContext.ApplicationName}/ConsulServer");
            if (cancellationToken.IsCancellationRequested)
                return;

            using (var client = new FabricClient(FabricClientRole.User))
            {
                long? handlerId = null;
                try
                {
                    handlerId = client.ServiceManager.RegisterServicePartitionResolutionChangeHandler(consulServerServiceUri, PartitionResolutionChange);

                    while (true)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            return;
                        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                    }
                }
                finally
                {
                    if (handlerId.HasValue)
                        client.ServiceManager.UnregisterServicePartitionResolutionChangeHandler(handlerId.Value);
                }
            }
        }

        private void PartitionResolutionChange(FabricClient source, long handlerId, ServicePartitionResolutionChange args)
        {
            if (args.HasException)
                return;
            var ac = FabricRuntime.GetActivationContext();
            var node = FabricRuntime.GetNodeContext();
            var sb = new StringBuilder($"{this.ServiceInitializationParameters.ServiceName}@{node.NodeName} PartitionResolutionChange");
            var i = 0;
            foreach (var endpoint in args.Result.Endpoints)
            {
                sb.AppendFormat($" {++i} {endpoint.Role} ");
                var json = JObject.Parse(endpoint.Address);
                var endpoints = (JObject)json["Endpoints"];
                foreach (var ep in endpoints)
                    sb.Append($"{ep.Key}:{ep.Value} ");
            }
            eventSource.Message(sb.ToString());
        }
    }

    internal sealed class ConsulServerService : ConsulAgentService
    {
        protected override StringBuilder BuildArgs(StringBuilder args)
        {
            return base.BuildArgs(args)
                .Append($" -server -bootstrap");
        }
    }
}
