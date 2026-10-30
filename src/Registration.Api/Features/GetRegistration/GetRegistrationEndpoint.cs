using Registration.Application.Features.GetRegistration;

namespace Registration.Api.Features.GetRegistration;

public static class GetRegistrationEndpoint
{
    public static IEndpointRouteBuilder MapGetRegistration(this IEndpointRouteBuilder app)
    {
        app.MapGet("/registrations/{userId:guid}", async (Guid userId, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetRegistrationQuery(userId), cancellationToken);
            return result.ToMinimalApiResult();
        })
        .WithName("GetRegistration")
        .WithSummary("Returns the user and, while it runs, the saga state. 404 for unknown or discarded users.");

        return app;
    }
}
