using System;
using System.Threading.Tasks;
using System.Fabric;
using System.Threading;
using System.Diagnostics;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.ServiceFabric.Services.Communication.Runtime;

namespace ServiceFabric.OutOfProcess
{
    public interface IProcessServiceEventSource
    {
        void Message(string message);
    }

    public abstract class ProcessService : StatelessServiceBase
    {
        protected abstract Task ConfigureProcessAsync(ProcessStartInfo psi, CancellationToken cancellationToken);

        protected Process process;
        protected readonly IProcessServiceEventSource eventSource;

        protected ProcessService(IProcessServiceEventSource eventSource)
        {
            this.eventSource = eventSource;
        }

        protected virtual bool StartProcessOnOpenAsync { get { return true; } }

        protected override async Task OnOpenAsync(IStatelessServicePartition partition, CancellationToken cancellationToken)
        {
            if (StartProcessOnOpenAsync)
                await StartProcessAsync(cancellationToken);
            await base.OnOpenAsync(partition, cancellationToken);
        }

        protected override Task OnCloseAsync(CancellationToken cancellationToken)
        {
            StopProcess(ref process);
            return base.OnCloseAsync(cancellationToken);
        }

        protected override void OnAbort()
        {
            StopProcess(ref process);
            base.OnAbort();
        }

        protected ServiceInstanceListener Listen(string endpointName)
        {
            return new ServiceInstanceListener(initializationParameters => new ProcessCommunicationListener(endpointName));
        }

        protected async Task StartProcessAsync(CancellationToken cancellationToken)
        {
            StopProcess(ref this.process);
            var process = new Process();
            try
            {
                process.OutputDataReceived += _process_OutputDataReceived;
                process.Exited += _process_Exited;
                process.EnableRaisingEvents = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                await ConfigureProcessAsync(process.StartInfo, cancellationToken);
                eventSource.Message($"{process.StartInfo.FileName} {process.StartInfo.Arguments}");
                if (!process.Start())
                    throw new Exception("could not start process");
                process.BeginOutputReadLine();
            }
            catch
            {
                StopProcess(ref process);
                throw;
            }
            this.process = process;
        }

        private void StopProcess(ref Process process)
        {
            if (process == null)
                return;
            try
            {
                process.Refresh();
                if (!process.HasExited)
                    process.StandardInput.Close();
            }
            finally
            {
                process.Dispose();
            }
        }

        private void _process_Exited(object sender, EventArgs e)
        {
            Console.WriteLine("process has exited");
        }

        private void _process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine(e.Data);
            eventSource.Message(e.Data);
        }
    }
}
