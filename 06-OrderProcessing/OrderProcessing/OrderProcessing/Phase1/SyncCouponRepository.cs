using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Storage;
namespace OrderProcessing.Phase1;

public class SyncCouponRepository
{
    public Coupon? GetByCode(string  code)
    {
        Log.Info("SyncCouponRepository", "Opening connection...");
        Thread.Sleep(200);

        Log.Info("SyncCouponRepository", $"Executing query for Coupon '{code}'...");
        Thread.Sleep(Random.Shared.Next(300, 601));

        var coupons = JsonFileStore.Read<Coupon>(DataFiles.Coupons);
        var coupon = coupons.FirstOrDefault(c => string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase));

        Log.Info("SyncCouponRepository", coupon is null ? $"Coupon '{code}' not found" : $"Coupon '{code}' found");
        return coupon;
    }
}