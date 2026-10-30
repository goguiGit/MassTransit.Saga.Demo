namespace Registration.Messaging.Models;

public sealed class AzureServiceBusOptions
{
    // Secret: comes from user-secrets or the Messaging__AzureServiceBus__ConnectionString variable, never from appsettings.
    public string? ConnectionString { get; set; }
}
