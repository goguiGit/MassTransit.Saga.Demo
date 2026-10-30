namespace Registration.Application.Features.ConfirmEmail;

public sealed record ConfirmEmailCommand(Guid UserId, string Code) : IRequest;
