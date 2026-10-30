using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Registration.Application.Behaviors;
using Registration.Application.Saga;

namespace Registration.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddRegistrationApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(x =>
        {
            x.RegisterServicesFromAssemblyContaining<RegistrationStateMachine>();
            x.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        services.AddValidatorsFromAssemblyContaining<RegistrationStateMachine>(includeInternalTypes: true);
        services.Configure<RegistrationOptions>(configuration.GetSection(RegistrationOptions.SectionName));
        return services;
    }
}
