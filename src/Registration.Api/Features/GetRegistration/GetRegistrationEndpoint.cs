using Registration.Application.Features.GetRegistration;

namespace Registration.Api.Features.GetRegistration;

public static class GetRegistrationEndpoint
{
    public static IEndpointRouteBuilder MapGetRegistration(this IEndpointRouteBuilder app)
    {
        app.MapGet("/registrations/{userId:guid}", async (Guid userId, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var status = await mediator.Send(new GetRegistrationQuery(userId), cancellationToken);
            return status is null ? Results.NotFound() : Results.Ok(status);
        })
        .WithName("GetRegistration")
        .WithSummary("Returns the saga state of a registration, or 404 once it finished.");

        return app;
    }
}
