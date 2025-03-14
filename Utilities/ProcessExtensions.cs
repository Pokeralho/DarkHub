using System.Diagnostics;

namespace DarkHub.Utilities
{
    public static class ProcessExtensions
    {
        public static Task WaitForExitAsync(this Process process)
        {
            var tcs = new TaskCompletionSource<bool>();
            process.EnableRaisingEvents = true;
            process.Exited += (s, e) => tcs.TrySetResult(true);
            if (process.HasExited) tcs.TrySetResult(true);
            return tcs.Task;
        }
    }
}