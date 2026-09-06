using FluentValidation;
using MediatR;

namespace ArtemisBankingPro.Core.Application.Behaviors;

/// <summary>
/// Ejecuta todos los validadores de FluentValidation asociados al request y, si hay
/// fallos, lanza la ValidationException propia de la capa Application con los errores
/// agrupados por propiedad. Se registra DESPUÉS del LoggingBehavior.
/// </summary>
public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);

            var results = await Task.WhenAll(
                validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            var failures = results
                .SelectMany(r => r.Errors)
                .Where(f => f is not null)
                .ToList();

            if (failures.Count != 0)
            {
                var errors = failures
                    .GroupBy(f => f.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(f => f.ErrorMessage).Distinct().ToArray());

                throw new Exceptions.ValidationException(errors);
            }
        }

        return await next();
    }
}
