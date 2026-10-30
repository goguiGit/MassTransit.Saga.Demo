namespace Registration.Domain.Tests;

[TestFixture]
public sealed class UserTests
{
    private static readonly DateTime Now = new(2026, 10, 30, 10, 0, 0, DateTimeKind.Utc);

    [Test]
    public void User_Register_StartsPendingConfirmation()
    {
        var user = User.Register(Guid.NewGuid(), "ana@example.com", Now);

        Assert.That(user.Status, Is.EqualTo(UserStatus.PendingConfirmation));
    }

    [Test]
    public void User_Register_KeepsEmailAndCreationTime()
    {
        var id = Guid.NewGuid();

        var user = User.Register(id, "ana@example.com", Now);

        Assert.That((user.Id, user.Email, user.CreatedAt), Is.EqualTo((id, "ana@example.com", Now)));
    }

    [Test]
    public void User_Activate_BecomesActive()
    {
        var user = User.Register(Guid.NewGuid(), "ana@example.com", Now);

        user.Activate(Now.AddHours(1));

        Assert.That((user.Status, user.ActivatedAt), Is.EqualTo((UserStatus.Active, (DateTime?)Now.AddHours(1))));
    }

    [Test]
    public void User_ActivateTwice_Throws()
    {
        var user = User.Register(Guid.NewGuid(), "ana@example.com", Now);
        user.Activate(Now);

        Assert.That(() => user.Activate(Now), Throws.InvalidOperationException);
    }
}
