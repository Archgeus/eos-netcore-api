using EOS.Data;
using Microsoft.EntityFrameworkCore;

namespace EOS.Services;

/// <summary>
/// Background service that periodically pings the database to keep connection alive.
/// Prevents idle connection timeouts.
/// </summary>
public class DatabaseHealthCheckService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseHealthCheckService> _logger;
    private readonly int _intervalSeconds = 300; // Ping every 5 minutes

    public DatabaseHealthCheckService(IServiceProvider serviceProvider, ILogger<DatabaseHealthCheckService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Database Health Check Service started. Pinging every {IntervalSeconds} seconds.", _intervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    
                    // Simple query to keep connection alive
                    await context.Database.ExecuteSqlRawAsync("SELECT 1", stoppingToken);
                    
                    _logger.LogDebug("Database ping successful at {Time}", DateTime.UtcNow);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Database ping failed: {Message}", ex.Message);
            }

            // Wait before next ping
            await Task.Delay(TimeSpan.FromSeconds(_intervalSeconds), stoppingToken);
        }

        _logger.LogInformation("Database Health Check Service stopped.");
    }
}
