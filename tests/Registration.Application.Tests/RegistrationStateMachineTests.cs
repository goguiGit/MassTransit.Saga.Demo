using Microsoft.Extensions.Options;
using Registration.Application.Saga;

namespace Registration.Application.Tests;

[TestFixture]
public sealed class RegistrationStateMachineTests
{
    private static readonly TimeSpan ConfirmationWindow = TimeSpan.FromSeconds(2);

    [Test]
    public async Task RegistrationStateMachine_UserRegistered_SendsVerificationEmail()
    {
        await using var provider = await StartBusAsync();
        var harness = provider.GetRequiredService<ITestHarness>();
        var userId = NewId.NextGuid();

        await harness.Bus.Publish(new UserRegistered(userId, "ana@example.com"));

        var sent = await harness.Published.Any<SendVerificationEmail>(x => x.Context.Message.Email == "ana@example.com");
        Assert.That(sent, Is.True);
    }

    [Test]
    public async Task RegistrationStateMachine_UserRegistered_AwaitsConfirmation()
    {
        await using var provider = await StartBusAsync();
        var harness = provider.GetRequiredService<ITestHarness>();
        var userId = NewId.NextGuid();

        await harness.Bus.Publish(new UserRegistered(userId, "ana@example.com"));

        var saga = harness.GetSagaStateMachineHarness<RegistrationStateMachine, RegistrationState>();
        Assert.That(await saga.Exists(userId, x => x.AwaitingConfirmation), Is.EqualTo(userId));
    }

    [Test]
    public async Task RegistrationStateMachine_CorrectCode_SendsWelcomeEmail()
    {
        await using var provider = await StartBusAsync();
        var harness = provider.GetRequiredService<ITestHarness>();
        var userId = NewId.NextGuid();
        var code = await RegisterAsync(harness, userId);

        await harness.Bus.Publish(new EmailConfirmed(userId, code));

        var sent = await harness.Published.Any<SendWelcomeEmail>(x => x.Context.Message.UserId == userId);
        Assert.That(sent, Is.True);
    }

    [Test]
    public async Task RegistrationStateMachine_CorrectCode_PublishesUserActivated()
    {
        await using var provider = await StartBusAsync();
        var harness = provider.GetRequiredService<ITestHarness>();
        var userId = NewId.NextGuid();
        var code = await RegisterAsync(harness, userId);

        await harness.Bus.Publish(new EmailConfirmed(userId, code));

        var activated = await harness.Published.Any<UserActivated>(x => x.Context.Message.UserId == userId);
        Assert.That(activated, Is.True);
    }

    [Test]
    public async Task RegistrationStateMachine_CorrectCode_CompletesSaga()
    {
        await using var provider = await StartBusAsync();
        var harness = provider.GetRequiredService<ITestHarness>();
        var userId = NewId.NextGuid();
        var code = await RegisterAsync(harness, userId);

        await harness.Bus.Publish(new EmailConfirmed(userId, code));
        await harness.Published.Any<SendWelcomeEmail>();

        var saga = harness.GetSagaStateMachineHarness<RegistrationStateMachine, RegistrationState>();
        Assert.That(await saga.NotExists(userId), Is.Null);
    }

    [Test]
    public async Task RegistrationStateMachine_CorrectCode_CancelsExpiration()
    {
        await using var provider = await StartBusAsync();
        var harness = provider.GetRequiredService<ITestHarness>();
        var userId = NewId.NextGuid();
        var code = await RegisterAsync(harness, userId);

        await harness.Bus.Publish(new EmailConfirmed(userId, code));
        await harness.Published.Any<SendWelcomeEmail>();
        await Task.Delay(ConfirmationWindow * 2);

        Assert.That(harness.Published.Select<RegistrationDiscarded>().Any(), Is.False);
    }

