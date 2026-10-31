using OrderProcessing.Shared.Models;

namespace OrderProcessing.Phase3;

public class SyncOrderProcessor
{
    private static readonly object StockLock = new();
    private readonly Dictionary<int, Customer> _customers;
    private readonly Dictionary<int, Product> _products;

    public SyncOrderProcessor(IEnumerable<Customer> customers, IEnumerable<Product> products)
    {
        _customers = customers.ToDictionary(c => c.Id);
        _products = products.ToDictionary(p => p.Id);
    }

    public void Process(Order order)
    {
        // Step 1: 
        Thread.Sleep(200 + Random.Shared.Next(300, 601));
        if (!_customers.TryGetValue(order.CustomerId, out var customer))
        {
            Reject(order, $"Customer #{order.CustomerId} is not found");
            return;
        }
        if (!customer.IsActive)
        {
            Reject(order, "Customer is inactive");
            return;
        }

        // Step 2: 
        Thread.Sleep(900 * order.Items!.Count);
        lock (StockLock)
        {
            bool ok = order.Items.All(i => _products[i.ProductId].StockQuantity >= i.Quantity);
            if (!ok)
            {
                Reject(order, "Insufficient stock");
                return;
            }
            foreach (var item in order.Items)
                _products[item.ProductId].StockQuantity -= item.Quantity;
        }

        // Step 3: 
        Thread.Sleep(950 + Random.Shared.Next(500, 1501));
        Thread.Sleep(950 + Random.Shared.Next(500, 1501));

        // Step 4: 
        Thread.Sleep(800);   // authorize
        Thread.Sleep(500);   // fraud
        Thread.Sleep(700);   // capture

        // Step 5: 
        Thread.Sleep(900);
        order.Status = OrderStatus.Completed;

        // Step 6: 
        Thread.Sleep(1000);
    }

    private void Reject(Order order, string reason)
    {
        Thread.Sleep(900);   // حفظ الـ Rejected
        order.Status = OrderStatus.Rejected;
        order.FailureReason = reason;
    }
}