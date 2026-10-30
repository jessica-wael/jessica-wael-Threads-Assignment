using OrderProcessing.Phase1;
using OrderProcessing.Shared.Models;

namespace OrderProcessing.Phase2;

public class OrderProcessorV2
{
    
        private readonly ProductRepository _products;
        private readonly OrderRepository _orders;
        private readonly OrderPricer _pricer;
        private readonly InvoiceGenerator _invoiceGenerator;
        
        public OrderProcessorV2(ProductRepository products, OrderRepository orders, OrderPricer pricer, InvoiceGenerator invoiceGenerator)
        {
            _products = products;
            _orders = orders;
            _pricer = pricer;
            _invoiceGenerator = invoiceGenerator;
        }

        public async Task<bool> ProcessValidatedOrderAsync(Order order , CancellationToken ct = default)
        {
            var reserved = new List<OrderItem>();

            foreach (var item in order.Items!)
            {
                bool ok = await _products.ReserveStockAsync(item.ProductId, item.Quantity, ct);

                if (!ok)
                {
                    foreach (var done in reserved)
                    {
                        await _products.ReleaseStockAsync(done.ProductId, done.Quantity, ct);
                    }

                    var product = await _products.GetByIdAsync(item.ProductId, ct);
                    order.Status = OrderStatus.Rejected;
                    order.FailureReason = $"Insufficient stock for '{product.Name}'";
                    await _orders.SaveAsync(order, ct);
                    return false;
                }
                reserved.Add(item);
            }

            await _pricer.PriceOrderAsync(order, ct);
            await _invoiceGenerator.GenerateAsync(order, ct);

            order.Status = OrderStatus.Completed;
            await _orders.SaveAsync(order, ct);
            return true;


        }
}