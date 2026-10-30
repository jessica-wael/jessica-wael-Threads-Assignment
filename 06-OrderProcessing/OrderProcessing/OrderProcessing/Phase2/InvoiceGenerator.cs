using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;

namespace OrderProcessing.Phase2;

public class InvoiceGenerator
{
    public async Task GenerateAsync(Order order, CancellationToken ct = default)
    {
        Log.Info("InvoiceGenerator", $"Rendering invoice for Order #{order.Id}...");

        double checksum = await Task.Run(() => Render(), ct);

        var folder = Path.Combine(AppContext.BaseDirectory, "Data", "invoices");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, $"invoice_{order.Id}.txt");

        var text =
            $"Invoice for order #{order.Id}\n" +
            $"Customer: {order.CustomerId}\n" +
            $"SubTotal: {order.Subtotal:F2}\n" +
            $"Discount: {order.Discount:F2}\n" +
            $"Shipping: {order.ShippingCost:F2}\n" +
            $"Total: {order.TotalAmount:F2}\n" +
            $"Render checksum: {checksum}\n";

        await File.WriteAllTextAsync(path, text, ct);

        Log.Success("InvoiceGenerator", $"Invoice saved for Order #{order.Id}");
    }

    private static double Render()
    {
        double sum = 0;
        for (int i = 0; i < 20_000_000; i++)
            sum += Math.Sqrt(i);
        return sum;
    }
}