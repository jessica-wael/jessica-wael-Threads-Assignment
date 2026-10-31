using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase4;

public interface IShippingCarrier
{
    string Name { get; }
    Task<decimal> GetQuoteAsync(Order order, CancellationToken ct);
}

public class ShippingQuote
{
    public string Carrier { get; set; } = "";
    public decimal Price { get; set; }
    public long ElapsedMs { get; set; }
    public List<string> Cancelled { get; set; } = new();
}

// الشغل المشترك بين الشركات التلاتة
internal static class CarrierSimulation
{
    public static async Task<decimal> GetQuoteAsync(
        string name, Order order, int minMs, int maxMs, decimal factor, bool fail, CancellationToken ct)
    {
        try
        {
            await Task.Delay(150, ct); // handshake

            if (fail)
                throw new ShippingProviderUnavailableException($"{name} unavailable");

            await Task.Delay(Random.Shared.Next(minMs, maxMs + 1), ct);

            decimal basePrice = await BasePriceAsync(order, ct);
            return Math.Round(basePrice * factor, 2, MidpointRounding.AwayFromZero);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"[{name}] cancelled");
            throw;
        }
    }

    // معادلة الشحن من 8.1
    private static async Task<decimal> BasePriceAsync(Order order, CancellationToken ct)
    {
        var products = await JsonFileStore.ReadAsync<Product>(DataFiles.Products, ct);

        decimal subTotal = 0;
        int qty = 0;
        decimal weight = 0;

        foreach (var item in order.Items!)
        {
            var p = products.First(x => x.Id == item.ProductId);
            subTotal += p.Price * item.Quantity;
            qty += item.Quantity;
            weight += p.WeightKg * item.Quantity;
        }

        if (subTotal >= 1000) return 0m;
        return 5.00m + 1.50m * qty + 2.00m * weight;
    }
}

public class FastShip : IShippingCarrier
{
    private readonly HashSet<int> _failForOrderIds;
    public FastShip(IEnumerable<int>? failForOrderIds = null)
        => _failForOrderIds = new HashSet<int>(failForOrderIds ?? Array.Empty<int>());

    public string Name => "FastShip";

    public Task<decimal> GetQuoteAsync(Order order, CancellationToken ct)
        => CarrierSimulation.GetQuoteAsync(Name, order, 500, 1500, 1.00m,
            _failForOrderIds.Contains(order.Id), ct);
}

public class CairoExpress : IShippingCarrier
{
    private readonly HashSet<int> _failForOrderIds;
    public CairoExpress(IEnumerable<int>? failForOrderIds = null)
        => _failForOrderIds = new HashSet<int>(failForOrderIds ?? Array.Empty<int>());

    public string Name => "CairoExpress";

    public Task<decimal> GetQuoteAsync(Order order, CancellationToken ct)
        => CarrierSimulation.GetQuoteAsync(Name, order, 300, 2000, 0.95m,
            order.Id % 2 == 0 || _failForOrderIds.Contains(order.Id), ct); // بتفشل دايماً مع الأرقام الزوجية
}

public class NileCourier : IShippingCarrier
{
    private readonly HashSet<int> _failForOrderIds;
    public NileCourier(IEnumerable<int>? failForOrderIds = null)
        => _failForOrderIds = new HashSet<int>(failForOrderIds ?? Array.Empty<int>());

    public string Name => "NileCourier";

    public Task<decimal> GetQuoteAsync(Order order, CancellationToken ct)
        => CarrierSimulation.GetQuoteAsync(Name, order, 800, 1200, 1.10m,
            _failForOrderIds.Contains(order.Id), ct);
}