using Registration.Api.Features.ConfirmEmail;
using Registration.Api.Features.GetRegistration;
using Registration.Api.Features.RegisterUser;
using Registration.Application;
using Registration.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddRegistrationApplication(builder.Configuration);
builder.Services.AddRegistrationInfrastructure(builder.Configuration);

var app = builder.Build();

await app.EnsureRegistrationDatabaseAsync();

app.MapOpenApi();
app.MapRegisterUser();
app.MapConfirmEmail();
app.MapGetRegistration();

app.Run();
