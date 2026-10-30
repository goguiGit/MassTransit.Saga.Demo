namespace Registration.Application.Features.GetRegistration;

public sealed record GetRegistrationQuery(Guid UserId) : IRequest<Result<RegistrationStatus>>;
