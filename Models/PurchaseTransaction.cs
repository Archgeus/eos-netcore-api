namespace EOS.Models;

public class PurchaseTransaction
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string RequestId { get; set; } = string.Empty;
    public int Price { get; set; }
    public int TransactionId { get; set; }
    public int ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? UserIp { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public User? User { get; set; }
}
