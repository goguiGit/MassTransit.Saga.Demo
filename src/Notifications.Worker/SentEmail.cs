namespace Notifications.Worker;

public sealed record SentEmail(string To, string Subject, string Body, DateTime SentAt);
