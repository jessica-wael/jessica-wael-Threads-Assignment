using OrderProcessing.Phase1;
using OrderProcessing.Phase2;
using OrderProcessing.Phase3;
using OrderProcessing.Phase4;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        using var cts = new CancellationTokenSource();

        int index = Array.IndexOf(args, "--cancel-after");
        if (index >= 0 && index + 1 < args.Length && int.TryParse(args[index + 1], out int ms))
        {
            cts.CancelAfter(ms);
            Console.WriteLine($"This run will be cancelled after {ms} ms");
        }

        string? mode = args.Length > 0 && !args[0].StartsWith("--") ? args[0].ToLower() : null;

        if (mode is null)
        {
            Console.WriteLine("1) Phase 1\n2) Phase 2\n3) Phase 3\n4) Phase 4\n5) All");
            Console.Write("Choose: ");
            mode = Console.ReadLine() switch
            {
                "1" => "phase1",
                "2" => "phase2",
                "3" => "phase3",
                "4" => "phase4",
                "5" => "all",
                _ => ""
            };
        }

        try
        {
            switch (mode)
            {
                case "phase1": await new Phase1Runner().RunAsync(cts.Token); break;
                case "phase2": await new Phase2Runner().RunAsync(cts.Token); break;
                case "phase3": await new Phase3Runner().RunAsync(cts.Token); break;
                case "phase4": await new Phase4Runner().RunAsync(cts.Token); break;
                case "all":
                    await new Phase1Runner().RunAsync(cts.Token);
                    await new Phase2Runner().RunAsync(cts.Token);
                    await new Phase3Runner().RunAsync(cts.Token);
                    await new Phase4Runner().RunAsync(cts.Token);
                    break;
                default:
                    Console.WriteLine("Unknown mode. Use: phase1 | phase2 | phase3 | phase4 | all");
                    break;
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Run cancelled.");
        }
    }
}