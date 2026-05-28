using Microsoft.EntityFrameworkCore;

namespace AadharLocation.Api.Data;

public class DatabaseSeeder(IServiceScopeFactory scopeFactory, ILogger<DatabaseSeeder> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (await db.Users.AnyAsync(cancellationToken))
            return;

        db.Users.Add(new Domain.Entities.User
        {
            Email = "admin@aadhar.local",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            Name = "System Admin",
            Role = "Admin"
        });

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Default admin user seeded");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
