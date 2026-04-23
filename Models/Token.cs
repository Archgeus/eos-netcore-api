namespace EOS.Models;

public class Token
{
    public int Id { get; set; }
    public string TokenValue { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public int UserId { get; set; }
    public User? User { get; set; }
}
