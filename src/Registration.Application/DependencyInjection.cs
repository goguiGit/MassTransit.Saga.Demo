using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Registration.Application.Saga;

namespace Registration.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddRegistrationApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(x => x.RegisterServicesFromAssemblyContaining<RegistrationStateMachine>());
        services.Configure<RegistrationOptions>(configuration.GetSection(RegistrationOptions.SectionName));
        return services;
    }
}
