namespace Registration.Messages;

public sealed record SendVerificationEmail(Guid UserId, string Email, string Code);
