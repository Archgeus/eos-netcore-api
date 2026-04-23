// Run: dotnet ef migrations add InitialCreate
// Then: dotnet ef database update

using System.Diagnostics;
using EOS.Data;
using Microsoft.EntityFrameworkCore;

namespace EOS;

public static class DbInitializer
{
    public static void Initialize(AppDbContext context)
    {
        // Apply pending migrations
        try
        {
            context.Database.Migrate();
            Debug.WriteLine("Database migrated successfully");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Database migration error: {ex.Message}");
        }
    }
}
