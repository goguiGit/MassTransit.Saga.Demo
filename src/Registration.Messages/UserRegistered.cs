namespace Registration.Messages;

public sealed record UserRegistered(Guid UserId, string Email);
