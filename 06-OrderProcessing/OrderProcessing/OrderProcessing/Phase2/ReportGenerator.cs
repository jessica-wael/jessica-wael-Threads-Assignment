using System.Text.Json;
using OrderProcessing.Phase1;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;

namespace OrderProcessing.Phase2;

public class ReportGenerator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly OrderRepository _orders = new();
    private readonly ProductRepository _products = new();

    public async Task<Report> GenerateAsync(CancellationToken ct = default)
    {
        var all = await _orders.GetAllAsync(ct);
        var completed = all.Where(o => o.Status == OrderStatus.Completed).ToList();

        var report = new Report
        {
            TotalOrders = all.Count,
            CompletedCount = completed.Count,
            RejectedCount = all.Count(o => o.Status == OrderStatus.Rejected),
            FailedCount = all.Count(o => o.Status == OrderStatus.Failed),
            PaymentFailedCount = all.Count(o => o.Status == OrderStatus.PaymentFailed),
            TotalRevenue = completed.Sum(o => o.TotalAmount),
            GeneratedAt = DateTime.Now
        };

        report.AverageOrderValue = report.CompletedCount == 0
            ? 0m
            : Math.Round(report.TotalRevenue / report.CompletedCount, 2, MidpointRounding.AwayFromZero);

        var top = completed
            .SelectMany(o => o.Items!)
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, Quantity = g.Sum(i => i.Quantity) })
            .OrderByDescending(x => x.Quantity)
            .ThenBy(x => x.ProductId)
            .FirstOrDefault();

        if (top is not null)
        {
            var product = await _products.GetByIdAsync(top.ProductId, ct);
            report.TopSellingProduct = product?.Name ?? $"#{top.ProductId}";
            report.TopSellingQuantity = top.Quantity;
        }

        var folder = Path.Combine(AppContext.BaseDirectory, "Data", "reports");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, $"report_{DateTime.Now:yyyyMMdd_HHmmss}.json");

        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(report, JsonOptions), ct);
        Log.Success("ReportGenerator", $"Report saved to {path}");

        return report;
    }
}