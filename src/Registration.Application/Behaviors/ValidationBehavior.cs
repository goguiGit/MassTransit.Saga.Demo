using System.Reflection;
using Ardalis.Result.FluentValidation;
using FluentValidation;

namespace Registration.Application.Behaviors;

// Validation failures come back as Result.Invalid, not as an exception: the endpoint maps them to 400 like
// any other Result. TResponse is Result or Result<T>; both expose a static Invalid(List<ValidationError>).
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Ardalis.Result.IResult
{
    private static readonly MethodInfo Invalid = typeof(TResponse).GetMethod(
        nameof(Result.Invalid), BindingFlags.Public | BindingFlags.Static, [typeof(List<ValidationError>)])
        ?? throw new InvalidOperationException($"{typeof(TResponse).Name} has no static Invalid(List<ValidationError>).");

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(validators.Select(x => x.ValidateAsync(context, cancellationToken)));
        var errors = results.SelectMany(x => x.AsErrors()).ToList();

        return errors.Count == 0
            ? await next(cancellationToken)
            : (TResponse)Invalid.Invoke(null, [errors])!;
    }
}
