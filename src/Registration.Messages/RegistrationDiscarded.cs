namespace Registration.Messages;

public sealed record RegistrationDiscarded(Guid UserId, string Email);
