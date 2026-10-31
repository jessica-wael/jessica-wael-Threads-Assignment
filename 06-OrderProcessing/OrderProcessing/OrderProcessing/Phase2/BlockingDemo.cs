using System.Diagnostics;
using OrderProcessing.Shared.Logging;

namespace OrderProcessing.Phase2;

public static class BlockingDemo
{
   
    public static void Run()
    {
        Log.Info("BlockingDemo", "Calling .Result on an unfinished task...");
        var sw = Stopwatch.StartNew();

        Task<int> task = SlowAsync();
        int value = task.Result;   

        sw.Stop();
        Log.Info("BlockingDemo", $".Result returned {value} after {sw.ElapsedMilliseconds} ms.");

        Log.Info("BlockingDemo", "Calling .Wait() on an unfinished task...");
        sw.Restart();
        Task.Delay(1000).Wait();
        sw.Stop();
        Log.Info("BlockingDemo", $".Wait() finished after {sw.ElapsedMilliseconds} ms. The thread was blocked.");
    }

    private static async Task<int> SlowAsync()
    {
        await Task.Delay(2000);
        return 42;
    }
}