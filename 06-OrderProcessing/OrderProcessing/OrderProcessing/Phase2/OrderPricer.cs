using OrderProcessing.Shared.Models;
namespace OrderProcessing.Phase2;

public class OrderPricer
{
    private readonly PricingService _service;
    public OrderPricer(PricingService service)
    {
        _service = service;
    }
    
    public async Task PriceOrderAsync(Order order,CancellationToken ct=default)
    {
        order.Subtotal = await _service.CalculateSubTotalAsync(order, ct);
        
        var discount =  _service.GetDiscountAsync(order, ct);
        var shipping = _service.CalculateShippingCostAsync(order, ct);

        order.Discount = await discount;
        order.ShippingCost = await shipping;
        order.TotalAmount = order.Subtotal - order.Discount + order.ShippingCost;
    }

    public async Task PriceOrderSequentialAsync(Order order, CancellationToken ct = default)
    {
        order.Subtotal = await _service.CalculateSubTotalAsync(order, ct);
        order.Discount = await _service.GetDiscountAsync(order, ct);
        order.ShippingCost = await _service.CalculateShippingCostAsync(order, ct);
        
        order.TotalAmount = order.Subtotal - order.Discount + order.ShippingCost;
    }
}