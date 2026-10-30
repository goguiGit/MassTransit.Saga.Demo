using Registration.Application.Abstractions;

namespace Registration.Application.Features.ConfirmEmail;

public sealed class ConfirmEmailCommandHandler(IUserRepository users, IPublishEndpoint publishEndpoint)
    : IRequestHandler<ConfirmEmailCommand, Result>
{
    public async Task<Result> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await users.FindAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Result.NotFound();
        }

        if (user.Status == UserStatus.Active)
        {
            return Result.Conflict("The account is already active.");
        }

        // Whether the code is right is the saga's call: it holds the code and the deadline.
        await publishEndpoint.Publish(new EmailConfirmed(request.UserId, request.Code), cancellationToken);
        return Result.Success();
    }
}
