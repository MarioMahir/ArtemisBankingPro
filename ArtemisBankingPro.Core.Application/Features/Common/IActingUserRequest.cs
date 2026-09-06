namespace ArtemisBankingPro.Core.Application.Features.Common;

/// <summary>
/// Commands que se ejecutan "en nombre de" un usuario autenticado (el id proviene
/// del JWT y lo asigna el controller). Permite al LoggingBehavior incluir el usuario
/// y a los servicios aplicar reglas como "no puede modificarse a sí mismo".
/// </summary>
public interface IActingUserRequest
{
    string ActingUserId { get; }
}
