using Registration.Application.Abstractions;

namespace Registration.Application.Consumers;

public sealed class RegistrationDiscardedConsumer(IUserRepository users) : IConsumer<RegistrationDiscarded>
{
    public async Task Consume(ConsumeContext<RegistrationDiscarded> context)
    {
        var user = await users.FindAsync(context.Message.UserId, context.CancellationToken);
        if (user is null)
        {
            return;
        }

        await users.RemoveAsync(user, context.CancellationToken);
    }
}
