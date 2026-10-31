using System.Diagnostics;
using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Models;

namespace OrderProcessing.Phase4;

public class ShippingQuoteService
{
    private readonly IEnumerable<IShippingCarrier> _carriers;

    public ShippingQuoteService(IEnumerable<IShippingCarrier> carriers)
    {
        _carriers = carriers;
    }

    public async Task<ShippingQuote> GetFastestQuoteAsync(Order order, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromMilliseconds(1200));

        
        var running = new Dictionary<Task<decimal>, IShippingCarrier>();
        foreach (var carrier in _carriers)
            running[carrier.GetQuoteAsync(order, cts.Token)] = carrier;

        var pending = running.Keys.ToList();

        while (pending.Count > 0)
        {
            var finished = await Task.WhenAny(pending);
            pending.Remove(finished);

           
            if (finished.IsCompletedSuccessfully)
            {
                cts.Cancel(); // R3: نلغي الباقي

                var cancelled = new List<string>();
                foreach (var loser in pending)
                {
                    try { await loser; }
                    catch (OperationCanceledException) { cancelled.Add(running[loser].Name); }
                    catch { } // شركة فشلت من نفسها، مش اتلغت
                }

                return new ShippingQuote
                {
                    Carrier = running[finished].Name,
                    Price = await finished,
                    ElapsedMs = sw.ElapsedMilliseconds,
                    Cancelled = cancelled
                };
            }
        }

       
        ct.ThrowIfCancellationRequested(); // R6

        if (cts.IsCancellationRequested) 
        {
            return new ShippingQuote
            {
                Carrier = "Flat rate",
                Price = 25.00m,
                ElapsedMs = sw.ElapsedMilliseconds
            };
        }

        throw new ShippingProviderUnavailableException("All carriers failed"); // R5
    }
}