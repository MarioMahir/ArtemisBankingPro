using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.CreditCards;
using ArtemisBankingPro.Core.Domain.Enums;

namespace ArtemisBankingPro.Core.Application.Interfaces.Services;

public interface ICreditCardService
{
    /// <summary>Listado paginado; los números SIEMPRE viajan enmascarados en los DTOs.</summary>
    Task<ServiceResult<PagedResult<CreditCardDto>>> GetPagedAsync(int page, ProductStatus? status, string? identification);

    /// <summary>Asigna una tarjeta: nº 16 dígitos, expiración hoy+3 años, CVC solo hash, deuda 0.</summary>
    Task<ServiceResult<CreditCardDto>> AssignAsync(string clientId, decimal creditLimit, string adminUserId);

    /// <summary>Detalle con consumos, más recientes primero.</summary>
    Task<ServiceResult<CreditCardDto>> GetDetailAsync(int cardId);

    /// <summary>Nuevo límite: tarjeta activa, mayor que cero y no inferior a la deuda actual.</summary>
    Task<ServiceResult> UpdateLimitAsync(int cardId, decimal newLimit);

    /// <summary>Cancela la tarjeta SOLO si la deuda es RD$0.00.</summary>
    Task<ServiceResult> CancelAsync(int cardId);

    /// <summary>Tarjetas activas del cliente (para el Home y las operaciones del cliente).</summary>
    Task<List<CreditCardDto>> GetActiveCardsByUserAsync(string userId);

    /// <summary>
    /// Número COMPLETO de una tarjeta propia, SOLO para uso interno del servidor
    /// (nunca se muestra ni se loguea). Devuelve null si la tarjeta no pertenece al usuario.
    /// </summary>
    Task<string?> GetCardNumberForOwnerAsync(int cardId, string ownerUserId);
}
