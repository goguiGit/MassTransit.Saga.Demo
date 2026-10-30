using Registration.Application.Abstractions;

namespace Registration.Application.Consumers;

public sealed class UserActivatedConsumer(IUserRepository users) : IConsumer<UserActivated>
{
    public async Task Consume(ConsumeContext<UserActivated> context)
    {
        var user = await users.FindAsync(context.Message.UserId, context.CancellationToken);
        if (user is null || user.Status == UserStatus.Active)
        {
            return;
        }

        user.Activate(DateTime.UtcNow);
        await users.UpdateAsync(user, context.CancellationToken);
    }
}
