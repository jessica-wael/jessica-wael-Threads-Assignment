using System.Collections.Concurrent;
using System.Diagnostics;
using OrderProcessing.Shared.Logging;

namespace OrderProcessing.Phase4;

public class FraudScorer
{
    private const int OrderCount = 10_000;

    private static int Score(int orderId)
    {
        long h = orderId;
        for (int i = 0; i < 100_000; i++)
            h = (h * 31 + i) % 1_000_000_007;
        return (int)(h % 100);
    }

    private (List<int> Flagged, long Ms) ScoreSequential()
    {
        var sw = Stopwatch.StartNew();
        var flagged = new List<int>();

        for (int id = 1; id <= OrderCount; id++)
            if (Score(id) >= 90)
                flagged.Add(id);

        sw.Stop();
        return (flagged, sw.ElapsedMilliseconds);
    }

    private (List<int> Flagged, long Ms, int Threads) ScoreParallel(CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var flagged = new ConcurrentBag<int>();
        var threads = new ConcurrentDictionary<int, byte>();

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            CancellationToken = ct
        };

        Parallel.For(1, OrderCount + 1, options, id =>
        {
            threads.TryAdd(Environment.CurrentManagedThreadId, 0);
            if (Score(id) >= 90)
                flagged.Add(id);
        });

        sw.Stop();
        return (flagged.OrderBy(x => x).ToList(), sw.ElapsedMilliseconds, threads.Count);
    }

    public void Run(CancellationToken ct = default)
    {
        var seq = ScoreSequential();
        var par = ScoreParallel(ct);

        Log.Info("FraudScorer", $"Flagged: {seq.Flagged.Count} (first five: {string.Join(", ", seq.Flagged.Take(5))})");
        Log.Info("FraudScorer", $"Sequential: {seq.Ms} ms");
        Log.Info("FraudScorer", $"Parallel: {par.Ms} ms");
        Log.Info("FraudScorer", $"Speed-up: {(double)seq.Ms / par.Ms:F2}x");
        Log.Info("FraudScorer", $"Distinct threads: {par.Threads}, ProcessorCount: {Environment.ProcessorCount}");
        Log.Info("FraudScorer", $"Same result in both runs: {seq.Flagged.SequenceEqual(par.Flagged)}");
    }
}