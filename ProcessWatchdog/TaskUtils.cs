using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessWatchdog
{
    public static class TaskUtils
    {
        public static async void ScheduleRepeatedly(Func<CancellationToken, Task> func, TimeSpan repeatFrequency, CancellationToken cancellationToken, TimeSpan initialDelay = default)
        {
            try
            {
                if (initialDelay != default)
                    await Task.Delay(initialDelay, cancellationToken);

                while (true)
                {
                    var startTime = DateTime.UtcNow;

                    try
                    {
                        await func(cancellationToken);
                    }
                    catch (TaskCanceledException)
                    {
                        throw;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }

                    var executionTimeInMs = Convert.ToInt32((DateTime.UtcNow - startTime).TotalMilliseconds);
                    var repeatTimeInMs = Convert.ToInt32(repeatFrequency.TotalMilliseconds);

                    if (executionTimeInMs > repeatTimeInMs)
                        Console.WriteLine("Execution time exceeded repeat time");
                    else
                        await Task.Delay(repeatTimeInMs - executionTimeInMs, cancellationToken);
                }
            }
            catch (TaskCanceledException e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}