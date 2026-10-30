using Microsoft.Extensions.Options;

namespace Registration.Application.Saga;

public sealed class RegistrationStateMachine : MassTransitStateMachine<RegistrationState>
{
    public RegistrationStateMachine(IOptions<RegistrationOptions> options)
    {
        InstanceState(x => x.CurrentState);

        Event(() => UserRegistered, x => x.CorrelateById(context => context.Message.UserId));
        Event(() => EmailConfirmed, x =>
        {
            x.CorrelateById(context => context.Message.UserId);
            x.OnMissingInstance(m => m.Discard());
        });
        Schedule(() => Expiration, x => x.ExpirationTokenId, x =>
        {
            x.Delay = options.Value.ConfirmationWindow;
            x.Received = r => r.CorrelateById(context => context.Message.UserId);
        });

        Initially(
            When(UserRegistered)
                .Then(context => context.Saga.Start(context.Message.Email, DateTime.UtcNow))
                .Schedule(Expiration, context => new RegistrationExpired(context.Saga.CorrelationId))
                .Publish(context => new SendVerificationEmail(context.Saga.CorrelationId, context.Saga.Email, context.Saga.VerificationCode))
                .TransitionTo(AwaitingConfirmation));

        During(AwaitingConfirmation,
            When(EmailConfirmed, context => context.Saga.Accepts(context.Message.Code))
                .Unschedule(Expiration)
                .Publish(context => new UserActivated(context.Saga.CorrelationId))
                .Publish(context => new SendWelcomeEmail(context.Saga.CorrelationId, context.Saga.Email))
                .Finalize(),
            Ignore(EmailConfirmed, context => !context.Saga.Accepts(context.Message.Code)),
            When(Expiration.Received)
                .Publish(context => new RegistrationDiscarded(context.Saga.CorrelationId, context.Saga.Email))
                .Finalize());

        SetCompletedWhenFinalized();
    }

    public State AwaitingConfirmation { get; private set; } = null!;
    public Event<UserRegistered> UserRegistered { get; private set; } = null!;
    public Event<EmailConfirmed> EmailConfirmed { get; private set; } = null!;
    public Schedule<RegistrationState, RegistrationExpired> Expiration { get; private set; } = null!;
}
