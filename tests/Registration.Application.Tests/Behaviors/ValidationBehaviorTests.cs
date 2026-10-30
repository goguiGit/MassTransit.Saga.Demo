using Ardalis.Result;
using FluentValidation;
using MediatR;
using Registration.Application.Behaviors;

namespace Registration.Application.Tests.Behaviors;

[TestFixture]
public sealed class ValidationBehaviorTests
{
    private sealed record Ping(string Name) : IRequest<Result<string>>;

    private sealed record Pong(string Name) : IRequest<Result>;

    private sealed class PingValidator : AbstractValidator<Ping>
    {
        public PingValidator() => RuleFor(x => x.Name).NotEmpty();
    }

    private sealed class PongValidator : AbstractValidator<Pong>
    {
        public PongValidator() => RuleFor(x => x.Name).NotEmpty();
    }

    [Test]
    public async Task ValidationBehavior_ValidRequest_CallsNext()
    {
        var behavior = new ValidationBehavior<Ping, Result<string>>([new PingValidator()]);

        var result = await behavior.Handle(new Ping("ok"), _ => Task.FromResult(Result.Success("handled")), CancellationToken.None);

        Assert.That(result.Value, Is.EqualTo("handled"));
    }

    [Test]
    public async Task ValidationBehavior_InvalidRequest_ReturnsInvalidWithoutCallingNext()
    {
        var behavior = new ValidationBehavior<Ping, Result<string>>([new PingValidator()]);
        var nextCalled = false;

        var result = await behavior.Handle(new Ping(""), _ => { nextCalled = true; return Task.FromResult(Result.Success("handled")); }, CancellationToken.None);

        Assert.That((result.Status, nextCalled, result.ValidationErrors.Select(x => x.Identifier)), Is.EqualTo((ResultStatus.Invalid, false, new[] { "Name" })));
    }

    [Test]
    public async Task ValidationBehavior_InvalidRequestWithNonGenericResult_ReturnsInvalid()
    {
        var behavior = new ValidationBehavior<Pong, Result>([new PongValidator()]);

        var result = await behavior.Handle(new Pong(""), _ => Task.FromResult(Result.Success()), CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
    }

    [Test]
    public async Task ValidationBehavior_NoValidators_CallsNext()
    {
        var behavior = new ValidationBehavior<Ping, Result<string>>([]);

        var result = await behavior.Handle(new Ping(""), _ => Task.FromResult(Result.Success("handled")), CancellationToken.None);

        Assert.That(result.Value, Is.EqualTo("handled"));
    }
}
