using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.Email;
using ArtemisBankingPro.Core.Application.Dtos.Loans;
using ArtemisBankingPro.Core.Application.Helpers;
using ArtemisBankingPro.Core.Application.Interfaces.Repositories;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Domain.Constants;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;
using AutoMapper;
using ArtemisBankingPro.Core.Application.Extensions;

namespace ArtemisBankingPro.Core.Application.Services;

public class LoanService(
    IGenericRepository<Loan> loanRepository,
    IGenericRepository<LoanInstallment> installmentRepository,
    IGenericRepository<SavingsAccount> accountRepository,
    IGenericRepository<Transaction> transactionRepository,
    IGenericRepository<CreditCard> cardRepository,
    IProductNumberGenerator numberGenerator,
    IAccountService accountService,
    IEmailService emailService,
    IUnitOfWork unitOfWork,
    IMapper mapper) : ILoanService
{
    public async Task<ServiceResult<PagedResult<LoanDto>>> GetPagedAsync(int page, LoanStatus? status, string? identification)
    {
        var (p, size) = PagedResult<LoanDto>.Normalize(page, null);
        var query = loanRepository.Query();

        if (!string.IsNullOrWhiteSpace(identification))
        {
            var client = await accountService.GetByIdentificationAsync(identification.Trim());
            if (client is null)
                return ServiceResult<PagedResult<LoanDto>>.Fail(Mensajes.ClienteNoExistePorCedula);

            var tienePrestamos = await loanRepository.Query().AnyAsync(l => l.UserId == client.Id);
            if (!tienePrestamos)
                return ServiceResult<PagedResult<LoanDto>>.Fail(Mensajes.ClienteSinPrestamos);

            query = query.Where(l => l.UserId == client.Id);
        }

        if (status.HasValue)
            query = query.Where(l => l.Status == status.Value);

        var total = await query.CountAsync();

        // Sin filtro de estado: activos primero, luego completados; cada grupo por recencia.
        var ordered = status.HasValue
            ? query.OrderByDescending(l => l.CreatedAt)
            : query.OrderBy(l => l.Status).ThenByDescending(l => l.CreatedAt);

        var rows = await ordered
            .Skip((p - 1) * size)
            .Take(size)
            .Select(l => new
            {
                Loan = l,
                TotalInstallments = l.Installments.Count,
                PaidInstallments = l.Installments.Count(i => i.Status == InstallmentStatus.Pagada),
                PendingAmount = l.Installments.Sum(i => i.PendingAmount),
                IsInArrears = l.Installments.Any(i => i.IsOverdue)
            })
            .ToListAsync();

        var dtos = rows.Select(r =>
        {
            var dto = ToDto(r.Loan);
            dto.TotalInstallments = r.TotalInstallments;
            dto.PaidInstallments = r.PaidInstallments;
            dto.PendingAmount = r.PendingAmount;
            dto.IsInArrears = r.IsInArrears;
            return dto;
        }).ToList();

        await EnrichWithClientsAsync(dtos);

        return ServiceResult<PagedResult<LoanDto>>.Ok(new PagedResult<LoanDto>
        {
            Items = dtos,
            Page = p,
            PageSize = size,
            TotalCount = total
        });
    }

    public async Task<List<EligibleClientDto>> GetEligibleClientsAsync()
    {
        var activeClients = await accountService.GetActiveClientsAsync();
        if (activeClients.Count == 0) return [];

        var ids = activeClients.Select(c => c.Id).ToList();

        var withActiveLoan = await loanRepository.Query()
            .Where(l => l.Status == LoanStatus.Activo && ids.Contains(l.UserId))
            .Select(l => l.UserId)
            .ToListAsync();

        var debts = await GetDebtsByUserAsync(ids);

        return activeClients
            .Where(c => !withActiveLoan.Contains(c.Id))
            .Select(c => new EligibleClientDto
            {
                UserId = c.Id,
                Identification = c.Identification,
                FullName = c.FullName,
                Email = c.Email,
                TotalDebt = debts.GetValueOrDefault(c.Id)
            })
            .ToList();
    }

    public async Task<decimal> GetClientDebtAsync(string userId)
    {
        var debts = await GetDebtsByUserAsync([userId]);
        return debts.GetValueOrDefault(userId);
    }

    public async Task<decimal> GetAverageDebtAsync()
    {
        var activeClients = await accountService.GetActiveClientsAsync();
        if (activeClients.Count == 0) return 0m;

        var debts = await GetDebtsByUserAsync(activeClients.Select(c => c.Id).ToList());
        var totalDebt = activeClients.Sum(c => debts.GetValueOrDefault(c.Id));
        return Math.Round(totalDebt / activeClients.Count, 2, MidpointRounding.AwayFromZero);
    }

    public async Task<ServiceResult<RiskEvaluationDto>> EvaluateRiskAsync(CreateLoanRequest request)
    {
        var validation = await ValidateLoanRequestAsync(request);
        if (!validation.Succeeded)
            return ServiceResult<RiskEvaluationDto>.Fail(validation.Message!);

        var currentDebt = await GetClientDebtAsync(request.ClientId);
        var averageDebt = await GetAverageDebtAsync();
        var totalToPay = AmortizationCalculator.TotalToPay(
            request.CapitalAmount, request.AnnualInterestRate, request.TermInMonths);

        var risk = RiskEvaluator.Evaluate(currentDebt, totalToPay, averageDebt);

        return ServiceResult<RiskEvaluationDto>.Ok(new RiskEvaluationDto
        {
            RiskLevel = risk,
            WarningMessage = risk switch
            {
                RiskLevel.CurrentHighRisk => Mensajes.ClienteAltoRiesgoActual,
                RiskLevel.ProjectedHighRisk => Mensajes.ClienteAltoRiesgoProyectado,
                _ => null
            },
            CurrentDebt = currentDebt,
            ProjectedDebt = currentDebt + totalToPay,
            AverageDebt = averageDebt,
            NewLoanTotalToPay = totalToPay
        });
    }

    public async Task<ServiceResult<LoanDto>> CreateLoanAsync(CreateLoanRequest request, string adminUserId)
    {
        var validation = await ValidateLoanRequestAsync(request);
        if (!validation.Succeeded)
            return ServiceResult<LoanDto>.Fail(validation.Message!);

        var principal = await accountRepository.Query()
            .FirstOrDefaultAsync(a => a.UserId == request.ClientId
                && a.Type == AccountType.Principal
                && a.Status == ProductStatus.Activa);
        if (principal is null)
            return ServiceResult<LoanDto>.Fail(Mensajes.ClienteSinCuentaPrincipal);

        var loanNumber = await numberGenerator.GenerateAccountOrLoanNumberAsync();
        var createdAt = DateTime.Now;
        var schedule = AmortizationCalculator.BuildSchedule(
            request.CapitalAmount, request.AnnualInterestRate, request.TermInMonths, createdAt);

        var loan = new Loan
        {
            LoanNumber = loanNumber,
            UserId = request.ClientId,
            AdminUserId = adminUserId,
            ApprovedAmount = request.CapitalAmount,
            TermMonths = request.TermInMonths,
            AnnualInterestRate = request.AnnualInterestRate,
            Status = LoanStatus.Activo,
            CreatedAt = createdAt
        };

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await loanRepository.AddAsync(loan);
            await unitOfWork.SaveChangesAsync();

            foreach (var entry in schedule)
            {
                await installmentRepository.AddAsync(new LoanInstallment
                {
                    LoanId = loan.Id,
                    Number = entry.Number,
                    DueDate = entry.DueDate,
                    Value = entry.Value,
                    InterestAmount = entry.InterestAmount,
                    PrincipalAmount = entry.PrincipalAmount,
                    PendingAmount = entry.Value,
                    Status = InstallmentStatus.Pendiente,
                    IsOverdue = false
                });
            }

            // Desembolso: el capital se suma a la cuenta principal del cliente.
            principal.Balance += request.CapitalAmount;
            accountRepository.Update(principal);

            await transactionRepository.AddAsync(new Transaction
            {
                AccountId = principal.Id,
                Amount = request.CapitalAmount,
                Type = TransactionType.Credito,
                Origin = loanNumber,
                Beneficiary = principal.AccountNumber,
                Status = TransactionStatus.Aprobada,
                CreatedAt = createdAt
            });
        });

        var installmentValue = schedule[0].Value;
        var client = await accountService.GetByIdAsync(request.ClientId);
        var emailSent = client is null || await emailService.SendAsync(new EmailRequest
        {
            To = client.Email,
            Subject = "Préstamo aprobado",
            HtmlBody = $"""
                <h2>Préstamo aprobado</h2>
                <p>Su préstamo ha sido aprobado con los siguientes términos:</p>
                <ul>
                    <li>Número de préstamo: <strong>{loanNumber}</strong></li>
                    <li>Monto: <strong>RD${request.CapitalAmount:#,##0.00}</strong></li>
                    <li>Plazo: <strong>{request.TermInMonths} meses</strong></li>
                    <li>Tasa de interés anual: <strong>{request.AnnualInterestRate:0.##}%</strong></li>
                    <li>Cuota mensual: <strong>RD${installmentValue:#,##0.00}</strong></li>
                </ul>
                """
        });

        var dto = ToDto(loan);
        dto.TotalInstallments = schedule.Count;
        dto.PendingAmount = schedule.Sum(e => e.Value);
        dto.TotalAmountToPay = schedule.Sum(e => e.Value);

        // El fallo de correo NUNCA revierte la operación.
        return emailSent
            ? ServiceResult<LoanDto>.Ok(dto)
            : ServiceResult<LoanDto>.Ok(dto, Mensajes.PrestamoCreadoCorreoFallido);
    }

    public async Task<ServiceResult<LoanDto>> GetDetailAsync(int loanId)
    {
        var loan = await loanRepository.Query().FirstOrDefaultAsync(l => l.Id == loanId);
        if (loan is null)
            return ServiceResult<LoanDto>.Fail(Mensajes.PrestamoNoExiste);

        var installments = await installmentRepository.Query()
            .Where(i => i.LoanId == loanId)
            .OrderBy(i => i.Number)
            .ToListAsync();

        var dto = ToDto(loan);
        dto.TotalInstallments = installments.Count;
        dto.PaidInstallments = installments.Count(i => i.Status == InstallmentStatus.Pagada);
        dto.PendingAmount = installments.Sum(i => i.PendingAmount);
        dto.TotalAmountToPay = installments.Sum(i => i.Value);
        dto.IsInArrears = installments.Any(i => i.IsOverdue);
        dto.Installments = installments.Select(mapper.Map<LoanInstallmentDto>).ToList();

        await EnrichWithClientsAsync([dto]);
        return ServiceResult<LoanDto>.Ok(dto);
    }

    public async Task<ServiceResult> UpdateRateAsync(int loanId, decimal newAnnualRate)
    {
        var loan = await loanRepository.Query().FirstOrDefaultAsync(l => l.Id == loanId);
        if (loan is null)
            return ServiceResult.Fail(Mensajes.PrestamoNoExiste);

        if (loan.Status != LoanStatus.Activo)
            return ServiceResult.Fail(Mensajes.SoloTasaPrestamosActivos);

        if (newAnnualRate < 0)
            return ServiceResult.Fail(Mensajes.TasaNegativa);

        var today = DateTime.Now.Date;
        var installments = await installmentRepository.Query()
            .Where(i => i.LoanId == loanId)
            .OrderBy(i => i.Number)
            .ToListAsync();

        // SOLO cuotas Pendientes con vencimiento posterior a hoy.
        var recalculable = installments
            .Where(i => i.Status == InstallmentStatus.Pendiente && i.DueDate.Date > today)
            .ToList();

        if (recalculable.Count == 0)
            return ServiceResult.Fail(Mensajes.SinCuotasFuturas);

        // Capital restante de esas cuotas; se regenera el plan manteniendo números y fechas.
        var remainingCapital = recalculable.Sum(i => i.PrincipalAmount);
        var referenceDate = recalculable[0].DueDate.AddMonths(-1);
        var newSchedule = AmortizationCalculator.BuildSchedule(
            remainingCapital, newAnnualRate, recalculable.Count, referenceDate);

        for (var i = 0; i < recalculable.Count; i++)
        {
            var installment = recalculable[i];
            var entry = newSchedule[i];

            installment.Value = entry.Value;
            installment.InterestAmount = entry.InterestAmount;
            installment.PrincipalAmount = entry.PrincipalAmount;
            installment.PendingAmount = entry.Value;
            installmentRepository.Update(installment);
        }

        loan.AnnualInterestRate = newAnnualRate;
        loanRepository.Update(loan);
        await unitOfWork.SaveChangesAsync();

        // Correo con la nueva tasa y la próxima cuota.
        var next = recalculable[0];
        var client = await accountService.GetByIdAsync(loan.UserId);
        if (client is not null)
        {
            await emailService.SendAsync(new EmailRequest
            {
                To = client.Email,
                Subject = "Actualización de tasa de interés",
                HtmlBody = $"""
                    <h2>Actualización de tasa de interés</h2>
                    <p>La tasa de interés anual de su préstamo <strong>{loan.LoanNumber}</strong> ha sido
                    actualizada a <strong>{newAnnualRate:0.##}%</strong>.</p>
                    <p>Próxima cuota: <strong>RD${next.Value:#,##0.00}</strong> con vencimiento el
                    <strong>{next.DueDate:dd/MM/yyyy}</strong>.</p>
                    """
            });
        }

        return ServiceResult.Ok();
    }

    public async Task<List<LoanDto>> GetActiveLoansByUserAsync(string userId)
    {
        var rows = await loanRepository.Query()
            .Where(l => l.UserId == userId && l.Status == LoanStatus.Activo)
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => new
            {
                Loan = l,
                TotalInstallments = l.Installments.Count,
                PaidInstallments = l.Installments.Count(i => i.Status == InstallmentStatus.Pagada),
                PendingAmount = l.Installments.Sum(i => i.PendingAmount),
                IsInArrears = l.Installments.Any(i => i.IsOverdue)
            })
            .ToListAsync();

        return rows.Select(r =>
        {
            var dto = ToDto(r.Loan);
            dto.TotalInstallments = r.TotalInstallments;
            dto.PaidInstallments = r.PaidInstallments;
            dto.PendingAmount = r.PendingAmount;
            dto.IsInArrears = r.IsInArrears;
            return dto;
        }).ToList();
    }

    // ---------------- Helpers ----------------

    private async Task<ServiceResult> ValidateLoanRequestAsync(CreateLoanRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ClientId))
            return ServiceResult.Fail(Mensajes.DebeSeleccionarCliente);

        var client = await accountService.GetByIdAsync(request.ClientId);
        if (client is null || !client.IsActive || client.Role != Roles.Cliente)
            return ServiceResult.Fail(Mensajes.DebeSeleccionarCliente);

        var hasActiveLoan = await loanRepository.Query()
            .AnyAsync(l => l.UserId == request.ClientId && l.Status == LoanStatus.Activo);
        if (hasActiveLoan)
            return ServiceResult.Fail(Mensajes.ClienteConPrestamoActivo);

        if (!AppConstants.AllowedLoanTerms.Contains(request.TermInMonths))
            return ServiceResult.Fail(Mensajes.PlazoInvalido);

        if (request.CapitalAmount <= 0)
            return ServiceResult.Fail(Mensajes.MontoPrestamoInvalido);

        if (request.AnnualInterestRate < 0)
            return ServiceResult.Fail(Mensajes.TasaNegativa);

        return ServiceResult.Ok();
    }

    /// <summary>Deuda por usuario: pendiente de préstamos activos + adeudado en tarjetas activas.</summary>
    private async Task<Dictionary<string, decimal>> GetDebtsByUserAsync(List<string> userIds)
    {
        var loanDebts = await loanRepository.Query()
            .Where(l => l.Status == LoanStatus.Activo && userIds.Contains(l.UserId))
            .Select(l => new { l.UserId, Pending = l.Installments.Sum(i => i.PendingAmount) })
            .GroupBy(x => x.UserId)
            .Select(g => new { UserId = g.Key, Amount = g.Sum(x => x.Pending) })
            .ToListAsync();

        var cardDebts = await cardRepository.Query()
            .Where(c => c.Status == ProductStatus.Activa && userIds.Contains(c.UserId))
            .GroupBy(c => c.UserId)
            .Select(g => new { UserId = g.Key, Amount = g.Sum(c => c.OwedAmount) })
            .ToListAsync();

        var result = new Dictionary<string, decimal>();
        foreach (var row in loanDebts)
            result[row.UserId] = result.GetValueOrDefault(row.UserId) + row.Amount;
        foreach (var row in cardDebts)
            result[row.UserId] = result.GetValueOrDefault(row.UserId) + row.Amount;
        return result;
    }

    private async Task EnrichWithClientsAsync(List<LoanDto> dtos)
    {
        if (dtos.Count == 0) return;

        var users = await accountService.GetByIdsAsync(dtos.Select(d => d.UserId));
        foreach (var dto in dtos)
        {
            if (users.TryGetValue(dto.UserId, out var user))
            {
                dto.ClientFullName = user.FullName;
                dto.ClientIdentification = user.Identification;
            }
        }
    }

    // Los agregados (TotalInstallments, PendingAmount, etc.) los setea cada caller
    // desde sus proyecciones EF; el mapeo base vive en EntityMappingProfile.
    private LoanDto ToDto(Loan loan) => mapper.Map<LoanDto>(loan);
}
