using System.Collections.Concurrent;

namespace Notifications.Worker;

public sealed class SentEmails
{
    private readonly ConcurrentQueue<SentEmail> _emails = new();

    public IReadOnlyList<SentEmail> All => [.. _emails];

    public void Add(SentEmail email) => _emails.Enqueue(email);
}
