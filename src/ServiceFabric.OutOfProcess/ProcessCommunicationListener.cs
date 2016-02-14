using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using System.Fabric;
using System.Fabric.Description;

namespace ServiceFabric.OutOfProcess
{
    internal class ProcessCommunicationListener : ICommunicationListener
    {
        private string endpointName;

        public ProcessCommunicationListener(string endpointName)
        {
            this.endpointName = endpointName;
        }

        public async Task<string> OpenAsync(CancellationToken cancellationToken)
        {
            var nodeContext = await FabricRuntime.GetNodeContextAsync(Timeout.InfiniteTimeSpan, cancellationToken);
            var activationContext = await FabricRuntime.GetActivationContextAsync(Timeout.InfiniteTimeSpan, cancellationToken);
            var endpoint = activationContext.GetEndpoint(endpointName);

            var ub = new UriBuilder();
            ub.Host = nodeContext.IPAddressOrFQDN;
            ub.Port = endpoint.Port;
            switch (endpoint.Protocol)
            {
                case EndpointProtocol.Http:
                    ub.Scheme = Uri.UriSchemeHttp;
                    break;
                case EndpointProtocol.Https:
                    ub.Scheme = Uri.UriSchemeHttps;
                    break;
                default:// case EndpointProtocol.Tcp:
                    ub.Scheme = "tcp";
                    break;
            }
            return ub.Uri.ToString();
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public void Abort()
        {
        }
    }
}