using OrderProcessing.Phase1;
using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;


namespace OrderProcessing.Phase2;

public class PricingService
{
    private readonly HashSet<int> _failShippingForOrderIds;
    private readonly CustomerRepository  _customers=new ();
    private readonly ProductRepository  _products=new ();
    private readonly CouponRepository _coupons = new();
    

    public PricingService(IEnumerable<int>  failShippingForOrderIds)
    {
        _failShippingForOrderIds = new HashSet<int>(failShippingForOrderIds);
    }


    public async Task<decimal> CalculateSubTotalAsync(Order  order, CancellationToken ct=default)
    {
        decimal subTotal =0;
        foreach (var item in order.Items!)
        {
            var product = await _products.GetByIdAsync(item.ProductId, ct);
            item.UnitPrice = product!.Price;
            subTotal += item.UnitPrice * item.Quantity;
        }
        return Math.Round(subTotal,2, MidpointRounding.AwayFromZero);
    }

    public async Task<decimal> GetDiscountAsync(Order order, CancellationToken ct = default)
    {
        Log.Info("PricingService", "Discount handshake...");
        await Task.Delay(150, ct);

        Log.Info("PricingService", "Discount send request...");
        await Task.Delay(300, ct);

        Log.Info("PricingService", "Discount waiting for response...");
        await Task.Delay(Random.Shared.Next(500, 1501), ct);
        
        var customer = await _customers.GetByIdAsync(order.CustomerId, ct);
        int percent = customer!.Tier switch
        { 
            LoyaltyTier.Silver=>5, 
            LoyaltyTier.Gold=>10,
            _ =>0
          
        };

        if (order.CouponCode != null)
        {
            var coupon = await _coupons.GetByCodeAsync(order.CouponCode, ct);
            if (coupon != null)
            {
                percent += coupon.DiscountPercent;

            }
        }

        if (percent > 30)
        { 
            percent = 30; 
        }

        var discount = order.Subtotal * percent / 100; 
        return Math.Round(discount, 2, MidpointRounding.AwayFromZero);
        
    }

    public async Task<decimal> CalculateShippingCostAsync(Order order, CancellationToken ct = default)
    {
    
        Log.Info("PricingService", "Shipping handshake...");
      await Task.Delay(150, ct); 

      if (_failShippingForOrderIds.Contains(order.Id))
          throw new ShippingProviderUnavailableException("Shipping provider unavailable");
      
      Log.Info("PricingService", "Shipping send request...");
      await Task.Delay(300, ct); 
      
      Log.Info("PricingService", "Shipping waiting for response...");
      await Task.Delay(Random.Shared.Next(500, 1501), ct);
     
      int totalQty = 0;
      decimal totalWeight = 0;
        
      foreach (var item in order.Items! )
      {
          var product = await _products.GetByIdAsync(item.ProductId, ct);
          totalQty += item.Quantity;
          totalWeight += product!.WeightKg*item.Quantity;
      }

      if (order.Subtotal >= 1000)
          return 0m;
         

      var ShippingCost = 5.00m + 1.50m * totalQty + 2.00m * totalWeight;
      return Math.Round(ShippingCost, 2, MidpointRounding.AwayFromZero);
    }

    
}