    [Test]
    public async Task RegistrationStateMachine_WrongCode_KeepsAwaitingConfirmation()
    {
        await using var provider = await StartBusAsync();
        var harness = provider.GetRequiredService<ITestHarness>();
        var userId = NewId.NextGuid();
        var code = await RegisterAsync(harness, userId);
        var saga = harness.GetSagaStateMachineHarness<RegistrationStateMachine, RegistrationState>();

        await harness.Bus.Publish(new EmailConfirmed(userId, "not-" + code));
        await harness.Consumed.Any<EmailConfirmed>();

        Assert.That(await saga.Exists(userId, x => x.AwaitingConfirmation), Is.EqualTo(userId));
    }

    [Test]
    public async Task RegistrationStateMachine_WrongCode_DoesNotSendWelcomeEmail()
    {
        await using var provider = await StartBusAsync();
        var harness = provider.GetRequiredService<ITestHarness>();
        var userId = NewId.NextGuid();
        var code = await RegisterAsync(harness, userId);

        await harness.Bus.Publish(new EmailConfirmed(userId, "not-" + code));
        await harness.Consumed.Any<EmailConfirmed>();

        Assert.That(harness.Published.Select<SendWelcomeEmail>().Any(), Is.False);
    }

    [Test]
    public async Task RegistrationStateMachine_ConfirmationWindowElapsed_DiscardsRegistration()
    {
        await using var provider = await StartBusAsync();
        var harness = provider.GetRequiredService<ITestHarness>();
        var userId = NewId.NextGuid();
        await RegisterAsync(harness, userId);

        var discarded = await harness.Published.Any<RegistrationDiscarded>(x => x.Context.Message.Email == "ana@example.com");
        Assert.That(discarded, Is.True);
    }

    [Test]
    public async Task RegistrationStateMachine_ConfirmationWindowElapsed_CompletesSaga()
    {
        await using var provider = await StartBusAsync();
        var harness = provider.GetRequiredService<ITestHarness>();
        var userId = NewId.NextGuid();
        await RegisterAsync(harness, userId);
        await harness.Published.Any<RegistrationDiscarded>();

        var saga = harness.GetSagaStateMachineHarness<RegistrationStateMachine, RegistrationState>();
        Assert.That(await saga.NotExists(userId), Is.Null);
    }

    [Test]
    public async Task RegistrationStateMachine_LateConfirmation_DoesNotSendWelcomeEmail()
    {
        await using var provider = await StartBusAsync();
        var harness = provider.GetRequiredService<ITestHarness>();
        var userId = NewId.NextGuid();
        var code = await RegisterAsync(harness, userId);
        await harness.Published.Any<RegistrationDiscarded>();

        await harness.Bus.Publish(new EmailConfirmed(userId, code));
        await harness.Consumed.Any<EmailConfirmed>();

        Assert.That(harness.Published.Select<SendWelcomeEmail>().Any(), Is.False);
    }

    private static async Task<string> RegisterAsync(ITestHarness harness, Guid userId)
    {
        await harness.Bus.Publish(new UserRegistered(userId, "ana@example.com"));
        var saga = harness.GetSagaStateMachineHarness<RegistrationStateMachine, RegistrationState>();
        await saga.Exists(userId, x => x.AwaitingConfirmation);
        return saga.Sagas.Contains(userId).VerificationCode;
    }

    private static async Task<ServiceProvider> StartBusAsync()
    {
        var provider = new ServiceCollection()
            .AddSingleton(Microsoft.Extensions.Options.Options.Create(new RegistrationOptions { ConfirmationWindow = ConfirmationWindow }))
            .Configure<TestHarnessOptions>(options => options.TestInactivityTimeout = ConfirmationWindow + TimeSpan.FromSeconds(1))
            .AddMassTransitTestHarness(x => x.AddSagaStateMachine<RegistrationStateMachine, RegistrationState>())
            .BuildServiceProvider(validateScopes: true);

        await provider.GetRequiredService<ITestHarness>().Start();
        return provider;
    }
}
