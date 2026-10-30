using Registration.Application.Abstractions;

namespace Registration.Application.Features.GetRegistration;

public sealed class GetRegistrationQueryHandler(IUserRepository users, IRegistrationReader registrations)
    : IRequestHandler<GetRegistrationQuery, Result<RegistrationStatus>>
{
    public async Task<Result<RegistrationStatus>> Handle(GetRegistrationQuery request, CancellationToken cancellationToken)
    {
        var user = await users.FindAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Result.NotFound();
        }

        var saga = await registrations.FindAsync(request.UserId, cancellationToken);
        return new RegistrationStatus(user.Id, user.Email, user.Status, saga?.CurrentState, user.CreatedAt, user.ActivatedAt);
    }
}
