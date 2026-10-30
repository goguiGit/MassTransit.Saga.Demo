using Registration.Application.Abstractions;

namespace Registration.Application.Features.GetRegistration;

public sealed class GetRegistrationHandler(IUserRepository users, IRegistrationReader registrations)
    : IRequestHandler<GetRegistrationQuery, RegistrationStatus?>
{
    public async Task<RegistrationStatus?> Handle(GetRegistrationQuery request, CancellationToken cancellationToken)
    {
        var user = await users.FindAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var saga = await registrations.FindAsync(request.UserId, cancellationToken);
        return new RegistrationStatus(user.Id, user.Email, user.Status, saga?.CurrentState, user.CreatedAt, user.ActivatedAt);
    }
}
