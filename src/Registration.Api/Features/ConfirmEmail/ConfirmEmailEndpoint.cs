using Registration.Application.Features.ConfirmEmail;

namespace Registration.Api.Features.ConfirmEmail;

public sealed record ConfirmEmailRequest(string Code);

public static class ConfirmEmailEndpoint
{
    public static IEndpointRouteBuilder MapConfirmEmail(this IEndpointRouteBuilder app)
    {
        app.MapPost("/registrations/{userId:guid}/confirm", async (Guid userId, ConfirmEmailRequest request, IMediator mediator, CancellationToken cancellationToken) =>
        {
            await mediator.Send(new ConfirmEmailCommand(userId, request.Code), cancellationToken);
            return Results.Accepted($"/registrations/{userId}");
        })
        .WithName("ConfirmEmail")
        .WithSummary("Submits the verification code received by email.");

        return app;
    }
}
