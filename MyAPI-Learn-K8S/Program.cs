using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MyAPI_Learn_K8S;
using MyAPI_Learn_K8S.Models;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var redisConnectionString = builder.Configuration["Redis:ConnectionString"];

builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisConnectionString!)
);

builder.Services.AddDbContext<ProductDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapGet(
    "/instance",
    () =>
    {
        return Results.Ok(new { Instance = Environment.MachineName, Time = DateTime.UtcNow });
    }
);

app.MapGet(
    "/redis-test",
    async (IConnectionMultiplexer redis) =>
    {
        try
        {
            var db = redis.GetDatabase();

            await db.StringSetAsync("message", "Hello from Redis inside Docker Compose!");

            var value = await db.StringGetAsync("message");

            return Results.Ok(new { Message = value.ToString() });
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Redis is unavailable",
                detail: ex.Message,
                statusCode: 503
            );
        }
    }
);

// Initialize database asynchronously
// Only run on the first instance (use a lock or check if DB exists)
Task.Run(async () =>
{
    try
    {
        await DBService.CreateDbAsync(app.Configuration);
        Console.WriteLine("Database initialized successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to initialize database: {ex.Message}");
    }
});

app.Run();
