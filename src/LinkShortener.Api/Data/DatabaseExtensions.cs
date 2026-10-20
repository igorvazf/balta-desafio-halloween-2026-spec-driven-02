using LinkShortener.Api.Application;
using Microsoft.EntityFrameworkCore;

namespace LinkShortener.Api.Data;

public static class DatabaseExtensions
{
    public const string ConnectionStringName = "Default";

    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"A connection string '{ConnectionStringName}' não foi configurada.");

        services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<ILinkRepository, LinkRepository>();

        return services;
    }

    public static void ApplyMigrations(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
    }
}
