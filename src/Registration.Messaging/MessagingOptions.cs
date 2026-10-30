namespace Registration.Messaging;

public enum MessagingTransport
{
    Sql,
    RabbitMq
}

public sealed class RabbitMqOptions
{
    public string Host { get; set; } = "localhost";
    public string VirtualHost { get; set; } = "/";
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
}

public sealed class MessagingOptions
{
    public const string SectionName = "Messaging";

    public MessagingTransport Transport { get; set; } = MessagingTransport.Sql;
    public RabbitMqOptions RabbitMq { get; set; } = new();
}
