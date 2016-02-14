using Microsoft.ServiceFabric.Services.Communication.Runtime;
using ServiceFabric.OutOfProcess;
using System.Collections.Generic;
using System.Diagnostics;
using System.Fabric;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ConsulService
{
    /// <summary>
    /// The FabricRuntime creates an instance of this class for each service type instance. 
    /// </summary>
    internal sealed class ConsulService : ProcessService
    {
        const string HttpEndpoint = "Http";
        const string HttpsEndpoint = "Https";
        const string DnsEndpoint = "Dns";
        const string RpcEndpoint = "Rpc";

        public ConsulService()
            : base(()=>ServiceEventSource.Current)
        {
        }

        protected override async Task ConfigureAsync(ProcessStartInfo psi, CancellationToken cancellationToken)
        {
            var nodeContext = await FabricRuntime.GetNodeContextAsync(Timeout.InfiniteTimeSpan, cancellationToken);
            var activationContext = await FabricRuntime.GetActivationContextAsync(Timeout.InfiniteTimeSpan, cancellationToken);
            var httpEndpoint = activationContext.GetEndpoint(HttpEndpoint);
            var dnsEndpoint = activationContext.GetEndpoint(DnsEndpoint);
            var rpcEndpoint = activationContext.GetEndpoint(RpcEndpoint);

            var ipHostEntry = await Dns.GetHostEntryAsync(nodeContext.IPAddressOrFQDN);
            var ipAddress = ipHostEntry.AddressList.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);

            psi.FileName = "consul.exe";
            psi.Arguments = $"agent -dev -node {nodeContext.NodeName} -bind {ipAddress} -http-port {httpEndpoint.Port}";// -dns {ipAddress}:{dnsEndpoint.Port} -rpc {ipAddress}:{rpcEndpoint.Port}";
        }

        /// <summary>
        /// Optional override to create listeners (like tcp, http) for this service instance.
        /// </summary>
        /// <returns>The collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            yield return Listen(HttpEndpoint);
            //yield return Listen(HttpsEndpoint);
            //yield return Listen(DnsEndpoint);
            //yield return Listen(RpcEndpoint);
        }
    }
}
