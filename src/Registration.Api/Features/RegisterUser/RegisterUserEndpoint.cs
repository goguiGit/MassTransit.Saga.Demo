using Registration.Application.Features.RegisterUser;

namespace Registration.Api.Features.RegisterUser;

public static class RegisterUserEndpoint
{
    public static IEndpointRouteBuilder MapRegisterUser(this IEndpointRouteBuilder app)
    {
        app.MapPost("/registrations", async (RegisterUserCommand command, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(command, cancellationToken);
            return result.ToMinimalApiResult();
        })
        .WithName("RegisterUser")
        .WithSummary("Registers a user and starts the confirmation saga. 201 with the user id, 400 if the email is invalid.");

        return app;
    }
}
