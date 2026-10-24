using CapitecDisputes.Application.Abstractions;
using CapitecDisputes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CapitecDisputes.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDisputesPersistence(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<DisputesDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure()));

        services.AddScoped<IDisputeRepository, DisputeRepository>();

        return services;
    }

    public static async Task ApplyDisputesMigrationsAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();

        await scope.ServiceProvider
            .GetRequiredService<DisputesDbContext>()
            .Database
            .MigrateAsync(cancellationToken);
    }
}
