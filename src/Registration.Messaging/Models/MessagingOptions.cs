using Registration.Messaging.Enums;

namespace Registration.Messaging.Models;

public sealed class MessagingOptions
{
    public const string SectionName = "Messaging";

    public MessagingTransport Transport { get; set; } = MessagingTransport.Sql;
    public RabbitMqOptions RabbitMq { get; set; } = new();
}
