using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Registration.Messaging.Enums;
using Registration.Messaging.Options;

namespace Registration.Messaging;

public static class MessagingExtensions
{
    public static IServiceCollection AddMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        string? sqlConnectionString,
        Action<IBusRegistrationConfigurator> configure)
    {
        var options = configuration.GetSection(MessagingOptions.SectionName).Get<MessagingOptions>() ?? new MessagingOptions();

        if (options.Transport == MessagingTransport.Sql)
        {
            AddSqlTransport(services, sqlConnectionString
                ?? throw new InvalidOperationException("The SQL transport needs ConnectionStrings:Registration."));
        }

        return services.AddMassTransit(x =>
        {
            configure(x);

            switch (options.Transport)
            {
                case MessagingTransport.Sql:
                    x.AddSqlMessageScheduler();
                    x.UsingSqlServer((context, cfg) =>
                    {
                        cfg.UseSqlMessageScheduler();
                        cfg.ConfigureEndpoints(context);
                    });
                    break;
                case MessagingTransport.RabbitMq:
                    x.AddDelayedMessageScheduler();
                    x.UsingRabbitMq((context, cfg) =>
                    {
                        cfg.Host(options.RabbitMq.Host, options.RabbitMq.VirtualHost, host =>
                        {
                            host.Username(options.RabbitMq.Username);
                            host.Password(options.RabbitMq.Password);
                        });
                        cfg.UseDelayedMessageScheduler();
                        cfg.ConfigureEndpoints(context);
                    });
                    break;
                case MessagingTransport.AzureServiceBus:
                    var connectionString = options.AzureServiceBus.ConnectionString;
                    if (string.IsNullOrWhiteSpace(connectionString))
                    {
                        throw new InvalidOperationException(
                            "Messaging:AzureServiceBus:ConnectionString is required. Set it with user-secrets or the Messaging__AzureServiceBus__ConnectionString variable.");
                    }

                    x.AddServiceBusMessageScheduler();
                    x.UsingAzureServiceBus((context, cfg) =>
                    {
                        cfg.Host(connectionString);
                        cfg.UseServiceBusMessageScheduler();
                        cfg.ConfigureEndpoints(context);
                    });
                    break;
                default:
                    throw new InvalidOperationException($"Unknown transport {options.Transport}.");
            }
        });
    }

    private static void AddSqlTransport(IServiceCollection services, string connectionString)
    {
        services.AddOptions<SqlTransportOptions>().Configure(options =>
        {
            options.ConnectionString = connectionString;
            options.Schema = "transport";
            options.Role = "transport";
        });
        services.AddSqlServerMigrationHostedService(options =>
        {
            options.CreateDatabase = false;
            options.CreateSchema = true;
            options.CreateInfrastructure = true;
        });
    }
}
