using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessWatchdog
{
    public class ProcessWatchdog
    {
        private readonly string _processFriendlyName;
        private readonly string _processName;
        private readonly string _processPath;

        private CancellationTokenSource _cancellationTokenSource = new();

        public ProcessWatchdog(string processFriendlyName, string processPath, TimeSpan checkOnProcessFrequency)
        {
            _processFriendlyName = processFriendlyName;
            _processPath = processPath;
            _processName = Path.GetFileNameWithoutExtension(processPath);

            TaskUtils.ScheduleRepeatedly(_ => WatchdogTask(), checkOnProcessFrequency, _cancellationTokenSource.Token);
        }

        private Task WatchdogTask()
        {
            if (IsProcessRunning(_processName)) return Task.CompletedTask;

            try
            {
                if (string.IsNullOrWhiteSpace(_processPath) || !File.Exists(_processPath))
                    throw new ProcessInitializationException($"Unable to find {_processFriendlyName} executable at specified path: {_processPath}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);

                CancelTokenSource();

                return Task.CompletedTask;
            }

            return StartProcess();
        }

        private static bool IsProcessRunning(string processName) => !string.IsNullOrWhiteSpace(processName) && Process.GetProcessesByName(processName).Any();

        private async Task StartProcess()
        {
            var versionInfo = FileVersionInfo.GetVersionInfo(_processPath);
            Console.WriteLine($"Starting {_processFriendlyName} v{versionInfo.FileVersion}");

            try
            {
                var process = new Process
                {
                    StartInfo =
                    {
                        UseShellExecute = false,
                        RedirectStandardError = true,
                        FileName = _processPath,
                        WorkingDirectory =  Path.GetDirectoryName(_processPath),
                        /* Add command line args using the "Arguments" property */
                    }
                };

                /* Add environment variables required by process here: */
                //if (!process.StartInfo.EnvironmentVariables.ContainsKey("ASPNETCORE_ENVIRONMENT"))
                //    process.StartInfo.EnvironmentVariables.Add("ASPNETCORE_ENVIRONMENT", "Production");

                process.Start();

                await Task.Delay(500);

                if (process.HasExited)
                    throw new Exception(process.StandardError.ReadToEnd());

                Console.WriteLine($"Started {_processFriendlyName} v{versionInfo.FileVersion}");
            }
            catch (Exception e)
            {
                CancelTokenSource();
                Console.WriteLine(e.Message);
            }
        }

        public void TerminateProcess()
        {
            Console.WriteLine($"Stopping {_processFriendlyName}");

            CancelTokenSource();

            try
            {
                foreach (var proc in Process.GetProcessesByName(_processName))
                    proc.Kill();

                Console.WriteLine($"Stopped {_processFriendlyName}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private void CancelTokenSource()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        private class ProcessInitializationException : Exception
        {
            public ProcessInitializationException(string message) : base(message) { }
        }
    }
}
