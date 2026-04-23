using Microsoft.AspNetCore.Mvc;
using EOS.Services;
using EOS.Data;
using EOS.Models;
using Microsoft.EntityFrameworkCore;

namespace EOS.Controllers;

/// <summary>
/// Admin/utility endpoints for user management and account operations.
/// </summary>
[ApiController]
public class AdminController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly UserService _userService;
    private readonly AppDbContext _dbContext;

    public AdminController(AuthService authService, UserService userService, AppDbContext dbContext)
    {
        _authService = authService;
        _userService = userService;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Get user information by ID.
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>User id, username, join_date, last_login</returns>
    [HttpGet("/GetUserInfoById")]
    public async Task<IActionResult> GetUserInfoById([FromQuery] int? id)
    {
        if (id == null || id <= 0)
            return BadRequest(new { error = "User ID required" });

        var user = await _dbContext.Users.FindAsync(id);
        if (user == null)
            return NotFound(new { error = "User not found" });

        return Ok(new
        {
            id = user.Id,
            username = user.Username,
            join_date = user.JoinDate,
            last_login = user.LastLogin
        });
    }

    /// <summary>
    /// List all user accounts.
    /// </summary>
    /// <returns>Array of all users with id, username, join_date, last_login</returns>
    [HttpGet("/ListAccounts")]
    public async Task<IActionResult> ListAccounts()
    {
        var users = await _dbContext.Users
            .Select(u => new
            {
                id = u.Id,
                username = u.Username,
                join_date = u.JoinDate,
                last_login = u.LastLogin
            })
            .ToListAsync();

        return Ok(users);
    }

    /// <summary>
    /// List currently online user accounts (users with valid, non-expired tokens).
    /// </summary>
    /// <returns>Array of online users with id, username, join_date, last_login</returns>
    [HttpGet("/ListOnlineAccounts")]
    public async Task<IActionResult> ListOnlineAccounts()
    {
        var onlineUsers = await _dbContext.Tokens
            .Where(t => t.CreatedAt > DateTime.UtcNow.AddMinutes(-20) && t.User != null)
            .Select(t => t.User!)
            .Distinct()
            .Select(u => new
            {
                id = u.Id,
                username = u.Username,
                join_date = u.JoinDate,
                last_login = u.LastLogin
            })
            .ToListAsync();

        return Ok(onlineUsers);
    }

    /// <summary>
    /// Add funds to a user account.
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="tnx_id">Transaction ID</param>
    /// <param name="amount">Amount to add</param>
    /// <returns>Updated balance</returns>
    [HttpPost("/AddFundsByUserId")]
    public async Task<IActionResult> AddFundsByUserId(
        [FromForm] int? id,
        [FromForm] string? tnx_id,
        [FromForm] int? amount)
    {
        if (id == null || id <= 0)
            return BadRequest(new { error = "User ID required" });

        if (amount == null || amount <= 0)
            return BadRequest(new { error = "Amount must be greater than 0" });

        var user = await _dbContext.Users.FindAsync(id);
        if (user == null)
            return NotFound(new { error = "User not found" });

        // Record balance before
        var balanceBefore = user.Balance;

        // Add funds (increase balance)
        user.Balance += amount.Value;
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync();

        // Save transaction record
        var transaction = new FundsTransaction
        {
            UserId = user.Id,
            TransactionId = tnx_id ?? string.Empty,
            Amount = amount.Value,
            Operation = "add",
            BalanceBefore = balanceBefore,
            BalanceAfter = user.Balance,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.FundsTransactions.Add(transaction);
        await _dbContext.SaveChangesAsync();

        Console.WriteLine($"[ADD_FUNDS] UserID={id}, Amount={amount}, TnxID={tnx_id}, BalanceBefore={balanceBefore}, BalanceAfter={user.Balance}");

        return Ok(new
        {
            message = "Funds added successfully",
            new_balance = user.Balance
        });
    }

    /// <summary>
    /// Remove funds from a user account.
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="tnx_id">Transaction ID</param>
    /// <param name="amount">Amount to remove</param>
    /// <returns>Updated balance or error if insufficient funds</returns>
    [HttpPost("/RemFundsByUserId")]
    public async Task<IActionResult> RemFundsByUserId(
        [FromForm] int? id,
        [FromForm] string? tnx_id,
        [FromForm] int? amount)
    {
        if (id == null || id <= 0)
            return BadRequest(new { error = "User ID required" });

        if (amount == null || amount <= 0)
            return BadRequest(new { error = "Amount must be greater than 0" });

        var user = await _dbContext.Users.FindAsync(id);
        if (user == null)
            return NotFound(new { error = "User not found" });

        if (user.Balance < amount)
        {
            Console.WriteLine($"[REM_FUNDS_FAILED] UserID={id}, Amount={amount}, CurrentBalance={user.Balance}, TnxID={tnx_id}");
            return BadRequest(new { error = "Insufficient balance" });
        }

        // Record balance before
        var balanceBefore = user.Balance;

        // Remove funds (decrease balance)
        user.Balance -= amount.Value;
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync();

        // Save transaction record
        var transaction = new FundsTransaction
        {
            UserId = user.Id,
            TransactionId = tnx_id ?? string.Empty,
            Amount = amount.Value,
            Operation = "remove",
            BalanceBefore = balanceBefore,
            BalanceAfter = user.Balance,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.FundsTransactions.Add(transaction);
        await _dbContext.SaveChangesAsync();

        Console.WriteLine($"[REM_FUNDS] UserID={id}, Amount={amount}, TnxID={tnx_id}, BalanceBefore={balanceBefore}, BalanceAfter={user.Balance}");

        return Ok(new
        {
            message = "Funds removed successfully",
            new_balance = user.Balance
        });
    }
}
