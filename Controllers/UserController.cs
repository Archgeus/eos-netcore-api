using Microsoft.AspNetCore.Mvc;
using EOS.Services;
using EOS.Data;
using EOS.Models;

namespace EOS.Controllers;

/// <summary>
/// User service endpoints for account info, balance, and in-game purchases.
/// Requires valid access_token in query parameters.
/// </summary>
[ApiController]
public class UserController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly UserService _userService;
    private readonly AppDbContext _dbContext;

    public UserController(AuthService authService, UserService userService, AppDbContext dbContext)
    {
        _authService = authService;
        _userService = userService;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Get authenticated user information.
    /// </summary>
    /// <param name="access_token">Valid access token from login</param>
    /// <param name="fields">Optional fields filter (uid, username)</param>
    /// <returns>User ID and username</returns>
    [HttpGet("/services/v2/user/me")]
    public async Task<IActionResult> GetMe([FromQuery] string? access_token, [FromQuery] string? fields)
    {
        var elapsed = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        if (string.IsNullOrWhiteSpace(access_token))
            return Unauthorized(new
            {
                error = new
                {
                    message = "Access token is invalid or expired",
                    type = "InvalidAccessTokenException",
                    code = 190
                },
                header = new { request_id = HttpContext.TraceIdentifier, elapsed_time = elapsed }
            });

        var user = await _authService.ValidateTokenAsync(access_token);
        if (user == null)
            return Unauthorized(new
            {
                error = new
                {
                    message = "Access token is invalid or expired",
                    type = "InvalidAccessTokenException",
                    code = 190
                },
                header = new { request_id = HttpContext.TraceIdentifier, elapsed_time = elapsed }
            });

        return Ok(new
        {
            data = new { uid = user.Id.ToString(), username = user.Username },
            header = new { request_id = HttpContext.TraceIdentifier, elapsed_time = elapsed }
        });
    }

    /// <summary>
    /// Get user account balance (account points / AP).
    /// </summary>
    /// <param name="access_token">Valid access token from login</param>
    /// <returns>User balance amount</returns>
    [HttpGet("/services/v2/user/mi/ap")]
    public async Task<IActionResult> GetBalance([FromQuery] string? access_token)
    {
        var elapsed = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        if (string.IsNullOrWhiteSpace(access_token))
            return Unauthorized(new
            {
                error = new
                {
                    message = "Access token is invalid or expired",
                    type = "InvalidAccessTokenException",
                    code = 190
                },
                header = new { request_id = HttpContext.TraceIdentifier, elapsed_time = elapsed }
            });

        var user = await _authService.ValidateTokenAsync(access_token);
        if (user == null)
            return Unauthorized(new
            {
                error = new
                {
                    message = "Access token is invalid or expired",
                    type = "InvalidAccessTokenException",
                    code = 190
                },
                header = new { request_id = HttpContext.TraceIdentifier, elapsed_time = elapsed }
            });

        var balance = await _userService.GetBalanceAsync(user.Id);
        return Ok(new
        {
            data = new { balance = balance.ToString() },
            header = new { request_id = HttpContext.TraceIdentifier, elapsed_time = elapsed }
        });
    }

    /// <summary>
    /// Purchase an item from the marketplace and deduct from user balance.
    /// </summary>
    /// <param name="access_token">Valid access token from login</param>
    /// <param name="price">Price of the item</param>
    /// <param name="txn_id">Transaction ID</param>
    /// <param name="item_id">Item ID being purchased</param>
    /// <param name="item_name">Item name</param>
    /// <param name="user_ip">User IP address</param>
    /// <returns>Updated balance and transaction details</returns>
    [HttpPost("/services/v2/user/mi/item_purchase")]
    public async Task<IActionResult> MakePurchase(
        [FromQuery] string? access_token,
        [FromForm] long? price,
        [FromForm] int? txn_id,
        [FromForm] int? item_id,
        [FromForm] string? item_name,
        [FromForm] string? user_ip)
    {
        var elapsed = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        // Log incoming request data
        Console.WriteLine($"[PURCHASE REQUEST] item_id={item_id}, item_name={item_name}, price={price}, txn_id={txn_id}, user_ip={user_ip}");
        if (string.IsNullOrWhiteSpace(access_token))
            return Unauthorized(new
            {
                error = new
                {
                    message = "Access token is invalid or expired",
                    type = "InvalidAccessTokenException",
                    code = 190
                },
                header = new { request_id = HttpContext.TraceIdentifier, elapsed_time = elapsed }
            });

        var user = await _authService.ValidateTokenAsync(access_token);
        if (user == null)
            return Unauthorized(new
            {
                error = new
                {
                    message = "Access token is invalid or expired",
                    type = "InvalidAccessTokenException",
                    code = 190
                },
                header = new { request_id = HttpContext.TraceIdentifier, elapsed_time = elapsed }
            });

        if (price == null || price <= 0)
            return BadRequest(new
            {
                error = new
                {
                    message = "Invalid price",
                    type = "InvalidPriceException",
                    code = 191
                },
                header = new { request_id = HttpContext.TraceIdentifier, elapsed_time = elapsed }
            });

        var success = await _userService.DeductBalanceAsync(user.Id, price.Value);
        if (!success)
            return BadRequest(new
            {
                error = new
                {
                    message = "Insufficient balance",
                    type = "InsufficientFundsException",
                    code = 191
                },
                header = new { request_id = HttpContext.TraceIdentifier, elapsed_time = elapsed }
            });

        var newBalance = await _userService.GetBalanceAsync(user.Id);

        // Save purchase transaction to database
        var transaction = new PurchaseTransaction
        {
            UserId = user.Id,
            RequestId = HttpContext.TraceIdentifier,
            Price = (int)price.Value,
            TransactionId = txn_id ?? 0,
            ItemId = item_id ?? 0,
            ItemName = item_name ?? string.Empty,
            UserIp = user_ip,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.PurchaseTransactions.Add(transaction);
        await _dbContext.SaveChangesAsync();
        
        Console.WriteLine($"[PURCHASE SAVED] TransactionID={transaction.Id}, ItemID={transaction.ItemId}, ItemName={transaction.ItemName}, Price={transaction.Price}");

        return Ok(new
        {
            data = new { balance = newBalance.ToString(), txn_detail = 0 },
            header = new { request_id = HttpContext.TraceIdentifier, elapsed_time = elapsed }
        });
    }
}
