namespace ArtemisBankingPro.Core.Application.Common;

/// <summary>
/// Resultado de operaciones de negocio. Los servicios NUNCA lanzan excepciones
/// para flujos de negocio: devuelven un resultado con el mensaje exacto del spec.
/// </summary>
public class ServiceResult
{
    public bool Succeeded { get; init; }
    public string? Message { get; init; }

    public static ServiceResult Ok(string? message = null) => new() { Succeeded = true, Message = message };
    public static ServiceResult Fail(string message) => new() { Succeeded = false, Message = message };
}

public class ServiceResult<T> : ServiceResult
{
    public T? Data { get; init; }

    public static ServiceResult<T> Ok(T data, string? message = null) =>
        new() { Succeeded = true, Data = data, Message = message };

    public static new ServiceResult<T> Fail(string message) =>
        new() { Succeeded = false, Message = message };
}
