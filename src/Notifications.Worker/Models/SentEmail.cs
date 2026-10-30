namespace Notifications.Worker.Models;

public sealed record SentEmail(string To, string Subject, string Body, DateTime SentAt);
