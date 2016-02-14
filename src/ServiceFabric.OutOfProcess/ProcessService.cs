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
        protected abstract Task ConfigureAsync(ProcessStartInfo psi, CancellationToken cancellationToken);

        private readonly object _lock = new object();
        private Process _process;
        private readonly Func<IProcessServiceEventSource> _getEventSource;

        protected ProcessService(Func<IProcessServiceEventSource> getEventSource)
        {
            _getEventSource = getEventSource;
        }

        protected override async Task OnOpenAsync(IStatelessServicePartition partition, CancellationToken cancellationToken)
        {
            Monitor.Enter(_lock);
            try
            {
                StopProcess(ref _process);
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
                    await ConfigureAsync(process.StartInfo, cancellationToken);
                    _getEventSource().Message($"{process.StartInfo.FileName} {process.StartInfo.Arguments}");
                    if (!process.Start())
                        throw new Exception("could not start process");
                    process.BeginOutputReadLine();
                }
                catch
                {
                    StopProcess(ref process);
                    throw;
                }
                _process = process;
            }
            finally
            {
                Monitor.Exit(_lock);
            }
            await base.OnOpenAsync(partition, cancellationToken);
        }

        protected override Task OnCloseAsync(CancellationToken cancellationToken)
        {
            lock (_lock)
                StopProcess(ref _process);
            return base.OnCloseAsync(cancellationToken);
        }

        protected override void OnAbort()
        {
            lock (_lock)
                StopProcess(ref _process);
            base.OnAbort();
        }

        protected ServiceInstanceListener Listen(string endpointName)
        {
            return new ServiceInstanceListener(initializationParameters => new ProcessCommunicationListener(endpointName));
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
            _getEventSource().Message(e.Data);
        }
    }
}
