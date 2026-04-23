using Microsoft.AspNetCore.Mvc;
using EOS.Services;

namespace EOS.Controllers;

/// <summary>
/// Authentication endpoints for user login, registration, and OAuth token exchange.
/// </summary>
[ApiController]
public class OAuthController : ControllerBase
{
    private readonly AuthService _authService;

    public OAuthController(AuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Register a new user account.
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="password">Password</param>
    /// <returns>Registration success message</returns>
    [HttpPost("/register")]
    public async Task<IActionResult> Register([FromForm] string? username, [FromForm] string? password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return BadRequest(new { error = "Username and password required" });

        var (success, message) = await _authService.RegisterAsync(username, password);
        
        if (!success)
            return BadRequest(new { error = message });

        return Ok(new { message = "Registration successful" });
    }

    /// <summary>
    /// Login to an existing user account and get access token.
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="password">Password</param>
    /// <returns>Access token for authenticated requests</returns>
    [HttpPost("/login")]
    public async Task<IActionResult> Login([FromForm] string? username, [FromForm] string? password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return BadRequest(new { error = "Username and password required" });

        var (success, token) = await _authService.LoginAsync(username, password);
        
        if (!success)
            return Unauthorized(new { error = "Invalid credentials" });

        return Ok(token);
    }

    /// <summary>
    /// OAuth 2.0 access token endpoint. Supports code flow and refresh token flow via POST.
    /// </summary>
    /// <param name="grant_type">Grant type (authorization_code or refresh_token)</param>
    /// <param name="code">Authorization code (for code flow)</param>
    /// <param name="refresh_token">Refresh token (for refresh flow)</param>
    /// <param name="client_id">Client ID</param>
    /// <param name="client_secret">Client secret</param>
    /// <param name="scope">OAuth scope</param>
    /// <returns>Access token response</returns>
    [HttpPost("/oauth/access_token")]
    public async Task<IActionResult> AccessToken(
        [FromForm] string? grant_type,
        [FromForm] string? code,
        [FromForm] string? client_id,
        [FromForm] string? client_secret,
        [FromForm] string? refresh_token,
        [FromForm] string? scope)
    {
        // OAuth code flow (authorization_code grant type)
        if (!string.IsNullOrWhiteSpace(code) && code.All(c => char.IsLetterOrDigit(c)))
        {
            var user = await _authService.ValidateTokenAsync(code);
            if (user == null)
                return BadRequest(new { error_code = 701, error_msg = "Authentication failed" });

            return Ok(new
            {
                access_token = code,
                expires_in = 1200,
                token_type = "bearer",
                scope = scope,
                refresh_token = code
            });
        }

        // Refresh token flow
        if (!string.IsNullOrWhiteSpace(refresh_token) && refresh_token.Length == 64)
        {
            var (success, token) = await _authService.RefreshTokenAsync(refresh_token);
            if (!success)
                return BadRequest(new { error_code = 701, error_msg = "Authentication failed" });

            return Ok(new
            {
                access_token = token,
                expires_in = 1200,
                token_type = "bearer",
                scope = scope,
                refresh_token = token
            });
        }

        return BadRequest(new { error_code = 701, error_msg = "Authentication failed" });
    }

    /// <summary>
    /// Refresh token endpoint (GET). Used when token expires and needs refresh from client.
    /// </summary>
    /// <param name="refresh_token">Refresh token (64 chars)</param>
    /// <returns>New access token</returns>
    [HttpGet("/oauth/access_token")]
    public async Task<IActionResult> RefreshTokenGet([FromQuery] string? refresh_token, [FromQuery] string? scope)
    {
        if (!string.IsNullOrWhiteSpace(refresh_token) && refresh_token.Length == 64)
        {
            var (success, token) = await _authService.RefreshTokenAsync(refresh_token);
            if (!success)
                return BadRequest(new { error_code = 701, error_msg = "Authentication failed" });

            return Ok(new
            {
                access_token = token,
                expires_in = 1200,
                token_type = "bearer",
                scope = scope,
                refresh_token = token
            });
        }

        return BadRequest(new { error_code = 701, error_msg = "Authentication failed" });
    }
}
