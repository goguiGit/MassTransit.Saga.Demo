using Registration.Application.Features.ConfirmEmail;

namespace Registration.Application.Tests.Features;

[TestFixture]
public sealed class ConfirmEmailCommandValidatorTests
{
    private readonly ConfirmEmailCommandValidator _validator = new();

    [Test]
    public void ConfirmEmailCommandValidator_SixDigitCode_IsValid()
    {
        var result = _validator.Validate(new ConfirmEmailCommand(Guid.NewGuid(), "482913"));

        Assert.That(result.IsValid, Is.True);
    }

    [TestCase("")]
    [TestCase("12345")]
    [TestCase("1234567")]
    [TestCase("48291a")]
    public void ConfirmEmailCommandValidator_CodeNotSixDigits_FailsOnCode(string code)
    {
        var result = _validator.Validate(new ConfirmEmailCommand(Guid.NewGuid(), code));

        Assert.That(result.Errors.Select(x => x.PropertyName).Distinct(), Is.EqualTo(["Code"]));
    }

    [Test]
    public void ConfirmEmailCommandValidator_EmptyUserId_FailsOnUserId()
    {
        var result = _validator.Validate(new ConfirmEmailCommand(Guid.Empty, "482913"));

        Assert.That(result.Errors.Select(x => x.PropertyName).Distinct(), Is.EqualTo(["UserId"]));
    }
}
