using EOS.Data;
using EOS.Models;
using Microsoft.EntityFrameworkCore;

namespace EOS.Services;

public class UserService
{
    private readonly AppDbContext _context;

    public UserService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<bool> UpdateBalanceAsync(int userId, long amount)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return false;

        user.Balance += (int)amount;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<long> GetBalanceAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user?.Balance ?? 0;
    }

    public async Task<bool> DeductBalanceAsync(int userId, long amount)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null || user.Balance < amount)
            return false;

        user.Balance -= (int)amount;
        await _context.SaveChangesAsync();
        return true;
    }
}
