using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Registration.Messaging.Enums;
using Registration.Messaging.Models;

namespace Registration.Messaging;

// Shared by every service so they agree on transport, schema, role and scheduler; a mismatch means
// two services that cannot see each other's queues.
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
                    // Needs the rabbitmq_delayed_message_exchange plugin (present in the masstransit/rabbitmq image).
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
                    // Standard or Premium namespace (Basic has no topics) and a connection string with Manage rights:
                    // MassTransit creates the topics, queues and forwarding subscriptions on startup.
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
        // CreateDatabase is off on purpose: that step always runs CREATE LOGIN with Username/Password, which has no
        // integrated-security path. Registration.Api creates the database with EF first; the schema step resolves
        // the current Windows login with ORIGINAL_LOGIN() and grants it the transport role.
        services.AddSqlServerMigrationHostedService(options =>
        {
            options.CreateDatabase = false;
            options.CreateSchema = true;
            options.CreateInfrastructure = true;
        });
    }
}
