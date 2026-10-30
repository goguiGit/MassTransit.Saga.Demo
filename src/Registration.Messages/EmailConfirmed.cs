namespace Registration.Messages;

public sealed record EmailConfirmed(Guid UserId, string Code);
