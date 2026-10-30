namespace Registration.Domain.Models;

public sealed class User
{
    private User(Guid id, string email, DateTime createdAt)
    {
        Id = id;
        Email = email;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public string Email { get; private set; }
    public UserStatus Status { get; private set; } = UserStatus.PendingConfirmation;
    public DateTime CreatedAt { get; private set; }
    public DateTime? ActivatedAt { get; private set; }

    public static User Register(Guid id, string email, DateTime now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        return new User(id, email, now);
    }

    public void Activate(DateTime now)
    {
        if (Status == UserStatus.Active)
        {
            throw new InvalidOperationException($"User {Id} is already active.");
        }

        Status = UserStatus.Active;
        ActivatedAt = now;
    }
}
