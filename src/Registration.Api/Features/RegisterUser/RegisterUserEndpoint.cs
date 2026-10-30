using Registration.Application.Features.RegisterUser;

namespace Registration.Api.Features.RegisterUser;

public static class RegisterUserEndpoint
{
    public static IEndpointRouteBuilder MapRegisterUser(this IEndpointRouteBuilder app)
    {
        app.MapPost("/registrations", async (RegisterUserCommand command, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var userId = await mediator.Send(command, cancellationToken);
            return Results.Accepted($"/registrations/{userId}", new { userId });
        })
        .WithName("RegisterUser")
        .WithSummary("Registers a user and starts the confirmation saga.");

        return app;
    }
}
