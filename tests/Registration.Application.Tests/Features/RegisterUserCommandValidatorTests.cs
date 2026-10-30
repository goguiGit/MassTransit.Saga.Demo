using Registration.Application.Features.RegisterUser;

namespace Registration.Application.Tests.Features;

[TestFixture]
public sealed class RegisterUserCommandValidatorTests
{
    private readonly RegisterUserCommandValidator _validator = new();

    [Test]
    public void RegisterUserCommandValidator_ValidEmail_IsValid()
    {
        var result = _validator.Validate(new RegisterUserCommand("ana@example.com"));

        Assert.That(result.IsValid, Is.True);
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("ana")]
    [TestCase("ana@")]
    public void RegisterUserCommandValidator_InvalidEmail_FailsOnEmail(string email)
    {
        var result = _validator.Validate(new RegisterUserCommand(email));

        Assert.That(result.Errors.Select(x => x.PropertyName).Distinct(), Is.EqualTo(["Email"]));
    }
}
