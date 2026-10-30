using Microsoft.Extensions.Options;
using Registration.Application.Abstractions;
using Registration.Application.Features.GetRegistration.Models;

namespace Registration.Application.Features.GetRegistration;

public sealed class GetRegistrationQueryHandler(IUserRepository users, IRegistrationReader registrations, IOptions<RegistrationOptions> options)
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
        var expiresAt = saga is null ? (DateTime?)null : saga.RegisteredAt + options.Value.ConfirmationWindow;
        return new RegistrationStatus(user.Id, user.Email, user.Status, saga?.CurrentState, user.CreatedAt, user.ActivatedAt, expiresAt);
    }
}
