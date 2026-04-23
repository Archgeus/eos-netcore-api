namespace EOS.Models;

public class FundsTransaction
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public int Amount { get; set; }
    public string Operation { get; set; } = string.Empty;  // "add" or "remove"
    public int BalanceBefore { get; set; }
    public int BalanceAfter { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public User? User { get; set; }
}
