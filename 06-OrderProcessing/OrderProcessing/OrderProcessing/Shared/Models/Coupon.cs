namespace OrderProcessing.Shared.Models;

public class Coupon
{//Code, DiscountPercent:int, ExpiryDate:DateTime
 
 public string? Code { get; set; }
 public int DiscountPercent { get; set; }
 public DateTime ExpiryDate { get; set; }
    
} 