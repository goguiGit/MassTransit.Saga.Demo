using Registration.Application.Abstractions;

namespace Registration.Application.Features.RegisterUser;

public sealed class RegisterUserHandler(IUserRepository users, IPublishEndpoint publishEndpoint) : IRequestHandler<RegisterUserCommand, Guid>
{
    public async Task<Guid> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var user = User.Register(NewId.NextGuid(), request.Email, DateTime.UtcNow);
        await users.AddAsync(user, cancellationToken);
        await publishEndpoint.Publish(new UserRegistered(user.Id, user.Email), cancellationToken);
        return user.Id;
    }
}
