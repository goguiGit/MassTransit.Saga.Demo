using Notifications.Worker;
using Registration.Messaging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton<SentEmails>();

// Same transport settings as Registration.Api (shared Registration.Messaging). With the SQL transport the
// database itself is created by Registration.Api, so start that one first.
builder.Services.AddMessaging(
    builder.Configuration,
    builder.Configuration.GetConnectionString("Registration"),
    x => x.AddConsumer<EmailSender>());

var app = builder.Build();

app.MapOpenApi();

app.MapGet("/emails", (SentEmails sentEmails) => sentEmails.All);

app.Run();
