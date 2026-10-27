using System;
using System.Collections.Generic;
using System.IO;
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
        IConfiguration configuration,
        string contentRootPath)
    {
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));

        services.Configure<SeedOptions>(options =>
        {
            var configuredPath = configuration["Seed:UsersFilePath"];

            options.UsersFilePath = Path.GetFullPath(
                string.IsNullOrWhiteSpace(configuredPath)
                    ? SeedOptions.DefaultUsersFileName
                    : configuredPath,
                contentRootPath);
        });

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

        var path = scope.ServiceProvider.GetRequiredService<IOptions<SeedOptions>>().Value.UsersFilePath;
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<UserSeeder>>();

        if (!File.Exists(path))
        {
            logger.LogWarning(
                "No seed user file at {Path}, so no portal accounts were created. "
                + "Copy seed-users.example.csv to that path to sign in.",
                path);

            return;
        }

        var source = Path.GetFileName(path);

        List<SeedUser> seedUsers;

        using (var reader = File.OpenText(path))
        {
            seedUsers = [.. SeedUserCsvReader.Read(reader, source)];
        }

        var seeder = new UserSeeder(
            scope.ServiceProvider.GetRequiredService<DisputesDbContext>(),
            scope.ServiceProvider.GetRequiredService<IPasswordHasher>(),
            scope.ServiceProvider.GetRequiredService<TimeProvider>(),
            logger);

        await seeder.SeedAsync(seedUsers, source, cancellationToken);
    }
}
