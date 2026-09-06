namespace ArtemisBankingPro.Core.Application.Exceptions;

/// <summary>
/// Lanzada por el ValidationBehavior cuando un Command/Query no supera la validación
/// ESTRUCTURAL (FluentValidation): requeridos, formatos, rangos, plazos, etc.
/// El Global Exception Handler de la API la traduce a 400 Bad Request con
/// Problem Details (RFC 7807) incluyendo el diccionario de errores por campo.
/// Las reglas de negocio con datos NO usan excepciones: viven en los servicios
/// y se comunican mediante ServiceResult.
/// </summary>
public class ValidationException(IDictionary<string, string[]> errors)
    : Exception("Se produjeron uno o más errores de validación.")
{
    public IDictionary<string, string[]> Errors { get; } = errors;
}
