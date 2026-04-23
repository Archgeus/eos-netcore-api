namespace EOS.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int Balance { get; set; } = 0;
    public DateTime JoinDate { get; set; } = DateTime.UtcNow;
    public DateTime? LastLogin { get; set; }
    
    public ICollection<Token> Tokens { get; set; } = new List<Token>();
    public ICollection<PurchaseTransaction> PurchaseTransactions { get; set; } = new List<PurchaseTransaction>();
    public ICollection<FundsTransaction> FundsTransactions { get; set; } = new List<FundsTransaction>();
}
