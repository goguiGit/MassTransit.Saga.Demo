using Notifications.Worker;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Registration")
    ?? throw new InvalidOperationException("ConnectionStrings:Registration is required.");

builder.Services.AddOpenApi();
builder.Services.AddSingleton<SentEmails>();

// Same transport database, schema and role as Registration.Api: the queues are shared tables. The database
// itself is created by Registration.Api, so start that one first.
builder.Services.AddOptions<SqlTransportOptions>().Configure(options =>
{
    options.ConnectionString = connectionString;
    options.Schema = "transport";
    options.Role = "transport";
});
builder.Services.AddSqlServerMigrationHostedService(options =>
{
    options.CreateDatabase = false;
    options.CreateSchema = true;
    options.CreateInfrastructure = true;
});

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<EmailSender>();
    x.UsingSqlServer((context, cfg) => cfg.ConfigureEndpoints(context));
});

var app = builder.Build();

app.MapOpenApi();

app.MapGet("/emails", (SentEmails sentEmails) => sentEmails.All);

app.Run();
