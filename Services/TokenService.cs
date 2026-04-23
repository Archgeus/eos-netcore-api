using EOS.Data;
using EOS.Models;
using Microsoft.EntityFrameworkCore;

namespace EOS.Services;

public class TokenService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;

    public TokenService(AppDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    public async Task<Token?> GetValidTokenAsync(string tokenValue)
    {
        var minutes = int.TryParse(_config["AppSettings:TokenExpiryMinutes"], out var m) ? m : 20;

        return await _context.Tokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenValue == tokenValue && 
                t.CreatedAt > DateTime.UtcNow.AddMinutes(-minutes));
    }

    public async Task<bool> RevokeTokenAsync(string tokenValue)
    {
        var token = await _context.Tokens.FirstOrDefaultAsync(t => t.TokenValue == tokenValue);
        if (token == null)
            return false;

        _context.Tokens.Remove(token);
        await _context.SaveChangesAsync();
        return true;
    }
}
