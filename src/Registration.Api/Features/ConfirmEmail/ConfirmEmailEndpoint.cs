using Registration.Application.Features.ConfirmEmail;

namespace Registration.Api.Features.ConfirmEmail;

public sealed record ConfirmEmailRequest(string Code);

public static class ConfirmEmailEndpoint
{
    public static IEndpointRouteBuilder MapConfirmEmail(this IEndpointRouteBuilder app)
    {
        app.MapPost("/registrations/{userId:guid}/confirm", async (Guid userId, ConfirmEmailRequest request, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new ConfirmEmailCommand(userId, request.Code), cancellationToken);
            return result.ToMinimalApiResult();
        })
        .WithName("ConfirmEmail")
        .WithSummary("Submits the verification code. 200 accepted for the saga to check, 400 malformed code, 404 unknown user, 409 already active.");

        return app;
    }
}
