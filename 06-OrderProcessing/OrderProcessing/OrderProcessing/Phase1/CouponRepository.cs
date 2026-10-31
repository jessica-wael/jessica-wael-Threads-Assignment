using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Storage;
namespace OrderProcessing.Phase1;

public class CouponRepository
{
    public async Task<Coupon?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        Log.Info("CouponRepository ", "Opening Connection...");
        await Task.Delay(200, ct);
        
        Log.Info("CouponRepository",$"Executing query for Coupon #{code}...");
        await Task.Delay(Random.Shared.Next(300, 601), ct);

        var coupons = await JsonFileStore.ReadAsync<Coupon>(DataFiles.Coupons, ct);
        var coupon=coupons.FirstOrDefault(c => string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase));

        Log.Info("CouponRepository", coupon is null ? $"Coupon #{code} not found" : $"Coupon #{code} found ({coupon.Code})");
        return coupon;
    }
}