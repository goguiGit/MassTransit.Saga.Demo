namespace Registration.Application.Features.RegisterUser;

public sealed record RegisterUserCommand(string Email) : IRequest<Guid>;
