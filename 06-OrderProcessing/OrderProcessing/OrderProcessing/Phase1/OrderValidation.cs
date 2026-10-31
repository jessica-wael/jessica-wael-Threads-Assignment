using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;

namespace OrderProcessing.Phase1;

public class OrderValidation
{
    
    private readonly CustomerRepository _customers = new();
    private readonly ProductRepository _products = new();
    private readonly CouponRepository _coupons = new();
    private readonly OrderRepository _orders = new();

    public async Task<bool> ValidateOrderAsync(Order order, CancellationToken ct = default)
    {
        bool ok = await CheckOrderAsync(order, ct);
        await _orders.SaveAsync(order, ct);
        return ok;
    }

    private  async Task<bool> CheckOrderAsync(Order order, CancellationToken ct = default)
    {
        var customer = await _customers.GetByIdAsync(order.CustomerId, ct);
        if (customer is null)
            return Reject(order, $"Customer #{order.CustomerId} not found");

        if (!customer.IsActive)
            return Reject(order, "Customer is inactive");
        
        var products = new Dictionary<int, Product>();
        
        foreach (var item in order.Items!)
        {
            var product = await _products.GetByIdAsync(item.ProductId, ct);
            if (product is null)
                return Reject(order, $"Product #{item.ProductId} not found");
            products[item.ProductId] = product;
        }

        foreach (var item in order.Items)
        {
            var product = products[item.ProductId];
            if (product.StockQuantity < item.Quantity)
                return Reject(order, $"Insufficient stock for '{product.Name}'");
        }
        
        if (order.CouponCode is not null)
        {
            var coupon = await _coupons.GetByCodeAsync(order.CouponCode, ct);
            if (coupon is null)
                return Reject(order, $"Coupon '{order.CouponCode}' does not exist");

            if (coupon.ExpiryDate < DateTime.Today)
                return Reject(order, $"Coupon '{order.CouponCode}' has expired");
        }

        order.Status = OrderStatus.Validated;
        Log.Success("OrderValidator", $"Order #{order.Id} validated");
        return true;
        
    }
    private static bool Reject(Order order, string reason)
    {
        order.Status = OrderStatus.Rejected;
        order.FailureReason = reason;
        Log.Error("OrderValidator", $"Order #{order.Id} rejected: {reason}");
        return false;
    }
}