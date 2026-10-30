using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Registration.Application.Abstractions;
using Registration.Application.Consumers;
using Registration.Application.Saga;
using Registration.Infrastructure.Persistence;
using Registration.Messaging;

namespace Registration.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "Registration";

    public static IServiceCollection AddRegistrationInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"ConnectionStrings:{ConnectionStringName} is required.");

        services.AddDbContext<RegistrationDbContext>(x => x.UseSqlServer(connectionString));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRegistrationReader, RegistrationReader>();

        services.AddMessaging(configuration, connectionString, x =>
        {
            x.AddConsumer<UserActivatedConsumer>();
            x.AddConsumer<RegistrationDiscardedConsumer>();
            x.AddSagaStateMachine<RegistrationStateMachine, RegistrationState>()
                .EntityFrameworkRepository(r =>
                {
                    r.ExistingDbContext<RegistrationDbContext>();
                    r.UseSqlServer();
                });
        });

        return services;
    }

    // Demo: create the tables on startup instead of shipping EF migrations.
    public static async Task EnsureRegistrationDatabaseAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<RegistrationDbContext>().Database.EnsureCreatedAsync();
    }
}
