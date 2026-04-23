using Microsoft.EntityFrameworkCore;
using EOS.Data;
using EOS.Services;
using EOS.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Configure listening URLs from appsettings
var host = builder.Configuration["Server:Host"] ?? "0.0.0.0";
var port = builder.Configuration["Server:Port"] ?? "5000";
builder.WebHost.UseUrls($"http://{host}:{port}");

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "EOS API",
        Version = "v1",
        Description = "Authentication and user service endpoints for EOS game platform",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "EOS Support"
        }
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (System.IO.File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

// DB context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
);

// Custom services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<TokenService>();

// Background health check service (keeps database connection alive)
builder.Services.AddHostedService<DatabaseHealthCheckService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// Test DB connection
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        // Apply any pending migrations automatically
        await context.Database.MigrateAsync();
        
        if (await context.Database.CanConnectAsync())
        {
            // Extract database name from connection string
            var dbName = connectionString?.Split("Database=")[1]?.Split(";")[0] ?? "unknown";
            var userCount = await context.Users.CountAsync();
            
            Console.WriteLine("\n✓ DATABASE CONNECTED SUCCESSFULLY");
            Console.WriteLine($"  Database: {dbName}");
            Console.WriteLine($"  Users: {userCount} accounts\n");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\n✗ DATABASE CONNECTION FAILED: {ex.Message}\n");
    }
}

// Middleware
app.UseMiddleware<ResponseLoggingMiddleware>();
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "EOS API v1");
    options.RoutePrefix = "swagger";
});

app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();
