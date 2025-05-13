using Ardalis.Result.FluentValidation;
using MediatR;

namespace HakwadagAssassinGame.Application.Mediator;

public sealed class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(validator => validator.ValidateAsync(context, cancellationToken)));

        var errors = validationResults
            .Where(validationResult => !validationResult.IsValid)
            .SelectMany(validationResult => validationResult.AsErrors())
            .ToList();

        if (errors.Count != 0) return GetValidatableResult(errors);

        var response = await next(cancellationToken);

        return response;
    }

    private static TResponse GetValidatableResult(List<ValidationError> validationErrors)
    {
#pragma warning disable CS8603
#pragma warning disable CS8602
#pragma warning disable CS8600
        return (TResponse)typeof(Result<>).MakeGenericType(typeof(TResponse).GetGenericArguments())
            .GetMethod("Invalid").Invoke(null, [validationErrors]);
#pragma warning restore CS8600
#pragma warning restore CS8602
#pragma warning restore CS8603
    }
}