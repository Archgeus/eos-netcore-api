using System.Security.Cryptography;
using System.Text;
using EOS.Data;
using EOS.Models;
using Microsoft.EntityFrameworkCore;

namespace EOS.Services;

public class AuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;
    private const string POOL = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

    public AuthService(AppDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    public string HashPassword(string password)
    {
        var salt = _config["AppSettings:AccountSalt"] ?? "@#56qtasfGSAG";
        var saltAndPwd = password + salt;
        
        using (var sha512 = SHA512.Create())
        {
            var hash = sha512.ComputeHash(Encoding.UTF8.GetBytes(saltAndPwd));
            var hex = BitConverter.ToString(hash).Replace("-", "");
            
            var result = new StringBuilder();
            for (int i = 0; i < hex.Length; i += 2)
            {
                var byteStr = hex.Substring(i, 2);
                var val = int.Parse(byteStr, System.Globalization.NumberStyles.HexNumber);
                if (val < 16)
                    result.Append(val.ToString());
                else
                    result.Append(byteStr);
            }
            
            return result.ToString().ToUpper();
        }
    }

    public bool VerifyPassword(string password, string hash)
    {
        var hashOfInput = HashPassword(password);
        return hashOfInput == hash;
    }

    public string GenerateToken(int length = 64)
    {
        var rng = new Random();
        var token = new StringBuilder();
        
        for (int i = 0; i < length; i++)
        {
            token.Append(POOL[rng.Next(POOL.Length)]);
        }
        
        return token.ToString();
    }

    public async Task<(bool success, string message)> RegisterAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return (false, "Username and password required");

        var exists = await _context.Users.AnyAsync(u => u.Username == username);
        if (exists)
            return (false, "Username already taken");

        var user = new User
        {
            Username = username,
            Password = HashPassword(password)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return (true, "Registration successful");
    }

    public async Task<(bool success, string token)> LoginAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return (false, "");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null || !VerifyPassword(password, user.Password))
            return (false, "");

        // Update LastLogin
        user.LastLogin = DateTime.UtcNow;

        // Delete expired tokens and create new one
        var expiredTokens = await _context.Tokens
            .Where(t => t.Username == username && 
                t.CreatedAt <= DateTime.UtcNow.AddMinutes(-20))
            .ToListAsync();
        
        if (expiredTokens.Any())
        {
            _context.Tokens.RemoveRange(expiredTokens);
            await _context.SaveChangesAsync();
        }

        // Reuse existing valid token if it exists
        var existingToken = await _context.Tokens
            .FirstOrDefaultAsync(t => t.Username == username && 
                t.CreatedAt > DateTime.UtcNow.AddMinutes(-20));

        if (existingToken != null)
        {
            // Just reuse the existing token (don't update CreatedAt like PHP does)
            await _context.SaveChangesAsync();
            return (true, existingToken.TokenValue);
        }

        // Create new token
        var token = new Token
        {
            TokenValue = GenerateToken(64),
            Username = username,
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow
        };

        _context.Tokens.Add(token);
        await _context.SaveChangesAsync();

        return (true, token.TokenValue);
    }

    public async Task<User?> ValidateTokenAsync(string accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken) || !accessToken.All(c => char.IsLetterOrDigit(c)))
            return null;

        // Just check if token exists (don't enforce expiry on API calls like PHP)
        var token = await _context.Tokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenValue == accessToken);

        return token?.User;
    }

    public async Task<(bool success, string token)> RefreshTokenAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken) || !refreshToken.All(c => char.IsLetterOrDigit(c)))
            return (false, "");

        // Find the refresh token in database
        var token = await _context.Tokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenValue == refreshToken);

        if (token?.User == null)
            return (false, "");

        // Update CreatedAt to reset the 20-minute window (like PHP does)
        token.CreatedAt = DateTime.UtcNow;
        _context.Tokens.Update(token);
        await _context.SaveChangesAsync();

        // Return the same token with fresh timestamp
        return (true, token.TokenValue);
    }
}
