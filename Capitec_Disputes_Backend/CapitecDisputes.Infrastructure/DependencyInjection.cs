using System;
using System.Threading;
using System.Threading.Tasks;
using CapitecDisputes.Application.Abstractions;
using CapitecDisputes.Infrastructure.Persistence;
using CapitecDisputes.Infrastructure.Persistence.Seeding;
using CapitecDisputes.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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

    public static IServiceCollection AddDisputesSecurity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.Configure<SeedUsersOptions>(configuration.GetSection("Seed"));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();

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

    public static async Task SeedConfiguredUsersAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();

        var seeder = new UserSeeder(
            scope.ServiceProvider.GetRequiredService<DisputesDbContext>(),
            scope.ServiceProvider.GetRequiredService<IPasswordHasher>(),
            scope.ServiceProvider.GetRequiredService<TimeProvider>(),
            scope.ServiceProvider.GetRequiredService<ILogger<UserSeeder>>());

        var seedUsers = scope.ServiceProvider.GetRequiredService<IOptions<SeedUsersOptions>>().Value.Users;

        await seeder.SeedAsync([.. seedUsers], cancellationToken);
    }
}
