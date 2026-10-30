using Notifications.Worker;
using Registration.Messaging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton<SentEmails>();

builder.Services.AddMessaging(
    builder.Configuration,
    builder.Configuration.GetConnectionString("Registration"),
    x => x.AddConsumer<EmailSender>());

var app = builder.Build();

app.MapOpenApi();

app.MapGet("/emails", (SentEmails sentEmails) => sentEmails.All);

app.Run();
