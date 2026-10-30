namespace Registration.Messages;

public sealed record SendWelcomeEmail(Guid UserId, string Email);
