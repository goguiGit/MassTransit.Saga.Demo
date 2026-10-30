using Registration.Messaging.Enums;

namespace Registration.Messaging.Options;

public sealed class MessagingOptions
{
    public const string SectionName = "Messaging";

    public MessagingTransport Transport { get; set; } = MessagingTransport.Sql;
    public RabbitMqOptions RabbitMq { get; set; } = new();
    public AzureServiceBusOptions AzureServiceBus { get; set; } = new();
}
