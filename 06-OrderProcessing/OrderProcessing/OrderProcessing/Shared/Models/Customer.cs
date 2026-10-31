namespace OrderProcessing.Shared.Models;

public class Customer
{//Name, Email, IsActive:bool, LoyaltyTier
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public  bool IsActive { get; set; }
    public LoyaltyTier Tier{ get; set; }
    
}