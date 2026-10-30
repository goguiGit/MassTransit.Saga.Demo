namespace Notifications.Worker;

public sealed class EmailSender(SentEmails sentEmails, ILogger<EmailSender> logger)
    : IConsumer<SendVerificationEmail>, IConsumer<SendWelcomeEmail>
{
    public Task Consume(ConsumeContext<SendVerificationEmail> context) =>
        Send(context.Message.Email, "Confirma tu email", $"Tu código es {context.Message.Code}");

    public Task Consume(ConsumeContext<SendWelcomeEmail> context) =>
        Send(context.Message.Email, "Bienvenida", "Tu cuenta ya está activa");

    private Task Send(string to, string subject, string body)
    {
        sentEmails.Add(new SentEmail(to, subject, body, DateTime.UtcNow));
        logger.LogInformation("Email to {To}: {Subject} - {Body}", to, subject, body);
        return Task.CompletedTask;
    }
}
