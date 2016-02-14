using System;
using System.Threading;
using System.Threading.Tasks;
using System.Fabric;
using System.Diagnostics;
using ServiceFabric.OutOfProcess;

namespace SpringService
{
    /// <summary>
    /// The FabricRuntime creates an instance of this class for each service type instance. 
    /// </summary>
    internal sealed class SpringService : ProcessService
    {
        const string HttpEndpoint = "Http";

        public SpringService()
            : base(ServiceEventSource.Current, HttpEndpoint)
        {
        }

        protected override async Task ConfigureAsync(ProcessStartInfo psi, CancellationToken cancellationToken)
        {
            var activationContext = await FabricRuntime.GetActivationContextAsync(Timeout.InfiniteTimeSpan, cancellationToken);
            var endpoint = activationContext.GetEndpoint(HttpEndpoint);
            var javaSection = activationContext.GetConfigurationPackageObject("Config")?.Settings.Sections["Java"];
            if (javaSection == null)
                throw new Exception("Expects <Section Name='Java'>");
            var javaPath = javaSection.Parameters["Exe"]?.Value;
            var jar = javaSection.Parameters["Jar"]?.Value;
            if (javaPath == null)
                throw new Exception("Expects <Section Name='Java'> <Parameter Name='Exe' Value='java.exe'/>");
            if (jar == null)
                throw new Exception("Expects <Section Name='Java'> <Parameter Name='Jar' Value='xx.jar'/>");

            psi.FileName = javaPath;
            psi.Arguments = string.Format("-Dserver.port={0} -jar {1}", endpoint.Port, jar);
        }
    }
}
