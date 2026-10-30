namespace Registration.Application.Features.ConfirmEmail;

public sealed class ConfirmEmailHandler(IPublishEndpoint publishEndpoint) : IRequestHandler<ConfirmEmailCommand>
{
    public Task Handle(ConfirmEmailCommand request, CancellationToken cancellationToken) =>
        publishEndpoint.Publish(new EmailConfirmed(request.UserId, request.Code), cancellationToken);
}
