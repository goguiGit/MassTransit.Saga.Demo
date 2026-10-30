using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Registration.Application.Abstractions;
using Registration.Application.Consumers;
using Registration.Application.Saga;
using Registration.Infrastructure.Persistence;

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

        services.AddOptions<SqlTransportOptions>().Configure(options =>
        {
            options.ConnectionString = connectionString;
            options.Schema = "transport";
            options.Role = "transport";
        });
        // CreateDatabase is off on purpose: that step always runs CREATE LOGIN with Username/Password, which has no
        // integrated-security path. EnsureRegistrationDatabaseAsync creates the database first; the schema step
        // resolves the current Windows login with ORIGINAL_LOGIN() and grants it the transport role.
        services.AddSqlServerMigrationHostedService(options =>
        {
            options.CreateDatabase = false;
            options.CreateSchema = true;
            options.CreateInfrastructure = true;
        });

        services.AddMassTransit(x =>
        {
            x.AddSqlMessageScheduler();
            x.AddConsumer<UserActivatedConsumer>();
            x.AddConsumer<RegistrationDiscardedConsumer>();
            x.AddSagaStateMachine<RegistrationStateMachine, RegistrationState>()
                .EntityFrameworkRepository(r =>
                {
                    r.ExistingDbContext<RegistrationDbContext>();
                    r.UseSqlServer();
                });
            x.UsingSqlServer((context, cfg) =>
            {
                cfg.UseSqlMessageScheduler();
                cfg.ConfigureEndpoints(context);
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
