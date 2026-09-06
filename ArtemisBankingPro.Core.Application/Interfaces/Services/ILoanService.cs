using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.Loans;
using ArtemisBankingPro.Core.Domain.Enums;

namespace ArtemisBankingPro.Core.Application.Interfaces.Services;

public interface ILoanService
{
    /// <summary>
    /// Listado paginado con estado del cliente (al día / en mora). Por cédula sin filtro
    /// de estado: activos primero, luego completados, cada grupo más reciente primero.
    /// </summary>
    Task<ServiceResult<PagedResult<LoanDto>>> GetPagedAsync(int page, LoanStatus? status, string? identification);

    /// <summary>Clientes activos SIN préstamo activo, con su deuda total.</summary>
    Task<List<EligibleClientDto>> GetEligibleClientsAsync();

    /// <summary>Deuda del cliente: pendiente de préstamos activos + adeudado en tarjetas activas.</summary>
    Task<decimal> GetClientDebtAsync(string userId);

    /// <summary>Deuda total de clientes activos / nº de clientes activos (RD$0.00 si no hay).</summary>
    Task<decimal> GetAverageDebtAsync();

    /// <summary>Evalúa el riesgo del nuevo préstamo (validaciones de plazo/monto/tasa incluidas).</summary>
    Task<ServiceResult<RiskEvaluationDto>> EvaluateRiskAsync(CreateLoanRequest request);

    /// <summary>Crea el préstamo, su amortización y el desembolso a la principal (transaccional).</summary>
    Task<ServiceResult<LoanDto>> CreateLoanAsync(CreateLoanRequest request, string adminUserId);

    /// <summary>Detalle del préstamo con la tabla de amortización.</summary>
    Task<ServiceResult<LoanDto>> GetDetailAsync(int loanId);

    /// <summary>Edita la tasa recalculando SOLO cuotas pendientes con vencimiento futuro.</summary>
    Task<ServiceResult> UpdateRateAsync(int loanId, decimal newAnnualRate);

    /// <summary>Préstamos ACTIVOS del cliente (Home del cliente y pago a préstamo).</summary>
    Task<List<LoanDto>> GetActiveLoansByUserAsync(string userId);
}
