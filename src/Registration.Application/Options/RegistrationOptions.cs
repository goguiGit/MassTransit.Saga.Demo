namespace Registration.Application.Options;

public sealed class RegistrationOptions
{
    public const string SectionName = "Registration";

    public TimeSpan ConfirmationWindow { get; set; } = TimeSpan.FromHours(24);
}
